using System.Collections.ObjectModel;
using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;

namespace MotorDebugStudio.ViewModels;

public sealed class ScopeViewModel : ObservableObject
{
    private readonly AppEventBus _bus;
    private readonly ITransportService _transport;
    private readonly List<List<double>> _buffers;
    private readonly int _maxPoints = 2400;
    private ScopeState _state = ScopeState.NoData;
    private double _sampleRateHz;
    private double _dropRatePercent;
    private double _bufferUsagePercent;
    private double _latencyMs;
    private ushort _periodMs = 20;
    private long _dataVersion;

    public ObservableCollection<ChannelViewModel> Channels { get; } = [];
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand AutoScaleCommand { get; }
    public RelayCommand TogglePauseReplayCommand { get; }
    public RelayCommand RefreshLayoutCommand { get; }

    public ScopeViewModel(AppEventBus bus, ITransportService transport)
    {
        _bus = bus;
        _transport = transport;
        _buffers = [];

        for (var i = 0; i < 8; i++)
        {
            Channels.Add(new ChannelViewModel { Index = i, Name = $"CH{i}", Unit = string.Empty });
            _buffers.Add([]);
        }

        StartCommand = new RelayCommand(_ => _ = StartStreamingAsync(), _ => State is ScopeState.NoData or ScopeState.Paused or ScopeState.Replay);
        StopCommand = new RelayCommand(_ => _ = StopStreamingAsync(), _ => State is ScopeState.Live or ScopeState.Replay or ScopeState.Paused);
        AutoScaleCommand = new RelayCommand(_ => _bus.PublishLog("Scope", "Auto scale requested."));
        TogglePauseReplayCommand = new RelayCommand(_ => TogglePauseReplay(), _ => State != ScopeState.NoData);
        RefreshLayoutCommand = new RelayCommand(_ => _ = RefreshLayoutAsync());

        _bus.OnSampleBatchReceived += OnSampleBatchReceived;
    }

    public ScopeState State
    {
        get => _state;
        set
        {
            if (!SetProperty(ref _state, value))
            {
                return;
            }

            Notify(nameof(StateLabel));
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            TogglePauseReplayCommand.RaiseCanExecuteChanged();
        }
    }

    public string StateLabel => State.ToString();

    public double SampleRateHz
    {
        get => _sampleRateHz;
        private set => SetProperty(ref _sampleRateHz, value);
    }

    public double DropRatePercent
    {
        get => _dropRatePercent;
        private set => SetProperty(ref _dropRatePercent, value);
    }

    public double BufferUsagePercent
    {
        get => _bufferUsagePercent;
        private set => SetProperty(ref _bufferUsagePercent, value);
    }

    public double LatencyMs
    {
        get => _latencyMs;
        private set => SetProperty(ref _latencyMs, value);
    }

    public ushort PeriodMs
    {
        get => _periodMs;
        set => SetProperty(ref _periodMs, (ushort)Math.Clamp(value, (ushort)1, (ushort)1000));
    }

    public long DataVersion
    {
        get => _dataVersion;
        private set => SetProperty(ref _dataVersion, value);
    }

    public IReadOnlyList<(string Name, double[] Data)> GetVisibleSeries()
    {
        var visible = new List<(string Name, double[] Data)>();
        foreach (var channel in Channels)
        {
            if (!channel.IsEnabled)
            {
                continue;
            }

            var label = string.IsNullOrWhiteSpace(channel.Unit) ? channel.Name : $"{channel.Name} [{channel.Unit}]";
            visible.Add((label, _buffers[channel.Index].ToArray()));
        }

        return visible;
    }

    public async Task RefreshLayoutAsync()
    {
        if (!_transport.IsConnected)
        {
            return;
        }

        try
        {
            var layout = await _transport.ScopeLayoutAsync();
            if (layout.Count == 0)
            {
                return;
            }

            var max = Math.Min(layout.Count, Channels.Count);
            for (var i = 0; i < max; i++)
            {
                Channels[i].Name = layout[i].Name;
                Channels[i].Unit = layout[i].Unit;
            }

            _bus.PublishLog("Scope", $"Layout synced. channels={layout.Count}");
        }
        catch (Exception ex)
        {
            _bus.PublishLog("Scope", $"Layout sync failed: {ex.Message}");
        }
    }

    private async Task StartStreamingAsync()
    {
        if (!_transport.IsConnected)
        {
            _bus.PublishLog("Scope", "Start rejected: not connected.");
            return;
        }

        var result = await _transport.ScopeControlAsync(true, PeriodMs);
        if (!result.Enabled)
        {
            _bus.PublishLog("Scope", "MCU rejected scope start.");
            return;
        }

        PeriodMs = result.PeriodMs;
        _transport.StartStreaming();
        State = ScopeState.Live;
        _bus.PublishLog("Scope", $"Scope started. period={result.PeriodMs}ms ch={result.ChannelCount}");
    }

    private async Task StopStreamingAsync()
    {
        await _transport.ScopeControlAsync(false, 0);
        _transport.StopStreaming();
        State = ScopeState.Paused;
        _bus.PublishLog("Scope", "Scope stopped.");
    }

    private void TogglePauseReplay()
    {
        if (State == ScopeState.Live)
        {
            State = ScopeState.Replay;
            _bus.PublishLog("Scope", "Replay mode.");
            return;
        }

        if (State is ScopeState.Replay or ScopeState.Paused)
        {
            State = ScopeState.Live;
            _bus.PublishLog("Scope", "Live mode.");
        }
    }

    private void OnSampleBatchReceived(SampleBatch batch)
    {
        if (State == ScopeState.NoData)
        {
            State = ScopeState.Live;
        }

        for (var i = 0; i < _buffers.Count && i < batch.Channels.Count; i++)
        {
            _buffers[i].AddRange(batch.Channels[i]);
            var overflow = _buffers[i].Count - _maxPoints;
            if (overflow > 0)
            {
                _buffers[i].RemoveRange(0, overflow);
            }
        }

        SampleRateHz = batch.SampleRateHz;
        DropRatePercent = batch.DroppedFrameRatePercent;
        BufferUsagePercent = batch.BufferUsagePercent;
        LatencyMs = batch.LatencyMs;
        DataVersion++;
    }
}
