using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;
using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Messaging;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;

namespace MotorDebugStudio.ViewModels;

public sealed class ParamViewModel : ObservableObject
{
    private const uint AddressRangeMin = 0x00000000;
    private const uint AddressRangeMax = 0x0013FFFF;

    private readonly AppEventBus _bus;
    private readonly ITransportService _transport;
    private readonly DispatcherTimer _autoRefreshTimer;
    private bool _autoRefreshBusy;
    private bool _autoRefreshEnabled;
    private int _autoRefreshMs = 500;

    private ParameterItemViewModel? _selectedParameter;
    private string _variableSearchText = string.Empty;

    private string _motorState = "IDLE";
    private double _runtimeSpeed;
    private double _runtimeIq;
    private double _runtimeBusVoltage;
    private double _runtimeBoardTemp;

    private string _artifactXPath = string.Empty;
    private string _artifactAbsPath = string.Empty;
    private string _artifactMapPath = string.Empty;
    private string _artifactMotPath = string.Empty;
    private string _mapImportStatus = "MAP not imported";

    private string _newVarName = string.Empty;
    private string _newVarAddressHex = "0x00000000";
    private UartValueType _newVarType = UartValueType.U32;
    private bool _newVarWritable;
    private string _newVarUnit = string.Empty;
    private string _newVarNote = string.Empty;

    public ObservableCollection<ParameterItemViewModel> Parameters { get; } = [];
    public ICollectionView FilteredParameters { get; }

    public ObservableCollection<UartValueType> SupportedTypes { get; } =
    [
        UartValueType.U8,
        UartValueType.S8,
        UartValueType.U16,
        UartValueType.S16,
        UartValueType.U32,
        UartValueType.S32,
        UartValueType.F32,
    ];

    public RelayCommand RefreshAllCommand { get; }
    public RelayCommand RefreshSelectedCommand { get; }
    public RelayCommand RefreshCheckedCommand { get; }
    public RelayCommand WriteSelectedCommand { get; }
    public RelayCommand StartMotorCommand { get; }
    public RelayCommand StopMotorCommand { get; }
    public RelayCommand ClearFaultCommand { get; }
    public RelayCommand SwitchModeCommand { get; }

    public RelayCommand BrowseXPathCommand { get; }
    public RelayCommand BrowseAbsPathCommand { get; }
    public RelayCommand BrowseMapPathCommand { get; }
    public RelayCommand BrowseMotPathCommand { get; }
    public RelayCommand ImportMapCommand { get; }
    public RelayCommand BrowseAndImportMapCommand { get; }
    public RelayCommand RefreshAddressesCommand { get; }

    public RelayCommand ToggleAutoRefreshCommand { get; }
    public RelayCommand AddCustomVariableCommand { get; }

