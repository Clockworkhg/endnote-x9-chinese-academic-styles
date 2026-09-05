# Windows EXE

`EndNoteStyleToolbox`是工具箱的正式图形客户端，基于 .NET 8 WinForms。它不启动 PowerShell 或 CMD，运行所需的47套样式与配套资料均作为程序集资源嵌入。

发布：

```powershell
dotnet publish EndNoteStyleToolbox/EndNoteStyleToolbox.csproj -c Release -r win-x64 --self-contained true -o artifacts/win-x64
```

无界面启动测试：

```powershell
$process = Start-Process .\artifacts\win-x64\EndNoteStyleToolbox.exe -ArgumentList @("--self-test", ".\self-test.json") -Wait -PassThru
$process.ExitCode
Get-Content .\self-test.json
```

`--self-test`不会显示窗口，也不会访问用户真实的 EndNote 目录；全部写入都发生在随机临时目录内。
