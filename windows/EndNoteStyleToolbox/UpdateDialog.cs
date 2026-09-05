namespace EndNoteStyleToolbox;

internal sealed class UpdateDialog : Form
{
    private readonly TextBox _notes = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = true, Text = "尚未检查更新。" };
    private readonly Button _check = new() { Text = "检查更新", AutoSize = true };
    private readonly Button _download = new() { Text = "下载更新", AutoSize = true, Enabled = false };
    private readonly CancellationTokenSource _cancel = new();
    private UpdateRelease? _release;
    private string? _staging;
    private bool _busy;

    public UpdateDialog()
    {
        Text = "软件更新";
        Font = new Font("Microsoft YaHei UI", 10);
        ClientSize = new Size(700, 470);
        StartPosition = FormStartPosition.CenterParent;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 4, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        root.Controls.Add(new Label { Text = $"当前版本：v{UpdateService.CurrentVersion}\n只检查本项目 GitHub 正式发布；不会自动更改已安装样式或文献库。", Dock = DockStyle.Fill }, 0, 0);
        root.Controls.Add(_notes, 0, 1);
        root.Controls.Add(_status, 0, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        buttons.Controls.Add(_check);
        buttons.Controls.Add(_download);
        var cancel = new Button { Text = "取消下载", AutoSize = true };
        cancel.Click += (_, _) => _cancel.Cancel();
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);
        _check.Click += async (_, _) => await Run(async () =>
        {
            _status.Text = "正在检查 GitHub 正式发布…";
            _release = await UpdateService.CheckAsync(_cancel.Token);
            _status.Text = _release == null ? "目前没有更新的正式版。" : $"发现 v{_release.Version}";
            _notes.Text = _release?.Notes ?? "你可以继续离线使用现有格式库。";
        });
        _download.Click += async (_, _) => await Run(async () =>
        {
            if (_staging == null)
                _staging = await UpdateService.DownloadAsync(_release!, new Progress<string>(s => { if (!IsDisposed) _status.Text = s; }), _cancel.Token);
            if (MessageBox.Show(this, "下载校验已通过。现在关闭工具箱并更新吗？\n样式目录与设置会保留，旧程序保留恢复副本。", "安装更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                UpdateService.StartApply(_staging);
                _busy = false;
                Application.Exit();
            }
            else _download.Text = "安装并重启";
        });
        FormClosing += (_, e) => { if (_busy) { _cancel.Cancel(); e.Cancel = true; _status.Text = "正在取消，请稍后关闭。"; } };
        FormClosed += (_, _) => _cancel.Dispose();
    }

    private async Task Run(Func<Task> action)
    {
        if (_busy) return;
        _busy = true;
        _check.Enabled = _download.Enabled = false;
        try { await action(); }
        catch (OperationCanceledException) { _status.Text = "操作已取消；旧版未改变。关闭并重新打开更新窗口可重试。"; }
        catch (Exception ex) { _status.Text = "操作未完成；旧版未改变。"; _notes.Text = ex.Message + "\r\n\r\n网络不可用时仍可正常使用本地格式库。"; Diagnostics.WriteCrashLog(ex); }
        finally
        {
            _busy = false;
            if (!IsDisposed) { _check.Enabled = !_cancel.IsCancellationRequested; _download.Enabled = _release != null && !_cancel.IsCancellationRequested; }
        }
    }
}
