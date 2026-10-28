<#
.SYNOPSIS
    Simula il rilascio di una nuova versione dell'app demo (Demo 1 - Circuit Pause & Resume).

.DESCRIPTION
    1. Verifica che l'applicazione sia in esecuzione (GET /health).
    2. Richiede uno shutdown controllato (POST /demo/shutdown): il server mette in pausa i circuit.
    3. Attende la terminazione del processo.
    4. Incrementa la versione applicativa (DEMO_VERSION, es. v1 -> v2).
    5. Riavvia l'applicazione sulla stessa porta.
    6. Attende che il server sia nuovamente disponibile.

    Prerequisito: l'app deve essere avviata con lo stesso profilo, ad esempio
        dotnet run --project src\Dottor.WPC2026.Web --launch-profile https

.EXAMPLE
    .\scripts\deploy-demo.ps1

.EXAMPLE
    .\scripts\deploy-demo.ps1 -LaunchProfile http -NewVersion v3
#>
[CmdletBinding()]
param(
    # Profilo di launchSettings.json usato per riavviare l'app: "https" (https://localhost:7255) o "http" (http://localhost:5050).
    [ValidateSet('https', 'http')]
    [string]$LaunchProfile = 'https',

    # Versione da impostare dopo il riavvio. Se omessa viene incrementata quella corrente (v1 -> v2).
    [string]$NewVersion,

    # Se specificato, la nuova istanza parte in background e scrive l'output in questo file invece di aprire una nuova finestra.
    [string]$LogFile,

    [int]$ShutdownTimeoutSeconds = 60,
    [int]$StartupTimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src\Dottor.WPC2026.Web\Dottor.WPC2026.Web.csproj'
$baseUrl = if ($LaunchProfile -eq 'https') { 'https://localhost:7255' } else { 'http://localhost:5050' }

function Write-Step([string]$message) {
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Get-Health {
    try {
        return Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 3
    }
    catch {
        return $null
    }
}

# 1. L'applicazione è in esecuzione?
Write-Step "Verifica dell'applicazione su $baseUrl"
$current = Get-Health
if (-not $current) {
    throw "L'applicazione non risponde su $baseUrl/health. Avviala con: dotnet run --project src\Dottor.WPC2026.Web --launch-profile $LaunchProfile"
}
Write-Host "    Versione $($current.version), PID $($current.processId), circuit connessi: $($current.connectedCircuits)"

if (-not $NewVersion) {
    $NewVersion = if ($current.version -match '^v(\d+)$') { "v$([int]$Matches[1] + 1)" } else { 'v2' }
}

# 2. Shutdown controllato: il server chiede ai browser di mettere in pausa i circuit.
Write-Step 'Richiesta di shutdown controllato (POST /demo/shutdown)'
Invoke-RestMethod -Method Post -Uri "$baseUrl/demo/shutdown" -TimeoutSec 10 | Out-Null

# 3. Attesa della terminazione del processo (nessuna terminazione forzata).
Write-Step "Attesa della terminazione del processo $($current.processId)"
$oldProcess = Get-Process -Id $current.processId -ErrorAction SilentlyContinue
if ($oldProcess) {
    try {
        $oldProcess | Wait-Process -Timeout $ShutdownTimeoutSeconds
    }
    catch {
        throw "Il processo $($current.processId) non è terminato entro $ShutdownTimeoutSeconds secondi."
    }
}
Write-Host '    Processo terminato.'

# 4. Nuova versione.
Write-Step "Nuova versione: $($current.version) -> $NewVersion"
$env:DEMO_VERSION = $NewVersion

# 5. Riavvio sulla stessa porta (il progetto è già compilato: --no-build rende il riavvio rapido).
Write-Step "Avvio della nuova istanza (profilo $LaunchProfile)"
$arguments = @('run', '--project', "`"$projectPath`"", '--no-build', '--launch-profile', $LaunchProfile)
if ($LogFile) {
    Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $repoRoot -WindowStyle Hidden `
        -RedirectStandardOutput $LogFile -RedirectStandardError "$LogFile.err" | Out-Null
}
else {
    Start-Process -FilePath 'dotnet' -ArgumentList $arguments -WorkingDirectory $repoRoot | Out-Null
}

# 6. Attesa della disponibilità.
Write-Step 'Attesa della disponibilità del server'
$deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
do {
    Start-Sleep -Milliseconds 500
    $updated = Get-Health
} while ((-not $updated -or $updated.processId -eq $current.processId) -and (Get-Date) -lt $deadline)

if (-not $updated) {
    throw "La nuova istanza non ha risposto entro $StartupTimeoutSeconds secondi."
}

Write-Host ''
Write-Host "Deploy completato: versione $($current.version) -> $($updated.version), PID $($current.processId) -> $($updated.processId)" -ForegroundColor Green
Write-Host 'Torna al browser: la sessione riprende automaticamente con lo stato precedente.'
