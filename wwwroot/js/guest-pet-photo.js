window.shoppetGuestPetPhoto = (() => {
    let currentUrl = null;

    const allowedTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
    const maxBytes = 5 * 1024 * 1024;

    function byId(id) {
        return document.getElementById(id);
    }

    function releaseCurrentUrl() {
        if (currentUrl) {
            URL.revokeObjectURL(currentUrl);
            currentUrl = null;
        }
    }

    function setEditorState(hasPhoto, message = "") {
        const image = byId("guestPetEditorPhoto");
        const placeholder = byId("guestPetPhotoPlaceholder");
        const status = byId("guestPetPhotoStatus");
        const remove = byId("guestPetRemovePhoto");
        const buttonText = byId("guestPetPhotoButtonText");

        if (image) {
            image.style.display = hasPhoto ? "block" : "none";
            if (hasPhoto && currentUrl) image.src = currentUrl;
            if (!hasPhoto) image.removeAttribute("src");
        }

        if (placeholder) placeholder.style.display = hasPhoto ? "none" : "grid";
        if (remove) remove.style.display = hasPhoto ? "inline-flex" : "none";
        if (buttonText) buttonText.textContent = hasPhoto ? "Change Photo" : "Choose Photo";

        if (status) {
            status.textContent = message;
            status.style.display = message ? "block" : "none";
        }
    }

    function previewFromInput(inputElement) {
        const file = inputElement?.files?.[0];

        if (!file) {
            setEditorState(!!currentUrl);
            return;
        }

        if (!allowedTypes.has((file.type || "").toLowerCase())) {
            inputElement.value = "";
            setEditorState(!!currentUrl, "Use a JPG, PNG or WEBP pet photo.");
            return;
        }

        if (file.size > maxBytes) {
            inputElement.value = "";
            setEditorState(!!currentUrl, "Choose a photo smaller than 5 MB.");
            return;
        }

        releaseCurrentUrl();
        currentUrl = URL.createObjectURL(file);
        setEditorState(true);
    }

    function applyEditor() {
        setEditorState(!!currentUrl);
        return !!currentUrl;
    }

    function applySaved() {
        const image = byId("guestPetSavedPhoto");
        const fallback = byId("guestPetSavedFallback");

        if (image) {
            image.style.display = currentUrl ? "block" : "none";
            if (currentUrl) image.src = currentUrl;
            else image.removeAttribute("src");
        }

        if (fallback) fallback.style.display = currentUrl ? "none" : "grid";
        return !!currentUrl;
    }

    function clear() {
        releaseCurrentUrl();

        const input = byId("guestPetPhotoInput");
        if (input) input.value = "";

        setEditorState(false);
        applySaved();
    }

    function hasPhoto() {
        return !!currentUrl;
    }

    window.addEventListener("beforeunload", releaseCurrentUrl);

    return {
        previewFromInput,
        applyEditor,
        applySaved,
        clear,
        hasPhoto
    };
})();