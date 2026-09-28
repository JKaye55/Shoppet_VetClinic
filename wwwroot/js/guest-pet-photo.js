window.shoppetGuestPetPhoto = (() => {
    let currentUrl = null;

    function releaseCurrentUrl() {
        if (currentUrl) {
            URL.revokeObjectURL(currentUrl);
            currentUrl = null;
        }
    }

    function preview(inputElement, imageElement) {
        if (!inputElement || !inputElement.files || inputElement.files.length === 0 || !imageElement) {
            return false;
        }

        const file = inputElement.files[0];

        releaseCurrentUrl();
        currentUrl = URL.createObjectURL(file);
        imageElement.src = currentUrl;
        return true;
    }

    function apply(imageElement) {
        if (imageElement && currentUrl) {
            imageElement.src = currentUrl;
            return true;
        }

        return false;
    }

    function clear(inputElement) {
        releaseCurrentUrl();

        if (inputElement) {
            inputElement.value = "";
        }
    }

    window.addEventListener("beforeunload", releaseCurrentUrl);

    return {
        preview,
        apply,
        clear
    };
})();