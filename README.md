# EndNote X9 中文学术脚注格式工具箱

[![Tests](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/actions/workflows/tests.yml/badge.svg)](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/actions/workflows/tests.yml)
[![Windows EXE](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/actions/workflows/windows-exe.yml/badge.svg)](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/actions/workflows/windows-exe.yml)
[![Release](https://img.shields.io/github/v/release/Clockworkhg/endnote-x9-chinese-academic-styles)](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/releases/latest)

面向中文人文社会科学写作的 EndNote X9 脚注样式库与原生 Windows 图形工具。保留 EndNote `Cited Pages` 工作流，让同一文献的每次引用可以分别填写具体页码。

> 本项目不是《中国社会科学》、相关期刊、出版社或 Clarivate 的官方产品。“参考某规范”表示根据公开规则和样例制作，投稿或出版前仍应以编辑部最新要求为准。

## 下载与使用

从 [Releases](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/releases/latest) 下载：

```text
EndNoteStyleToolbox.exe
```

双击即可打开，不需要解压运行环境、不需要 PowerShell，也不需要管理员权限。18套 ENS、测试矩阵、64条测试文献和多语种参考类型配置均已嵌入 EXE。

工具支持：

- 搜索、分类和格式示例预览；
- 单套或全部安装；
- 已安装/其他版本状态检测；
- 可恢复卸载，覆盖或卸载前自动备份；
- 导出测试矩阵、RIS测试集和参考类型配置。

文件只写入当前用户的`文档\EndNote\Styles`。安装后重新打开 EndNote，在 Word/WPS 的 EndNote 选项卡中选择对应格式即可。

## Windows启动验证

每次提交都会在 GitHub `windows-latest` 环境中：

1. 使用 .NET 8 发布64位、自包含、单文件 EXE；
2. 真正启动发布后的 EXE；
3. 通过`--self-test`验证18个嵌入样式及SHA-256；
4. 在临时目录执行安装、覆盖备份、状态检测、卸载恢复和配套资料导出；
5. 仅在全部检查通过后上传构建产物。

打标签发布时，同一套 Windows 启动测试会再次运行；失败则不会创建 Release。

## 当前格式库

| 类别 | 格式 |
|---|---|
| 综合 | 中国社会科学（2026）、综合性期刊文献引证技术规范 |
| 出版社/高校 | 人民出版社、清华大学（人文社科） |
| 政治学与国际关系 | 政治学研究、世界经济与政治、外交评论、国际政治研究、现代国际关系 |
| 历史与文学 | 历史研究、世界历史、文学评论 |
| 新闻传播 | 新闻与传播研究 |
| 法学 | 法学引注手册（第二版）、中国政法大学、中外法学 |
| 国家标准 | GB/T 7714—2015（注释·双语）、GB/T 7714—2025（注释·双语） |

《中国社会科学（2026）》以 Alpha 2.7 为稳定基线，已完成 EndNote X9＋WPS 基础实测；其余17套已经过 ENS 结构与关键字段自动校验，属于预览版。脚注序号、字体、字号和行距仍由 Word/WPS 控制。

## 文献覆盖与边界

常规模板覆盖图书、译著、期刊论文、图书章节、学位论文、会议论文、报纸、报告、网页、政府文件、档案、古籍等类型，并提供英文、日文和预排版特殊注文所需的统一参考类型。

样式只能排列 EndNote 记录中已经存在的字段，不能凭空补出缺失的译者、出版社、学校、卷期或访问日期。复杂资料可使用`Preformatted Footnote`兜底类型。

## 仓库结构

```text
windows/   原生.NET 8 WinForms EXE源码与无界面自检
app/       18套ENS、清单、测试资料及旧版0.2启动脚本
assets/    构建所需的基础样式、参考类型和来源示例
src/       ENS解析器、CSS 2.7及多样式生成器
tests/     二进制结构、模板字段、EXE安全边界测试
release/   历史发布包
licenses/  各组成部分的完整许可证
```

## 开发与测试

ENS生成与静态测试需要 Python 3.10+；Windows程序需要 .NET 8 SDK。

```bash
python src/build_css2026.py
python src/build_style_library.py
python -m pytest -q
dotnet publish windows/EndNoteStyleToolbox/EndNoteStyleToolbox.csproj -c Release -r win-x64 --self-contained true
```

Windows EXE 自检：

```powershell
EndNoteStyleToolbox.exe --self-test self-test.json
```

## 贡献与许可

提交 Issue 时请提供：EndNote参考类型、相关字段、实际输出、期望输出和 Word/WPS 版本。请勿上传包含隐私或未公开书稿的完整文档。

原创程序代码为 MIT；根据`zotero-chinese/styles`公开规则和样例衍生的样式数据为 CC BY-SA 3.0；第三方参考类型材料保留原 MIT 许可。详见[LICENSE](LICENSE)和[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

维护者：Clockworkhg <hershelgao@gmail.com>
