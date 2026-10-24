param([switch]$NoBrowser, [switch]$NoPause, [switch]$CheckOnly)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$apiDirectory = Join-Path $projectRoot 'todo.API'
$webDirectory = Join-Path $projectRoot 'todo.Web'
$apiProject = Join-Path $apiDirectory 'todo.API.csproj'
$apiBase = 'http://localhost:7070'
$apiReadyUrl = "$apiBase/api/auth/config"
$webBase = 'http://localhost:4200'
$startedProcesses = @()

function Find-Node {
    $candidates = @()
    if ($env:TODO_NODE_EXE) { $candidates += $env:TODO_NODE_EXE }
    $candidates += Join-Path $env:LOCALAPPDATA 'hermes\node\node.exe'
    $candidates += Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
    $candidates += @(Get-Command node.exe -CommandType Application -All -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source)
    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
        $versionText = & $candidate --version 2>$null
        if ($LASTEXITCODE -ne 0) { continue }
        $version = [Version]($versionText.Trim().TrimStart('v'))
        if (($version.Major -eq 20 -and $version.Minor -ge 19) -or
            ($version.Major -eq 22 -and $version.Minor -ge 12) -or $version.Major -ge 24) {
            return @{ Path = $candidate; Version = $versionText.Trim() }
        }
    }
    throw 'Angular icin uygun Node.js bulunamadi. Node 20.19+, 22.12+ veya 24+ gerekli.'
}

function Test-ApiReady([string]$Url) {
    try {
        $config = Invoke-RestMethod -Uri $Url -TimeoutSec 2
        return ($config.PSObject.Properties.Name -contains 'googleClientId')
    } catch { return $false }
}

function Test-WebReady {
    try {
        $page = Invoke-WebRequest -UseBasicParsing -Uri "$webBase/" -TimeoutSec 2
        if ($page.StatusCode -ne 200 -or $page.Content -notmatch '<app-root') { return $false }
        return (Test-ApiReady "$webBase/api/auth/config")
    } catch { return $false }
}

function Assert-PortFree([int]$Port) {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $Port)
    try { $listener.Start() }
    catch { throw "${Port} portu baska bir uygulama tarafindan kullaniliyor; beklenen Todo sunucusu cevap vermiyor." }
    finally { $listener.Stop() }
}

