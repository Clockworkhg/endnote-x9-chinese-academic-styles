$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

trap {
    [System.Windows.Forms.MessageBox]::Show(
        $_.Exception.Message,
        "工具箱启动失败",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
    break
}

$script:Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:StylesDirectory = Join-Path $script:Root "Styles"
$script:ManifestPath = Join-Path $script:Root "style-manifest.json"
$script:Documents = [Environment]::GetFolderPath("MyDocuments")
$script:TargetDirectory = Join-Path $script:Documents "EndNote\Styles"
$script:Manifest = @(Get-Content -LiteralPath $script:ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json)
$script:CurrentStyle = $null

function Get-Category([string]$Family) {
    switch ($Family) {
        "css" { "综合人文社科" }
        "technical" { "综合与高校" }
        "publisher" { "出版社" }
        "politics" { "政治学" }
        "international" { "国际关系" }
        "foreign_affairs" { "国际关系" }
        "intl_politics" { "国际关系" }
        "modern_ir" { "历史与国际关系" }
        "history" { "历史学" }
        "literature" { "文学" }
        "news" { "新闻传播" }
        "law_manual" { "法学" }
        "law_review" { "法学" }
        "gbt2015" { "国家标准" }
        "gbt2025" { "国家标准" }
        default { "其他" }
    }
}

function Get-InstalledState($Style) {
    $source = Join-Path $script:StylesDirectory $Style.filename
    $target = Join-Path $script:TargetDirectory $Style.filename
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) { return "未安装" }
    try {
        $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
        $targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        if ($sourceHash -eq $targetHash) { return "已安装" }
        return "已安装（其他版本）"
    }
    catch { return "已安装（待检测）" }
}

function New-BackupDirectory([string]$Prefix) {
    New-Item -ItemType Directory -Path $script:TargetDirectory -Force | Out-Null
    $path = Join-Path $script:TargetDirectory ($Prefix + (Get-Date -Format "yyyyMMdd-HHmmss"))
    New-Item -ItemType Directory -Path $path -Force | Out-Null
    return $path
}

function Install-Style($Style, [string]$BackupDirectory) {
    $source = Join-Path $script:StylesDirectory $Style.filename
    $target = Join-Path $script:TargetDirectory $Style.filename
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "找不到内置样式：$($Style.filename)"
    }
    New-Item -ItemType Directory -Path $script:TargetDirectory -Force | Out-Null
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        Copy-Item -LiteralPath $target -Destination (Join-Path $BackupDirectory $Style.filename) -Force
    }
    Copy-Item -LiteralPath $source -Destination $target -Force
}

function Remove-Style($Style, [string]$BackupDirectory) {
    $target = Join-Path $script:TargetDirectory $Style.filename
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        Move-Item -LiteralPath $target -Destination (Join-Path $BackupDirectory $Style.filename) -Force
        return $true
    }
    return $false
}

