# EndNote X9 中文学术脚注格式工具箱

[![Tests](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/actions/workflows/tests.yml/badge.svg)](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/actions/workflows/tests.yml)
[![Release](https://img.shields.io/github/v/release/Clockworkhg/endnote-x9-chinese-academic-styles)](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/releases/latest)

面向中文人文社会科学写作的 EndNote X9 脚注样式库与 Windows 可视化安装工具。保留 EndNote `Cited Pages` 工作流，让同一文献的每次引用可以分别填写具体页码。

> 本项目不是《中国社会科学》、相关期刊、出版社或 Clarivate 的官方产品。“参考某规范”表示根据公开规则和样例制作，投稿或出版前仍应以编辑部最新要求为准。

## 下载与使用

从 [Releases](https://github.com/Clockworkhg/endnote-x9-chinese-academic-styles/releases/latest) 下载最新版，完整解压后双击：

```text
打开中文学术格式工具箱.cmd
```

在中文界面中可以搜索格式、查看示例、检测状态，以及安装或卸载单套/全部样式。安装和卸载均不需要管理员权限，文件只写入当前用户的`文档\EndNote\Styles`；覆盖或卸载前会自动备份。

## 当前状态

- 《中国社会科学（2026）》：以 Alpha 2.7 为稳定基线，已完成 EndNote X9＋WPS 基础实测；
- 其余17套：ENS结构与关键字段已自动校验，属于预览版；
- 18套样式均保留`Cited Pages`，关闭正文/文末自动参考文献表；
- 连续引用不使用 EndNote 易产生孤立句号的`Ibid.`特殊替换；
- 脚注序号、字体、字号和行距由 Word/WPS 控制。

## 内置格式

| 类别 | 格式 |
|---|---|
| 综合 | 中国社会科学（2026）、综合性期刊文献引证技术规范 |
| 出版社/高校 | 人民出版社、清华大学（人文社科） |
| 政治学与国际关系 | 政治学研究、世界经济与政治、外交评论、国际政治研究、现代国际关系 |
| 历史与文学 | 历史研究、世界历史、文学评论 |
| 新闻传播 | 新闻与传播研究 |
| 法学 | 法学引注手册（第二版）、中国政法大学、中外法学 |
| 国家标准 | GB/T 7714—2015（注释·双语）、GB/T 7714—2025（注释·双语） |

## 文献覆盖

常规模板覆盖图书、译著、期刊论文、图书章节、学位论文、会议论文、报纸、报告、网页、政府文件、档案、古籍等类型。配套自定义参考类型支持英文、日文和预排版特殊注文。

样式只能排列 EndNote 记录中已经存在的字段，不能凭空补出缺失的译者、出版社、学校、卷期或访问日期。复杂资料可使用`Preformatted Footnote`兜底类型。

## 仓库结构

```text
app/       可直接使用的Windows便携版、18套ENS和测试资料
assets/    构建所需的基础样式、参考类型和来源示例
src/       ENS解析器、CSS 2.7及多样式生成器
tests/     二进制结构、模板字段、GUI安全边界测试
release/   可分发的0.2便携版压缩包
licenses/  各组成部分的完整许可证
```

## 开发与测试

需要 Python 3.10 或更高版本。ENS生成和解析本身没有第三方运行依赖。

```bash
python src/build_css2026.py
python src/build_style_library.py
python src/build_css2026_testdata.py
python -m pip install pytest
python -m pytest -q
```

自动测试检查18个ENS是否可解析、文件哈希是否一致、`Cited Pages`是否存在、`Ibid.`是否清除、正文/文末参考文献表是否关闭，以及GUI是否只操作当前用户的EndNote样式目录。

## 贡献

实际书稿是发现样式边界问题的最好方式。提交Issue时请提供：EndNote参考类型、相关字段、实际输出、期望输出和Word/WPS版本。请勿上传包含隐私或未公开书稿的完整文档。

## 许可与署名

本仓库采用分组件许可：原创程序代码为MIT；根据`zotero-chinese/styles`公开规则和样例衍生的样式数据为CC BY-SA 3.0；第三方参考类型材料保留原MIT许可。详见[LICENSE](LICENSE)和[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

维护者：Clockworkhg <hershelgao@gmail.com>
