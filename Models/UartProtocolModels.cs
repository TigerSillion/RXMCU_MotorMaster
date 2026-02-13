namespace MotorDebugStudio.Models;

[Flags]
public enum ProtocolCapabilities : uint
{
    None = 0,
    Hello = 1u << 0,
    Heartbeat = 1u << 1,
    MotorControl = 1u << 2,
    ReadTyped = 1u << 3,
    WriteTyped = 1u << 4,
    ReadBlock = 1u << 5,
    WriteBlock = 1u << 6,
    EventLog = 1u << 7,
    Scope = 1u << 8,
}

public enum UartType : byte
{
    Req = 0x01,
    Rsp = 0x02,
    Evt = 0x03,
}

public enum UartCommand : ushort
{
    Hello = 0x0001,
    Heartbeat = 0x0002,
    MotorCtrl = 0x0100,
    ReadTyped = 0x0300,
    WriteTyped = 0x0301,
    ReadBlock = 0x0302,
    WriteBlock = 0x0303,
    ScopeLayout = 0x0400,
    ScopeCtrl = 0x0401,
    EvtLogText = 0x8001,
    EvtScopeData = 0x8400,
}

public enum UartErr : byte
{
    Ok = 0x00,
    CrcFail = 0x01,
    BadLength = 0x02,
    BadCmd = 0x03,
    BadParam = 0x05,
    ForbiddenAddr = 0x07,
    Internal = 0x08,
}

public enum UartValueType : byte
{
    U8 = 1,
    S8 = 2,
    U16 = 3,
    S16 = 4,
    U32 = 5,
    S32 = 6,
    F32 = 7,
    Raw = 8,
}

public sealed record UartFrame(byte Ver, UartType Type, ushort Seq, UartCommand Cmd, byte[] Payload);

public sealed record UartResponse(bool Success, byte Status, UartErr Err, ushort Detail, byte[] Data)
{
    public static UartResponse Failed(UartErr err, string reason)
    {
        return new UartResponse(false, 1, err, 0, []) { ErrorMessage = reason };
    }

    public string ErrorMessage { get; init; } = string.Empty;
}

public sealed record HelloInfo(ushort ProtocolVersion, ProtocolCapabilities Capabilities);

public sealed record HeartbeatInfo(uint LoopTick, byte SystemMode, ushort RxDrop, ushort TxDrop);

public sealed record ScopeChannelInfo(byte Id, UartValueType Type, string Name, string Unit);
