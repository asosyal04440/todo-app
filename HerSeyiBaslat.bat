@echo off
setlocal
chcp 65001 >nul
title Todo Uygulamasi Baslaticisi

set "ROOT=%~dp0"
set "NODE_DIR=%LOCALAPPDATA%\hermes\node"
set "API_URL=http://localhost:7070/api/auth/config"
set "WEB_URL=http://localhost:4200/"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo HATA: .NET bulunamadi.
    pause
    exit /b 1
)

dotnet --list-runtimes | findstr /B /C:"Microsoft.AspNetCore.App 8." >nul
if errorlevel 1 (
    echo HATA: .NET 8 ASP.NET Core Runtime bulunamadi.
    pause
    exit /b 1
)

if not exist "%NODE_DIR%\node.exe" (
    echo HATA: Angular icin Node.js bulunamadi:
    echo %NODE_DIR%\node.exe
    pause
    exit /b 1
)

if not exist "%ROOT%todo.Web\node_modules\@angular\cli\bin\ng.js" (
    echo HATA: Angular bagimliliklari bulunamadi.
    echo Once todo.Web klasorunde npm install calistir.
    pause
    exit /b 1
)

set "PATH=%NODE_DIR%;%PATH%"

sc.exe query "MSSQL$SQLEXPRESS" 2>nul | findstr /I "RUNNING" >nul
if errorlevel 1 (
    echo SQL Server Express baslatiliyor...
    net start "MSSQL$SQLEXPRESS" >nul 2>&1
    if errorlevel 1 (
        echo HATA: SQL Server Express baslatilamadi.
        echo Bu dosyayi yonetici olarak calistirman gerekebilir.
        pause
        exit /b 1
    )
)

dotnet user-secrets list --project "%ROOT%todo.API\todo.API.csproj" 2>nul | findstr /B /C:"Jwt:SigningKey" >nul
if errorlevel 1 (
    echo HATA: JWT imzalama anahtari tanimli degil.
    echo Bir kez su komutu calistir ^(degeri rastgele ve en az 32 karakter yap^):
    echo dotnet user-secrets set "Jwt:SigningKey" "UZUN-RASTGELE-BIR-DEGER" --project todo.API
    pause
    exit /b 1
)

dotnet user-secrets list --project "%ROOT%todo.API\todo.API.csproj" 2>nul | findstr /B /C:"Email:Smtp:Password" >nul
if errorlevel 1 (
    echo UYARI: Gmail SMTP bilgileri tanimli degil, hatirlatma mailleri gonderilmeyecek.
    echo Uygulama yine de calisir. Mail icin Email:Smtp:Username ve Email:Smtp:Password user secret olarak eklenmeli.
)

call :WAIT_FOR_URL "%API_URL%" 1
if not errorlevel 1 goto API_RUNNING

dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo HATA: dotnet-ef araci bulunamadi.
    echo Bir kez su komutu calistir:
    echo dotnet tool install --global dotnet-ef --version 8.*
    pause
    exit /b 1
)

echo Veritabani guncelleniyor...
dotnet ef database update --project "%ROOT%todo.Infrastructure\todo.Infrastructure.csproj" --startup-project "%ROOT%todo.API\todo.API.csproj"
if errorlevel 1 (
    echo HATA: Veritabani guncellenemedi. Yukaridaki hata mesajini kontrol et.
    pause
    exit /b 1
)

echo Todo API baslatiliyor...
start "Todo API" /D "%ROOT%" dotnet run --no-build --project "todo.API\todo.API.csproj" --urls http://localhost:7070

:API_RUNNING

echo Todo API'nin hazir olmasi bekleniyor...
call :WAIT_FOR_URL "%API_URL%" 60
if errorlevel 1 (
    echo HATA: Todo API 60 saniye icinde hazir olmadi.
    echo Acik Todo API penceresindeki hata mesajini kontrol et.
    pause
    exit /b 1
)

call :WAIT_FOR_URL "%WEB_URL%" 1
if errorlevel 1 (
    echo Angular sitesi baslatiliyor...
    start "Todo Web" /D "%ROOT%todo.Web" "%NODE_DIR%\node.exe" "%ROOT%todo.Web\node_modules\@angular\cli\bin\ng.js" serve --host localhost --port 4200
)

echo Sitenin hazir olmasi bekleniyor...
call :WAIT_FOR_URL "%WEB_URL%" 90
if errorlevel 1 (
    echo HATA: Site 90 saniye icinde hazir olmadi.
    echo Acik Todo Web penceresindeki hata mesajini kontrol et.
    pause
    exit /b 1
)

start "" "%WEB_URL%"
echo Todo sitesi acildi: %WEB_URL%
echo Arka plan isleri paneli: http://localhost:7070/hangfire
echo Loglar: %ROOT%todo.API\logs
exit /b 0

:WAIT_FOR_URL
set "WAIT_URL=%~1"
set "WAIT_COUNT=%~2"
for /L %%i in (1,1,%WAIT_COUNT%) do (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "try { $response = Invoke-WebRequest -UseBasicParsing -Uri '%WAIT_URL%' -TimeoutSec 2; if ($response.StatusCode -lt 500) { exit 0 }; exit 1 } catch { exit 1 }" >nul 2>&1
    if not errorlevel 1 exit /b 0
    ping.exe -n 2 127.0.0.1 >nul
)
exit /b 1
