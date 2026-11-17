<#
.SYNOPSIS
	Simula un deployment "a caldo" dell'app web della demo.
.DESCRIPTION
	Ferma in modo controllato l'istanza in esecuzione e ne avvia una nuova (con versione
	diversa) sugli stessi URL, verificando tramite /health che il cambio sia avvenuto.
	L'applicazione deve essere già compilata e in esecuzione prima di lanciare lo script.
#>
[CmdletBinding()]
param(
	[string]$LaunchProfile = 'https',
	[string]$Urls,
	[string]$Version,
	[ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
	[ValidateRange(10, 300)][int]$TimeoutSeconds = 90
)

# Qualsiasi errore interrompe lo script: meglio fermarsi che lasciare la demo in uno stato incoerente.
$ErrorActionPreference = 'Stop'
$repository = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repository 'src\Dottor.WPC2026.Web'
$assembly = Join-Path $project "bin\$Configuration\net11.0\Dottor.WPC2026.Web.dll"
$environment = @{ ASPNETCORE_ENVIRONMENT = 'Development' }

# Se gli URL non sono passati esplicitamente, li legge (insieme alle variabili d'ambiente)
# dal profilo indicato in launchSettings.json, così la nuova istanza replica la configurazione di Visual Studio.
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

# La nuova istanza deve ascoltare sugli stessi URL: ogni tab del browser può riprendere il circuito solo sulla propria origin.
$applicationUrls = @($Urls -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$httpUrl = $applicationUrls | ForEach-Object { [Uri]$_ } | Where-Object { $_.Scheme -eq 'http' } | Select-Object -First 1
if (-not $httpUrl) {
	throw 'Serve almeno un URL http: lo script lo usa per health check e shutdown.'
}
# Kestrel associa localhost e *.localhost al loopback, mentre PowerShell non risolve *.dev.localhost: si usa quindi 127.0.0.1.
$baseUrl = "http://127.0.0.1:$($httpUrl.Port)"

if (-not (Test-Path -LiteralPath $assembly)) {
	throw "Assembly non trovato: $assembly. Compilare prima con dotnet build -c $Configuration."
}

# Interroga l'istanza corrente per conoscerne PID e versione, verificando che sia davvero
# un processo .NET della demo prima di chiederne lo spegnimento.
try {
	$previous = Invoke-RestMethod "$baseUrl/health" -TimeoutSec 5
} catch {
	throw "L'applicazione non risponde su $baseUrl. Avviarla prima del deployment. Dettaglio: $($_.Exception.Message)"
}
if ($previous.status -ne 'Healthy' -or -not $previous.processId) {
	throw 'Health endpoint inatteso: deployment annullato.'
}
$previousProcess = Get-Process -Id $previous.processId -ErrorAction Stop
if ($previousProcess.ProcessName -notin @('dotnet', 'Dottor.WPC2026.Web')) {
	throw "Il PID $($previous.processId) non appartiene a un processo .NET atteso."
}
# Calcola la nuova versione se non specificata: incrementa "vN" -> "vN+1", altrimenti usa un timestamp.
if (-not $Version) {
	if ($previous.version -match '^v(\d+)$') {
		$Version = 'v' + (1 + [int]$Matches[1])
	} else {
		$Version = 'deploy-' + (Get-Date -Format 'yyyyMMdd-HHmmssfff')
	}
}
if ($Version -eq $previous.version) { throw 'La nuova versione deve essere diversa da quella corrente.' }

# Chiede all'app di spegnersi da sola (graceful shutdown) e attende l'uscita del processo.
# Volutamente non c'è alcun kill forzato: se il processo non termina, lo script si ferma.
Write-Host "Shutdown controllato di $($previous.version), PID $($previous.processId)..."
Invoke-RestMethod "$baseUrl/demo/shutdown" -Method Post -Headers @{ 'X-Demo-Deployment' = 'local' } -TimeoutSec 10 | Out-Null
if (-not $previousProcess.WaitForExit($TimeoutSeconds * 1000)) {
	throw "Timeout: PID $($previous.processId) ancora attivo. Nessuna terminazione forzata eseguita."
}
Write-Host 'Processo precedente terminato. Avvio della nuova istanza...'

# Prepara i file di log della nuova istanza (stdout/stderr) in .demo\logs.
$logDirectory = Join-Path $repository '.demo\logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmssfff'
$stdout = Join-Path $logDirectory "$stamp.stdout.log"
$stderr = Join-Path $logDirectory "$stamp.stderr.log"
# Start-Process eredita l'ambiente del processo corrente: le variabili (inclusa DEMO_VERSION)
# vengono impostate temporaneamente e poi ripristinate nel finally, per non sporcare la sessione PowerShell.
$environment['DEMO_VERSION'] = $Version
$oldEnvironment = @{}
foreach ($key in $environment.Keys) {
	$oldEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
try {
	foreach ($key in $environment.Keys) {
		[Environment]::SetEnvironmentVariable($key, $environment[$key], 'Process')
	}
	# Avvia la nuova istanza in background direttamente dalla DLL compilata, sugli stessi URL della precedente.
	$process = Start-Process -FilePath (Get-Command dotnet -CommandType Application).Source `
		-ArgumentList @(('"{0}"' -f $assembly), '--urls', ('"{0}"' -f ($applicationUrls -join ';'))) `
		-WorkingDirectory $project -NoNewWindow -PassThru `
		-RedirectStandardOutput $stdout -RedirectStandardError $stderr
} finally {
	foreach ($key in $oldEnvironment.Keys) {
		[Environment]::SetEnvironmentVariable($key, $oldEnvironment[$key], 'Process')
	}
}

# Polling di /health fino al timeout: il deployment è riuscito solo quando risponde
# il nuovo processo (PID corretto) con la nuova versione. Se il processo muore, si esce subito con errore.
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
$lastHealthError = 'Nessuna risposta'
while ([DateTime]::UtcNow -lt $deadline) {
	$process.Refresh()
	if ($process.HasExited) {
		throw "Nuova istanza terminata con codice $($process.ExitCode). Consultare $stdout e $stderr."
	}
	try {
		$health = Invoke-RestMethod "$baseUrl/health" -TimeoutSec 2
	} catch {
		$lastHealthError = $_.Exception.Message
		Start-Sleep -Milliseconds 500
		continue
	}
	if ($health.status -eq 'Healthy' -and $health.version -eq $Version -and $health.processId -eq $process.Id) {
		Write-Host "Deployment riuscito: $($previous.version) -> $Version; PID $($previous.processId) -> $($health.processId)."
		Write-Host "URL: $(($applicationUrls | ForEach-Object { $_.TrimEnd('/') + '/demo/circuit-pause' }) -join ' ; ')"
		Write-Host "Log: $stdout"
		Write-Host "Errori: $stderr"
		return
	}
	throw "Risposta health inattesa: PID/versione non corrispondono alla nuova istanza ($($process.Id), $Version)."
}
throw "Timeout health: $lastHealthError. PID avviato: $($process.Id). Consultare $stdout e $stderr; nessuna terminazione forzata eseguita."