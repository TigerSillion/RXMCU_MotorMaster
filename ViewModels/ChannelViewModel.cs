using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.ViewModels;

public sealed class ChannelViewModel : ObservableObject
{
    private bool _isEnabled = true;
    private string _name = string.Empty;
    private string _unit = string.Empty;

    public int Index { get; init; }

    public UartValueType Type { get; init; } = UartValueType.F32;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Unit
    {
        get => _unit;
        set => SetProperty(ref _unit, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}
