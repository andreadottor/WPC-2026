# WPC 2026 – Le novità di Blazor in .NET 11

Demo della sessione **"Blazor .NET 11"** di WPC 2026 (45 minuti). Ogni pagina mostra una novità di Blazor
in .NET 11 con un esempio realistico, piccolo e facile da spiegare dal vivo (dominio: ticket e documenti di progetto).

La soluzione `Dottor.WPC2026.slnx` contiene un solo progetto: `src/Dottor.WPC2026.Web`, una Blazor Web App
con interattività **Interactive Server** per pagina. I dati sono in memoria: non servono database né container.

## Requisiti

- **.NET 11 SDK RC 3** (`11.0.100-rc.3.26522.148` o successivo). Verifica con `dotnet --version`.
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

| # | Demo | URL | Novità mostrata |
|---|------|-----|-----------------|
| 1 | Circuit pause / resume | `/demo/circuit-pause` | Pausa dei circuit allo shutdown (`Circuit.RequestCircuitPauseAsync`) e ripristino con `[PersistentState]` |
| 2 | Validazione asincrona | `/demo/async-validation` | `AsyncValidationAttribute` + stato "pending" nell'`EditForm` |
| 3 | Validazione Static SSR | `/demo/static-ssr-validation` | Form SSR con validazione, PRG e `[SupplyParameterFromTempData]` |
| 4 | Virtualize | `/demo/virtualize` | `AnchorMode`, `ScrollToItemAsync`, elementi a altezza variabile |
| 5 | QuickGrid | `/demo/quickgrid` | Ordinamento e paginazione nell'URL, `OnRowClick` |
| 6 | Agentic UI | `/demo/agentic-ui` | `Microsoft.AspNetCore.Components.AI`: stato condiviso, UI action, tool server, approvazioni |
| – | Altre novità | `/demo/other-features` | `DisplayName`, `EnvironmentView`, `CacheView`, TempData, `WithBrowserOptions`, `ConfigureConnection`, `NavLink` relativo |

Tutte le demo sono raggiungibili dalle card della homepage.

## Provider AI (demo 6)

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

1. legge versione e PID da `/health`;
2. chiede uno shutdown controllato (`POST /demo/shutdown`, solo in Development e da localhost);
3. l'app mette in pausa i circuit: lo stato persistente viene inviato al browser;
4. attende la fine del processo e avvia una nuova istanza con `DEMO_VERSION` incrementata;
5. il modal di riconnessione rileva il server e chiama `Blazor.resumeCircuit()`.

```powershell
# app già avviata con: dotnet run --project src/Dottor.WPC2026.Web --launch-profile https
pwsh ./scripts/deploy-demo.ps1                     # profilo https (default)
pwsh ./scripts/deploy-demo.ps1 -LaunchProfile http
pwsh ./scripts/deploy-demo.ps1 -NewVersion v42 -LogFile ./deploy.log
```

Compila prima di eseguire lo script: la nuova istanza parte con `dotnet run --no-build`.
La nuova istanza resta attiva in background: fermala dal Task Manager o con `Stop-Process -Id <PID>`
(il PID è mostrato nella pagina e restituito da `/health`).

## Preparazione delle demo in conferenza

Prima della sessione: `dotnet build`, avvia l'app con il profilo `https`, apri la homepage e verifica il
badge "Modalità dimostrativa" nella demo 6.

1. **Circuit pause** – Incrementa il contatore e scrivi titolo/descrizione del ticket. Esegui
   `pwsh ./scripts/deploy-demo.ps1`. *Atteso:* compare il modal di pausa, poi la sessione riprende da sola;
   versione e PID cambiano, contatore e bozza sono intatti e compare il messaggio "Stato ripristinato!".
2. **Validazione asincrona** – Digita `mario.rossi@example.com` (già registrata), `errore@example.com`
   (errore del servizio) e un'email libera.
   *Atteso:* indicatore "verifica in corso", poi il messaggio corretto; il submit attende la validazione.
3. **Static SSR** – Invia il form vuoto, poi un ticket valido. *Atteso:* errori di validazione senza circuit;
   dopo l'invio redirect (PRG) e messaggio flash mostrato una sola volta (ricarica la pagina per verificarlo).
4. **Virtualize** – Cambia `AnchorMode`, premi "Flusso in cima", inserisci un indice e premi "Vai". Avvia il log viewer.
   *Atteso:* la posizione resta stabile quando arrivano nuovi elementi; il log resta ancorato in fondo.
5. **QuickGrid** – Ordina per priorità, cambia pagina, clicca una riga e torna indietro.
   *Atteso:* `sort`, `direction` e `page` nell'URL; i pulsanti Indietro/Avanti del browser ripristinano la griglia.
6. **Agentic UI** – Usa i suggerimenti: "Rendi la descrizione più tecnica" (diff + Accetta/Rifiuta),
   "Riassumi il documento" (card del tool server), "Pubblica il documento" (approvazione).
   *Atteso:* il documento cambia solo dopo "Accetta"; la pubblicazione avviene solo dopo "Approva".
7. **Altre novità** – Mostra `DisplayName`, `EnvironmentView`, le opzioni del browser lette dal server,
   il flash TempData e il `CacheView` (l'orario resta fermo per 30 secondi).

## Limitazioni

- **.NET 11 è in preview (RC 3)**: le API possono cambiare prima del rilascio finale.
- **`Microsoft.AspNetCore.Components.AI` è sperimentale** (`0.1.0-preview`): API e componenti potrebbero cambiare.
  Le risposte del client simulato sono predefinite; con un modello reale testo e scelta dei tool variano.
- Il ripristino dello stato del circuit (demo 1) funziona per uno shutdown controllato: se il processo viene
  terminato bruscamente lo stato non persistito va perso. Lo stato vive solo nel browser che lo ha ricevuto.
- `[SupplyParameterFromTempData]` riscrive il valore in TempData alla fine di ogni richiesta: per un messaggio
  "mostra una volta" il codice copia il valore in un campo e azzera la proprietà.
- L'auto-pausa dei circuit inattivi (pacchetto separato) non è inclusa.
- Tutti i dati sono in memoria e si azzerano al riavvio.

