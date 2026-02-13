namespace MotorDebugStudio.Models;

public sealed record SampleBatch(
    DateTime Timestamp,
    IReadOnlyList<double[]> Channels,
    double SampleRateHz,
    double DroppedFrameRatePercent,
    double BufferUsagePercent,
    double LatencyMs,
    uint SourceSeq = 0);

public sealed record FaultEvent(
    DateTime Timestamp,
    FaultState Severity,
    string Code,
    string Message,
    string Context);

public sealed record ParamWriteResult(
    DateTime Timestamp,
    string Name,
    bool Success,
    string Detail);

public sealed record LogEntry(
    DateTime Timestamp,
    string Category,
    string Message);