function Wait-Ready([scriptblock]$Check, [System.Diagnostics.Process]$Process, [int]$Seconds, [string]$Name, [string]$Log) {
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    do {
        if (& $Check) { return }
        if ($null -ne $Process -and $Process.HasExited) {
            throw "$Name kapandi (cikis kodu $($Process.ExitCode)). Hata logu: $Log"
        }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "$Name $Seconds saniye icinde hazir olmadi. Hata logu: $Log"
}

try {
    $dotnet = (Get-Command dotnet.exe -CommandType Application).Source
    $sdks = @(& $dotnet --list-sdks)
    if (-not ($sdks | Where-Object { $_ -match '^(8|9|[1-9][0-9])\.' })) { throw '.NET 8 veya daha yeni bir SDK gerekli.' }
    $runtimes = @(& $dotnet --list-runtimes)
    if (-not ($runtimes | Where-Object { $_ -match '^Microsoft.AspNetCore.App 8\.' })) { throw '.NET 8 ASP.NET Core Runtime bulunamadi.' }
    $node = Find-Node
    $env:PATH = (Split-Path -Parent $node.Path) + ';' + $env:PATH
    Write-Host "Node: $($node.Version) ($($node.Path))"
    $ngScript = Join-Path $webDirectory 'node_modules\@angular\cli\bin\ng.js'
    if (-not (Test-Path -LiteralPath $ngScript)) { throw 'Angular bagimliliklari bulunamadi (todo.Web/node_modules).' }
    $secrets = @(& $dotnet user-secrets list --project $apiProject 2>$null)
    if (-not $env:Jwt__SigningKey -and -not ($secrets | Where-Object { $_ -match '^Jwt:SigningKey\s*=' })) {
        throw 'JWT imzalama anahtari tanimli degil. API icin Jwt:SigningKey user-secret veya Jwt__SigningKey ortam ayari gerekli.'
    }
    $secrets = $null
    $sqlService = Get-Service -Name 'MSSQL$SQLEXPRESS'
    if ($sqlService.Status -ne 'Running') {
        if ($CheckOnly) { throw 'SQL Server Express calismiyor.' }
        Write-Host 'SQL Server Express baslatiliyor...'
        try { Start-Service -Name 'MSSQL$SQLEXPRESS' }
        catch { throw 'SQL Server Express baslatilamadi. Baslaticiyi yonetici olarak acman gerekebilir.' }
        $sqlService.WaitForStatus('Running', [TimeSpan]::FromSeconds(30))
    }
    $apiRunning = Test-ApiReady $apiReadyUrl
    $webRunning = Test-WebReady
    if (-not $apiRunning) { Assert-PortFree 7070 }
    if (-not $webRunning) { Assert-PortFree 4200 }
    if ($CheckOnly) {
        Write-Host "On kontrol basarili. API hazir: $apiRunning. Web hazir: $webRunning."
        exit 0
    }
    $logDirectory = Join-Path $apiDirectory 'logs'
    New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
    $apiLog = Join-Path $logDirectory 'launcher-api-error.log'
    $webLog = Join-Path $logDirectory 'launcher-web-error.log'
    if (-not $apiRunning) {
        Write-Host 'Veritabani guncelleniyor...'
        Push-Location $projectRoot
        try {
            & $dotnet run --project (Join-Path $projectRoot 'tools\TodoDatabaseMigrator\TodoDatabaseMigrator.csproj') -- $apiDirectory
            if ($LASTEXITCODE -ne 0) { throw 'Veritabani guncellenemedi; API baslatilmadi.' }
        } finally { Pop-Location }
        Write-Host 'Todo API baslatiliyor...'
        $apiProcess = Start-Process -FilePath $dotnet -WorkingDirectory $apiDirectory -WindowStyle Hidden -PassThru `
            -ArgumentList ('run --project "{0}" --urls {1} --launch-profile TodoApi' -f $apiProject, $apiBase) `
            -RedirectStandardOutput (Join-Path $logDirectory 'launcher-api.log') -RedirectStandardError $apiLog
        $startedProcesses += $apiProcess
    } else { Write-Host 'Todo API zaten acik; mevcut sunucu kullaniliyor.' }
    Wait-Ready { Test-ApiReady $apiReadyUrl } $apiProcess 90 'Todo API' $apiLog
    if (-not $webRunning) {
        Write-Host 'Angular sitesi baslatiliyor...'
        $proxy = Join-Path $webDirectory 'proxy.conf.json'
        $webProcess = Start-Process -FilePath $node.Path -WorkingDirectory $webDirectory -WindowStyle Hidden -PassThru `
            -ArgumentList ('"{0}" serve --host localhost --port 4200 --proxy-config "{1}"' -f $ngScript, $proxy) `
            -RedirectStandardOutput (Join-Path $logDirectory 'launcher-web.log') -RedirectStandardError $webLog
        $startedProcesses += $webProcess
    } else { Write-Host 'Todo Web zaten acik; mevcut sunucu kullaniliyor.' }
    Wait-Ready { Test-WebReady } $webProcess 90 'Todo Web' $webLog
    if (-not $NoBrowser) { Start-Process -FilePath "$webBase/" }
    Write-Host "Todo hazir: $webBase/"
    Write-Host "Loglar: $logDirectory"
    exit 0
} catch {
    foreach ($process in $startedProcesses) {
        if (-not $process.HasExited) { & taskkill.exe /PID $process.Id /T /F >$null 2>&1 }
    }
    Write-Host "HATA: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Hata loglari: todo.API/logs'
    if (-not $NoPause) { Read-Host 'Kapatmak icin Enter' | Out-Null }
    exit 1
}
