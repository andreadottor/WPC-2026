# WPC 2026 – Le novità di Blazor in .NET 11

Demo della sessione **"Blazor .NET 11"** di WPC 2026 (45 minuti). Ogni pagina mostra una novità di Blazor
in .NET 11 con un esempio realistico, piccolo e facile da spiegare dal vivo (dominio: ticket e documenti di progetto).

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
| 9 | Agentic UI | Agentic UI | `/demo/agentic-ui` | `Microsoft.AspNetCore.Components.AI`: stato condiviso, UI action, tool server, approvazioni |
| – | Extra | Altre novità | `/demo/other-features` | `DisplayName`, `EnvironmentView`, `WithBrowserOptions`, antiforgery automatico, `ConfigureConnection`, `NavLink` relativo |
Tutte le demo sono raggiungibili dalle card della homepage.

## Provider AI (demo 9)

Per default la demo usa un **client simulato** (`AI:Provider = "Simulated"`): risposte deterministiche,
nessuna chiamata esterna, nessuna chiave. È la modalità consigliata sul palco. Il client simulato riconosce
parole chiave nel prompt: *tecnico, sicurezza, titolo, chiarezza/migliora* → proposta di modifica;
*riassumi/analizza* → tool server di analisi; *pubblica* → tool con approvazione.

Per un modello reale (qualsiasi endpoint compatibile OpenAI: OpenAI, Azure OpenAI v1, GitHub Models):

```powershell
dotnet user-secrets --project src/Dottor.WPC2026.Web set "AI:Provider" "OpenAI"
dotnet user-secrets --project src/Dottor.WPC2026.Web set "AI:Endpoint" "https://models.github.ai/inference"
dotnet user-secrets --project src/Dottor.WPC2026.Web set "AI:Model" "openai/gpt-4.1-mini"
dotnet user-secrets --project src/Dottor.WPC2026.Web set "AI:ApiKey" "<token>"
```

In alternativa usa le variabili d'ambiente `AI__Provider`, `AI__Endpoint`, `AI__Model`, `AI__ApiKey`.
Senza `ApiKey` l'app torna alla modalità simulata. Il badge in alto nella pagina indica la modalità attiva.
Non salvare mai la chiave nel repository.

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

### Pagina di manutenzione

`/maintenance` (voce **Strumenti → Manutenzione** nel menu) mostra in tempo reale i circuiti connessi tracciati da
`CircuitShutdownService` e permette di arrestare l'applicazione dalla UI con `IHostApplicationLifetime.StopApplication()`.
Parte lo stesso shutdown controllato dello script: i circuiti vanno in pausa e salvano lo stato. Lo script però avvia anche
la nuova istanza, la pagina no: dopo l'arresto riavvia l'app a mano. Il pulsante è abilitato solo in Development.

## Preparazione delle demo in conferenza

Prima della sessione: `dotnet build`, avvia l'app con il profilo `https`, apri la homepage e verifica il
badge "Modalità dimostrativa" nella demo 9.

1. **Circuit pause** – Incrementa il contatore e scrivi titolo/descrizione del ticket. Esegui
   `pwsh ./scripts/deploy-demo.ps1`. *Atteso:* compare il modal di pausa, poi la sessione riprende da sola;
   versione e PID cambiano, contatore e bozza sono intatti e compare il messaggio "Stato ripristinato!".
   Poi premi `Blazor.pauseCircuit()` e *Riprendi*; infine nascondi la scheda per più di 10 secondi (AutoPause, `AutoPause:HiddenDelay`).
2. **Validazione asincrona** – Digita `mario.rossi@example.com` (già registrata), `errore@example.com`
   (errore del servizio) e un'email libera.
   *Atteso:* indicatore "verifica in corso", poi il messaggio corretto; il submit attende la validazione.
3. **Static SSR** – Invia il form vuoto, poi un ticket valido (due volte). *Atteso:* errori di validazione senza circuit;
   dopo l'invio redirect (PRG), messaggio TempData mostrato una sola volta e l'elenco "I tuoi ticket in questa sessione" che cresce.
4. **CacheView** – Ricarica la pagina, cambia categoria, prova "software + utm".
   *Atteso:* il report in cache non cambia numero di generazione per 20 secondi; `utm` non crea una nuova voce.
5. **Web Worker** – Attendi "pronto", poi analizza sul thread della UI e nel Web Worker.
   *Atteso:* nel primo caso pallina e orologio si bloccano, nel secondo restano fluidi.
6. **IHostedService** – Osserva il contatore degli aggiornamenti, crea un ticket nella demo 3, cambia pagina e torna.
   *Atteso:* il totale si aggiorna; il servizio non riparte (orario di avvio invariato).
7. **Virtualize** – Cambia `AnchorMode`, premi "Flusso in cima", inserisci un indice e premi "Vai". Avvia il log viewer.
   *Atteso:* la posizione resta stabile quando arrivano nuovi elementi; il log resta ancorato in fondo.
8. **QuickGrid** – Ordina per priorità, cambia pagina, clicca una riga e torna indietro.
   *Atteso:* `sort`, `direction` e `page` nell'URL; i pulsanti Indietro/Avanti del browser ripristinano la griglia.
9. **Agentic UI** – Usa i suggerimenti: "Rendi la descrizione più tecnica" (diff + Accetta/Rifiuta),
   "Riassumi il documento" (card del tool server), "Pubblica il documento" (approvazione).
   *Atteso:* il documento cambia solo dopo "Accetta"; la pubblicazione avviene solo dopo "Approva".
10. **Altre novità** – Mostra `DisplayName`, `EnvironmentView`, le opzioni del browser lette dal server (inclusa AutoPause)
    e la nota sull'antiforgery.

## Limitazioni

- **`Microsoft.AspNetCore.Components.AI` è sperimentale** (`0.1.0-preview`): API e componenti potrebbero cambiare.
  Le risposte del client simulato sono predefinite; con un modello reale testo e scelta dei tool variano.
- Il ripristino dello stato del circuit (demo 1) funziona per uno shutdown controllato: se il processo viene
  terminato bruscamente lo stato non persistito va perso. Lo stato vive solo nel browser che lo ha ricevuto.
- `[SupplyParameterFromTempData]` riscrive il valore in TempData alla fine di ogni richiesta: per un messaggio
  "mostra una volta" il codice copia il valore in un campo e azzera la proprietà.
- `[SupplyParameterFromSession]` richiede `AddSession()` e `UseSession()`; qui la sessione usa la cache distribuita in memoria.
- Il Web Worker riceve e restituisce stringhe (`[JSExport]`): l'annullamento interrompe solo l'attesa, non il calcolo nel worker.
- Il servizio della demo 6 vive nel runtime WebAssembly della scheda e si ferma quando la scheda viene chiusa o ricaricata.
- Tutti i dati sono in memoria e si azzerano al riavvio.

