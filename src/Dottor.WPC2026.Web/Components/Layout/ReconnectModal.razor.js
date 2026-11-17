// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);
const diagnostic = document.getElementById("resume-diagnostic");
let autoResumeRunning = false;
let resumeRunning = false;

function handleReconnectStateChanged(event) {
    const state = event.detail.state;
    if (["paused", "resume-failed"].includes(state)) {
        // remote === true only when the server requested the pause (deployment);
        // false when the browser paused the circuit (auto-pause on hidden tab).
        reconnectModal.dataset.pauseOrigin = event.detail.remote ? "server" : "client";
        console.info(`[demo] ${new Date().toLocaleTimeString()} circuito ${state}, origine: ${reconnectModal.dataset.pauseOrigin}`);
    }
    if (["show", "paused", "resume-failed"].includes(state)) {
        reconnectModal.showModal();
        // A client pause is resumed by the auto-pause package when the tab becomes visible again:
        // polling /health here would resume the circuit while the tab is still hidden.
        if (state === "resume-failed" || (state === "paused" && event.detail.remote)) {
            void autoResume();
        }
    } else if (state === "hide") {
        if (reconnectModal.dataset.pauseOrigin) {
            console.info(`[demo] ${new Date().toLocaleTimeString()} circuito ripreso dopo pausa ${reconnectModal.dataset.pauseOrigin}`);
        }
        delete reconnectModal.dataset.pauseOrigin;
        reconnectModal.close();
        diagnostic.textContent = "";
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        diagnostic.textContent = "Circuito non recuperabile. Ricaricare crea una nuova sessione e perde la bozza non ripristinata.";
        reconnectModal.showModal();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        // Reconnect will asynchronously return:
        // - true to mean success
        // - false to mean we reached the server, but it rejected the connection (e.g., unknown circuit ID)
        // - exception to mean we didn't reach the server (this can be sync or async)
        const successful = await Blazor.reconnect();
        if (!successful) {
            // We have been able to reach the server, but the circuit is no longer available.
            // Try to resume it from the state persisted in the browser instead of reloading the page.
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                diagnostic.textContent = "Stato non ripristinabile. Aggiornare la pagina per avviare una nuova sessione.";
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        console.warn("Server non disponibile per la riconnessione", err);
        // We got an exception, server is currently unavailable
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    if (resumeRunning) return;
    resumeRunning = true;
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            diagnostic.textContent = reconnectModal.dataset.pauseOrigin === "client"
                ? "Ripristino rifiutato. Lo stato persistito potrebbe non essere più valido: ricaricare crea una nuova sessione."
                : "Ripristino rifiutato. Non ricaricare finché il deployment non è terminato.";
        } else {
            reconnectModal.close();
            diagnostic.textContent = "";
        }
    } catch (error) {
        console.warn("Ripresa del circuito non riuscita", error);
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    } finally {
        resumeRunning = false;
    }
}

async function autoResume() {
    if (autoResumeRunning) return;
    autoResumeRunning = true;
    try {
        for (let attempt = 1; attempt <= 90 && reconnectModal.open; attempt++) {
            await new Promise(resolve => setTimeout(resolve, 1000));
            try {
                const response = await fetch(new URL("health", document.baseURI), {
                    cache: "no-store", signal: AbortSignal.timeout(2000)
                });
                if (response.ok) await resume();
            } catch (error) {
                if (!(error instanceof TypeError) && error.name !== "TimeoutError") {
                    console.error("Errore controllo disponibilita server", error);
                    diagnostic.textContent = "Controllo server non riuscito: consultare la console del browser.";
                    break;
                }
            }
        }
        if (reconnectModal.open) diagnostic.textContent = "Ripresa automatica terminata. Verifica il server e premi Riprendi.";
    } finally {
        autoResumeRunning = false;
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
