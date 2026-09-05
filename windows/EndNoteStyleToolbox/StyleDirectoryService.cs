using Microsoft.Win32;

namespace EndNoteStyleToolbox;

internal sealed record DirectoryCandidate(string Path, string Source)
{
    public override string ToString() => $"{Path} — {Source}";
}

internal static class StyleDirectoryService
{
    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EndNote", "Styles");

    public static IReadOnlyList<DirectoryCandidate> Discover()
    {
        var found = new List<DirectoryCandidate>();
        void Add(string? path, string source)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
                if (!found.Any(c => c.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
                    found.Add(new(path, source));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException) { }
        }

        // Inspect only EndNote's own per-user settings, without assuming a version-specific value name.
        try
        {
            using var root = Registry.CurrentUser.OpenSubKey(@"Software\ISI ResearchSoft\EndNote");
            void Inspect(RegistryKey key, int depth)
            {
                foreach (var name in key.GetValueNames())
                {
                    if (key.GetValue(name) is string value &&
                        (name.Contains("style", StringComparison.OrdinalIgnoreCase) ||
                         key.Name.Contains("style", StringComparison.OrdinalIgnoreCase)) &&
                        Path.IsPathFullyQualified(value) && Directory.Exists(value))
                        Add(value, "EndNote 设置候选，需核对");
                }
                if (depth == 0) return;
                foreach (var name in key.GetSubKeyNames())
                {
                    using var child = key.OpenSubKey(name);
                    if (child != null) Inspect(child, depth - 1);
                }
            }
            if (root != null) Inspect(root, 3);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException) { }
        Add(DefaultDirectory, "Windows 个人文档默认位置，需核对");
        foreach (var variable in new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
        {
            var folder = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                var path = Path.Combine(folder, "Documents", "EndNote", "Styles");
                if (Directory.Exists(path)) Add(path, "OneDrive 候选，需核对");
            }
        }
        return found;
    }

    public static string Validate(string path)
    {
        if (!Path.IsPathFullyQualified(path)) throw new IOException("请选择完整的 Styles 文件夹路径。");
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        if (full.Equals(Path.TrimEndingDirectorySeparator(Path.GetPathRoot(full)!), StringComparison.OrdinalIgnoreCase))
            throw new IOException("不能将磁盘或共享根目录作为样式目录。");
        foreach (var special in new[] { Environment.SpecialFolder.Windows, Environment.SpecialFolder.ProgramFiles,
                     Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.UserProfile,
                     Environment.SpecialFolder.MyDocuments })
        {
            var protectedPath = Environment.GetFolderPath(special);
            if (string.IsNullOrWhiteSpace(protectedPath)) continue;
            if (full.Equals(protectedPath, StringComparison.OrdinalIgnoreCase) ||
                (special is Environment.SpecialFolder.Windows or Environment.SpecialFolder.ProgramFiles or Environment.SpecialFolder.ProgramFilesX86 &&
                 full.StartsWith(protectedPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                throw new IOException("请选择个人 Styles 子文件夹，不要选择系统目录或整个用户/文档目录。");
        }
        for (var current = new DirectoryInfo(full); current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("该路径经过链接或重定向点，请选择对应的实际文件夹路径。");
        return full;
    }

    public static string CheckWritable(string path)
    {
        var full = Validate(path);
        Directory.CreateDirectory(full);
        var probe = Path.Combine(full, ".endnote-toolbox-probe-" + Guid.NewGuid().ToString("N"));
        using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
        return full;
    }
}
