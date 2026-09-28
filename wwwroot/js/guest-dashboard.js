window.shoppetGuestDashboard = (() => {
    const key = "shoppetcare.guestPet.v2";
    let photoUrl = null;

    const breeds = {
        Dog: ["Shih Tzu","Golden Retriever","Beagle","Poodle","Aspin / Mixed Breed","Other Dog Breed"],
        Cat: ["Persian","Siamese","Domestic Shorthair","Puspin / Mixed Breed","British Shorthair","Other Cat Breed"],
        Rabbit: ["Holland Lop","Lionhead","Mini Rex","Mixed Breed Rabbit","Netherland Dwarf","Other Rabbit Breed"],
        Bird: ["Lovebird","Parakeet","Cockatiel","Canary","African Grey","Other Bird"]
    };

    function el(id) { return document.getElementById(id); }

    function readState() {
        try {
            return JSON.parse(sessionStorage.getItem(key) || "null");
        } catch {
            return null;
        }
    }

    function writeState(state) {
        sessionStorage.setItem(key, JSON.stringify(state));
    }

    function setMessage(message, isError = false) {
        const box = el("gdMessage");
        if (!box) return;
        box.textContent = message || "";
        box.style.display = message ? "block" : "none";
        box.classList.toggle("error", !!isError);
    }

    function updateBreeds(species, selectedBreed = "") {
        const wrap = el("gdBreeds");
        if (!wrap) return;

        wrap.innerHTML = "";
        (breeds[species] || []).forEach(value => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "gd-choice" + (value === selectedBreed ? " active" : "");
            button.textContent = value;
            button.dataset.breed = value;
            button.onclick = () => selectBreed(value);
            wrap.appendChild(button);
        });
    }

    function selectSpecies(value) {
        document.querySelectorAll("[data-species]").forEach(button => {
            button.classList.toggle("active", button.dataset.species === value);
        });
        const hidden = el("gdSpeciesValue");
        if (hidden) hidden.value = value;
        updateBreeds(value);
        setMessage("");
    }

    function selectBreed(value) {
        document.querySelectorAll("[data-breed]").forEach(button => {
            button.classList.toggle("active", button.dataset.breed === value);
        });
        const hidden = el("gdBreedValue");
        if (hidden) hidden.value = value;
        setMessage("");
    }

    function openEditor() {
        const editor = el("gdEditor");
        const empty = el("gdEmpty");
        const saved = el("gdSaved");
        if (editor) editor.style.display = "grid";
        if (empty) empty.style.display = "none";
        if (saved) saved.style.display = "none";

        const state = readState();
        const name = el("gdPetName");
        if (state) {
            if (name) name.value = state.name || "";
            const species = state.species || "Dog";
            const breed = state.breed || "";
            selectSpecies(species);
            if (breed) {
                setTimeout(() => selectBreed(breed), 0);
            }
        } else {
            selectSpecies("Dog");
        }
        applyPhotoToEditor();
    }

    function cancelEditor() {
        const state = readState();
        if (state) showSaved(state);
        else {
            const editor = el("gdEditor");
            const empty = el("gdEmpty");
            if (editor) editor.style.display = "none";
            if (empty) empty.style.display = "block";
        }
        setMessage("");
    }

    function save() {
        const name = (el("gdPetName")?.value || "").trim();
        const species = el("gdSpeciesValue")?.value || "";
        const breed = el("gdBreedValue")?.value || "";

        if (!name) {
            setMessage("Enter your pet's name before saving.", true);
            el("gdPetName")?.focus();
            return;
        }
        if (name.length > 50) {
            setMessage("Pet name cannot exceed 50 characters.", true);
            return;
        }
        if (!species || !breed) {
            setMessage("Choose a species and breed before saving.", true);
            return;
        }

        const state = { name, species, breed };
        writeState(state);
        showSaved(state);
        setMessage("");
    }

    function showSaved(state) {
        const editor = el("gdEditor");
        const empty = el("gdEmpty");
        const saved = el("gdSaved");
        if (editor) editor.style.display = "none";
        if (empty) empty.style.display = "none";
        if (saved) saved.style.display = "block";

        const name = el("gdSavedName");
        const details = el("gdSavedDetails");
        const fallback = el("gdSavedFallback");

        if (name) name.textContent = state.name || "Pet";
        if (details) details.textContent = [state.species, state.breed].filter(Boolean).join(" · ");
        if (fallback) fallback.textContent = (state.name || "P").trim().charAt(0).toUpperCase();

        applyPhotoToSaved();
    }

    function previewPhoto(input) {
        const file = input?.files?.[0];
        if (!file) return;

        const allowed = ["image/jpeg","image/png","image/webp"];
        if (!allowed.includes((file.type || "").toLowerCase())) {
            input.value = "";
            setMessage("Use a JPG, PNG or WEBP pet photo.", true);
            return;
        }
        if (file.size > 5 * 1024 * 1024) {
            input.value = "";
            setMessage("Choose a photo smaller than 5 MB.", true);
            return;
        }

        if (photoUrl) URL.revokeObjectURL(photoUrl);
        photoUrl = URL.createObjectURL(file);
        applyPhotoToEditor();
        applyPhotoToSaved();
        setMessage("");
    }

    function removePhoto() {
        if (photoUrl) URL.revokeObjectURL(photoUrl);
        photoUrl = null;
        const input = el("gdPhotoInput");
        if (input) input.value = "";
        applyPhotoToEditor();
        applyPhotoToSaved();
    }

    function applyPhotoToEditor() {
        const img = el("gdEditorPhoto");
        const placeholder = el("gdPhotoPlaceholder");
        const remove = el("gdRemovePhoto");
        if (img) {
            img.style.display = photoUrl ? "block" : "none";
            if (photoUrl) img.src = photoUrl;
            else img.removeAttribute("src");
        }
        if (placeholder) placeholder.style.display = photoUrl ? "none" : "grid";
        if (remove) remove.style.display = photoUrl ? "inline-flex" : "none";
    }

    function applyPhotoToSaved() {
        const img = el("gdSavedPhoto");
        const fallback = el("gdSavedFallback");
        if (img) {
            img.style.display = photoUrl ? "block" : "none";
            if (photoUrl) img.src = photoUrl;
            else img.removeAttribute("src");
        }
        if (fallback) fallback.style.display = photoUrl ? "none" : "grid";
    }

    function init() {
        if (!el("guestDashboardRoot")) return;
        const state = readState();
        if (state?.name) showSaved(state);
        else {
            const empty = el("gdEmpty");
            const editor = el("gdEditor");
            const saved = el("gdSaved");
            if (empty) empty.style.display = "block";
            if (editor) editor.style.display = "none";
            if (saved) saved.style.display = "none";
        }
    }

    const observer = new MutationObserver(() => init());
    observer.observe(document.documentElement, { childList: true, subtree: true });
    window.addEventListener("pageshow", init);
    document.addEventListener("DOMContentLoaded", init);
    window.addEventListener("beforeunload", () => {
        if (photoUrl) URL.revokeObjectURL(photoUrl);
    });

    return { init, openEditor, cancelEditor, save, selectSpecies, selectBreed, previewPhoto, removePhoto };
})();