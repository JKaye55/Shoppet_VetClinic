const reconnectModal = document.getElementById("components-reconnect-modal");

if (reconnectModal) {
    reconnectModal.addEventListener("components-reconnect-state-changed", event => {
        const state = event.detail?.state;

        if (state === "show") {
            if (!reconnectModal.open) reconnectModal.showModal();
            return;
        }

        if (state === "hide") {
            if (reconnectModal.open) reconnectModal.close();
            return;
        }

        if (state === "rejected") {
            window.location.reload();
        }
    });
}
