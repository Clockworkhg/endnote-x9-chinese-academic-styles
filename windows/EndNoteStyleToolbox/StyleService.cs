using System.Security.Cryptography;

namespace EndNoteStyleToolbox;

internal sealed class StyleService
{
    public StyleService(string? targetDirectory = null)
    {
        TargetDirectory = StyleDirectoryService.Validate(targetDirectory ?? StyleDirectoryService.DefaultDirectory);
    }

    public string TargetDirectory { get; }

    public InstalledState GetState(StyleInfo style)
    {
        var target = TargetPath(style);
        if (!File.Exists(target))
        {
            return InstalledState.NotInstalled;
        }

        var expected = Convert.FromHexString(style.Sha256);
        using var stream = File.OpenRead(target);
        var actual = SHA256.HashData(stream);
        return CryptographicOperations.FixedTimeEquals(expected, actual)
            ? InstalledState.Installed
            : InstalledState.DifferentVersion;
    }

    public OperationResult Install(StyleInfo style)
    {
        StyleDirectoryService.CheckWritable(TargetDirectory);
        var target = TargetPath(style);
        string? backupDirectory = null;

        if (File.Exists(target))
        {
            backupDirectory = CreateBackupDirectory("CN-Academic-Backup-");
            File.Copy(target, Path.Combine(backupDirectory, style.Filename), overwrite: true);
        }

        WriteStyle(style, target);
        MarkManaged(style);

        return new OperationResult($"已安装：{style.Title}", backupDirectory);
    }

    public OperationResult InstallAll(IEnumerable<StyleInfo> styles)
    {
        var requested = styles.ToArray();
        StyleDirectoryService.CheckWritable(TargetDirectory);
        var existing = requested.Where(style => File.Exists(TargetPath(style))).ToArray();
        string? backupDirectory = null;
        if (existing.Length > 0)
        {
            backupDirectory = CreateBackupDirectory("CN-Academic-Backup-");
            foreach (var style in existing)
            {
                File.Copy(TargetPath(style), Path.Combine(backupDirectory, style.Filename), overwrite: true);
            }
        }

        foreach (var style in requested)
        {
            WriteStyle(style, TargetPath(style));
            MarkManaged(style);
        }

        var suffix = backupDirectory is not null ? "；原文件已备份" : string.Empty;
        return new OperationResult($"已安装{requested.Length}套格式{suffix}。", backupDirectory);
    }

    public OperationResult Uninstall(StyleInfo style)
    {
        var target = TargetPath(style);
        if (!IsManaged(style)) return new OperationResult($"未卸载：{style.Title}。没有匹配的安装记录，或文件已被修改；已保留原文件。");
        if (!File.Exists(target))
        {
            return new OperationResult($"尚未安装：{style.Title}");
        }

        var backupDirectory = CreateBackupDirectory("CN-Academic-Removed-");
        File.Move(target, Path.Combine(backupDirectory, style.Filename), overwrite: true);
        return new OperationResult($"已卸载并备份：{style.Title}", backupDirectory);
    }

    public OperationResult UninstallAll(IEnumerable<StyleInfo> styles)
    {
        var installed = styles.Where(IsManaged).ToArray();
        if (installed.Length == 0)
        {
            return new OperationResult("没有检测到由本工具箱管理的样式。");
        }

        var backupDirectory = CreateBackupDirectory("CN-Academic-Removed-");
        foreach (var style in installed)
        {
            File.Move(TargetPath(style), Path.Combine(backupDirectory, style.Filename), overwrite: true);
        }

        return new OperationResult($"已卸载并备份{installed.Length}套格式。", backupDirectory);
    }

    private string TargetPath(StyleInfo style)
    {
        var filename = Path.GetFileName(style.Filename);
        if (!filename.Equals(style.Filename, StringComparison.Ordinal))
        {
            throw new InvalidDataException("样式文件名包含非法路径。");
        }
        return Path.Combine(TargetDirectory, filename);
    }

    private string ReceiptPath(StyleInfo style) => Path.Combine(TargetDirectory, ".toolbox-receipts", Path.GetFileName(style.Filename) + ".sha256");

    private void MarkManaged(StyleInfo style)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReceiptPath(style))!);
        File.WriteAllText(ReceiptPath(style), style.Sha256);
    }

    private bool IsManaged(StyleInfo style)
    {
        var target = TargetPath(style);
        var receipt = ReceiptPath(style);
        if (!File.Exists(target) || !File.Exists(receipt)) return false;
        using var stream = File.OpenRead(target);
        return Convert.ToHexString(SHA256.HashData(stream)).Equals(File.ReadAllText(receipt).Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteStyle(StyleInfo style, string target)
    {
        var temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            var bytes = EmbeddedAssets.ReadStyle(style.Filename);
            if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(style.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("内置样式校验失败，未覆盖目标文件。");
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, target, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private string CreateBackupDirectory(string prefix)
    {
        Directory.CreateDirectory(TargetDirectory);
        var path = Path.Combine(TargetDirectory, prefix + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
        Directory.CreateDirectory(path);
        return path;
    }

}
