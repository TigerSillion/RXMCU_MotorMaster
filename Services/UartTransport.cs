using System.IO;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.Services;

public sealed class UartTransport : ITransportService
{
    private readonly AppEventBus _bus;
    private readonly UartStreamDecoder _decoder = new();
    private readonly object _sendLock = new();
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<UartResponse>> _pending = new();
    private ushort _seq = 1;
    private SerialPort? _port;
    private long _scopeEventCount;
    private DateTime _scopeStatsWindowStart = DateTime.UtcNow;

    public UartTransport(AppEventBus bus)
    {
        _bus = bus;
    }

    public string Name => "UART";
    public bool IsConnected { get; private set; }
    public bool IsStreaming { get; private set; }

    public IReadOnlyList<string> GetAvailablePorts()
    {
        return SerialPort.GetPortNames().OrderBy(static x => x).ToList();
    }

    public Task ConnectAsync(string port, int baudRate)
    {
        _bus.PublishTransportState(TransportState.Connecting);
        _bus.PublishLog("UART", $"Opening {port}@{baudRate}");

        _port = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 80,
            WriteTimeout = 160
        };
        _port.DataReceived += OnDataReceived;
        _port.Open();

        IsConnected = true;
        _scopeEventCount = 0;
        _scopeStatsWindowStart = DateTime.UtcNow;
        _bus.PublishTransportState(TransportState.Connected);
        _bus.PublishLog("UART", "Port connected.");
        return Task.CompletedTask;
    }

    public void Disconnect()
    {
        StopStreaming();
        var port = _port;
        _port = null;

        if (port is not null)
        {
            port.DataReceived -= OnDataReceived;
            if (port.IsOpen)
            {
                port.Close();
            }

            port.Dispose();
        }

        foreach (var pair in _pending)
        {
            pair.Value.TrySetException(new IOException("Port disconnected"));
        }

        _pending.Clear();
        IsConnected = false;
        _bus.PublishTransportState(TransportState.Idle);
        _bus.PublishLog("UART", "Port disconnected.");
    }

    public async Task<HelloInfo?> HelloAsync()
    {
        var rsp = await RequestAsync(UartCommand.Hello, [], 700);
        if (!UartFrameCodec.TryParseHello(rsp, out var hello))
        {
            _bus.PublishLog("PROTO", $"HELLO failed: {rsp.ErrorMessage}");
            return null;
        }

        return hello;
    }

    public async Task<HeartbeatInfo?> HeartbeatAsync()
    {
        var rsp = await RequestAsync(UartCommand.Heartbeat, [], 700);
        if (!UartFrameCodec.TryParseHeartbeat(rsp, out var hb))
        {
            _bus.PublishLog("PROTO", $"HEARTBEAT failed: {rsp.ErrorMessage}");
            return null;
        }

        return hb;
    }

    public async Task<bool> MotorCtrlAsync(byte mode)
    {
        var rsp = await RequestAsync(UartCommand.MotorCtrl, [mode], 700);
        if (!rsp.Success)
        {
            _bus.PublishLog("CMD", $"MOTOR_CTRL mode={mode} failed err={(byte)rsp.Err}");
            return false;
        }

        return true;
    }

    public async Task<IReadOnlyList<ScopeChannelInfo>> ScopeLayoutAsync()
    {
        var rsp = await RequestAsync(UartCommand.ScopeLayout, [], 700);
        if (!UartFrameCodec.TryParseScopeLayout(rsp, out var channels))
        {
            _bus.PublishLog("PROTO", $"SCOPE_LAYOUT failed: {rsp.ErrorMessage}");
            return [];
        }

        return channels;
    }

    public async Task<(bool Enabled, ushort PeriodMs, byte ChannelCount)> ScopeControlAsync(bool enable, ushort periodMs)
    {
        var payload = new byte[3];
        payload[0] = enable ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(1, 2), periodMs);

        var rsp = await RequestAsync(UartCommand.ScopeCtrl, payload, 1000);
        if (!rsp.Success || rsp.Data.Length < 4)
        {
            _bus.PublishLog("PROTO", $"SCOPE_CTRL failed: {rsp.ErrorMessage}");
            return (false, 0, 0);
        }

        var enabled = rsp.Data[0] != 0;
        var appliedPeriod = BinaryPrimitives.ReadUInt16LittleEndian(rsp.Data.AsSpan(1, 2));
        var channelCount = rsp.Data[3];
        return (enabled, appliedPeriod, channelCount);
    }

    public async Task<double?> ReadTypedAsDoubleAsync(uint addr, UartValueType type)
    {
        var payload = new byte[7];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), addr);
        payload[4] = (byte)type;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(5, 2), 1);

        var rsp = await RequestAsync(UartCommand.ReadTyped, payload, 700);
        if (!rsp.Success || rsp.Data.Length < 3)
        {
            return null;
        }

        var rspType = (UartValueType)rsp.Data[0];
        var count = BinaryPrimitives.ReadUInt16LittleEndian(rsp.Data.AsSpan(1, 2));
        if (count != 1)
        {
            return null;
        }

        return TryReadAsDouble(rspType, rsp.Data, 3, out var value) ? value : null;
    }

    public async Task<bool> WriteTypedFromDoubleAsync(uint addr, UartValueType type, double value)
    {
        if (!TryEncodeFromDouble(type, value, out var raw))
        {
            return false;
        }

        var payload = new byte[7 + raw.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0, 4), addr);
        payload[4] = (byte)type;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(5, 2), 1);
        raw.CopyTo(payload.AsSpan(7));

        var rsp = await RequestAsync(UartCommand.WriteTyped, payload, 700);
        return rsp.Success;
    }

    public void StartStreaming()
    {
        if (!IsConnected)
        {
            _bus.PublishLog("UART", "Start rejected: not connected.");
            return;
        }

        IsStreaming = true;
        _scopeEventCount = 0;
        _scopeStatsWindowStart = DateTime.UtcNow;
        _bus.PublishTransportState(TransportState.Streaming);
    }

    public void StopStreaming()
    {
        IsStreaming = false;
        _bus.PublishTransportState(IsConnected ? TransportState.Connected : TransportState.Idle);
    }

    private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        var port = _port;
        if (port is null)
        {
            return;
        }

        try
        {
            var bytes = port.BytesToRead;
            if (bytes <= 0)
            {
                return;
            }

            var chunk = new byte[bytes];
            var read = port.Read(chunk, 0, bytes);
            if (read <= 0)
            {
                return;
            }

            var frames = _decoder.Feed(chunk.AsSpan(0, read));
            foreach (var frame in frames)
            {
                HandleFrame(frame);
            }
        }
        catch (Exception ex)
        {
            _bus.PublishLog("UART", $"RX error: {ex.Message}");
            _bus.PublishTransportState(TransportState.Error);
        }
    }

    private void HandleFrame(UartFrame frame)
    {
        if (frame.Type == UartType.Evt)
        {
            HandleEventFrame(frame);
            return;
        }

        if (frame.Type != UartType.Rsp)
        {
            return;
        }

        if (!UartFrameCodec.TryParseResponse(frame, out var response))
        {
            return;
        }

        if (_pending.TryRemove(frame.Seq, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }

    private void HandleEventFrame(UartFrame frame)
    {
        switch (frame.Cmd)
        {
            case UartCommand.EvtLogText:
            {
                var text = Encoding.UTF8.GetString(frame.Payload);
                _bus.PublishLog("MCU", text);
                break;
            }
            case UartCommand.EvtScopeData:
            {
                if (!IsStreaming)
                {
                    return;
                }

                if (!UartFrameCodec.TryParseScopeEvent(frame, out var sampleSeq, out var values))
                {
                    return;
                }

                _scopeEventCount++;
                var elapsed = (DateTime.UtcNow - _scopeStatsWindowStart).TotalSeconds;
                if (elapsed < 0.001)
                {
                    elapsed = 0.001;
                }

                var sampleRate = _scopeEventCount / elapsed;
                var channels = values.Select(static x => new[] { (double)x }).ToList();
                _bus.PublishSampleBatch(new SampleBatch(
                    DateTime.Now,
                    channels,
                    sampleRate,
                    0,
                    0,
                    0,
                    sampleSeq));
                break;
            }
        }
    }

    private async Task<UartResponse> RequestAsync(UartCommand cmd, byte[] payload, int timeoutMs)
    {
        if (_port is null || !IsConnected)
        {
            return UartResponse.Failed(UartErr.Internal, "port is not connected");
        }

        var seq = NextSeq();
        var tcs = new TaskCompletionSource<UartResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[seq] = tcs;

        try
        {
            var frame = UartFrameCodec.Encode(UartType.Req, seq, cmd, payload);
            lock (_sendLock)
            {
                _port.Write(frame, 0, frame.Length);
            }

            using var cts = new CancellationTokenSource(timeoutMs);
            using var reg = cts.Token.Register(() => tcs.TrySetException(new TimeoutException($"cmd 0x{(ushort)cmd:X4} timeout")));
            var rsp = await tcs.Task;
            if (!rsp.Success)
            {
                rsp = rsp with { ErrorMessage = $"cmd=0x{(ushort)cmd:X4} status={rsp.Status} err={(byte)rsp.Err} detail=0x{rsp.Detail:X4}" };
            }

            return rsp;
        }
        catch (TimeoutException ex)
        {
            _bus.PublishLog("PROTO", ex.Message);
            return UartResponse.Failed(UartErr.Internal, ex.Message);
        }
        catch (Exception ex)
        {
            _bus.PublishLog("PROTO", $"Request failed cmd=0x{(ushort)cmd:X4}: {ex.Message}");
            return UartResponse.Failed(UartErr.Internal, ex.Message);
        }
        finally
        {
            _pending.TryRemove(seq, out _);
        }
    }

    private ushort NextSeq()
    {
        lock (_sendLock)
        {
            var next = _seq;
            _seq++;
            if (_seq == 0)
            {
                _seq = 1;
            }

            return next;
        }
    }

    private static bool TryReadAsDouble(UartValueType type, byte[] data, int offset, out double value)
    {
        value = 0;
        var raw = data.AsSpan(offset);
        switch (type)
        {
            case UartValueType.U8 when raw.Length >= 1:
                value = raw[0];
                return true;
            case UartValueType.S8 when raw.Length >= 1:
                value = (sbyte)raw[0];
                return true;
            case UartValueType.U16 when raw.Length >= 2:
                value = BinaryPrimitives.ReadUInt16LittleEndian(raw);
                return true;
            case UartValueType.S16 when raw.Length >= 2:
                value = BinaryPrimitives.ReadInt16LittleEndian(raw);
                return true;
            case UartValueType.U32 when raw.Length >= 4:
                value = BinaryPrimitives.ReadUInt32LittleEndian(raw);
                return true;
            case UartValueType.S32 when raw.Length >= 4:
                value = BinaryPrimitives.ReadInt32LittleEndian(raw);
                return true;
            case UartValueType.F32 when raw.Length >= 4:
                value = BitConverter.ToSingle(raw[..4]);
                return true;
            default:
                return false;
        }
    }

    private static bool TryEncodeFromDouble(UartValueType type, double value, out byte[] raw)
    {
        raw = [];
        try
        {
            raw = type switch
            {
                UartValueType.U8 => [(byte)Math.Clamp(value, byte.MinValue, byte.MaxValue)],
                UartValueType.S8 => [(byte)(sbyte)Math.Clamp(value, sbyte.MinValue, sbyte.MaxValue)],
                UartValueType.U16 => BitConverter.GetBytes((ushort)Math.Clamp(value, ushort.MinValue, ushort.MaxValue)),
                UartValueType.S16 => BitConverter.GetBytes((short)Math.Clamp(value, short.MinValue, short.MaxValue)),
                UartValueType.U32 => BitConverter.GetBytes((uint)Math.Clamp(value, uint.MinValue, uint.MaxValue)),
                UartValueType.S32 => BitConverter.GetBytes((int)Math.Clamp(value, int.MinValue, int.MaxValue)),
                UartValueType.F32 => BitConverter.GetBytes((float)value),
                _ => []
            };
            return raw.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}


