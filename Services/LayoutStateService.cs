using System.IO;
using System.Text.Json;
using MotorDebugStudio.Models;

namespace MotorDebugStudio.Services;

public sealed class LayoutStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _layoutFilePath;

    public LayoutStateService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var root = Path.Combine(appData, "MotorDebugStudio");
        _layoutFilePath = Path.Combine(root, "layout.json");
    }

    public LayoutState Load()
    {
        try
        {
            if (!File.Exists(_layoutFilePath))
            {
                return new LayoutState();
            }

            var json = File.ReadAllText(_layoutFilePath);
            var state = JsonSerializer.Deserialize<LayoutState>(json, JsonOptions);
            return state ?? new LayoutState();
        }
        catch
        {
            return new LayoutState();
        }
    }

    public void Save(LayoutState state)
    {
        var dir = Path.GetDirectoryName(_layoutFilePath);
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(_layoutFilePath, json);
    }
}
