using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace EndNoteStyleToolbox;

internal static class UpdateSelfTestRunner
{
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
            if (scenario == "launch-failure") File.WriteAllText(source, "not a Windows executable");
            else File.Copy(executable, source);
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
            using var fileLock = scenario == "locked-target" ? new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
            using var process = Process.Start(start) ?? throw new IOException("Update test helper failed to start.");
            if (!process.WaitForExit(55000))
            {
                process.Kill(entireProcessTree: true);
                throw new IOException("Update test timed out: " + scenario);
            }
            if (scenario == "success")
            {
                if (process.ExitCode != 0 || !File.Exists(marker) || !File.Exists(Path.Combine(staging, "previous.exe")))
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
