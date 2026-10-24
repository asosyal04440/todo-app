@echo off
setlocal
chcp 65001 >nul
title Todo Uygulamasi Baslaticisi
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Start-Todo.ps1" %*
exit /b %ERRORLEVEL%
