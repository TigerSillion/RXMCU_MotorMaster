namespace MotorDebugStudio.Models;

public sealed class MotorProfile
{
    public string ProfileName { get; set; } = "RX26T Default";
    public string McuModel { get; set; } = "RX26T";
    public string ProtocolVersion { get; set; } = "UART5 v1.0";
    public string BuildId { get; set; } = string.Empty;
    public List<ChannelProfile> Channels { get; set; } = [];
    public List<ParameterProfile> Parameters { get; set; } = [];
}

public sealed class ChannelProfile
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public sealed class ParameterProfile
{
    public string Name { get; set; } = string.Empty;
    public uint Address { get; set; }
    public UartValueType Type { get; set; } = UartValueType.F32;
    public double Min { get; set; }
    public double Max { get; set; }
    public bool Writable { get; set; }
    public int SafetyLevel { get; set; }
}
