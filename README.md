# WPC 2026 – Le novità di Blazor in .NET 11

Demo della sessione **"Blazor .NET 11"** di WPC 2026 (45 minuti). Ogni pagina mostra una novità di Blazor
in .NET 11 con un esempio realistico, piccolo e facile da spiegare dal vivo (dominio: 🍕 la pizzeria **"Blazor da Asporto"**: ordini, cucina, rider, menu e pizza del mese).

La soluzione `Dottor.WPC2026.slnx` contiene tre progetti:

- `src/Dottor.WPC2026.Web`: Blazor Web App (host), con interattività **per pagina** (Static SSR, Interactive Server e Interactive WebAssembly);
- `src/Dottor.WPC2026.Web.Client`: pagine e servizi eseguiti in **WebAssembly** (demo 5 e 6);
- `src/Dottor.WPC2026.WebWorker`: libreria creata con il template `blazorwebworker` (demo 5).

I dati sono in memoria: non servono database né container.

## Requisiti

- **.NET 11 SDK** `11.0.100` (fissato in `global.json`, con roll forward sulle patch). Verifica con `dotnet --version`.
- Windows, macOS o Linux con un browser moderno.
- PowerShell 7 (`pwsh`) per lo script di deployment.
- Certificato di sviluppo HTTPS: `dotnet dev-certs https --trust`.

## Compilazione e avvio

```powershell
dotnet restore Dottor.WPC2026.slnx
dotnet build Dottor.WPC2026.slnx
dotnet run --project src/Dottor.WPC2026.Web --launch-profile https
```

L'app è disponibile su <https://localhost:7255> (profilo `http`: <http://localhost:5050>).

## Demo

Le demo seguono i capitoli della sessione.

| # | Capitolo | Demo | URL | Novità mostrata |
|---|----------|------|-----|-----------------|
| 1 | Circuit | Circuit pause / resume | `/demo/circuit-pause` | Pausa dei circuit allo shutdown (`Circuit.RequestCircuitPauseAsync`) con `[PersistentState]`, `Blazor.pauseCircuit()` (.NET 10) e AutoPause con `WithBrowserOptions` |
| 2 | Form e Static SSR | Validazione asincrona | `/demo/async-validation` | `AsyncValidationAttribute` + stato "pending" nell'`EditForm` |
| 3 | Form e Static SSR | Validazione Static SSR | `/demo/static-ssr-validation` | Validazione client-side, PRG, `[SupplyParameterFromTempData]` e `[SupplyParameterFromSession]` |
| 4 | Form e Static SSR | CacheView | `/demo/cacheview` | `<CacheView>`: `VaryByQuery`, `ExpiresAfter`, `CacheKey` |
| 5 | WebAssembly | Web Worker | `/demo/web-worker` | Template `blazorwebworker`: analisi di un CSV nel worker vs sul thread della UI |
| 6 | WebAssembly | IHostedService | `/demo/hosted-service` | `BackgroundService` registrato con `AddHostedService` nel client WebAssembly |
| 7 | Componenti | Virtualize | `/demo/virtualize` | `AnchorMode`, `ScrollToItemAsync`, elementi a altezza variabile |
| 8 | Componenti | QuickGrid | `/demo/quickgrid` | Ordinamento e paginazione nell'URL, `OnRowClick` |
| 9 | Agentic UI | Chat minimale | `/demo/chat` | `<ChatPage>` + `UIAgent` sull'`IChatClient` di Microsoft.Extensions.AI: una chat completa in poche righe |
| 10 | Agentic UI | Agentic UI | `/demo/agentic-ui` | `Microsoft.AspNetCore.Components.AI`: stato condiviso, UI action, tool server, approvazioni |
| – | Extra | Altre novità | `/demo/other-features` | `DisplayName`, `EnvironmentView`, `WithBrowserOptions`, antiforgery automatico, `ConfigureConnection`, `NavLink` relativo |
Tutte le demo sono raggiungibili dalle card della homepage.

## Modello AI (demo 9 e 10)

Le demo AI usano **Microsoft.Extensions.AI**: un `IChatClient` creato dal client OpenAI e puntato sull'endpoint v1
di **Microsoft Foundry** (`AI:Endpoint` = `https://blazor-ai-resource.openai.azure.com/openai/v1`,
deployment `AI:Model` = `gpt-6.1-sol`, entrambi in `appsettings.json`). La chiave API va **solo** nei User Secrets:

```powershell
dotnet user-secrets --project src/Dottor.WPC2026.Web set "AI:ApiKey" "<chiave>"
```

In alternativa usa la variabile d'ambiente `AI__ApiKey`. Senza chiave l'app parte comunque: il badge della homepage
indica "chiave API mancante" e la chat mostra un errore con le istruzioni. Non salvare mai la chiave nel repository.

## Script di deployment (demo 1)

`scripts/deploy-demo.ps1` simula un rilascio mentre il browser è aperto:

1. legge versione e PID da `/health` (sempre su `http://127.0.0.1:<porta http del profilo>`);
2. chiede uno shutdown controllato (`POST /demo/shutdown` con header `X-Demo-Deployment: local`, solo in Development e da loopback);
3. l'app mette in pausa i circuit: lo stato persistente viene inviato al browser;
4. attende la fine del processo e avvia una nuova istanza con `DEMO_VERSION` incrementata,
   in ascolto su tutti gli URL del profilo (con `https`: `https://localhost:7255` e `http://localhost:5050`);
5. il modal di riconnessione rileva il server e chiama `Blazor.resumeCircuit()`.

Lo script non termina mai i processi in modo forzato: se lo shutdown supera il timeout si ferma con un errore.

```powershell
# app già avviata con: dotnet run --project src/Dottor.WPC2026.Web --launch-profile https
pwsh ./scripts/deploy-demo.ps1                     # profilo https (default), nessun parametro necessario
pwsh ./scripts/deploy-demo.ps1 -LaunchProfile http
pwsh ./scripts/deploy-demo.ps1 -Version v42 -Configuration Release
```

Compila prima di eseguire lo script: la nuova istanza esegue direttamente
`bin/<Configuration>/net11.0/Dottor.WPC2026.Web.dll` con `--urls` del profilo.
L'output della nuova istanza viene scritto in `.demo/logs/`.
La nuova istanza resta attiva in background: fermala dal Task Manager o con `Stop-Process -Id <PID>`
(il PID è mostrato nella pagina e restituito da `/health`).

