using System.Globalization;
using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.ViewModels;

public sealed class ParameterItemViewModel : ObservableObject
{
    private string _name = string.Empty;
    private string _unit = string.Empty;
    private uint _address;
    private UartValueType _type = UartValueType.F32;
    private double _min;
    private double _max;
    private bool _writable;
    private int _safetyLevel;
    private double _currentValue;
    private double _targetValue;
    private ParamState _state;
    private bool _autoRead;
    private string _note = string.Empty;

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

    public uint Address
    {
        get => _address;
        set
        {
            if (SetProperty(ref _address, value))
            {
                Notify(nameof(AddressHex));
            }
        }
    }

    public string AddressHex
    {
        get => $"0x{Address:X8}";
        set
        {
            if (TryParseAddress(value, out var addr))
            {
                Address = addr;
            }
        }
    }

    public UartValueType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public double Min
    {
        get => _min;
        set => SetProperty(ref _min, value);
    }

    public double Max
    {
        get => _max;
        set => SetProperty(ref _max, value);
    }

    public bool Writable
    {
        get => _writable;
        set => SetProperty(ref _writable, value);
    }

    public int SafetyLevel
    {
        get => _safetyLevel;
        set => SetProperty(ref _safetyLevel, value);
    }

    public double CurrentValue
    {
        get => _currentValue;
        set => SetProperty(ref _currentValue, value);
    }

    public double TargetValue
    {
        get => _targetValue;
        set => SetProperty(ref _targetValue, value);
    }

    public ParamState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public bool AutoRead
    {
        get => _autoRead;
        set => SetProperty(ref _autoRead, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    private static bool TryParseAddress(string? text, out uint address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var raw = text.Trim();
        if (raw.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            raw = raw[2..];
        }

        if (uint.TryParse(raw, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address))
        {
            return true;
        }

        return uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out address);
    }
}
