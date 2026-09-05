using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace EndNoteStyleToolbox;

internal sealed record UpdateRelease(Version Version, string DownloadUrl, string ChecksumUrl, string Notes);
internal sealed record UpdatePlan(string Target, string Sha256, int ParentId, string HealthFile);

internal static class UpdateService
{
    public const string Repository = "Clockworkhg/endnote-x9-chinese-academic-styles";
    public static Version CurrentVersion => new(0, 5, 0);
    private const string Filename = "EndNoteStyleToolbox.exe";
    private static HttpClient Client()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("EndNoteStyleToolbox/0.5.0");
        return client;
    }

    public static async Task<UpdateRelease?> CheckAsync(CancellationToken token)
    {
        using var client = Client();
        using var response = await client.GetAsync($"https://api.github.com/repos/{Repository}/releases/latest", token);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var root = json.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Version.TryParse(tag.TrimStart('v'), out var version)) throw new InvalidDataException("发布版本号无法识别。");
        if (version <= CurrentVersion) return null;
        string Find(string name)
        {
            var asset = root.GetProperty("assets").EnumerateArray().Single(a => a.GetProperty("name").GetString() == name);
            var url = asset.GetProperty("browser_download_url").GetString()!;
            var expected = $"https://github.com/{Repository}/releases/download/{tag}/{name}";
            if (url != expected) throw new InvalidDataException("更新下载地址不属于本项目发布资产。");
            return url;
        }
        return new(version, Find(Filename), Find(Filename + ".sha256"), root.GetProperty("body").GetString() ?? "");
    }

    public static bool MatchesHash(string path, string expected)
    {
        if (expected.Length != 64 || !expected.All(Uri.IsHexDigit)) return false;
        using var stream = File.OpenRead(path);
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(stream), Convert.FromHexString(expected));
    }

    public static async Task<string> DownloadAsync(UpdateRelease release, IProgress<string> progress, CancellationToken token)
    {
        var target = Environment.ProcessPath ?? throw new IOException("无法确定程序所在位置。");
        if (!Path.GetFileName(target).Equals(Filename, StringComparison.OrdinalIgnoreCase))
            throw new IOException($"自动更新要求程序文件名为 {Filename}。请恢复该名称，或从发布页手动更新。");
        var staging = Path.Combine(Path.GetDirectoryName(target)!, ".toolbox-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        using var client = Client();
        var checksumText = await client.GetStringAsync(release.ChecksumUrl, token);
        var parts = checksumText.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || parts[1] != Filename || parts[0].Length != 64 || !parts[0].All(Uri.IsHexDigit))
            throw new InvalidDataException("发布校验文件格式无效，已停止更新。");
        var download = Path.Combine(staging, "new.exe");
        using (var response = await client.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, token))
        {
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync(token);
            await using var output = new FileStream(download, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await input.ReadAsync(buffer, token)) > 0)
            {
                total += read;
                if (total > 512L * 1024 * 1024) throw new InvalidDataException("更新文件超过大小限制。");
                await output.WriteAsync(buffer.AsMemory(0, read), token);
                progress.Report($"正在下载：{total / 1048576.0:F1} MB");
            }
        }
        if (!MatchesHash(download, parts[0])) throw new InvalidDataException("更新文件 SHA-256 不匹配，未替换旧版。");
        File.Copy(target, Path.Combine(staging, "updater.exe"));
        var plan = new UpdatePlan(target, parts[0], Environment.ProcessId, Path.Combine(staging, "healthy"));
        File.WriteAllText(Path.Combine(staging, "plan.json"), JsonSerializer.Serialize(plan));
        progress.Report("下载校验通过，等待确认重启。");
        return staging;
    }

    public static void StartApply(string staging)
    {
        var start = new ProcessStartInfo(Path.Combine(staging, "updater.exe")) { UseShellExecute = false };
        start.ArgumentList.Add("--apply-update");
        start.ArgumentList.Add(Path.Combine(staging, "plan.json"));
        _ = Process.Start(start) ?? throw new IOException("无法启动更新程序。");
    }

    public static int Apply(string planPath)
    {
        string? backup = null;
        UpdatePlan? plan = null;
        Process? child = null;
        var replaced = false;
        try
        {
            var staging = Path.GetDirectoryName(Path.GetFullPath(planPath))!;
            if (!string.Equals(Path.Combine(staging, "updater.exe"), Environment.ProcessPath, StringComparison.OrdinalIgnoreCase))
                throw new IOException("更新计划与启动程序位置不匹配。");
            plan = JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(planPath)) ?? throw new IOException("更新计划无效。");
            if (Path.GetFileName(plan.Target) != Filename ||
                !string.Equals(Directory.GetParent(staging)?.FullName, Path.GetDirectoryName(plan.Target), StringComparison.OrdinalIgnoreCase) ||
                plan.HealthFile != Path.Combine(staging, "healthy")) throw new IOException("更新目标路径无效。");
            var source = Path.Combine(staging, "new.exe");
            if (!MatchesHash(source, plan.Sha256)) throw new IOException("更新前校验失败。");
            try
            {
                using var parent = Process.GetProcessById(plan.ParentId);
                if (!parent.WaitForExit(30000)) throw new IOException("旧版仍在运行，已取消替换。");
            }
            catch (ArgumentException) { /* Parent already exited. */ }
            backup = Path.Combine(staging, "previous.exe");
            File.Replace(source, plan.Target, backup);
            replaced = true;
            var start = new ProcessStartInfo(plan.Target) { UseShellExecute = false };
            start.ArgumentList.Add("--update-health");
            start.ArgumentList.Add(plan.HealthFile);
            child = Process.Start(start) ?? throw new IOException("新版启动失败。");
            var timer = Stopwatch.StartNew();
            while (!File.Exists(plan.HealthFile) && !child.HasExited && timer.Elapsed < TimeSpan.FromSeconds(30))
                Thread.Sleep(100);
            if (!File.Exists(plan.HealthFile)) throw new IOException("新版未通过启动检查。");
            return 0;
        }
        catch (Exception ex)
        {
            var message = ex.Message;
            try
            {
                if (replaced && plan != null && backup != null)
                {
                    if (child != null && !child.HasExited) { child.Kill(); child.WaitForExit(5000); }
                    File.Move(backup, plan.Target, true);
                    Process.Start(new ProcessStartInfo(plan.Target) { UseShellExecute = true });
                    message += "\n已恢复并重新启动旧版。";
                }
                else message += "\n原程序未被替换。";
            }
            catch (Exception rollback) { message += $"\n自动恢复失败：{rollback.Message}\n旧版备份：{backup}"; }
            Diagnostics.WriteCrashLog(ex);
            MessageBox.Show(message, "更新未完成", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return 1;
        }
        finally { child?.Dispose(); }
    }
}