function Show-Error([string]$Message) {
    [System.Windows.Forms.MessageBox]::Show(
        $Message,
        "操作未完成",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "EndNote X9 中文学术格式工具箱"
$form.StartPosition = "CenterScreen"
$form.Size = New-Object System.Drawing.Size(1120, 760)
$form.MinimumSize = New-Object System.Drawing.Size(980, 680)
$form.BackColor = [System.Drawing.Color]::FromArgb(245, 247, 250)
$form.Font = New-Object System.Drawing.Font("Microsoft YaHei UI", 9)

$header = New-Object System.Windows.Forms.Panel
$header.Dock = "Top"
$header.Height = 92
$header.BackColor = [System.Drawing.Color]::FromArgb(33, 52, 85)
$form.Controls.Add($header)

$title = New-Object System.Windows.Forms.Label
$title.Text = "EndNote X9 中文学术格式工具箱"
$title.ForeColor = [System.Drawing.Color]::White
$title.Font = New-Object System.Drawing.Font("Microsoft YaHei UI", 18, [System.Drawing.FontStyle]::Bold)
$title.AutoSize = $true
$title.Location = New-Object System.Drawing.Point(24, 14)
$header.Controls.Add($title)

$subtitle = New-Object System.Windows.Forms.Label
$subtitle.Text = "18套中文脚注格式 · 中国社会科学2.7稳定基线 · 安装前自动备份"
$subtitle.ForeColor = [System.Drawing.Color]::FromArgb(210, 220, 234)
$subtitle.AutoSize = $true
$subtitle.Location = New-Object System.Drawing.Point(27, 57)
$header.Controls.Add($subtitle)

$toolbar = New-Object System.Windows.Forms.Panel
$toolbar.Dock = "Top"
$toolbar.Height = 62
$toolbar.Padding = New-Object System.Windows.Forms.Padding(18, 13, 18, 9)
$toolbar.BackColor = [System.Drawing.Color]::White
$form.Controls.Add($toolbar)

$searchLabel = New-Object System.Windows.Forms.Label
$searchLabel.Text = "搜索"
$searchLabel.AutoSize = $true
$searchLabel.Location = New-Object System.Drawing.Point(20, 20)
$toolbar.Controls.Add($searchLabel)

$searchBox = New-Object System.Windows.Forms.TextBox
$searchBox.Location = New-Object System.Drawing.Point(65, 16)
$searchBox.Size = New-Object System.Drawing.Size(260, 27)
$toolbar.Controls.Add($searchBox)

$categoryLabel = New-Object System.Windows.Forms.Label
$categoryLabel.Text = "类别"
$categoryLabel.AutoSize = $true
$categoryLabel.Location = New-Object System.Drawing.Point(350, 20)
$toolbar.Controls.Add($categoryLabel)

$categoryBox = New-Object System.Windows.Forms.ComboBox
$categoryBox.DropDownStyle = "DropDownList"
$categoryBox.Location = New-Object System.Drawing.Point(395, 16)
$categoryBox.Size = New-Object System.Drawing.Size(170, 28)
[void]$categoryBox.Items.Add("全部")
$categories = @($script:Manifest | ForEach-Object { Get-Category $_.family } | Sort-Object -Unique)
foreach ($category in $categories) { [void]$categoryBox.Items.Add($category) }
$categoryBox.SelectedIndex = 0
$toolbar.Controls.Add($categoryBox)

$refreshButton = New-Object System.Windows.Forms.Button
$refreshButton.Text = "刷新状态"
$refreshButton.Location = New-Object System.Drawing.Point(585, 14)
$refreshButton.Size = New-Object System.Drawing.Size(100, 31)
$toolbar.Controls.Add($refreshButton)

$openFolderButton = New-Object System.Windows.Forms.Button
$openFolderButton.Text = "打开安装目录"
$openFolderButton.Location = New-Object System.Drawing.Point(695, 14)
$openFolderButton.Size = New-Object System.Drawing.Size(125, 31)
$toolbar.Controls.Add($openFolderButton)

$content = New-Object System.Windows.Forms.SplitContainer
$content.Dock = "Fill"
$content.SplitterDistance = 525
$content.Panel1.Padding = New-Object System.Windows.Forms.Padding(18, 14, 8, 12)
$content.Panel2.Padding = New-Object System.Windows.Forms.Padding(10, 14, 18, 12)
$content.BackColor = [System.Drawing.Color]::FromArgb(245, 247, 250)
$form.Controls.Add($content)

$grid = New-Object System.Windows.Forms.DataGridView
$grid.Dock = "Fill"
$grid.AllowUserToAddRows = $false
$grid.AllowUserToDeleteRows = $false
$grid.AllowUserToResizeRows = $false
$grid.MultiSelect = $false
$grid.ReadOnly = $true
$grid.RowHeadersVisible = $false
$grid.SelectionMode = "FullRowSelect"
$grid.AutoGenerateColumns = $false
$grid.BackgroundColor = [System.Drawing.Color]::White
$grid.BorderStyle = "FixedSingle"
$grid.AutoSizeRowsMode = "AllCells"
$grid.ColumnHeadersHeight = 34
$grid.RowTemplate.Height = 34
$grid.EnableHeadersVisualStyles = $false
$grid.ColumnHeadersDefaultCellStyle.BackColor = [System.Drawing.Color]::FromArgb(232, 237, 245)
$grid.ColumnHeadersDefaultCellStyle.ForeColor = [System.Drawing.Color]::FromArgb(35, 48, 68)
$content.Panel1.Controls.Add($grid)

$columns = @(
    @{ Name = "No"; Header = "序号"; Width = 48 },
    @{ Name = "StyleName"; Header = "格式名称"; Width = 230 },
    @{ Name = "Category"; Header = "类别"; Width = 105 },
    @{ Name = "State"; Header = "安装状态"; Width = 120 }
)
foreach ($columnData in $columns) {
    $column = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
    $column.Name = $columnData.Name
    $column.HeaderText = $columnData.Header
    $column.Width = $columnData.Width
    if ($columnData.Name -eq "StyleName") { $column.AutoSizeMode = "Fill" }
    [void]$grid.Columns.Add($column)
}

$details = New-Object System.Windows.Forms.Panel
$details.Dock = "Fill"
$details.BackColor = [System.Drawing.Color]::White
$details.Padding = New-Object System.Windows.Forms.Padding(22)
$content.Panel2.Controls.Add($details)

$detailTitle = New-Object System.Windows.Forms.Label
$detailTitle.Text = "请选择一种格式"
$detailTitle.Font = New-Object System.Drawing.Font("Microsoft YaHei UI", 15, [System.Drawing.FontStyle]::Bold)
$detailTitle.ForeColor = [System.Drawing.Color]::FromArgb(31, 45, 65)
$detailTitle.AutoEllipsis = $true
$detailTitle.Location = New-Object System.Drawing.Point(22, 20)
$detailTitle.Size = New-Object System.Drawing.Size(500, 36)
$details.Controls.Add($detailTitle)

$detailMeta = New-Object System.Windows.Forms.Label
$detailMeta.Text = ""
$detailMeta.ForeColor = [System.Drawing.Color]::FromArgb(92, 105, 124)
$detailMeta.Location = New-Object System.Drawing.Point(24, 64)
$detailMeta.Size = New-Object System.Drawing.Size(500, 48)
$details.Controls.Add($detailMeta)

$exampleLabel = New-Object System.Windows.Forms.Label
$exampleLabel.Text = "输出示例"
$exampleLabel.Font = New-Object System.Drawing.Font("Microsoft YaHei UI", 10, [System.Drawing.FontStyle]::Bold)
$exampleLabel.Location = New-Object System.Drawing.Point(24, 116)
$exampleLabel.AutoSize = $true
$details.Controls.Add($exampleLabel)

$exampleBox = New-Object System.Windows.Forms.TextBox
$exampleBox.Multiline = $true
$exampleBox.ReadOnly = $true
$exampleBox.ScrollBars = "Vertical"
$exampleBox.BackColor = [System.Drawing.Color]::FromArgb(249, 250, 252)
$exampleBox.Location = New-Object System.Drawing.Point(24, 143)
$exampleBox.Size = New-Object System.Drawing.Size(500, 275)
$exampleBox.Anchor = "Top,Left,Right,Bottom"
$details.Controls.Add($exampleBox)

$installSelectedButton = New-Object System.Windows.Forms.Button
$installSelectedButton.Text = "安装选中格式"
$installSelectedButton.BackColor = [System.Drawing.Color]::FromArgb(39, 103, 73)
$installSelectedButton.ForeColor = [System.Drawing.Color]::White
$installSelectedButton.FlatStyle = "Flat"
$installSelectedButton.Location = New-Object System.Drawing.Point(24, 438)
$installSelectedButton.Size = New-Object System.Drawing.Size(145, 38)
$installSelectedButton.Anchor = "Left,Bottom"
$details.Controls.Add($installSelectedButton)

$removeSelectedButton = New-Object System.Windows.Forms.Button
$removeSelectedButton.Text = "卸载选中格式"
$removeSelectedButton.Location = New-Object System.Drawing.Point(181, 438)
$removeSelectedButton.Size = New-Object System.Drawing.Size(145, 38)
$removeSelectedButton.Anchor = "Left,Bottom"
$details.Controls.Add($removeSelectedButton)

$sourceButton = New-Object System.Windows.Forms.Button
$sourceButton.Text = "查看规范来源"
$sourceButton.Location = New-Object System.Drawing.Point(338, 438)
$sourceButton.Size = New-Object System.Drawing.Size(140, 38)
$sourceButton.Anchor = "Left,Bottom"
$details.Controls.Add($sourceButton)

$allPanel = New-Object System.Windows.Forms.FlowLayoutPanel
$allPanel.FlowDirection = "LeftToRight"
$allPanel.WrapContents = $false
$allPanel.Location = New-Object System.Drawing.Point(24, 492)
$allPanel.Size = New-Object System.Drawing.Size(500, 40)
$allPanel.Anchor = "Left,Right,Bottom"
$details.Controls.Add($allPanel)

$installAllButton = New-Object System.Windows.Forms.Button
$installAllButton.Text = "安装全部18套"
$installAllButton.Size = New-Object System.Drawing.Size(112, 34)
$allPanel.Controls.Add($installAllButton)

$removeAllButton = New-Object System.Windows.Forms.Button
$removeAllButton.Text = "卸载本工具箱格式"
$removeAllButton.Size = New-Object System.Drawing.Size(132, 34)
$allPanel.Controls.Add($removeAllButton)

$guideButton = New-Object System.Windows.Forms.Button
$guideButton.Text = "打开使用说明"
$guideButton.Size = New-Object System.Drawing.Size(112, 34)
$allPanel.Controls.Add($guideButton)

$testButton = New-Object System.Windows.Forms.Button
$testButton.Text = "打开测试矩阵"
$testButton.Size = New-Object System.Drawing.Size(112, 34)
$allPanel.Controls.Add($testButton)

$statusBar = New-Object System.Windows.Forms.StatusStrip
$statusLabel = New-Object System.Windows.Forms.ToolStripStatusLabel
$statusLabel.Text = "就绪。安装和卸载均不需要管理员权限。"
$statusLabel.Spring = $true
$statusLabel.TextAlign = "MiddleLeft"
[void]$statusBar.Items.Add($statusLabel)
$form.Controls.Add($statusBar)

function Update-Grid {
    $previousNumber = if ($script:CurrentStyle) { [int]$script:CurrentStyle.number } else { -1 }
    $query = $searchBox.Text.Trim()
    $category = [string]$categoryBox.SelectedItem
    $grid.Rows.Clear()
    foreach ($style in $script:Manifest) {
        $styleCategory = Get-Category $style.family
        if ($category -ne "全部" -and $styleCategory -ne $category) { continue }
        if ($query -and ($style.title -notlike "*$query*") -and ($style.display_name -notlike "*$query*")) { continue }
        $state = Get-InstalledState $style
        $index = $grid.Rows.Add($style.number, $style.title, $styleCategory, $state)
        $grid.Rows[$index].Tag = $style
        if ($state -eq "已安装") {
            $grid.Rows[$index].Cells["State"].Style.ForeColor = [System.Drawing.Color]::FromArgb(35, 120, 72)
        }
        elseif ($state -like "已安装*") {
            $grid.Rows[$index].Cells["State"].Style.ForeColor = [System.Drawing.Color]::FromArgb(184, 112, 25)
        }
        if ([int]$style.number -eq $previousNumber) {
            $grid.Rows[$index].Selected = $true
        }
    }
    if ($grid.Rows.Count -gt 0 -and $grid.SelectedRows.Count -eq 0) {
        $grid.Rows[0].Selected = $true
    }
    $statusLabel.Text = "当前显示 $($grid.Rows.Count) 套格式；安装目录：$script:TargetDirectory"
}

function Update-Details {
    if ($grid.SelectedRows.Count -eq 0) {
        $script:CurrentStyle = $null
        $detailTitle.Text = "请选择一种格式"
        $detailMeta.Text = ""
        $exampleBox.Text = ""
        return
    }
    $style = $grid.SelectedRows[0].Tag
    if (-not $style) { return }
    $script:CurrentStyle = $style
    $state = Get-InstalledState $style
    $detailTitle.Text = $style.title
    $detailMeta.Text = "类别：$(Get-Category $style.family)    状态：$($style.status)`r`n本机：$state    文件：$($style.filename)"
    $lines = New-Object System.Collections.Generic.List[string]
    $limit = [Math]::Min(5, @($style.examples).Count)
    for ($i = 0; $i -lt $limit; $i++) {
        $lines.Add("$($i + 1). $($style.examples[$i])")
        $lines.Add("")
    }
    $exampleBox.Text = ($lines -join "`r`n")
}

$grid.add_SelectionChanged({ Update-Details })
$searchBox.add_TextChanged({ Update-Grid })
$categoryBox.add_SelectedIndexChanged({ Update-Grid })
$refreshButton.add_Click({ Update-Grid; Update-Details })

$openFolderButton.add_Click({
    try {
        New-Item -ItemType Directory -Path $script:TargetDirectory -Force | Out-Null
        Start-Process explorer.exe -ArgumentList ('"' + $script:TargetDirectory + '"')
    }
    catch { Show-Error $_.Exception.Message }
})

$installSelectedButton.add_Click({
    if (-not $script:CurrentStyle) { return }
    try {
        $backup = New-BackupDirectory "CN-Academic-Backup-"
        Install-Style $script:CurrentStyle $backup
        $statusLabel.Text = "已安装：$($script:CurrentStyle.title)。请重启Word/WPS后选择该格式。"
        Update-Grid; Update-Details
    }
    catch { Show-Error $_.Exception.Message }
})

$removeSelectedButton.add_Click({
    if (-not $script:CurrentStyle) { return }
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "确定卸载“$($script:CurrentStyle.title)”吗？卸载文件会移动到带时间戳的备份目录。",
        "确认卸载",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Question
    )
    if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) { return }
    try {
        $backup = New-BackupDirectory "CN-Academic-Removed-"
        $removed = Remove-Style $script:CurrentStyle $backup
        if ($removed) { $statusLabel.Text = "已卸载并备份：$($script:CurrentStyle.title)" }
        else { $statusLabel.Text = "该格式尚未安装。" }
        Update-Grid; Update-Details
    }
    catch { Show-Error $_.Exception.Message }
})

