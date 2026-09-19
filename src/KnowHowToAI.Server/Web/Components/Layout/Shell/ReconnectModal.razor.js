// Offizieller Anpassungsmechanismus der .NET-10-Reconnect-Oberfläche: Blazor
// setzt die Klassen components-reconnect-* auf #components-reconnect-modal und
// sendet "components-reconnect-state-changed". Dieses Modul steuert nur
// Darstellung (showModal/close), den deterministischen Fokus auf die sichere
// nächste Aktion und die Aktionen selbst; die Verbindungslogik bleibt im
// Framework (Blazor.reconnect / Blazor.resumeCircuit). Keine eigene
// SignalR-Verbindung, keine eigene Retryschleife.

const reconnectModal = document.getElementById("components-reconnect-modal");
reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);

const retryButton = document.getElementById("components-reconnect-button");
retryButton.addEventListener("click", retry);

const reloadButton = document.getElementById("components-reconnect-reload-button");
reloadButton.addEventListener("click", () => location.reload());

const resumeButton = document.getElementById("components-resume-button");
resumeButton.addEventListener("click", resume);

// Ein Schließen mit Escape würde die blockierende Oberfläche ohne
// Wiederherstellung verlassen; der Fokus bleibt deshalb im Dialog.
reconnectModal.addEventListener("cancel", (event) => event.preventDefault());

// Reload warnt nur bei tatsächlich ungespeicherten Änderungen: die
// Kontextleiste trägt den Dirty-Zustand als data-ktai-dirty-Attribut
// (einzige Wahrheit ist der ViewModel-Vertrag, auch im Prerendering
// vorhanden); fehlt das Element, gilt die Seite als nicht dirty; eine
// persistierte Transaction gilt nie als ungespeichert.
document.addEventListener("beforeunload", (event) => {
    if (document.querySelector("[data-ktai-dirty]")?.dataset.ktaiDirty !== "true") {
        return;
    }
    event.preventDefault();
    event.returnValue = "";
});

function handleReconnectStateChanged(event) {
    const state = event.detail.state;
    if (state === "hide") {
        reconnectModal.close();
        return;
    }

    openModal();
    if (state === "failed") {
        // Ein einzelner Versuch, sobald das Dokument wieder sichtbar wird;
        // regelmäßig wiederholt Blazor im Rahmen seines begrenzten Retryplans.
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
        focusPrimaryAction(retryButton);
    } else if (state === "rejected") {
        focusPrimaryAction(reloadButton);
    } else if (state === "resume-failed") {
        focusPrimaryAction(resumeButton);
    }
}

function openModal() {
    if (!reconnectModal.open) {
        reconnectModal.showModal();
    }
}

function focusPrimaryAction(button) {
    if (button.offsetParent !== null) {
        button.focus();
    }
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);

    try {
        // Reconnect liefert: true = Erfolg, false = Server erreichbar, aber
        // Circuit abgelehnt (unbekannt/abgelaufen), Exception = Server unerreichbar.
        const successful = await Blazor.reconnect();
        if (!successful) {
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else {
                reconnectModal.close();
            }
        }
    } catch (err) {
        // Server momentan unerreichbar; erneut versuchen, sobald das Dokument
        // wieder sichtbar wird.
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
