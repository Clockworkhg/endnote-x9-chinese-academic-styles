namespace EndNoteStyleToolbox;

internal sealed class DirectoryDialog : Form
{
    private readonly ComboBox _paths = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDown, DisplayMember = nameof(DirectoryCandidate.Path), Height = 36 };
    private readonly Label _status = new() { Dock = DockStyle.Fill, Padding = new Padding(0, 18, 0, 0) };
    private readonly CheckBox _confirmed = new() { Text = "我已核对：这与 EndNote 的 Styles Folder 设置一致", AutoSize = true };
    public string SelectedDirectory { get; private set; } = "";
    public bool DirectoryConfirmed => _confirmed.Checked;

    public DirectoryDialog(AppSettings settings)
    {
        Text = "样式安装位置";
        Font = new Font("Microsoft YaHei UI", 10);
        ClientSize = new Size(760, 280);
        MinimumSize = new Size(740, 320);
        StartPosition = FormStartPosition.CenterParent;
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.Controls.Add(new Label { Text = "先在 EndNote 的 Edit → Preferences → Folder Locations 核对 Styles Folder。\n选择目录不会修改 EndNote 自身设置；自动检测结果需要核对。", Dock = DockStyle.Fill }, 0, 0);
        root.Controls.Add(_paths, 0, 1);
        root.Controls.Add(_status, 0, 2);
        root.Controls.Add(_confirmed, 0, 3);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
        void Button(string title, Action action)
        {
            var button = new Button { Text = title, AutoSize = true, Height = 32 };
            button.Click += (_, _) => { try { action(); } catch (Exception ex) { _status.Text = ex.Message; } };
            actions.Controls.Add(button);
        }
        Button("自动检测", Scan);
        Button("选择文件夹…", () =>
        {
            using var dialog = new FolderBrowserDialog { Description = "选择 EndNote 个人 Styles 目录", UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) _paths.Text = dialog.SelectedPath;
        });
        Button("默认位置", () => _paths.Text = StyleDirectoryService.DefaultDirectory);
        Button("检查写入", () => { var path = StyleDirectoryService.CheckWritable(CurrentPath()); _status.Text = $"可写：{path}\n现有 {Directory.EnumerateFiles(path, "*.ens").Count()} 个样式。仍需核对 EndNote 设置。"; });
        Button("保存", () =>
        {
            SelectedDirectory = StyleDirectoryService.CheckWritable(CurrentPath());
            DialogResult = DialogResult.OK;
            Close();
        });
        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);
        _paths.TextChanged += (_, _) => { _confirmed.Checked = false; _status.Text = "手动指定位置。\n尚未确认 EndNote 是否读取此位置。"; };
        _paths.SelectionChangeCommitted += (_, _) =>
        {
            _confirmed.Checked = false;
            if (_paths.SelectedItem is DirectoryCandidate candidate)
                _status.Text = $"来源：{candidate.Source}\n请与 EndNote 的 Styles Folder 核对。";
        };
        _paths.Text = settings.StyleDirectory ?? StyleDirectoryService.DefaultDirectory;
        _confirmed.Checked = settings.DirectoryConfirmed;
    }

    private string CurrentPath() => _paths.Text.Trim();
    private void Scan()
    {
        var previous = CurrentPath();
        _paths.Items.Clear();
        foreach (var candidate in StyleDirectoryService.Discover()) _paths.Items.Add(candidate);
        _paths.Text = previous;
        _status.Text = $"找到 {_paths.Items.Count} 个候选位置，请展开列表选择。原路径未自动改变。";
        _paths.DroppedDown = true;
    }
}
