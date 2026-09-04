@echo off
setlocal
start "" powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -STA -WindowStyle Hidden -File "%~dp0StyleToolbox.ps1"
exit /b 0
