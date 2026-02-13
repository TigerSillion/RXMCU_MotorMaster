using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;

namespace MotorDebugStudio.ViewModels;

public sealed class ConnectionViewModel : ObservableObject
{
    private readonly AppEventBus _bus;
    private readonly ITransportService _transport;
    private readonly DispatcherTimer _heartbeatTimer;
    private readonly bool _verboseLinkLogs;

    private TransportState _state = TransportState.Idle;
    private string _selectedPort = "COM6";
    private int _selectedBaudRate = 115200;

    public ObservableCollection<string> Ports { get; } = [];
    public ObservableCollection<int> BaudRates { get; } = [115200, 230400, 375000, 460800, 921600, 1000000, 1500000];

    public RelayCommand ConnectCommand { get; }
    public RelayCommand DisconnectCommand { get; }
    public RelayCommand RefreshPortsCommand { get; }

    public ConnectionViewModel(AppEventBus bus, ITransportService transport)
    {
        _bus = bus;
        _transport = transport;
        RefreshPorts();

        ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => State is TransportState.Idle or TransportState.Error);
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => State != TransportState.Idle);
        RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());
        _bus.OnTransportStateChanged += SetState;

        _heartbeatTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        _heartbeatTimer.Tick += async (_, _) => await PollHeartbeatAsync();
        _verboseLinkLogs = false;
    }

    public TransportState State
    {
        get => _state;
        private set
        {
            if (!SetProperty(ref _state, value))
            {
                return;
            }

            Notify(nameof(StateLabel));
            Notify(nameof(StateBrush));
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
        }
    }

    public string SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetProperty(ref _selectedBaudRate, value);
    }

    public string StateLabel => State.ToString();

    public Brush StateBrush => State switch
    {
        TransportState.Streaming => (Brush)App.Current.Resources["BrushSuccess"],
        TransportState.Connected => (Brush)App.Current.Resources["BrushAccent"],
        TransportState.Connecting or TransportState.Reconnecting => (Brush)App.Current.Resources["BrushWarning"],
        TransportState.Error => (Brush)App.Current.Resources["BrushDanger"],
        _ => (Brush)App.Current.Resources["BrushTextSecondary"]
    };

    private async Task ConnectAsync()
    {
        try
        {
            await _transport.ConnectAsync(SelectedPort, SelectedBaudRate);
            var hello = await _transport.HelloAsync();
            if (hello is null)
            {
                throw new InvalidOperationException("HELLO failed");
            }

            _heartbeatTimer.Start();
            LogVerbose($"HELLO ok proto=0x{hello.ProtocolVersion:X4} caps=0x{(uint)hello.Capabilities:X8}");
        }
        catch (Exception ex)
        {
            _heartbeatTimer.Stop();
            _bus.PublishTransportState(TransportState.Error);
            _bus.PublishLog("Transport", $"Connect failed: {ex.Message}");
        }
    }

    private void Disconnect()
    {
        _heartbeatTimer.Stop();
        _transport.Disconnect();
    }

    private async Task PollHeartbeatAsync()
    {
        if (!_transport.IsConnected)
        {
            return;
        }

        var hb = await _transport.HeartbeatAsync();
        if (hb is null)
        {
            LogVerbose("Heartbeat timeout");
            return;
        }

        LogVerbose($"Heartbeat tick={hb.LoopTick} mode={hb.SystemMode} rxDrop={hb.RxDrop} txDrop={hb.TxDrop}");
    }

    private void RefreshPorts()
    {
        Ports.Clear();
        foreach (var item in _transport.GetAvailablePorts())
        {
            Ports.Add(item);
        }

        if (Ports.Count > 0 && !Ports.Contains(SelectedPort))
        {
            SelectedPort = Ports[0];
        }
    }

    private void SetState(TransportState state)
    {
        State = state;
    }

    private void LogVerbose(string message)
    {
        if (_verboseLinkLogs)
        {
            _bus.PublishLog("LinkDebug", message);
        }
    }
}
