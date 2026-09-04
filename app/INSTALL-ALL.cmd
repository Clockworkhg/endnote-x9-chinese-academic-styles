@echo off
setlocal
title EndNote X9 Chinese Academic Style Library Installer
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0install_all_styles.ps1"
set "INSTALL_EXIT=%ERRORLEVEL%"
echo.
if not "%INSTALL_EXIT%"=="0" echo Installation did not complete. Please capture this window.
pause
exit /b %INSTALL_EXIT%
