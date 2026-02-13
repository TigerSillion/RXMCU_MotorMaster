using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using MotorDebugStudio.Infrastructure;
using MotorDebugStudio.Models;
using MotorDebugStudio.Services;

namespace MotorDebugStudio.ViewModels;

public sealed class SessionViewModel : ObservableObject
{
    private readonly SessionService _sessionService;
    private readonly Func<MotorProfile> _profileFactory;
    private readonly Action<MotorProfile> _profileLoader;
    private readonly Func<IEnumerable<LogEntry>> _logsProvider;
    private readonly Func<IEnumerable<FaultEvent>> _faultProvider;

    public ObservableCollection<LogEntry> Logs { get; } = [];
    public RelayCommand SaveProfileCommand { get; }
    public RelayCommand LoadProfileCommand { get; }
    public RelayCommand ExportCsvCommand { get; }

    public SessionViewModel(
        SessionService sessionService,
        Func<MotorProfile> profileFactory,
        Action<MotorProfile> profileLoader,
        Func<IEnumerable<LogEntry>> logsProvider,
        Func<IEnumerable<FaultEvent>> faultProvider)
    {
        _sessionService = sessionService;
        _profileFactory = profileFactory;
        _profileLoader = profileLoader;
        _logsProvider = logsProvider;
        _faultProvider = faultProvider;
        SaveProfileCommand = new RelayCommand(async _ => await SaveProfileAsync());
        LoadProfileCommand = new RelayCommand(async _ => await LoadProfileAsync());
        ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
    }

    private async Task SaveProfileAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Motor Profile (*.motorprofile.json)|*.motorprofile.json",
            FileName = "rx26t_default.motorprofile.json"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _sessionService.SaveProfileAsync(_profileFactory(), dialog.FileName);
        Logs.Insert(0, new LogEntry(DateTime.Now, "Session", $"Profile saved: {dialog.FileName}"));
    }

    private async Task LoadProfileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Motor Profile (*.motorprofile.json)|*.motorprofile.json"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var profile = await _sessionService.LoadProfileAsync(dialog.FileName);
        _profileLoader(profile);
        Logs.Insert(0, new LogEntry(DateTime.Now, "Session", $"Profile loaded: {dialog.FileName}"));
    }

    private async Task ExportCsvAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            FileName = "session_export_logs.csv"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _sessionService.ExportLogsCsvAsync(_logsProvider(), dialog.FileName);
        var faultPath = Path.Combine(Path.GetDirectoryName(dialog.FileName) ?? ".", "session_export_faults.csv");
        await _sessionService.ExportFaultsCsvAsync(_faultProvider(), faultPath);
        Logs.Insert(0, new LogEntry(DateTime.Now, "Session", $"CSV exported: {dialog.FileName} + {faultPath}"));
    }
}
