using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace EndNoteStyleToolbox;

internal sealed class UpdateDialog : Form
{
    private readonly TextBox _notes = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Label _status = new() { AutoSize = true, Text = "尚未检查更新。" };
    private readonly Button _check = new() { Text = "检查更新", AutoSize = true };
    private readonly Button _download = new() { Text = "下载更新", AutoSize = true, Enabled = false };
    private CancellationTokenSource _cancel = new();
    private readonly Button _cancelButton = new() { Text = "取消操作", AutoSize = true, Enabled = false };
    private readonly System.Windows.Forms.Timer _cooldown = new() { Interval = 1000 };
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
        _cancelButton.Click += (_, _) => _cancel.Cancel();
        buttons.Controls.Add(_cancelButton);
        var releases = new Button { Text = "打开发布页", AutoSize = true };
        releases.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo($"https://github.com/{UpdateService.Repository}/releases") { UseShellExecute = true }); }
            catch (Exception ex) { _status.Text = "无法打开浏览器，请稍后重试。"; Diagnostics.WriteCrashLog(ex); }
        };
        buttons.Controls.Add(releases);
        root.Controls.Add(buttons, 0, 3);
        Controls.Add(root);
        _check.Click += async (_, _) => await Run(async () =>
        {
            _release = null;
            _staging = null;
            _download.Text = "下载更新";
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
        _cooldown.Tick += (_, _) => RefreshCheckButton();
        _cooldown.Start();
        RefreshCheckButton();
        FormClosed += (_, _) => { _cooldown.Dispose(); _cancel.Dispose(); };
    }

    private async Task Run(Func<Task> action)
    {
        if (_busy) return;
        if (_cancel.IsCancellationRequested) { _cancel.Dispose(); _cancel = new CancellationTokenSource(); }
        _busy = true;
        _cancelButton.Enabled = true;
        _check.Enabled = _download.Enabled = false;
        try { await action(); }
        catch (UpdateRateLimitException ex)
        {
            _status.Text = "更新服务暂时限流，未能检查最新版本。";
            _notes.Text = $"GitHub 暂时限制了当前网络的更新请求。\r\n建议在 {ex.RetryAt.ToLocalTime():HH:mm:ss} 后重试。\r\n\r\n不需要重新授权 GitHub，也不影响安装和使用本地样式。\r\n你也可以点击“打开发布页”手动查看版本。";
        }
        catch (OperationCanceledException) { _status.Text = _cancel.IsCancellationRequested ? "操作已取消；可重新尝试，旧版未改变。" : "连接超时，请稍后重试；旧版未改变。"; }
        catch (Exception ex) { _status.Text = "操作未完成；旧版未改变。"; _notes.Text = DescribeFailure(ex) + "\r\n\r\n仍可正常使用本地格式库。详细原因已记录到诊断日志。"; Diagnostics.WriteCrashLog(ex); }
        finally
        {
            _busy = false;
            if (!IsDisposed) { _cancelButton.Enabled = false; RefreshCheckButton(); _download.Enabled = _release != null; }
        }
    }

    private void RefreshCheckButton()
    {
        var remaining = UpdateService.NextCheckAllowedAt - DateTimeOffset.UtcNow;
        _check.Enabled = !_busy && remaining <= TimeSpan.Zero;
        _check.Text = remaining > TimeSpan.Zero ? $"等待 {Math.Ceiling(remaining.TotalMinutes)} 分钟" : "检查更新";
    }

    internal static string DescribeFailure(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Forbidden } => "GitHub 拒绝了本次更新请求（403）。可稍后重试或打开发布页查看。",
        HttpRequestException { StatusCode: HttpStatusCode.NotFound } => "暂时找不到对应的发布信息或下载文件（404）。请打开发布页核对。",
        HttpRequestException => "无法连接 GitHub 更新服务，请检查网络后重试，或打开发布页查看。",
        UnauthorizedAccessException => "程序所在文件夹不可写，请将工具箱放到自己可写的文件夹后重试。",
        InvalidDataException => ex.Message,
        IOException => "无法读写更新文件，请检查文件夹权限、磁盘空间或文件是否被占用。",
        _ => "更新信息暂时无法处理，请稍后重试或打开发布页查看。"
    };
}
