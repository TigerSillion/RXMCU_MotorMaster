using System.Collections.ObjectModel;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;

namespace MotorDebugStudio.ViewModels;

public sealed class MainViewModel
{
    private const bool UseUartTransportByDefault = true;
    private readonly AppEventBus _bus;
    private readonly ITransportService _transport;

    public ConnectionViewModel Connection { get; }
    public ScopeViewModel Scope { get; }
    public ParamViewModel Param { get; }
    public FaultViewModel Fault { get; }
    public ObservableCollection<LogEntry> Logs { get; } = [];

    public MainViewModel()
    {
        _bus = new AppEventBus();
        _transport = UseUartTransportByDefault
            ? new UartTransport(_bus)
            : new TransportSimulator(_bus);

        Connection = new ConnectionViewModel(_bus, _transport);
        Scope = new ScopeViewModel(_bus, _transport);
        Param = new ParamViewModel(_bus, _transport);
        Fault = new FaultViewModel(_bus);

        _bus.OnLogAdded += log => Logs.Insert(0, log);

        _bus.OnParamWriteResult += result =>
            _bus.PublishLog("ParamWrite", $"{result.Name} result={result.Success} detail={result.Detail}");

        _bus.OnTransportStateChanged += state =>
        {
            if (state == TransportState.Connected)
            {
                _ = Scope.RefreshLayoutAsync();
                _ = Param.RefreshAllAsync();
            }
        };

        _bus.PublishLog("System", $"Transport initialized: {_transport.Name}");
    }
}
