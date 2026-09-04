namespace EndNoteStyleToolbox;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
        {
            var reportPath = args.Length > 1 ? args[1] : null;
            return SelfTestRunner.Run(reportPath);
        }

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
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