    public ParamViewModel(AppEventBus bus, ITransportService transport)
    {
        _bus = bus;
        _transport = transport;

        RefreshAllCommand = new RelayCommand(_ => _ = RefreshAllAsync(), _ => _transport.IsConnected);
        RefreshSelectedCommand = new RelayCommand(_ => _ = RefreshSelectedAsync(), _ => _transport.IsConnected && SelectedParameter is not null);
        RefreshCheckedCommand = new RelayCommand(_ => _ = RefreshCheckedAsync(), _ => _transport.IsConnected && Parameters.Any(static p => p.AutoRead));
        WriteSelectedCommand = new RelayCommand(_ => _ = WriteSelectedAsync(), _ => SelectedParameter?.Writable == true && _transport.IsConnected);
        StartMotorCommand = new RelayCommand(_ => _ = SendMotorCommandAsync(1, "RUN"), _ => _transport.IsConnected);
        StopMotorCommand = new RelayCommand(_ => _ = SendMotorCommandAsync(0, "STOP"), _ => _transport.IsConnected);
        ClearFaultCommand = new RelayCommand(_ => _ = SendMotorCommandAsync(3, "RESET"), _ => _transport.IsConnected);
        SwitchModeCommand = new RelayCommand(_ => _ = ToggleLoopModeAsync(), _ => _transport.IsConnected);

        BrowseXPathCommand = new RelayCommand(_ => BrowseFile("CCRX X file|*.x", path => ArtifactXPath = path));
        BrowseAbsPathCommand = new RelayCommand(_ => BrowseFile("CCRX ABS file|*.abs", path => ArtifactAbsPath = path));
        BrowseMapPathCommand = new RelayCommand(_ => BrowseFile("CCRX MAP file|*.map", path => ArtifactMapPath = path));
        BrowseMotPathCommand = new RelayCommand(_ => BrowseFile("CCRX MOT file|*.mot", path => ArtifactMotPath = path));
        ImportMapCommand = new RelayCommand(_ => ImportFromMap(replaceExisting: true), _ => !string.IsNullOrWhiteSpace(ArtifactMapPath));
        BrowseAndImportMapCommand = new RelayCommand(_ => BrowseAndImportMap());
        RefreshAddressesCommand = new RelayCommand(_ => RefreshAddressesFromMap(), _ => !string.IsNullOrWhiteSpace(ArtifactMapPath));

        ToggleAutoRefreshCommand = new RelayCommand(_ => ToggleAutoRefresh());
        AddCustomVariableCommand = new RelayCommand(_ => AddCustomVariable());

        _autoRefreshTimer = new DispatcherTimer();
        _autoRefreshTimer.Interval = TimeSpan.FromMilliseconds(_autoRefreshMs);
        _autoRefreshTimer.Tick += async (_, _) => await AutoRefreshTickAsync();

        BuildDefaultParameters();
        Parameters.CollectionChanged += OnParametersCollectionChanged;
        AttachParameterHandlers(Parameters);
        FilteredParameters = CollectionViewSource.GetDefaultView(Parameters);
        FilteredParameters.Filter = FilterPredicate;
        SelectedParameter = Parameters.FirstOrDefault();

        _bus.OnSampleBatchReceived += OnSampleBatchReceived;
        _bus.OnTransportStateChanged += OnTransportStateChanged;
    }

    public string MotorState
    {
        get => _motorState;
        private set => SetProperty(ref _motorState, value);
    }

    public double RuntimeSpeed
    {
        get => _runtimeSpeed;
        private set => SetProperty(ref _runtimeSpeed, value);
    }

    public double RuntimeIq
    {
        get => _runtimeIq;
        private set => SetProperty(ref _runtimeIq, value);
    }

    public double RuntimeBusVoltage
    {
        get => _runtimeBusVoltage;
        private set => SetProperty(ref _runtimeBusVoltage, value);
    }

    public double RuntimeBoardTemp
    {
        get => _runtimeBoardTemp;
        private set => SetProperty(ref _runtimeBoardTemp, value);
    }

    public string VariableSearchText
    {
        get => _variableSearchText;
        set
        {
            if (SetProperty(ref _variableSearchText, value))
            {
                FilteredParameters.Refresh();
            }
        }
    }

    public string ArtifactXPath
    {
        get => _artifactXPath;
        set => SetProperty(ref _artifactXPath, value);
    }

    public string ArtifactAbsPath
    {
        get => _artifactAbsPath;
        set => SetProperty(ref _artifactAbsPath, value);
    }

