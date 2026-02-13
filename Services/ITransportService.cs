using MotorDebugStudio.Models;

namespace MotorDebugStudio.Services;

public interface ITransportService
{
    string Name { get; }
    bool IsConnected { get; }
    bool IsStreaming { get; }

    IReadOnlyList<string> GetAvailablePorts();
    Task ConnectAsync(string port, int baudRate);
    void Disconnect();

    Task<HelloInfo?> HelloAsync();
    Task<HeartbeatInfo?> HeartbeatAsync();
    Task<bool> MotorCtrlAsync(byte mode);

    Task<IReadOnlyList<ScopeChannelInfo>> ScopeLayoutAsync();
    Task<(bool Enabled, ushort PeriodMs, byte ChannelCount)> ScopeControlAsync(bool enable, ushort periodMs);

    Task<double?> ReadTypedAsDoubleAsync(uint addr, UartValueType type);
    Task<bool> WriteTypedFromDoubleAsync(uint addr, UartValueType type, double value);

    void StartStreaming();
    void StopStreaming();
}
