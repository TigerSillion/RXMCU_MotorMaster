using System.Windows.Threading;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.Services;

public sealed class TransportSimulator : ITransportService
{
    private readonly AppEventBus _bus;
    private readonly DispatcherTimer _timer;
    private readonly Random _random = new();
    private int _sampleIndex;
    private int _faultTickCounter;

    public string Name => "Simulator";
    public bool IsConnected { get; private set; }
    public bool IsStreaming { get; private set; }

    public TransportSimulator(AppEventBus bus)
    {
        _bus = bus;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += (_, _) => EmitBatch();
    }

    public async Task ConnectAsync(string port, int baudRate)
    {
        _bus.PublishTransportState(TransportState.Connecting);
        _bus.PublishLog("Transport", $"Connecting {port} @ {baudRate}...");
        await Task.Delay(200);
        IsConnected = true;
        _bus.PublishTransportState(TransportState.Connected);
    }

    public IReadOnlyList<string> GetAvailablePorts()
    {
        return ["SIM_COM1", "SIM_COM2"];
    }

    public void Disconnect()
    {
        IsConnected = false;
        IsStreaming = false;
        _timer.Stop();
        _bus.PublishTransportState(TransportState.Idle);
    }

    public Task<HelloInfo?> HelloAsync()
    {
        return Task.FromResult<HelloInfo?>(new HelloInfo(0x0100, ProtocolCapabilities.Hello | ProtocolCapabilities.Heartbeat | ProtocolCapabilities.Scope | ProtocolCapabilities.EventLog));
    }

    public Task<HeartbeatInfo?> HeartbeatAsync()
    {
        return Task.FromResult<HeartbeatInfo?>(new HeartbeatInfo((uint)Environment.TickCount64, 1, 0, 0));
    }

    public Task<bool> MotorCtrlAsync(byte mode)
    {
        _bus.PublishLog("SIM", $"MotorCtrl mode={mode}");
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<ScopeChannelInfo>> ScopeLayoutAsync()
    {
        IReadOnlyList<ScopeChannelInfo> channels =
        [
            new(0, UartValueType.F32, "speed_rad", "rad/s"),
            new(1, UartValueType.F32, "iq_ref_ctrl", "A"),
            new(2, UartValueType.F32, "iq_ad", "A"),
            new(3, UartValueType.F32, "vdc", "V"),
            new(4, UartValueType.F32, "iu", "A"),
            new(5, UartValueType.F32, "iv", "A"),
            new(6, UartValueType.F32, "iw", "A"),
            new(7, UartValueType.U16, "err_status", ""),
        ];
        return Task.FromResult(channels);
    }

    public Task<(bool Enabled, ushort PeriodMs, byte ChannelCount)> ScopeControlAsync(bool enable, ushort periodMs)
    {
        return Task.FromResult((enable, periodMs == 0 ? (ushort)20 : periodMs, (byte)8));
    }

    public Task<double?> ReadTypedAsDoubleAsync(uint addr, UartValueType type)
    {
        return Task.FromResult<double?>(_random.NextDouble() * 10.0);
    }

    public Task<bool> WriteTypedFromDoubleAsync(uint addr, UartValueType type, double value)
    {
        _bus.PublishLog("SIM", $"Write 0x{addr:X8} {type} <= {value:F3}");
        return Task.FromResult(true);
    }

    public void StartStreaming()
    {
        if (!IsConnected)
        {
            return;
        }

        IsStreaming = true;
        _timer.Start();
        _bus.PublishTransportState(TransportState.Streaming);
    }

    public void StopStreaming()
    {
        IsStreaming = false;
        _timer.Stop();
        _bus.PublishTransportState(IsConnected ? TransportState.Connected : TransportState.Idle);
    }

    private void EmitBatch()
    {
        const int channelCount = 8;
        const int samplesPerTick = 16;
        var channels = new List<double[]>(channelCount);
        for (var ch = 0; ch < channelCount; ch++)
        {
            var data = new double[samplesPerTick];
            for (var i = 0; i < samplesPerTick; i++)
            {
                var phase = (_sampleIndex + i) * 0.08;
                data[i] = Math.Sin(phase * (1.0 + ch * 0.1)) + (_random.NextDouble() - 0.5) * 0.05;
            }

            channels.Add(data);
        }

        _sampleIndex += samplesPerTick;
        _faultTickCounter++;
        _bus.PublishSampleBatch(new SampleBatch(DateTime.Now, channels, 320, 0, 0, 0, (uint)_sampleIndex));

        if (_faultTickCounter % 200 == 0)
        {
            _bus.PublishFault(new FaultEvent(DateTime.Now, FaultState.Warning, "F_SIM", "Simulator warning", "scope synthetic"));
        }
    }
}
