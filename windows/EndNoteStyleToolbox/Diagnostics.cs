using System.Text;
using System.Text.Json;

namespace EndNoteStyleToolbox;

internal static class Diagnostics
{
    public static string WriteCrashLog(Exception exception)
    {
        var path = Path.Combine(Path.GetTempPath(), "EndNote-Chinese-Styles-Toolbox.log");
        var text = new StringBuilder()
            .AppendLine(DateTime.Now.ToString("O"))
            .AppendLine($"OS: {Environment.OSVersion}")
            .AppendLine($"Runtime: {Environment.Version}")
            .AppendLine($"64-bit process: {Environment.Is64BitProcess}")
            .AppendLine(exception.ToString())
            .ToString();
        File.WriteAllText(path, text, Encoding.UTF8);
        return path;
    }

    public static void WriteSelfTestReport(string path, object report)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        }), Encoding.UTF8);
    }
}
