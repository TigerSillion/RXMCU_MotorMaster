namespace MotorDebugStudio.Models;

public enum TransportState
{
    Idle,
    Connecting,
    Connected,
    Streaming,
    Reconnecting,
    Error
}

public enum ScopeState
{
    NoData,
    Live,
    Paused,
    Replay
}

public enum ParamState
{
    Readable,
    Writable,
    PendingWrite,
    WriteBlocked,
    Stale
}

public enum FaultState
{
    Hidden,
    Warning,
    Critical,
    Acknowledged
}

public enum CommandState
{
    Enabled,
    DisabledByState,
    ConfirmRequired,
    Executing
}
