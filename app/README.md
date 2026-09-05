# 内置格式与测试资料

本目录保存 Windows EXE 编译时嵌入的47套 ENS、样式清单和配套测试资料。正式用户请从 GitHub Releases 下载并运行`EndNoteStyleToolbox.exe`，不再使用 PowerShell WinForms 启动链。

## 内置内容

- `Styles`：47套 EndNote ENS 脚注样式；
- `style-manifest.json`：格式名称、分类、来源、状态、示例和SHA-256；
- `格式库测试矩阵.xlsx`：108项 Word/WPS 回归测试记录表；
- `64条标准测试文献.ris`：可导入 EndNote 的覆盖测试集；
- `统一多语种文献类型.xml`：中文、英文、日文和特殊注文所需的参考类型；
- `THIRD_PARTY_NOTICES.md`：第三方来源及许可说明。

`INSTALL-ALL.cmd`、`REMOVE-ALL.cmd`及相应安装脚本仅作为0.2版本的历史兼容入口保留，不进入0.4.0 EXE发布产物。

## 当前状态

- 《中国社会科学（2026）》：以 Alpha 2.7 为稳定基线，已完成 EndNote X9＋WPS 基础实测；
- 46套上游脚注样式：覆盖固定版本`zotero-chinese/styles`中全部`class="note"`条目；
- 中国传媒大学参考版：根据中传公开人文社科脚注要求制作，属于实验版，并非学校官方统一样式；
- 除《中国社会科学（2026）》外，均为经过结构与关键模板字段自动校验的预览/实验版；
- 所有样式均保留`Cited Pages`并关闭正文/文末自动参考文献表；
- 脚注序号、字体、字号和行距由 Word/WPS 控制。

样式只能排列 EndNote 记录中已经存在的字段，不能凭空补出缺失的译者、出版社、学校、访问日期或卷期信息。投稿或出版前仍应以编辑部最新要求为准。
