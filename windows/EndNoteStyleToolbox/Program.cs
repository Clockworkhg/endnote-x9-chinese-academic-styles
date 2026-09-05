namespace EndNoteStyleToolbox;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--apply-update") return UpdateService.Apply(args[1]);
        if (args.Length > 0 && args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
        {
            var reportPath = args.Length > 1 ? args[1] : null;
            return SelfTestRunner.Run(reportPath);
        }

        try
        {
            ApplicationConfiguration.Initialize();
            using var form = new MainForm();
            if (args.Length == 2 && args[0] == "--update-health")
            {
                var marker = Path.GetFullPath(args[1]);
                var parent = Directory.GetParent(marker);
                if (Path.GetFileName(marker) != "healthy" || parent == null ||
                    !parent.Name.StartsWith(".toolbox-update-", StringComparison.Ordinal) ||
                    !string.Equals(parent.Parent?.FullName, Path.GetDirectoryName(Environment.ProcessPath), StringComparison.OrdinalIgnoreCase))
                    throw new IOException("启动检查路径无效。");
                form.Shown += (_, _) => form.BeginInvoke(new Action(() =>
                {
                    File.WriteAllText(marker, "ready");
                    if (Environment.GetEnvironmentVariable("ENDNOTE_TOOLBOX_UPDATE_TEST") == "1") form.Close();
                }));
            }
            Application.Run(form);
            return 0;
        }
        catch (Exception exception)
        {
            var logPath = Diagnostics.WriteCrashLog(exception);
            MessageBox.Show(
                $"工具箱未能启动。\r\n\r\n{exception.Message}\r\n\r\n诊断日志：\r\n{logPath}",
                "EndNote中文学术格式工具箱",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return 1;
        }
    }
}
