// Set up event handlers
const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

const autoResumeStatus = document.getElementById("components-auto-resume-status");
let autoResumeTimer = null;

function handleReconnectStateChanged(event) {
    if (event.detail.state === "show") {
        reconnectModal.showModal();
    } else if (event.detail.state === "hide") {
        stopAutoResume();
        reconnectModal.close();
    } else if (event.detail.state === "paused") {
        // Demo 1: il server ha messo in pausa il circuito (es. durante un deploy).
        // Lo stato è già nel browser: aspettiamo che il server torni e riprendiamo da soli.
        if (!reconnectModal.open) {
            reconnectModal.showModal();
        }
        if (event.detail.remote) {
            startAutoResume();
        }
    } else if (event.detail.state === "failed") {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } else if (event.detail.state === "rejected") {
        location.reload();
    }
}

function startAutoResume() {
    stopAutoResume();
    let attempts = 0;
    autoResumeTimer = setInterval(async () => {
        attempts++;
        autoResumeStatus.textContent = `In attesa del server... (${attempts}s)`;
        try {
            const response = await fetch(new URL("health", document.baseURI), { cache: "no-store" });
            if (!response.ok) {
                return;
            }
            const health = await response.json();
            stopAutoResume();
            autoResumeStatus.textContent = `Server disponibile (${health.version}, PID ${health.processId}): ripresa della sessione...`;
            await resume();
        } catch {
            // Server non ancora disponibile: riproviamo al prossimo giro.
        }
    }, 1000);
}

function stopAutoResume() {
    if (autoResumeTimer) {
        clearInterval(autoResumeTimer);
        autoResumeTimer = null;
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
            // We'll reload the page so the user can continue using the app as quickly as possible.
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // We got an exception, server is currently unavailable
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    }
}

async function resume() {
    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
