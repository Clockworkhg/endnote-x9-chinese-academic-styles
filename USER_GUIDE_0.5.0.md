# v0.5.0 使用说明

v0.5.0 为工具箱正式版；内置样式仍保留各自的实测、预览或实验状态。

## 第一次使用

1. 将 `EndNoteStyleToolbox.exe` 放在自己可写的文件夹，双击启动。
2. 点击左侧“安装位置”。在 EndNote X9 的 Edit → Preferences → Folder Locations
   中找到 Styles Folder，将两边路径核对一致。可“自动检测”候选位置，或“选择文件夹”。
3. 勾选“我已核对”，保存。工具箱不会替你修改 EndNote 的偏好设置。
4. 在格式库搜索需要的样式，点击“安装所选格式”。已有同名文件会先备份。
5. 重新打开 EndNote X9，进入 Edit → Output Styles → Open Style Manager，找到安装的样式并勾选。
   回到 Word/WPS，在 EndNote 的 Style 列表选择相应样式；未列出时使用 Select Another Style。
6. 脚注序号继续由 Word/WPS 设置；本次引用页码在 Edit & Manage Citation(s) → Pages 填写。

自动检测只列出可能的个人目录，不证明 EndNote 一定使用该位置。
程序安装目录、磁盘根目录、链接目录不会被当作默认写入目标。
如目录不可写或处于链接中，请选择实际的个人 Styles 目录，不需要关闭安全软件。

## 安装、卸载与恢复

“已安装”按当前目录显示文件状态。更新工具箱本身不会自动替换已经安装的 ENS。
需要更新样式时，再选择安装；原样式会先备份。

卸载只处理有本工具箱安装记录、且内容与该记录一致的样式。用户修改过的文件，
以及旧版或其他工具安装的同名文件，默认保留。不是卸载失败。

“备份与恢复”可以打开当前目录：`CN-Academic-Backup-*` 是覆盖前备份，
`CN-Academic-Removed-*` 是卸载备份。关闭 EndNote 后，可将备份中的 ENS 复制回样式目录。
覆盖恢复前，请另存现有文件，以免丢失后续修改。

## 检查更新

点击“更新中心 → 检查更新”。仅检查本项目的 GitHub 正式发布。
下载通过 SHA-256 与 EXE 格式校验后，询问是否关闭工具箱并更新。
网络中断或取消下载不会替换原程序。新版启动检查失败会尝试恢复旧版。
恢复副本保存在程序旁 `.toolbox-update-*` 文件夹中的 `previous.exe`，不要在排错时删除它。

SHA-256 用于校验下载内容，不等同于 Windows 代码签名。
若系统提示未知发布者，请核对下载来源；不要为了运行而关闭系统安全保护。

## 使用时的核对项目

- 在个人电脑上检查窗口与按钮是否完整可见，特别是高缩放屏幕。
- 在空白 Word/WPS 文档中，测试中文书籍、英文书籍、译著、期刊及具体页码。
- 同一文献连续引用和间隔再次引用均应有可读内容，不能仅剩一个句号。
- 不应在正文另外生成不需要的参考文献表。
- 样式的来源、适配说明和实验性标记不代表出版社或学校官方认证。

GUI 自动测试不包含 EndNote/Word/WPS 的真实引文渲染；不同软件版本仍需实际核对。

## 中、英、日文模板的选择

当前使用 Reference Type（文献类型）分流，不按 Language 自动判断；文档保持同一个 Style，
也无需在每次插入前切换整个文档的样式。作者、题名不会自动翻译。

首次使用，在工具箱点击“导出测试与配置”。在 EndNote X9 的 Edit → Preferences → Reference Types
中先 Export 备份原有类型定义，再 Import “统一多语种文献类型.xml”。这是类型配置导入，
不是 File → Import 的文献导入。已有自定义类型时，先核对冲突，不能直接覆盖已有工作流程。

| 文献 | Reference Type | 作者字段 |
|---|---|---|
| 中文及中文译本 | Book / Journal Article / Book Section | Author |
| 英文图书 | English Book | English Author |
| 英文期刊论文 | English Journal Article | English Author |
| 英文章节 | English Book Section | English Author；编者核对 English Editor |
| 日文图书、期刊、章节 | Japanese Book / Japanese Journal Article / Japanese Book Section | Japanese Authors (formatted)；章节编者填 Japanese Editor (formatted) |

以 Peter Brooks 的英文图书为例：打开记录，改成 English Book，将 Original Author (backup)
中的作者复制到 English Author，填写 `Brooks, Peter`。多位作者一人一行，保留原作者备份。
日文作者字段使用已排好作者顺序和分隔符的文字。切换前后核对原字段，勿覆盖已有专用字段内容。
保存记录，在 Word/WPS 点击 Update Citations and Bibliography；必要时通过
Edit & Manage Citation(s) → Update from My Library 同步库中修改。

当前没有按 Language 批量转换记录的功能。普通 Book 中即使 Language 写 English，仍走普通模板。
多语言样式的名称沿用上游；当前英日文模板主要继承中国社会科学基线，不能据此声称已完整适配
上游所有语言和规则。其他语种或特殊文献须按实际模板核对，不能承诺任意文献自动准确显示。
