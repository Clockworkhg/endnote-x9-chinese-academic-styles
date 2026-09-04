@echo off
setlocal
title Remove EndNote X9 Chinese Academic Style Library
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0remove_all_styles.ps1"
set "REMOVE_EXIT=%ERRORLEVEL%"
echo.
pause
exit /b %REMOVE_EXIT%
