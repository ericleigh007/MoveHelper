using System.Text.Json;

namespace MoveHelper;

internal sealed class Settings
{
    public bool Enabled { get; set; } = true;

    public GripMode Mode { get; set; } = GripMode.NearPointer;

    public int HoverDelayMs { get; set; } = 200;

    public bool CornersEnabled { get; set; } = true;

    public int CornerReach { get; set; } = 28;

    public List<string> ExcludedProcesses { get; set; } = [];
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string Path { get; } = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MoveHelper",
        "settings.json");

    public static Settings Load()
    {
        try
        {
            if (!File.Exists(Path))
            {
                return new Settings();
            }

            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path), JsonOptions) ?? new Settings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new Settings();
        }
    }

    public static void Save(Settings settings)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
