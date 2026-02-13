using System.Collections.ObjectModel;
using System.Windows.Media;
using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.ViewModels;

public sealed class FaultViewModel : ObservableObject
{
    private readonly AppEventBus _bus;
    private FaultState _bannerState = FaultState.Hidden;
    private string _bannerText = "No active fault";

    public ObservableCollection<FaultEvent> FaultTimeline { get; } = [];

    public FaultViewModel(AppEventBus bus)
    {
        _bus = bus;
        _bus.OnFaultRaised += OnFaultRaised;
    }

    public FaultState BannerState
    {
        get => _bannerState;
        private set
        {
            if (SetProperty(ref _bannerState, value))
            {
                Notify(nameof(BannerBrush));
            }
        }
    }

    public string BannerText
    {
        get => _bannerText;
        private set => SetProperty(ref _bannerText, value);
    }

    public Brush BannerBrush => BannerState switch
    {
        FaultState.Critical => (Brush)App.Current.Resources["BrushDanger"],
        FaultState.Warning => (Brush)App.Current.Resources["BrushWarning"],
        FaultState.Acknowledged => (Brush)App.Current.Resources["BrushAccent"],
        _ => (Brush)App.Current.Resources["BrushTextSecondary"]
    };

    private void OnFaultRaised(FaultEvent fault)
    {
        FaultTimeline.Insert(0, fault);
        BannerState = fault.Severity;
        BannerText = $"{fault.Code}: {fault.Message}";
        _bus.PublishLog("Fault", $"[{fault.Severity}] {fault.Code} {fault.Message}");
    }
}
