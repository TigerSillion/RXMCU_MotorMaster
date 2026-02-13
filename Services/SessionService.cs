using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.Services;

public sealed class SessionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task SaveProfileAsync(MotorProfile profile, string filePath)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
    }

    public async Task<MotorProfile> LoadProfileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<MotorProfile>(json) ?? new MotorProfile();
    }

    public async Task ExportLogsCsvAsync(IEnumerable<LogEntry> logs, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Category,Message");
        foreach (var log in logs)
        {
            var escaped = log.Message.Replace("\"", "\"\"");
            sb.AppendLine($"{log.Timestamp:O},{log.Category},\"{escaped}\"");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    public async Task ExportFaultsCsvAsync(IEnumerable<FaultEvent> faults, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,Severity,Code,Message,Context");
        foreach (var fault in faults)
        {
            sb.AppendLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{fault.Timestamp:O},{fault.Severity},{fault.Code},\"{fault.Message.Replace("\"", "\"\"")}\",\"{fault.Context.Replace("\"", "\"\"")}\""));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }
}
