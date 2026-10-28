<#
.SYNOPSIS
    Simula il rilascio di una nuova versione dell'app demo (Demo 1 - Circuit Pause & Resume).

.DESCRIPTION
    1. Verifica che l'applicazione sia in esecuzione (GET /health).
    2. Richiede uno shutdown controllato (POST /demo/shutdown): il server mette in pausa i circuit.
    3. Attende la terminazione del processo.
    4. Incrementa la versione applicativa (DEMO_VERSION, es. v1 -> v2).
    5. Riavvia l'applicazione sugli stessi endpoint del profilo (https + http).
    6. Attende che il server sia nuovamente disponibile.

    Prerequisito: l'app deve essere avviata con lo stesso profilo, ad esempio
        dotnet run --project src\Dottor.WPC2026.Web --launch-profile https

.EXAMPLE
    .\scripts\deploy-demo.ps1

.EXAMPLE
    .\scripts\deploy-demo.ps1 -LaunchProfile http -Version v3
#>
[CmdletBinding()]
param(
    # Profilo di launchSettings.json da cui leggere URL e variabili d'ambiente.
    [string]$LaunchProfile = 'https',

    # URL espliciti (separati da ';'). Se omessi si usa applicationUrl del profilo.
    [string]$Urls,

    # Versione da impostare dopo il riavvio. Se omessa viene incrementata quella corrente (v1 -> v2).
    [string]$Version,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateRange(10, 300)]
    [int]$TimeoutSeconds = 90
)

$ErrorActionPreference = 'Stop'

$repository = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repository 'src\Dottor.WPC2026.Web'
$assembly = Join-Path $project "bin\$Configuration\net11.0\Dottor.WPC2026.Web.dll"
$environment = @{ ASPNETCORE_ENVIRONMENT = 'Development' }

if (-not $Urls) {
    $settings = Get-Content (Join-Path $project 'Properties\launchSettings.json') -Raw | ConvertFrom-Json
    $profileSettings = $settings.profiles.$LaunchProfile
    if (-not $profileSettings.applicationUrl) {
        throw "Profilo '$LaunchProfile' senza applicationUrl in launchSettings.json."
    }
    $Urls = $profileSettings.applicationUrl
    if ($profileSettings.environmentVariables) {
        foreach ($property in $profileSettings.environmentVariables.PSObject.Properties) {
            $environment[$property.Name] = $property.Value
        }
    }
}

