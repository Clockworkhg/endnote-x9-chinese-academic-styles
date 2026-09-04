# v0.3.0：真正的Windows EXE

这一版彻底移除了 PowerShell WinForms 启动链。用户只需下载并双击`EndNoteStyleToolbox.exe`，不需要解压脚本、不需要调整执行策略，也不需要管理员权限。

## 主要变化

- 原生 .NET 8 WinForms 图形界面；
- 64位、自包含、单文件 Windows EXE；
- 内嵌18套中文学术脚注格式；
- 内嵌格式测试矩阵、64条RIS测试文献和多语种参考类型配置；
- 支持搜索、分类、示例预览、状态检测、单套/全部安装；
- 覆盖前自动备份，卸载也保留可恢复副本；
- 不调用 PowerShell 或 CMD，不写入系统目录。

## 发布前验证

本 Release 的 EXE 由 GitHub `windows-latest` 构建，并在同一 Windows 环境中实际启动。发布任务会调用程序内置的`--self-test`，验证18个样式资源与哈希，并在临时目录完成安装、覆盖备份、状态检测、卸载恢复和配套资料导出。任何一步失败都不会创建 Release。

下载`EndNoteStyleToolbox.exe`后直接双击即可。`EndNoteStyleToolbox.exe.sha256`用于校验文件完整性，`self-test.json`是该发布产物的 Windows 自检报告。
