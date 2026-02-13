using System.Buffers.Binary;
using System.Text;
using System.Runtime.InteropServices;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.Services;

public static class UartFrameCodec
{
    private const byte Sof1 = 0x55;
    private const byte Sof2 = 0xAA;

    public static byte ProtocolVersion => 0x02;

    public static byte[] Encode(UartType type, ushort seq, UartCommand cmd, ReadOnlySpan<byte> payload)
    {
        var len = payload.Length;
        var frame = new byte[2 + 8 + len + 2];
        frame[0] = Sof1;
        frame[1] = Sof2;
        frame[2] = ProtocolVersion;
        frame[3] = (byte)type;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(4, 2), seq);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6, 2), (ushort)cmd);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8, 2), (ushort)len);
        payload.CopyTo(frame.AsSpan(10, len));
        var crc = Crc16Ccitt.Compute(frame.AsSpan(2, 8 + len));
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(10 + len, 2), crc);
        return frame;
    }

    public static bool TryParseResponse(UartFrame frame, out UartResponse response)
    {
        response = UartResponse.Failed(UartErr.BadLength, "response payload too short");
        if (frame.Payload.Length < 4)
        {
            return false;
        }

        var status = frame.Payload[0];
        var err = (UartErr)frame.Payload[1];
        var detail = BinaryPrimitives.ReadUInt16LittleEndian(frame.Payload.AsSpan(2, 2));
        var data = frame.Payload.Skip(4).ToArray();
        response = new UartResponse(status == 0, status, err, detail, data)
        {
            ErrorMessage = status == 0 ? string.Empty : $"status={status} err={(byte)err}"
        };
        return true;
    }

    public static bool TryParseHello(UartResponse response, out HelloInfo info)
    {
        info = new HelloInfo(0, ProtocolCapabilities.None);
        if (!response.Success || response.Data.Length < 6)
        {
            return false;
        }

        var proto = BinaryPrimitives.ReadUInt16LittleEndian(response.Data.AsSpan(0, 2));
        var capsRaw = BinaryPrimitives.ReadUInt32LittleEndian(response.Data.AsSpan(2, 4));
        info = new HelloInfo(proto, (ProtocolCapabilities)capsRaw);
        return true;
    }

    public static bool TryParseHeartbeat(UartResponse response, out HeartbeatInfo info)
    {
        info = new HeartbeatInfo(0, 0, 0, 0);
        if (!response.Success || response.Data.Length < 9)
        {
            return false;
        }

        var tick = BinaryPrimitives.ReadUInt32LittleEndian(response.Data.AsSpan(0, 4));
        var mode = response.Data[4];
        var rxDrop = BinaryPrimitives.ReadUInt16LittleEndian(response.Data.AsSpan(5, 2));
        var txDrop = BinaryPrimitives.ReadUInt16LittleEndian(response.Data.AsSpan(7, 2));
        info = new HeartbeatInfo(tick, mode, rxDrop, txDrop);
        return true;
    }

    public static bool TryParseScopeLayout(UartResponse response, out IReadOnlyList<ScopeChannelInfo> channels)
    {
        channels = [];
        if (!response.Success || response.Data.Length < 1)
        {
            return false;
        }

        var count = response.Data[0];
        var idx = 1;
        var output = new List<ScopeChannelInfo>(count);
        for (var i = 0; i < count; i++)
        {
            if (idx + 3 > response.Data.Length)
            {
                return false;
            }

            var chId = response.Data[idx];
            var type = (UartValueType)response.Data[idx + 1];
            var nameLen = response.Data[idx + 2];
            idx += 3;
            if (idx + nameLen + 1 > response.Data.Length)
            {
                return false;
            }

            var name = Encoding.UTF8.GetString(response.Data, idx, nameLen);
            idx += nameLen;
            var unitLen = response.Data[idx];
            idx += 1;
            if (idx + unitLen > response.Data.Length)
            {
                return false;
            }

            var unit = Encoding.UTF8.GetString(response.Data, idx, unitLen);
            idx += unitLen;
            output.Add(new ScopeChannelInfo(chId, type, name, unit));
        }

        channels = output;
        return true;
    }

    public static bool TryParseScopeEvent(UartFrame frame, out uint sampleSeq, out float[] values)
    {
        sampleSeq = 0;
        values = [];
        if (frame.Cmd != UartCommand.EvtScopeData || frame.Payload.Length < 5)
        {
            return false;
        }

        sampleSeq = BinaryPrimitives.ReadUInt32LittleEndian(frame.Payload.AsSpan(0, 4));
        var count = frame.Payload[4];
        if (frame.Payload.Length != 5 + count * 4)
        {
            return false;
        }

        values = new float[count];
        var offset = 5;
        for (var i = 0; i < count; i++)
        {
            values[i] = BitConverter.ToSingle(frame.Payload, offset);
            offset += 4;
        }

        return true;
    }
}

public sealed class UartStreamDecoder
{
    private readonly List<byte> _buffer = [];
    private readonly int _maxPayload;

    public UartStreamDecoder(int maxPayload = 240)
    {
        _maxPayload = maxPayload;
    }

    public IReadOnlyList<UartFrame> Feed(ReadOnlySpan<byte> chunk)
    {
        if (chunk.Length > 0)
        {
            _buffer.AddRange(chunk.ToArray());
        }

        var frames = new List<UartFrame>();
        while (TryDecodeOne(out var frame))
        {
            frames.Add(frame);
        }

        return frames;
    }

    private bool TryDecodeOne(out UartFrame frame)
    {
        frame = new UartFrame(0, UartType.Req, 0, UartCommand.Hello, []);
        if (_buffer.Count < 12)
        {
            return false;
        }

        var sofPos = FindSof();
        if (sofPos < 0)
        {
            _buffer.Clear();
            return false;
        }

        if (sofPos > 0)
        {
            _buffer.RemoveRange(0, sofPos);
        }

        if (_buffer.Count < 12)
        {
            return false;
        }

        var ver = _buffer[2];
        var typ = _buffer[3];
        var seq = (ushort)(_buffer[4] | (_buffer[5] << 8));
        var cmd = (ushort)(_buffer[6] | (_buffer[7] << 8));
        var len = _buffer[8] | (_buffer[9] << 8);
        if (len > _maxPayload)
        {
            _buffer.RemoveRange(0, 2);
            return false;
        }

        var frameLen = 2 + 8 + len + 2;
        if (_buffer.Count < frameLen)
        {
            return false;
        }

        var calc = Crc16Ccitt.Compute(CollectionsMarshal.AsSpan(_buffer).Slice(2, 8 + len));
        var recv = (ushort)(_buffer[10 + len] | (_buffer[11 + len] << 8));
        if (calc != recv)
        {
            _buffer.RemoveAt(0);
            return false;
        }

        var payload = _buffer.Skip(10).Take(len).ToArray();
        _buffer.RemoveRange(0, frameLen);
        frame = new UartFrame(ver, (UartType)typ, seq, (UartCommand)cmd, payload);
        return true;
    }

    private int FindSof()
    {
        for (var i = 0; i < _buffer.Count - 1; i++)
        {
            if (_buffer[i] == 0x55 && _buffer[i + 1] == 0xAA)
            {
                return i;
            }
        }

        return -1;
    }
}

