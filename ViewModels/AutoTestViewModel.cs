using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;

namespace MotorDebugStudio.ViewModels;

public sealed class AutoTestViewModel : ObservableObject
{
    private readonly AppEventBus _bus;
    private readonly ITransportService _transport;
    private bool _isRunning;
    private string _lastResult = "Not run";
    private string _lastDuration = "-";
    private int _scopeEventCount;

    private const uint AddrSystemMode = 0x00001801;
    private const uint AddrRefSpeedRpm = 0x00001DC0;

    public RelayCommand RunAutoTestCommand { get; }

    public AutoTestViewModel(AppEventBus bus, ITransportService transport)
    {
        _bus = bus;
        _transport = transport;

        RunAutoTestCommand = new RelayCommand(_ => _ = RunAsync(), _ => _transport.IsConnected && !IsRunning);

        _bus.OnSampleBatchReceived += _ =>
        {
            if (IsRunning)
            {
                ScopeEventCount++;
            }
        };

        _bus.OnTransportStateChanged += _ => RunAutoTestCommand.RaiseCanExecuteChanged();
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                RunAutoTestCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string LastResult
    {
        get => _lastResult;
        private set => SetProperty(ref _lastResult, value);
    }

    public string LastDuration
    {
        get => _lastDuration;
        private set => SetProperty(ref _lastDuration, value);
    }

    public int ScopeEventCount
    {
        get => _scopeEventCount;
        private set => SetProperty(ref _scopeEventCount, value);
    }

    private async Task RunAsync()
    {
        if (IsRunning)
        {
            return;
        }

        IsRunning = true;
        ScopeEventCount = 0;
        var startedAt = DateTime.Now;
        _bus.PublishLog("AutoTest", "Start UART auto test.");

        try
        {
            var hello = await _transport.HelloAsync();
            if (hello is null)
            {
                throw new InvalidOperationException("HELLO failed");
            }

            var hb = await _transport.HeartbeatAsync();
            if (hb is null)
            {
                throw new InvalidOperationException("HEARTBEAT failed");
            }

            var layout = await _transport.ScopeLayoutAsync();
            if (layout.Count == 0)
            {
                throw new InvalidOperationException("SCOPE_LAYOUT empty");
            }

            var scopeStart = await _transport.ScopeControlAsync(true, 20);
            if (!scopeStart.Enabled)
            {
                throw new InvalidOperationException("SCOPE_CTRL start rejected");
            }

            _transport.StartStreaming();
            await Task.Delay(1200);

            _transport.StopStreaming();
            var scopeStop = await _transport.ScopeControlAsync(false, 0);
            if (scopeStop.Enabled)
            {
                throw new InvalidOperationException("SCOPE_CTRL stop rejected");
            }

            if (ScopeEventCount < 8)
            {
                throw new InvalidOperationException($"Scope event too few: {ScopeEventCount}");
            }

            var modeVal = await _transport.ReadTypedAsDoubleAsync(AddrSystemMode, UartValueType.U8);
            if (!modeVal.HasValue)
            {
                throw new InvalidOperationException("READ_TYPED system mode failed");
            }

            var speedVal = await _transport.ReadTypedAsDoubleAsync(AddrRefSpeedRpm, UartValueType.F32);
            if (!speedVal.HasValue)
            {
                throw new InvalidOperationException("READ_TYPED ref speed failed");
            }

            var writeOk = await _transport.WriteTypedFromDoubleAsync(AddrRefSpeedRpm, UartValueType.F32, speedVal.Value);
            if (!writeOk)
            {
                throw new InvalidOperationException("WRITE_TYPED ref speed failed");
            }

            var readback = await _transport.ReadTypedAsDoubleAsync(AddrRefSpeedRpm, UartValueType.F32);
            if (!readback.HasValue)
            {
                throw new InvalidOperationException("READBACK ref speed failed");
            }

            var delta = Math.Abs(readback.Value - speedVal.Value);
            if (delta > 0.01)
            {
                throw new InvalidOperationException($"READBACK mismatch delta={delta:F4}");
            }

            var stopOk = await _transport.MotorCtrlAsync(0);
            if (!stopOk)
            {
                throw new InvalidOperationException("MOTOR_CTRL stop failed");
            }

            LastResult = "PASS";
            _bus.PublishLog("AutoTest", $"PASS mode={modeVal.Value:F0} scopeEvt={ScopeEventCount}");
        }
        catch (Exception ex)
        {
            LastResult = $"FAIL: {ex.Message}";
            _bus.PublishFault(new FaultEvent(DateTime.Now, FaultState.Warning, "AUTO_TEST", ex.Message, "UART auto test"));
            _bus.PublishLog("AutoTest", LastResult);
        }
        finally
        {
            LastDuration = $"{(DateTime.Now - startedAt).TotalSeconds:F2}s";
            IsRunning = false;
        }
    }
}