    public string ArtifactMapPath
    {
        get => _artifactMapPath;
        set
        {
            if (SetProperty(ref _artifactMapPath, value))
            {
                ImportMapCommand.RaiseCanExecuteChanged();
                RefreshAddressesCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string ArtifactMotPath
    {
        get => _artifactMotPath;
        set => SetProperty(ref _artifactMotPath, value);
    }

    public string MapImportStatus
    {
        get => _mapImportStatus;
        private set => SetProperty(ref _mapImportStatus, value);
    }

    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        private set
        {
            if (SetProperty(ref _autoRefreshEnabled, value))
            {
                Notify(nameof(AutoRefreshStatus));
            }
        }
    }

    public int AutoRefreshMs
    {
        get => _autoRefreshMs;
        set
        {
            var clamped = Math.Clamp(value, 50, 5000);
            if (SetProperty(ref _autoRefreshMs, clamped))
            {
                _autoRefreshTimer.Interval = TimeSpan.FromMilliseconds(clamped);
                Notify(nameof(AutoRefreshStatus));
            }
        }
    }

    public string AutoRefreshStatus => AutoRefreshEnabled ? $"ON ({AutoRefreshMs} ms)" : "OFF";

    public string NewVarName
    {
        get => _newVarName;
        set => SetProperty(ref _newVarName, value);
    }

    public string NewVarAddressHex
    {
        get => _newVarAddressHex;
        set => SetProperty(ref _newVarAddressHex, value);
    }

    public UartValueType NewVarType
    {
        get => _newVarType;
        set => SetProperty(ref _newVarType, value);
    }

    public bool NewVarWritable
    {
        get => _newVarWritable;
        set => SetProperty(ref _newVarWritable, value);
    }

    public string NewVarUnit
    {
        get => _newVarUnit;
        set => SetProperty(ref _newVarUnit, value);
    }

    public string NewVarNote
    {
        get => _newVarNote;
        set => SetProperty(ref _newVarNote, value);
    }

    public ParameterItemViewModel? SelectedParameter
    {
        get => _selectedParameter;
        set
        {
            if (SetProperty(ref _selectedParameter, value))
            {
                WriteSelectedCommand?.RaiseCanExecuteChanged();
                RefreshSelectedCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    private bool FilterPredicate(object obj)
    {
        if (obj is not ParameterItemViewModel item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(VariableSearchText))
        {
            return true;
        }

        var term = VariableSearchText.Trim();
        return item.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.AddressHex.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.Note.Contains(term, StringComparison.OrdinalIgnoreCase)
            || item.Type.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void BuildDefaultParameters()
    {
        Parameters.Clear();
        Parameters.Add(BuildParam("com_u1_system_mode", 0x00001801, UartValueType.U8, 0, 3, true, 2, string.Empty, 0, true, "System mode"));
        Parameters.Add(BuildParam("com_f4_ref_speed_rpm", 0x00001DC0, UartValueType.F32, 0, 6000, true, 2, "rpm", 500, true, "Speed reference"));
        Parameters.Add(BuildParam("com_f4_speed_rate_limit_rpm", 0x00001DC4, UartValueType.F32, 0, 20000, true, 1, "rpm/s", 1000, false, string.Empty));
        Parameters.Add(BuildParam("com_f4_overspeed_limit_rpm", 0x00001DC8, UartValueType.F32, 100, 12000, true, 2, "rpm", 4500, false, string.Empty));
        Parameters.Add(BuildParam("com_u1_ctrl_loop_mode", 0x00001807, UartValueType.U8, 0, 4, true, 1, string.Empty, 0, false, string.Empty));
        Parameters.Add(BuildParam("com_u1_sw_userif", 0x00001805, UartValueType.U8, 0, 2, true, 1, string.Empty, 0, false, string.Empty));
        Parameters.Add(BuildParam("g_u1_system_mode", 0x00001802, UartValueType.U8, 0, 255, false, 0, string.Empty, 0, true, string.Empty));
        Parameters.Add(BuildParam("com_u1_enable_write", 0x00001803, UartValueType.U8, 0, 1, false, 0, string.Empty, 0, false, string.Empty));
    }

    public async Task RefreshAllAsync()
    {
        if (!_transport.IsConnected)
        {
            return;
        }

        foreach (var item in Parameters)
        {
            await RefreshOneAsync(item);
        }

        UpdateMotorStateFromVariables();
        _bus.PublishLog("Param", $"Read all complete. vars={Parameters.Count}");
    }

    private async Task RefreshSelectedAsync()
    {
        if (!_transport.IsConnected || SelectedParameter is null)
        {
            return;
        }

        await RefreshOneAsync(SelectedParameter);
        UpdateMotorStateFromVariables();
    }

    private async Task RefreshCheckedAsync()
    {
        if (!_transport.IsConnected)
        {
            return;
        }

        var checkedVars = Parameters.Where(static p => p.AutoRead).ToList();
        if (checkedVars.Count == 0)
        {
            return;
        }

        foreach (var item in checkedVars)
        {
            await RefreshOneAsync(item);
        }

        UpdateMotorStateFromVariables();
    }

    private async Task RefreshOneAsync(ParameterItemViewModel item)
    {
        var value = await _transport.ReadTypedAsDoubleAsync(item.Address, item.Type);
        if (!value.HasValue)
        {
            item.State = ParamState.Stale;
            return;
        }

        item.CurrentValue = value.Value;
        if (!item.Writable)
        {
            item.TargetValue = value.Value;
        }

        item.State = item.Writable ? ParamState.Writable : ParamState.Readable;
    }

    private async Task WriteSelectedAsync()
    {
        if (SelectedParameter is null)
        {
            return;
        }

        if (!SelectedParameter.Writable)
        {
            _bus.PublishLog("Param", $"{SelectedParameter.Name} blocked: readonly.");
            return;
        }

        if (SelectedParameter.TargetValue < SelectedParameter.Min || SelectedParameter.TargetValue > SelectedParameter.Max)
        {
            MessageBox.Show(
                $"{SelectedParameter.Name} target out of range [{SelectedParameter.Min}, {SelectedParameter.Max}]",
                "Range Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            SelectedParameter.State = ParamState.WriteBlocked;
            return;
        }

        if (SelectedParameter.SafetyLevel >= 2)
        {
            var ret = MessageBox.Show(
                $"Write {SelectedParameter.Name} @0x{SelectedParameter.Address:X8} to {SelectedParameter.TargetValue:F3}{SelectedParameter.Unit} ?",
                "Second Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (ret != MessageBoxResult.Yes)
            {
                return;
            }
        }

        SelectedParameter.State = ParamState.PendingWrite;
        var ok = await _transport.WriteTypedFromDoubleAsync(SelectedParameter.Address, SelectedParameter.Type, SelectedParameter.TargetValue);
        if (!ok)
        {
            SelectedParameter.State = ParamState.Stale;
            _bus.PublishParamWriteResult(new ParamWriteResult(DateTime.Now, SelectedParameter.Name, false, "write failed"));
            return;
        }

        var readback = await _transport.ReadTypedAsDoubleAsync(SelectedParameter.Address, SelectedParameter.Type);
        if (!readback.HasValue)
        {
            SelectedParameter.State = ParamState.Stale;
            _bus.PublishParamWriteResult(new ParamWriteResult(DateTime.Now, SelectedParameter.Name, false, "readback failed"));
            return;
        }

        SelectedParameter.CurrentValue = readback.Value;
        SelectedParameter.State = ParamState.Writable;
        _bus.PublishParamWriteResult(new ParamWriteResult(DateTime.Now, SelectedParameter.Name, true, "ACK + Readback verified"));
    }

    private async Task SendMotorCommandAsync(byte mode, string stateLabel)
    {
        var ok = await _transport.MotorCtrlAsync(mode);
        if (!ok)
        {
            _bus.PublishFault(new FaultEvent(DateTime.Now, FaultState.Warning, "CMD_FAIL", $"MotorCtrl {mode} failed", "UART protocol"));
            return;
        }

        MotorState = stateLabel;
        _bus.PublishLog("Command", $"MOTOR_CTRL mode={mode}");
    }

    private async Task ToggleLoopModeAsync()
    {
        var modeVar = Parameters.FirstOrDefault(static p => p.Name == "com_u1_ctrl_loop_mode");
        if (modeVar is null)
        {
            return;
        }

        var next = modeVar.TargetValue < 0.5 ? 1 : 0;
        modeVar.TargetValue = next;
        SelectedParameter = modeVar;
        await WriteSelectedAsync();
    }

    private void BrowseAndImportMap()
    {
        BrowseFile("CCRX MAP file|*.map", path => ArtifactMapPath = path);
        if (!string.IsNullOrWhiteSpace(ArtifactMapPath))
        {
            ImportFromMap(replaceExisting: true);
        }
    }

    private void RefreshAddressesFromMap()
    {
        if (string.IsNullOrWhiteSpace(ArtifactMapPath))
        {
            return;
        }

        var symbols = CcrxMapParser.Parse(ArtifactMapPath);
        var map = symbols.ToDictionary(static s => s.Name, StringComparer.Ordinal);
        var found = 0;

        foreach (var item in Parameters)
        {
            if (map.TryGetValue(item.Name, out var sym))
            {
                item.Address = sym.Address;
                if (sym.SizeBytes > 0)
                {
                    item.Type = InferType(item.Name, sym.SizeBytes);
                }

                item.Note = item.Note.Replace("NOT FOUND", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                found++;
            }
            else
            {
                item.Note = string.IsNullOrWhiteSpace(item.Note) ? "NOT FOUND" : $"{item.Note} | NOT FOUND";
                item.State = ParamState.Stale;
            }
        }

        MapImportStatus = $"Refresh addresses done. found={found}, total={Parameters.Count}";
        _bus.PublishLog("Map", MapImportStatus);
    }

    private void ImportFromMap(bool replaceExisting)
    {
        var symbols = CcrxMapParser.Parse(ArtifactMapPath);
        if (symbols.Count == 0)
        {
            MapImportStatus = "MAP parse failed or no symbols.";
            _bus.PublishLog("Map", MapImportStatus);
            return;
        }

        var inRange = symbols.Where(static s => s.Address >= AddressRangeMin && s.Address <= AddressRangeMax).ToList();
        var imported = inRange.Select(BuildParamFromSymbol).ToList();

        if (imported.Count == 0)
        {
            MapImportStatus = $"No symbols in valid range 0x{AddressRangeMin:X8}..0x{AddressRangeMax:X8}";
            _bus.PublishLog("Map", MapImportStatus);
            return;
        }

        var old = Parameters.ToDictionary(static p => p.Name, StringComparer.Ordinal);

        if (replaceExisting)
        {
            Parameters.Clear();
            foreach (var item in imported.OrderBy(static p => p.Address))
            {
                if (old.TryGetValue(item.Name, out var prev))
                {
                    item.AutoRead = prev.AutoRead;
                    item.Note = prev.Note;
                    item.CurrentValue = prev.CurrentValue;
                    item.TargetValue = prev.TargetValue;
                }

                Parameters.Add(item);
            }
        }
        else
        {
            foreach (var item in imported)
            {
                if (old.ContainsKey(item.Name))
                {
                    continue;
                }

                Parameters.Add(item);
            }
        }

        FilteredParameters.Refresh();
        SelectedParameter = Parameters.FirstOrDefault();
        MapImportStatus = $"Parsed {symbols.Count}, in-range {inRange.Count}, imported {imported.Count}, table {Parameters.Count}";
        _bus.PublishLog("Map", MapImportStatus);
        RefreshCheckedCommand.RaiseCanExecuteChanged();
    }

    private ParameterItemViewModel BuildParamFromSymbol(CcrxSymbol sym)
    {
        var type = InferType(sym.Name, sym.SizeBytes);
        var writable = sym.Name.StartsWith("com_", StringComparison.Ordinal);
        var (min, max) = InferRange(type);
        var safety = sym.Name.Contains("system_mode", StringComparison.Ordinal) || sym.Name.Contains("speed", StringComparison.Ordinal)
            ? 2
            : (writable ? 1 : 0);

        return BuildParam(sym.Name, sym.Address, type, min, max, writable, safety, InferUnit(sym.Name), 0, false, string.Empty);
    }

    private static ParameterItemViewModel BuildParam(string name, uint address, UartValueType type, double min, double max, bool writable, int safety, string unit, double target, bool autoRead, string note)
    {
        return new ParameterItemViewModel
        {
            Name = name,
            Unit = unit,
            Address = address,
            Type = type,
            Min = min,
            Max = max,
            Writable = writable,
            SafetyLevel = safety,
            CurrentValue = 0,
            TargetValue = target,
            AutoRead = autoRead,
            Note = note,
            State = writable ? ParamState.Writable : ParamState.Readable
        };
    }

    private static string InferUnit(string name)
    {
        if (name.Contains("speed", StringComparison.OrdinalIgnoreCase)) return "rpm";
        if (name.Contains("temp", StringComparison.OrdinalIgnoreCase)) return "C";
        if (name.Contains("volt", StringComparison.OrdinalIgnoreCase) || name.Contains("vdc", StringComparison.OrdinalIgnoreCase)) return "V";
        if (name.Contains("current", StringComparison.OrdinalIgnoreCase) || name.Contains("iq", StringComparison.OrdinalIgnoreCase) || name.Contains("id", StringComparison.OrdinalIgnoreCase)) return "A";
        return string.Empty;
    }

    private static (double Min, double Max) InferRange(UartValueType type)
    {
        return type switch
        {
            UartValueType.U8 => (0, byte.MaxValue),
            UartValueType.S8 => (sbyte.MinValue, sbyte.MaxValue),
            UartValueType.U16 => (0, ushort.MaxValue),
            UartValueType.S16 => (short.MinValue, short.MaxValue),
            UartValueType.U32 => (0, uint.MaxValue),
            UartValueType.S32 => (int.MinValue, int.MaxValue),
            UartValueType.F32 => (-1.0e9, 1.0e9),
            _ => (0, uint.MaxValue),
        };
    }

    private static UartValueType InferType(string name, int size)
    {
        if (name.Contains("_f4_", StringComparison.Ordinal)) return UartValueType.F32;
        if (name.Contains("_u1_", StringComparison.Ordinal)) return UartValueType.U8;
        if (name.Contains("_s1_", StringComparison.Ordinal)) return UartValueType.S8;
        if (name.Contains("_u2_", StringComparison.Ordinal)) return UartValueType.U16;
        if (name.Contains("_s2_", StringComparison.Ordinal)) return UartValueType.S16;
        if (name.Contains("_u4_", StringComparison.Ordinal)) return UartValueType.U32;
        if (name.Contains("_s4_", StringComparison.Ordinal)) return UartValueType.S32;

        return size switch
        {
            1 => UartValueType.U8,
            2 => UartValueType.U16,
            4 => UartValueType.U32,
            _ => UartValueType.U32,
        };
    }

    private void ToggleAutoRefresh()
    {
        if (AutoRefreshEnabled)
        {
            _autoRefreshTimer.Stop();
            AutoRefreshEnabled = false;
        }
        else
        {
            _autoRefreshTimer.Interval = TimeSpan.FromMilliseconds(AutoRefreshMs);
            _autoRefreshTimer.Start();
            AutoRefreshEnabled = true;
        }
    }

    private async Task AutoRefreshTickAsync()
    {
        if (_autoRefreshBusy || !AutoRefreshEnabled || !_transport.IsConnected)
        {
            return;
        }

        _autoRefreshBusy = true;
        try
        {
            await RefreshCheckedAsync();
        }
        finally
        {
            _autoRefreshBusy = false;
        }
    }

    private void AddCustomVariable()
    {
        if (!TryParseAddress(NewVarAddressHex, out var address))
        {
            MapImportStatus = "Invalid custom address format.";
            return;
        }

        if (address < AddressRangeMin || address > AddressRangeMax)
        {
            MapImportStatus = $"Address out of range: 0x{address:X8}";
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewVarName) ? $"custom_{address:X8}" : NewVarName.Trim();
        var (min, max) = InferRange(NewVarType);
        var item = BuildParam(name, address, NewVarType, min, max, NewVarWritable, NewVarWritable ? 1 : 0, NewVarUnit, 0, false, NewVarNote);

        var old = Parameters.FirstOrDefault(p => p.Name.Equals(name, StringComparison.Ordinal));
        if (old is not null)
        {
            Parameters.Remove(old);
        }

        Parameters.Add(item);
        SelectedParameter = item;
        FilteredParameters.Refresh();
        MapImportStatus = $"Added custom variable: {name} @0x{address:X8}";
        _bus.PublishLog("Param", MapImportStatus);
    }

    private static bool TryParseAddress(string text, out uint address)
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

    private void BrowseFile(string filter, Action<string> setter)
    {
        var dlg = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select CCRX artifact"
        };

        if (dlg.ShowDialog() == true)
        {
            setter(dlg.FileName);
        }
    }

    private void UpdateMotorStateFromVariables()
    {
        var stateVar = Parameters.FirstOrDefault(static p => p.Name == "g_u1_system_mode");
        if (stateVar is not null)
        {
            MotorState = $"MODE={stateVar.CurrentValue.ToString("F0", CultureInfo.InvariantCulture)}";
        }
    }

    private void OnSampleBatchReceived(SampleBatch batch)
    {
        if (batch.Channels.Count < 4)
        {
            return;
        }

        RuntimeSpeed = batch.Channels[0].Length > 0 ? batch.Channels[0][^1] : RuntimeSpeed;
        RuntimeIq = batch.Channels[2].Length > 0 ? batch.Channels[2][^1] : RuntimeIq;
        RuntimeBusVoltage = batch.Channels[3].Length > 0 ? batch.Channels[3][^1] : RuntimeBusVoltage;
        RuntimeBoardTemp = RuntimeBoardTemp * 0.98 + 35.0 * 0.02;
    }

    private void OnTransportStateChanged(TransportState _)
    {
        RefreshCommandStates();
        if (!_transport.IsConnected && AutoRefreshEnabled)
        {
            _autoRefreshTimer.Stop();
            AutoRefreshEnabled = false;
        }
    }

    private void RefreshCommandStates()
    {
        RefreshAllCommand.RaiseCanExecuteChanged();
        RefreshSelectedCommand.RaiseCanExecuteChanged();
        RefreshCheckedCommand.RaiseCanExecuteChanged();
        WriteSelectedCommand.RaiseCanExecuteChanged();
        StartMotorCommand.RaiseCanExecuteChanged();
        StopMotorCommand.RaiseCanExecuteChanged();
        ClearFaultCommand.RaiseCanExecuteChanged();
        SwitchModeCommand.RaiseCanExecuteChanged();
        ImportMapCommand.RaiseCanExecuteChanged();
        RefreshAddressesCommand.RaiseCanExecuteChanged();
    }
    private void OnParametersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<ParameterItemViewModel>())
            {
                item.PropertyChanged -= OnParameterItemPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<ParameterItemViewModel>())
            {
                item.PropertyChanged += OnParameterItemPropertyChanged;
            }
        }

        RefreshCheckedCommand.RaiseCanExecuteChanged();
        WriteSelectedCommand.RaiseCanExecuteChanged();
    }

    private void AttachParameterHandlers(IEnumerable<ParameterItemViewModel> items)
    {
        foreach (var item in items)
        {
            item.PropertyChanged += OnParameterItemPropertyChanged;
        }
    }

    private void OnParameterItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterItemViewModel.AutoRead))
        {
            RefreshCheckedCommand.RaiseCanExecuteChanged();
        }

        if (sender == SelectedParameter && e.PropertyName is nameof(ParameterItemViewModel.Writable))
        {
            WriteSelectedCommand.RaiseCanExecuteChanged();
        }
    }

}
