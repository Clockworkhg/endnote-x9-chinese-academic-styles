using System.Diagnostics;

namespace EndNoteStyleToolbox;

internal sealed class MainForm : Form
{
    private sealed record CategoryFilter(string Key, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly IReadOnlyList<StyleInfo> _styles;
    private StyleService _service;
    private AppSettings _settings;
    private bool _installedOnly;
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _categoryBox = new();
    private readonly DataGridView _grid = new();
    private readonly SplitContainer _contentSplit = new();
    private readonly Label _detailTitle = new();
    private readonly Label _detailMeta = new();
    private readonly TextBox _examplesBox = new();
    private readonly Label _pathLabel = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly Button _installButton = new();
    private readonly Button _uninstallButton = new();
    private readonly Button _sourceButton = new();

    public MainForm()
    {
        _styles = EmbeddedAssets.LoadManifest();
        _settings = AppSettings.Load();
        try { _service = new StyleService(_settings.StyleDirectory); }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            _settings = new AppSettings();
            _service = new StyleService();
        }

        Text = "EndNote中文学术格式工具箱";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1140, 680);
        ClientSize = new Size(1280, 780);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = Color.FromArgb(246, 248, 251);

        BuildInterface();
        Load += (_, _) =>
        {
            ConfigureInitialSplit();
            PopulateCategories();
            RunSafely(() => RefreshGrid());
        };
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18, 16, 18, 10),
            BackColor = BackColor
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var navigation = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(10, 24, 10, 10), BackColor = Color.FromArgb(230, 237, 247) };
        void Navigate(string label, Action action)
        {
            var button = new Button { Text = label, Size = new Size(125, 44), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 0, 12) };
            button.Click += (_, _) => RunSafely(action);
            navigation.Controls.Add(button);
        }
        Navigate("格式库", () => { _installedOnly = false; RefreshGrid(); });
        Navigate("已安装", () => { _installedOnly = true; RefreshGrid(); });
        Navigate("安装位置", ConfigureDirectory);
        Navigate("备份与恢复", OpenBackups);
        Navigate("更新中心", () => { using var dialog = new UpdateDialog(); dialog.ShowDialog(this); });
        Navigate("使用帮助", ShowHelp);
        shell.Controls.Add(navigation, 0, 0);
        shell.Controls.Add(root, 1, 0);
        Controls.Add(shell);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildFilters(), 0, 1);
        root.Controls.Add(BuildContent(), 0, 2);
        root.Controls.Add(BuildActions(), 0, 3);

        var statusStrip = new StatusStrip
        {
            SizingGrip = false,
            BackColor = Color.White,
            Padding = new Padding(12, 0, 12, 0)
        };
        _statusLabel.Text = "就绪";
        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        statusStrip.Items.Add(new ToolStripStatusLabel("v0.5.0 · 64位 · 个人目录安装"));
        Controls.Add(statusStrip);
    }

    private Control BuildHeader()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        var title = new Label
        {
            AutoSize = true,
            Text = "EndNote中文学术格式工具箱",
            Font = new Font(Font.FontFamily, 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 39, 68),
            Location = new Point(0, 2)
        };
        var subtitle = new Label
        {
            AutoSize = true,
            Text = $"{_styles.Count}套中文学术脚注格式 · 一键安装、检测与可恢复卸载",
            ForeColor = Color.FromArgb(82, 96, 119),
            Location = new Point(2, 46)
        };
        panel.Controls.Add(title);
        panel.Controls.Add(subtitle);
        return panel;
    }

    private Control BuildFilters()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Padding = new Padding(0, 6, 0, 7)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));

        panel.Controls.Add(NewFilterLabel("搜索"), 0, 0);
        _searchBox.Dock = DockStyle.Fill;
        _searchBox.PlaceholderText = "输入期刊、出版社或学科名称";
        _searchBox.TextChanged += (_, _) => RunSafely(() => RefreshGrid());
        panel.Controls.Add(_searchBox, 1, 0);

        panel.Controls.Add(NewFilterLabel("分类"), 2, 0);
        _categoryBox.Dock = DockStyle.Fill;
        _categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoryBox.SelectedIndexChanged += (_, _) => RunSafely(() => RefreshGrid());
        panel.Controls.Add(_categoryBox, 3, 0);

        var refreshButton = NewButton("刷新状态", secondary: true);
        refreshButton.Dock = DockStyle.Fill;
        refreshButton.Margin = new Padding(8, 0, 0, 0);
        refreshButton.Click += (_, _) => RunSafely(() => RefreshGrid(keepSelection: true));
        panel.Controls.Add(refreshButton, 4, 0);
        return panel;
    }

    private static Label NewFilterLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(55, 67, 86)
    };

    private Control BuildContent()
    {
        _contentSplit.Dock = DockStyle.Fill;
        _contentSplit.Orientation = Orientation.Vertical;
        _contentSplit.SplitterWidth = 8;
        _contentSplit.BackColor = BackColor;
        _contentSplit.Panel1.Padding = new Padding(0, 0, 4, 0);
        _contentSplit.Panel2.Padding = new Padding(4, 0, 0, 0);

        ConfigureGrid();
        _contentSplit.Panel1.Controls.Add(_grid);
        _contentSplit.Panel2.Controls.Add(BuildDetails());
        return _contentSplit;
    }

    private void ConfigureInitialSplit()
    {
        const int leftMinimum = 520;
        const int rightMinimum = 300;
        var available = _contentSplit.ClientSize.Width - _contentSplit.SplitterWidth;
        if (available < leftMinimum + rightMinimum)
        {
            return;
        }

        var desired = Math.Clamp((int)(available * 0.62), leftMinimum, available - rightMinimum);
        _contentSplit.SplitterDistance = desired;
        _contentSplit.Panel1MinSize = leftMinimum;
        _contentSplit.Panel2MinSize = rightMinimum;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.MultiSelect = false;
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.RowTemplate.Height = 34;
        _grid.ColumnHeadersHeight = 38;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 247);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(32, 48, 75);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(218, 232, 251);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 36, 62);
        _grid.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 254);
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Number",
            HeaderText = "序号",
            Width = 58,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Title",
            HeaderText = "格式名称",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 250,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Category",
            HeaderText = "分类",
            Width = 112,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "State",
            HeaderText = "安装状态",
            Width = 104,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        _grid.SelectionChanged += (_, _) => UpdateDetails();
    }

    private Control BuildDetails()
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 7
        };
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        _detailTitle.AutoSize = true;
        _detailTitle.Font = new Font(Font.FontFamily, 13F, FontStyle.Bold);
        _detailTitle.ForeColor = Color.FromArgb(25, 42, 70);
        _detailTitle.MaximumSize = new Size(380, 0);
        card.Controls.Add(_detailTitle, 0, 0);

        _detailMeta.AutoSize = true;
        _detailMeta.ForeColor = Color.FromArgb(88, 100, 120);
        _detailMeta.Margin = new Padding(0, 8, 0, 0);
        card.Controls.Add(_detailMeta, 0, 1);

        var examplesLabel = new Label
        {
            AutoSize = true,
            Text = "格式示例",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 58, 79)
        };
        card.Controls.Add(examplesLabel, 0, 3);

        _examplesBox.Dock = DockStyle.Fill;
        _examplesBox.Multiline = true;
        _examplesBox.ReadOnly = true;
        _examplesBox.ScrollBars = ScrollBars.Vertical;
        _examplesBox.BackColor = Color.FromArgb(250, 251, 253);
        _examplesBox.BorderStyle = BorderStyle.FixedSingle;
        _examplesBox.Margin = new Padding(0, 8, 0, 12);
        card.Controls.Add(_examplesBox, 0, 4);

        _pathLabel.AutoSize = true;
        _pathLabel.MaximumSize = new Size(390, 0);
        _pathLabel.ForeColor = Color.FromArgb(92, 102, 117);
        UpdatePathLabel();
        card.Controls.Add(_pathLabel, 0, 5);

        var links = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0)
        };
        _sourceButton.Text = "查看规范来源";
        _sourceButton.AutoSize = true;
        _sourceButton.FlatStyle = FlatStyle.Flat;
        _sourceButton.FlatAppearance.BorderSize = 0;
        _sourceButton.ForeColor = Color.FromArgb(32, 94, 174);
        _sourceButton.Click += (_, _) => OpenSelectedSource();
        var folderButton = new Button
        {
            Text = "打开安装目录",
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(32, 94, 174),
            Margin = new Padding(12, 0, 0, 0)
        };
        folderButton.FlatAppearance.BorderSize = 0;
        folderButton.Click += (_, _) => RunSafely(OpenTargetDirectory);
        links.Controls.Add(_sourceButton);
        links.Controls.Add(folderButton);
        card.Controls.Add(links, 0, 6);
        return card;
    }

    private Control BuildActions()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            Padding = new Padding(0, 10, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));

        _installButton.Text = "安装所选格式";
        StyleButton(_installButton, secondary: false);
        _installButton.Click += (_, _) => InstallSelected();
        panel.Controls.Add(_installButton, 0, 0);

        _uninstallButton.Text = "卸载所选格式";
        StyleButton(_uninstallButton, secondary: true);
        _uninstallButton.Click += (_, _) => UninstallSelected();
        panel.Controls.Add(_uninstallButton, 1, 0);

        var installAllButton = NewButton("安装全部", secondary: true);
        installAllButton.Click += (_, _) => InstallAll();
        panel.Controls.Add(installAllButton, 2, 0);

        var uninstallAllButton = NewButton("卸载全部", secondary: true);
        uninstallAllButton.Click += (_, _) => UninstallAll();
        panel.Controls.Add(uninstallAllButton, 3, 0);

        var exportButton = NewButton("导出测试与配置", secondary: true);
        exportButton.Click += (_, _) => ExportSupportFiles();
        panel.Controls.Add(exportButton, 5, 0);

        return panel;
    }

    private Button NewButton(string text, bool secondary)
    {
        var button = new Button { Text = text };
        StyleButton(button, secondary);
        return button;
    }

    private void StyleButton(Button button, bool secondary)
    {
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 8, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.Cursor = Cursors.Hand;
        button.Font = new Font(Font, secondary ? FontStyle.Regular : FontStyle.Bold);
        button.FlatAppearance.BorderSize = 1;
        if (secondary)
        {
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(42, 67, 102);
            button.FlatAppearance.BorderColor = Color.FromArgb(184, 195, 210);
        }
        else
        {
            button.BackColor = Color.FromArgb(41, 104, 184);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(41, 104, 184);
        }
    }

    private void PopulateCategories()
    {
        _categoryBox.Items.Clear();
        _categoryBox.Items.Add(new CategoryFilter("", "全部分类"));
        foreach (var family in _styles.Select(style => style.Family).Distinct().OrderBy(CategoryName))
        {
            _categoryBox.Items.Add(new CategoryFilter(family, CategoryName(family)));
        }
        _categoryBox.SelectedIndex = 0;
    }

    private void RefreshGrid(bool keepSelection = false)
    {
        if (!IsHandleCreated && _categoryBox.Items.Count == 0)
        {
            return;
        }

        var selectedNumber = keepSelection ? SelectedStyle()?.Number : null;
        var search = _searchBox.Text.Trim();
        var family = (_categoryBox.SelectedItem as CategoryFilter)?.Key ?? string.Empty;
        var filtered = _styles.Where(style =>
            (family.Length == 0 || style.Family.Equals(family, StringComparison.OrdinalIgnoreCase)) &&
            (search.Length == 0 ||
             style.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
             CategoryName(style.Family).Contains(search, StringComparison.CurrentCultureIgnoreCase)));

        _grid.Rows.Clear();
        foreach (var style in filtered)
        {
            var state = _service.GetState(style);
            if (_installedOnly && state == InstalledState.NotInstalled) continue;
            var index = _grid.Rows.Add(
                style.Number.ToString("00"),
                style.Title,
                CategoryName(style.Family),
                StateText(state));
            var row = _grid.Rows[index];
            row.Tag = style;
            row.Cells[3].Style.ForeColor = state switch
            {
                InstalledState.Installed => Color.FromArgb(23, 124, 69),
                InstalledState.DifferentVersion => Color.FromArgb(181, 100, 10),
                _ => Color.FromArgb(106, 116, 131)
            };
            if (style.Number == selectedNumber)
            {
                row.Selected = true;
                _grid.CurrentCell = row.Cells[1];
            }
        }

        if (_grid.Rows.Count > 0 && _grid.SelectedRows.Count == 0)
        {
            _grid.Rows[0].Selected = true;
            _grid.CurrentCell = _grid.Rows[0].Cells[1];
        }
        UpdateDetails();
        _statusLabel.Text = $"显示 {_grid.Rows.Count} / {_styles.Count} 套格式";
    }

    private void UpdateDetails()
    {
        var style = SelectedStyle();
        var enabled = style is not null;
        _installButton.Enabled = enabled;
        _uninstallButton.Enabled = enabled;
        _sourceButton.Enabled = enabled && !string.IsNullOrWhiteSpace(style!.SourceUrl);
        if (style is null)
        {
            _detailTitle.Text = "未选择格式";
            _detailMeta.Text = string.Empty;
            _examplesBox.Text = string.Empty;
            return;
        }

        var state = _service.GetState(style);
        _detailTitle.Text = style.Title;
        _detailMeta.Text = $"{CategoryName(style.Family)} · {StateText(state)} · {style.Status}";
        _examplesBox.Text = string.Join("\r\n\r\n", style.Examples.Select((example, index) => $"{index + 1}. {example}"));
    }

    private StyleInfo? SelectedStyle() =>
        _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].Tag as StyleInfo : null;

    private void InstallSelected()
    {
        var style = SelectedStyle();
        if (style is null) return;
        if (!EnsureDirectory()) return;
        RunSafely(() => CompleteOperation(_service.Install(style)));
    }

    private void UninstallSelected()
    {
        var style = SelectedStyle();
        if (style is null) return;
        if (!Confirm($"确定卸载“{style.Title}”吗？\r\n\r\n文件不会被删除，而会移入安装目录内的恢复备份文件夹。"))
        {
            return;
        }
        RunSafely(() => CompleteOperation(_service.Uninstall(style)));
    }

    private void InstallAll()
    {
        if (!EnsureDirectory()) return;
        if (!Confirm($"确定安装全部{_styles.Count}套中文学术格式吗？\r\n\r\n同名文件将先自动备份。"))
        {
            return;
        }
        RunSafely(() => CompleteOperation(_service.InstallAll(_styles)));
    }

    private void UninstallAll()
    {
        if (!Confirm("确定卸载本工具箱管理的全部格式吗？\r\n\r\n现有文件将统一移入可恢复备份文件夹。"))
        {
            return;
        }
        RunSafely(() => CompleteOperation(_service.UninstallAll(_styles)));
    }

    private void CompleteOperation(OperationResult result)
    {
        RefreshGrid(keepSelection: true);
        _statusLabel.Text = result.Message;
        var backup = result.BackupDirectory is null ? string.Empty : $"\r\n\r\n备份位置：\r\n{result.BackupDirectory}";
        MessageBox.Show(
            result.Message + backup + "\r\n\r\n请重新打开EndNote或刷新样式列表后使用。",
            "操作完成",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OpenTargetDirectory()
    {
        Directory.CreateDirectory(_service.TargetDirectory);
        OpenPath(_service.TargetDirectory);
    }

    private void OpenSelectedSource()
    {
        var style = SelectedStyle();
        if (style is null || !Uri.TryCreate(style.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }
        RunSafely(() => OpenPath(uri.AbsoluteUri));
    }

    private void ExportSupportFiles()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择测试资料和配置文件的导出位置",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        RunSafely(() =>
        {
            var folder = Path.Combine(dialog.SelectedPath, "EndNote中文格式工具箱-测试资料");
            EmbeddedAssets.ExportSupportFiles(folder);
            _statusLabel.Text = "测试与配置文件已导出";
            if (MessageBox.Show($"已导出到：\r\n{folder}\r\n\r\n是否立即打开？", "导出完成",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                OpenPath(folder);
            }
        });
    }

    private void ShowHelp()
    {
        const string help = "安装并启用格式\r\n\r\n" +
            "1. 选择格式，点击“安装所选格式”。\r\n" +
            "2. 重新打开 EndNote X9，进入 Edit → Output Styles → Open Style Manager，找到安装的样式并勾选。\r\n" +
            "3. 在 Word/WPS 的 EndNote 选项卡选择相应 Style；未列出时使用 Select Another Style。\r\n" +
            "4. 用 Word/WPS 插入脚注，再在脚注内插入文献。本次页码在 Edit & Manage Citation(s) → Pages 填写。\r\n\r\n" +
            "中、英、日文如何切换\r\n\r\n" +
            "当前按文献的 Reference Type 选择模板；仅填写 Language 或选中文献不会切换。文档保持同一个 Style。\r\n" +
            "首次使用：点击“导出测试与配置”。在 EndNote 的 Edit → Preferences → Reference Types 中先 Export 备份现有类型定义，再 Import 导出的“统一多语种文献类型.xml”。这不是 File → Import 的文献导入。已有自定义类型时，请先检查是否冲突。\r\n" +
            "中文：使用 Book、Journal Article、Book Section 等普通类型，作者填 Author。中文译本按中文文献处理。\r\n" +
            "英文：打开记录，将 Reference Type 改成 English Book、English Journal Article 或 English Book Section；把 Original Author (backup) 中的作者复制到 English Author，一人一行，例如 Brooks, Peter。原作者备份保留。章节编者核对 English Editor。\r\n" +
            "日文：选择 Japanese Book、Japanese Journal Article 或 Japanese Book Section；在 Japanese Authors (formatted) 填写排好顺序及分隔符的作者文字。章节编者填 Japanese Editor (formatted)。\r\n" +
            "保存记录，回到 Word/WPS 点击 Update Citations and Bibliography。必要时在 Edit & Manage Citation(s) 中使用 Update from My Library 同步库中修改。\r\n" +
            "切换前后检查作者、编者、题名、出版项和页码，勿覆盖已有专用字段。当前没有自动翻译，也没有按 Language 批量转换功能。\r\n" +
            "多语言名称来自上游样式；当前英日文模板主要继承中国社会科学基线，不表示已完整实现上游所有语种和细则。\r\n\r\n" +
            "安装、卸载与恢复\r\n\r\n" +
            "安装位置须与 EndNote 的 Styles Folder 一致。覆盖前备份；卸载只处理有匹配记录且未修改的文件。旧版或自行修改的文件会保留。可从“备份与恢复”查看副本。\r\n\r\n" +
            "脚注序号和排版由 Word/WPS 控制。预览/实验性样式仍需实际核对。本项目与 Clarivate 及各期刊、出版社无隶属关系。";
        using var dialog = new Form
        {
            Text = "使用帮助", StartPosition = FormStartPosition.CenterParent,
            Size = new Size(780, 580), MinimumSize = new Size(540, 360),
            MinimizeBox = false, MaximizeBox = false, Font = Font
        };
        var content = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill, Text = help, BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.None, Margin = new Padding(16)
        };
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        panel.Controls.Add(content);
        dialog.Controls.Add(panel);
        dialog.ShowDialog(this);
    }

    private void RunSafely(Action action)
    {
        try
        {
            UseWaitCursor = true;
            action();
        }
        catch (Exception exception)
        {
            var logPath = Diagnostics.WriteCrashLog(exception);
            MessageBox.Show(
                $"操作未完成。\r\n\r\n{exception.Message}\r\n\r\n诊断日志：\r\n{logPath}",
                "操作失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _statusLabel.Text = "操作失败，已生成诊断日志";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void UpdatePathLabel() => _pathLabel.Text = $"安装目录（{(_settings.DirectoryConfirmed ? "用户已确认" : "待核对 EndNote 设置")}）：{_service.TargetDirectory}";

    private void ConfigureDirectory()
    {
        using var dialog = new DirectoryDialog(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var settings = new AppSettings { StyleDirectory = dialog.SelectedDirectory, DirectoryConfirmed = dialog.DirectoryConfirmed };
        settings.Save();
        _settings = settings;
        _service = new StyleService(settings.StyleDirectory);
        UpdatePathLabel();
        RefreshGrid();
    }

    private bool EnsureDirectory()
    {
        if (_settings.DirectoryConfirmed) return true;
        ConfigureDirectory();
        if (_settings.DirectoryConfirmed) return true;
        MessageBox.Show(this, "请先核对 EndNote 的 Styles Folder，并在安装位置窗口勾选确认，再安装。", "请确认样式位置");
        return false;
    }

    private void OpenBackups()
    {
        OpenTargetDirectory();
        MessageBox.Show(this, "CN-Academic-Backup-* 是覆盖前备份，CN-Academic-Removed-* 是卸载备份。\n\n恢复方法：先关闭 EndNote，将需要恢复的 ENS 复制回当前安装目录。同名文件请另存备份后再覆盖。\n工具箱不会自动恢复安装记录；恢复后的文件将按保护规则保留。", "备份与恢复");
    }

    private bool Confirm(string message) => MessageBox.Show(
        message,
        "请确认",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question,
        MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    private static void OpenPath(string path) => Process.Start(new ProcessStartInfo
    {
        FileName = path,
        UseShellExecute = true
    });

    private static string StateText(InstalledState state) => state switch
    {
        InstalledState.Installed => "已安装",
        InstalledState.DifferentVersion => "发现其他版本",
        _ => "未安装"
    };

    private static string CategoryName(string family) => family switch
    {
        "css" => "综合人文社科",
        "technical" or "university_note" or "hunan_note" => "综合与高校",
        "cuc" => "中国传媒大学",
        "publisher" => "出版社",
        "politics" => "政治学",
        "international" or "foreign_affairs" or "intl_politics" => "国际关系",
        "modern_ir" => "历史与国际关系",
        "history" => "历史学",
        "literature" => "文学",
        "marxism" => "马克思主义",
        "news" => "新闻传播",
        "law_manual" or "law_review" => "法学",
        "gbt2015" or "gbt2025" or "gbt2015_clean" or "gbt2025_clean" => "国家标准",
        "taiwan_note" => "繁体中文",
        _ => "其他"
    };
}
