using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Net;
using System.Net.Http;

namespace EndNoteStyleToolbox;

internal static class UpdateSelfTestRunner
{
    private sealed class FixtureHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(respond(request));
        }
    }

    private sealed class SilentProgress : IProgress<string> { public void Report(string value) { } }

    public static async Task NetworkAsync(ICollection<string> checks)
    {
        const string tag = "v99.0.0";
        var url = $"https://github.com/{UpdateService.Repository}/releases/download/{tag}/EndNoteStyleToolbox.exe";
        var fixture = JsonSerializer.Serialize(new { draft = false, prerelease = false, tag_name = tag, body = "Fixture release", assets = new[] {
            new { name = "EndNoteStyleToolbox.exe", browser_download_url = url },
            new { name = "EndNoteStyleToolbox.exe.sha256", browser_download_url = url + ".sha256" } } });
        using var client = new HttpClient(new FixtureHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(fixture) }));
        var release = await UpdateService.CheckAsync(CancellationToken.None, client);
        if (release?.Version != new Version(99, 0, 0)) throw new IOException("Release parsing fixture failed.");
        checks.Add("Update metadata parsing uses a deterministic HTTP fixture.");
        using var offline = new HttpClient(new FixtureHandler(_ => throw new HttpRequestException("offline fixture")));
        var rejected = false;
        try { await UpdateService.CheckAsync(CancellationToken.None, offline); } catch (HttpRequestException) { rejected = true; }
        if (!rejected) throw new IOException("Offline fixture was not reported.");
        checks.Add("Offline check reports failure without starting replacement.");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var cancelledClient = new HttpClient(new FixtureHandler(_ => throw new IOException("Cancelled request reached network.")));
        rejected = false;
        try { await UpdateService.CheckAsync(cancelled.Token, cancelledClient); } catch (OperationCanceledException) { rejected = true; }
        if (!rejected) throw new IOException("Cancellation fixture failed.");
        checks.Add("Cancelled update check performs no network request.");
        using var invalid = new HttpClient(new FixtureHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(fixture.Replace(url, "https://example.invalid/payload.exe")) }));
        rejected = false;
        try { await UpdateService.CheckAsync(CancellationToken.None, invalid); } catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new IOException("Foreign update URL was accepted.");
        checks.Add("Foreign release asset URL is rejected.");
        using var downloadCancelled = new HttpClient(new FixtureHandler(_ => throw new IOException("Cancelled download reached network.")));
        rejected = false;
        try { await UpdateService.DownloadAsync(release!, new SilentProgress(), cancelled.Token, downloadCancelled); }
        catch (OperationCanceledException) { rejected = true; }
        if (!rejected) throw new IOException("Cancelled download was not stopped.");
        checks.Add("Cancelled download stops before replacement.");
        using var badDownload = new HttpClient(new FixtureHandler(request => new(HttpStatusCode.OK) {
            Content = new StringContent(request.RequestUri!.AbsolutePath.EndsWith(".sha256") ?
                new string('0', 64) + "  EndNoteStyleToolbox.exe" : "corrupted payload") }));
        rejected = false;
        try { await UpdateService.DownloadAsync(release!, new SilentProgress(), CancellationToken.None, badDownload); }
        catch (InvalidDataException) { rejected = true; }
        if (!rejected) throw new IOException("Corrupted download was accepted.");
        checks.Add("Downloaded payload with mismatched checksum is rejected.");
        foreach (var status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.TooManyRequests })
        {
            var reset = DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds();
            using var limited = new HttpClient(new FixtureHandler(_ =>
            {
                var response = new HttpResponseMessage(status) { Content = new StringContent("API rate limit exceeded") };
                response.Headers.Add("X-RateLimit-Remaining", "0");
                response.Headers.Add("X-RateLimit-Reset", reset.ToString());
                return response;
            }));
            rejected = false;
            try { await UpdateService.CheckAsync(CancellationToken.None, limited); }
            catch (UpdateRateLimitException ex) { rejected = ex.RetryAt.ToUnixTimeSeconds() >= reset; }
            if (!rejected) throw new IOException("Rate-limit response did not preserve retry time.");
            checks.Add($"HTTP {(int)status} rate limit returns a Chinese error with server retry time.");
        }
        var forbidden = UpdateDialog.DescribeFailure(new HttpRequestException("English server error", null, HttpStatusCode.Forbidden));
        if (!forbidden.Contains("403") || forbidden.Contains("English server error")) throw new IOException("Raw HTTP error leaked to the UI.");
        checks.Add("Generic HTTP 403 is localized without being misreported as a rate limit.");
    }

    public static void Run(string root, ICollection<string> checks)
    {
        var executable = Environment.ProcessPath!;
        foreach (var scenario in new[] { "success", "launch-failure", "hash-mismatch", "locked-target" })
        {
            var directory = Path.Combine(root, "更新测试 " + scenario);
            var staging = Path.Combine(directory, ".toolbox-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            var target = Path.Combine(directory, "EndNoteStyleToolbox.exe");
            var source = Path.Combine(staging, "new.exe");
            var helper = Path.Combine(staging, "updater.exe");
            File.Copy(executable, target);
            File.Copy(executable, helper);
            File.Copy(executable, source);
            // A valid PE with distinguishable bytes proves rollback restores the old version.
            using (var append = new FileStream(source, FileMode.Append)) append.WriteByte(0);
            string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
            var originalHash = Hash(target);
            var hash = scenario == "hash-mismatch" ? new string('0', 64) : Hash(source);
            var planPath = Path.Combine(staging, "plan.json");
            var marker = Path.Combine(staging, "healthy");
            File.WriteAllText(planPath, JsonSerializer.Serialize(new UpdatePlan(target, hash, int.MaxValue, marker)));
            var start = new ProcessStartInfo(helper) { UseShellExecute = false };
            start.ArgumentList.Add("--apply-update");
            start.ArgumentList.Add(planPath);
            start.Environment["ENDNOTE_TOOLBOX_UPDATE_TEST"] = "1";
            if (scenario == "launch-failure") start.Environment["ENDNOTE_TOOLBOX_FAIL_HEALTH_TEST"] = "1";
            using var fileLock = scenario == "locked-target" ? new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
            using var process = Process.Start(start) ?? throw new IOException("Update test helper failed to start.");
            if (!process.WaitForExit(55000))
            {
                process.Kill(entireProcessTree: true);
                throw new IOException("Update test timed out: " + scenario);
            }
            if (scenario == "success")
            {
                if (process.ExitCode != 0 || !File.Exists(marker) || !File.Exists(Path.Combine(staging, "previous.exe")) ||
                    Hash(target) != hash || Hash(Path.Combine(staging, "previous.exe")) != originalHash)
                    throw new IOException("Successful update did not pass real GUI health acknowledgement.");
            }
            else if (process.ExitCode == 0 || !File.Exists(target) || Hash(target) != originalHash)
                throw new IOException("Failed update did not preserve original executable: " + scenario);
            if (scenario == "launch-failure" && !File.Exists(Path.Combine(staging, "previous.exe")))
                throw new IOException("Rollback did not retain recovery backup.");
            checks.Add("Real updater process: " + scenario);
        }
    }
}