# La nuova istanza deve ascoltare sugli stessi URL: ogni tab può riprendere il circuit solo sulla propria origin.
$applicationUrls = @($Urls -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$parsedUrls = @($applicationUrls | ForEach-Object { [Uri]$_ })
# L'app usa sempre UseHttpsRedirection: se disponibile si preferisce l'endpoint https per evitare redirect.
$controlUrl = ($parsedUrls | Where-Object { $_.Scheme -eq 'https' } | Select-Object -First 1)
if (-not $controlUrl) { $controlUrl = $parsedUrls | Select-Object -First 1 }
if (-not $controlUrl) {
    throw 'Nessun URL valido: impossibile eseguire health check e shutdown.'
}
$baseUrl = "$($controlUrl.Scheme)://localhost:$($controlUrl.Port)"

if (-not (Test-Path -LiteralPath $assembly)) {
    throw "Assembly non trovato: $assembly. Compilare prima con dotnet build -c $Configuration."
}

function Write-Step([string]$message) {
    Write-Host "==> $message" -ForegroundColor Cyan
}

# 1. L'applicazione è in esecuzione?
Write-Step "Verifica dell'applicazione su $baseUrl"
try {
    $previous = Invoke-RestMethod "$baseUrl/health" -TimeoutSec 5
}
catch {
    throw "L'applicazione non risponde su $baseUrl. Avviala con: dotnet run --project src\Dottor.WPC2026.Web --launch-profile $LaunchProfile. Dettaglio: $($_.Exception.Message)"
}
if ($previous.status -ne 'ok' -or -not $previous.processId) {
    throw 'Health endpoint inatteso: deployment annullato.'
}
$previousProcess = Get-Process -Id $previous.processId -ErrorAction Stop
if ($previousProcess.ProcessName -notin @('dotnet', 'Dottor.WPC2026.Web')) {
    throw "Il PID $($previous.processId) non appartiene a un processo .NET atteso."
}
Write-Host "    Versione $($previous.version), PID $($previous.processId), circuit connessi: $($previous.connectedCircuits)"

if (-not $Version) {
    if ($previous.version -match '^v(\d+)$') {
        $Version = 'v' + (1 + [int]$Matches[1])
    }
    else {
        $Version = 'deploy-' + (Get-Date -Format 'yyyyMMdd-HHmmssfff')
    }
}
if ($Version -eq $previous.version) { throw 'La nuova versione deve essere diversa da quella corrente.' }

# 2. Shutdown controllato: il server chiede ai browser di mettere in pausa i circuit.
Write-Step 'Richiesta di shutdown controllato (POST /demo/shutdown)'
Invoke-RestMethod "$baseUrl/demo/shutdown" -Method Post -TimeoutSec 10 | Out-Null

# 3. Attesa della terminazione del processo (nessuna terminazione forzata).
Write-Step "Attesa della terminazione del processo $($previous.processId)"
if (-not $previousProcess.WaitForExit($TimeoutSeconds * 1000)) {
    throw "Timeout: PID $($previous.processId) ancora attivo. Nessuna terminazione forzata eseguita."
}
Write-Host '    Processo terminato.'

# 4. Nuova versione.
Write-Step "Nuova versione: $($previous.version) -> $Version"
$environment['DEMO_VERSION'] = $Version

# 5. Riavvio sugli stessi URL, eseguendo direttamente l'assembly già compilato.
Write-Step "Avvio della nuova istanza su $($applicationUrls -join ' ; ')"
$logDirectory = Join-Path $repository '.demo\logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmssfff'
$stdout = Join-Path $logDirectory "$stamp.stdout.log"
$stderr = Join-Path $logDirectory "$stamp.stderr.log"
$oldEnvironment = @{}
foreach ($key in $environment.Keys) {
    $oldEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
try {
    foreach ($key in $environment.Keys) {
        [Environment]::SetEnvironmentVariable($key, $environment[$key], 'Process')
    }
    $process = Start-Process -FilePath (Get-Command dotnet -CommandType Application | Select-Object -First 1).Source `
        -ArgumentList @(('"{0}"' -f $assembly), '--urls', ('"{0}"' -f ($applicationUrls -join ';'))) `
        -WorkingDirectory $project -NoNewWindow -PassThru `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
}
finally {
    foreach ($key in $oldEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($key, $oldEnvironment[$key], 'Process')
    }
}

# 6. Attesa della disponibilità.
Write-Step 'Attesa della disponibilità del server'
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
$lastHealthError = 'Nessuna risposta'
while ([DateTime]::UtcNow -lt $deadline) {
    $process.Refresh()
    if ($process.HasExited) {
        throw "Nuova istanza terminata con codice $($process.ExitCode). Consultare $stdout e $stderr."
    }
    try {
        $health = Invoke-RestMethod "$baseUrl/health" -TimeoutSec 2
    }
    catch {
        $lastHealthError = $_.Exception.Message
        Start-Sleep -Milliseconds 500
        continue
    }
    if ($health.status -eq 'ok' -and $health.version -eq $Version -and $health.processId -eq $process.Id) {
        Write-Host ''
        Write-Host "Deploy completato: versione $($previous.version) -> $Version, PID $($previous.processId) -> $($health.processId)" -ForegroundColor Green
        Write-Host "URL: $($applicationUrls -join ' ; ')"
        Write-Host "Log: $stdout"
        Write-Host "Errori: $stderr"
        Write-Host 'Torna al browser: la sessione riprende automaticamente con lo stato precedente.'
        return
    }
    throw "Risposta health inattesa: PID/versione non corrispondono alla nuova istanza ($($process.Id), $Version)."
}
throw "Timeout health: $lastHealthError. PID avviato: $($process.Id). Consultare $stdout e $stderr; nessuna terminazione forzata eseguita."