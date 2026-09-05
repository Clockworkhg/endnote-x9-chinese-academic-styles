using System.Text.Json;

namespace EndNoteStyleToolbox;

internal sealed class AppSettings
{
    public string? StyleDirectory { get; set; }
    public bool DirectoryConfirmed { get; set; }
    public static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EndNoteStyleToolbox", "settings.json");

    public static AppSettings Load(string? path = null)
    {
        path ??= SettingsPath;
        if (!File.Exists(path)) return new();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // Keep the original file for diagnosis; settings failure must not prevent startup.
            return new();
        }
    }

    public void Save(string? path = null)
    {
        path ??= SettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
