using System.Security.Cryptography;

namespace EndNoteStyleToolbox;

internal static class SelfTestRunner
{
    public static int Run(string? reportPath)
    {
        var checks = new List<string>();
        var temporaryRoot = Path.Combine(Path.GetTempPath(), "EndNoteStyleToolboxSelfTest-" + Guid.NewGuid().ToString("N"));

        try
        {
            var styles = EmbeddedAssets.LoadManifest();
            Require(styles.Count == 18, "Manifest contains 18 styles.", checks);

            ApplicationConfiguration.Initialize();
            using (var form = new MainForm())
            {
                Require(form.Text == "EndNote中文学术格式工具箱", "Main window constructs successfully.", checks);
                Require(form.Handle != IntPtr.Zero, "Main window handle can be created.", checks);
            }

            var resources = EmbeddedAssets.ResourceNames();
            Require(resources.Count(name => name.EndsWith(".ens", StringComparison.OrdinalIgnoreCase)) == 18,
                "Assembly contains 18 ENS resources.", checks);

            foreach (var style in styles)
            {
                var data = EmbeddedAssets.ReadStyle(style.Filename);
                var hash = Convert.ToHexString(SHA256.HashData(data));
                Require(hash.Equals(style.Sha256, StringComparison.OrdinalIgnoreCase),
                    $"SHA-256 matches: {style.Filename}", checks);
            }

            var installDirectory = Path.Combine(temporaryRoot, "Documents", "EndNote", "Styles");
            var service = new StyleService(installDirectory);
            var sample = styles[0];

            Require(service.GetState(sample) == InstalledState.NotInstalled, "Initial state is not installed.", checks);
            service.Install(sample);
            Require(service.GetState(sample) == InstalledState.Installed, "Install writes the embedded ENS.", checks);

            File.WriteAllText(Path.Combine(installDirectory, sample.Filename), "different version");
            Require(service.GetState(sample) == InstalledState.DifferentVersion, "Modified file is detected.", checks);

            var reinstall = service.Install(sample);
            Require(reinstall.BackupDirectory is not null && Directory.EnumerateFiles(reinstall.BackupDirectory).Any(),
                "Reinstall backs up the existing file.", checks);
            Require(service.GetState(sample) == InstalledState.Installed, "Reinstall restores the bundled version.", checks);

            var uninstall = service.Uninstall(sample);
            Require(uninstall.BackupDirectory is not null && Directory.EnumerateFiles(uninstall.BackupDirectory).Any(),
                "Uninstall moves the file into a recoverable backup.", checks);
            Require(service.GetState(sample) == InstalledState.NotInstalled, "Uninstall removes the active copy.", checks);

            var supportDirectory = EmbeddedAssets.ExportSupportFiles(Path.Combine(temporaryRoot, "Support"));
            Require(Directory.EnumerateFiles(supportDirectory).Count() == 4, "Support files export successfully.", checks);

            WriteReport(reportPath, new
            {
                status = "passed",
                version = "0.3.0",
                os = Environment.OSVersion.ToString(),
                runtime = Environment.Version.ToString(),
                architecture = Environment.Is64BitProcess ? "x64" : "x86",
                checks
            });
            return 0;
        }
        catch (Exception exception)
        {
            WriteReport(reportPath, new
            {
                status = "failed",
                version = "0.3.0",
                error = exception.ToString(),
                checks
            });
            return 1;
        }
        finally
        {
            try
            {
                if (Directory.Exists(temporaryRoot))
                {
                    Directory.Delete(temporaryRoot, recursive: true);
                }
            }
            catch
            {
                // A self-test cleanup failure must not alter the verified result.
            }
        }
    }

    private static void Require(bool condition, string message, ICollection<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Self-test failed: " + message);
        }
        checks.Add(message);
    }

    private static void WriteReport(string? path, object report)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            Diagnostics.WriteSelfTestReport(path, report);
        }
    }
}