$installAllButton.add_Click({
    try {
        $backup = New-BackupDirectory "CN-Academic-Backup-"
        foreach ($style in $script:Manifest) { Install-Style $style $backup }
        $statusLabel.Text = "18套格式已全部安装。请重启Word/WPS。"
        Update-Grid; Update-Details
    }
    catch { Show-Error $_.Exception.Message }
})

$removeAllButton.add_Click({
    $answer = [System.Windows.Forms.MessageBox]::Show(
        "确定卸载本工具箱安装的18套格式吗？文件会移动到备份目录，不会删除EndNote自带样式。",
        "确认卸载全部",
        [System.Windows.Forms.MessageBoxButtons]::YesNo,
        [System.Windows.Forms.MessageBoxIcon]::Warning
    )
    if ($answer -ne [System.Windows.Forms.DialogResult]::Yes) { return }
    try {
        $backup = New-BackupDirectory "CN-Academic-Removed-"
        $count = 0
        foreach ($style in $script:Manifest) {
            if (Remove-Style $style $backup) { $count++ }
        }
        $statusLabel.Text = "已卸载并备份 $count 套格式。"
        Update-Grid; Update-Details
    }
    catch { Show-Error $_.Exception.Message }
})

$sourceButton.add_Click({
    if (-not $script:CurrentStyle) { return }
    try { Start-Process ([string]$script:CurrentStyle.source_url) }
    catch { Show-Error $_.Exception.Message }
})

$guideButton.add_Click({
    try { Start-Process (Join-Path $script:Root "README.md") }
    catch { Show-Error $_.Exception.Message }
})

$testButton.add_Click({
    try { Start-Process (Join-Path $script:Root "格式库测试矩阵.xlsx") }
    catch { Show-Error $_.Exception.Message }
})

$form.add_Shown({ Update-Grid; Update-Details })
[void]$form.ShowDialog()
