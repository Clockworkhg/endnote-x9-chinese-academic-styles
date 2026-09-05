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
            Require(styles.Count == 47, "Manifest contains all 46 upstream note styles plus the CUC profile.", checks);

            ApplicationConfiguration.Initialize();
            using (var form = new MainForm())
            {
                Require(form.Text == "EndNote中文学术格式工具箱", "Main window constructs successfully.", checks);
                Require(form.Handle != IntPtr.Zero, "Main window handle can be created.", checks);
                form.Show();
                Application.DoEvents();
                Require(form.Visible, "Main window is shown and processes UI events.", checks);
                Require(Descendants(form).OfType<Button>().Count(b => b.Text == "使用帮助") == 1 &&
                    !Descendants(form).OfType<Button>().Any(b => b.Text == "测试与帮助"), "Main window has exactly one help entry.", checks);
                form.Close();
            }
            using (var dialog = new DirectoryDialog(new AppSettings()))
            {
                dialog.Show(); Application.DoEvents();
                Require(dialog.Visible, "Directory dialog renders.", checks);
                var picker = Descendants(dialog).OfType<ComboBox>().Single();
                var candidatePath = Path.Combine(temporaryRoot, "Styles");
                picker.Items.Add(new DirectoryCandidate(candidatePath, "来源说明不能进入路径"));
                picker.SelectedIndex = 0;
                Application.DoEvents();
                Require(picker.Text == candidatePath, "Directory picker displays a clean path without source annotation.", checks);
                var pathEditor = Descendants(dialog).OfType<TextBox>().Single();
                Require(pathEditor.Text == candidatePath, "Selecting a candidate fills the separate path editor.", checks);
                pathEditor.Text = candidatePath + "-manual";
                Require(pathEditor.Text == candidatePath + "-manual", "Directory candidate remains manually editable.", checks);
                dialog.Close();
            }
            using (var dialog = new UpdateDialog())
            {
                dialog.Show(); Application.DoEvents();
                Require(dialog.Visible, "Update dialog renders without network access.", checks);
                dialog.Close();
            }

            var resources = EmbeddedAssets.ResourceNames();
            Require(resources.Count(name => name.EndsWith(".ens", StringComparison.OrdinalIgnoreCase)) == styles.Count,
                $"Assembly contains {styles.Count} ENS resources.", checks);

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
            var settingsFile = Path.Combine(temporaryRoot, "config", "settings.json");
            new AppSettings { StyleDirectory = installDirectory, DirectoryConfirmed = true }.Save(settingsFile);
            Require(AppSettings.Load(settingsFile).StyleDirectory == installDirectory && AppSettings.Load(settingsFile).DirectoryConfirmed,
                "Custom directory and confirmation survive settings reload.", checks);
            File.WriteAllText(settingsFile, "broken json");
            Require(AppSettings.Load(settingsFile).StyleDirectory == null && File.ReadAllText(settingsFile) == "broken json",
                "Corrupt settings safely fall back without destroying the diagnostic file.", checks);
            Require(StyleDirectoryService.Discover().Count > 0, "Directory detection includes the Known Folder default.", checks);
            var rejected = false;
            try { StyleDirectoryService.Validate(Path.GetPathRoot(temporaryRoot)!); } catch (IOException) { rejected = true; }
            Require(rejected, "Disk root cannot be an install destination.", checks);

            Require(service.GetState(sample) == InstalledState.NotInstalled, "Initial state is not installed.", checks);
            service.Install(sample);
            Require(service.GetState(sample) == InstalledState.Installed, "Install writes the embedded ENS.", checks);
            Require(UpdateService.MatchesHash(Path.Combine(installDirectory, sample.Filename), sample.Sha256), "Update hash verifier accepts matching file.", checks);
            Require(!UpdateService.MatchesHash(Path.Combine(installDirectory, sample.Filename), new string('0', 64)), "Update hash verifier rejects corrupted file.", checks);
            Require(!UpdateService.MatchesHash(Path.Combine(installDirectory, sample.Filename), "invalid"), "Update hash verifier rejects invalid digest.", checks);

            File.WriteAllText(Path.Combine(installDirectory, sample.Filename), "different version");
            Require(service.GetState(sample) == InstalledState.DifferentVersion, "Modified file is detected.", checks);
            service.Uninstall(sample);
            Require(File.ReadAllText(Path.Combine(installDirectory, sample.Filename)) == "different version", "Uninstall preserves a user-modified style.", checks);

            var reinstall = service.Install(sample);
            Require(reinstall.BackupDirectory is not null && Directory.EnumerateFiles(reinstall.BackupDirectory).Any(),
                "Reinstall backs up the existing file.", checks);
            Require(service.GetState(sample) == InstalledState.Installed, "Reinstall restores the bundled version.", checks);

            var uninstall = service.Uninstall(sample);
            Require(uninstall.BackupDirectory is not null && Directory.EnumerateFiles(uninstall.BackupDirectory).Any(),
                "Uninstall moves the file into a recoverable backup.", checks);
            Require(service.GetState(sample) == InstalledState.NotInstalled, "Uninstall removes the active copy.", checks);
            var unmanagedDirectory = Path.Combine(temporaryRoot, "Unmanaged", "Styles");
            Directory.CreateDirectory(unmanagedDirectory);
            File.WriteAllBytes(Path.Combine(unmanagedDirectory, sample.Filename), EmbeddedAssets.ReadStyle(sample.Filename));
            new StyleService(unmanagedDirectory).UninstallAll(styles);
            Require(File.Exists(Path.Combine(unmanagedDirectory, sample.Filename)), "Uninstall leaves untracked same-name files intact.", checks);
            service.InstallAll(styles);
            Require(styles.All(s => service.GetState(s) == InstalledState.Installed), "All styles install in a custom directory.", checks);
            service.UninstallAll(styles);
            Require(styles.All(s => service.GetState(s) == InstalledState.NotInstalled), "All managed styles uninstall with backups.", checks);

            var supportDirectory = EmbeddedAssets.ExportSupportFiles(Path.Combine(temporaryRoot, "Support"));
            Require(Directory.EnumerateFiles(supportDirectory).Count() == 4, "Support files export successfully.", checks);
            UpdateSelfTestRunner.Run(temporaryRoot, checks);
            Task.Run(() => UpdateSelfTestRunner.NetworkAsync(checks)).GetAwaiter().GetResult();

            WriteReport(reportPath, new
            {
                status = "passed",
                version = "0.5.0",
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
                version = "0.5.0",
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

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void WriteReport(string? path, object report)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            Diagnostics.WriteSelfTestReport(path, report);
        }
    }
}
