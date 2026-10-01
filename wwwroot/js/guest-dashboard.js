window.shoppetGuestDashboard = (() => {
    const key = "shoppetcare.guestPet.v2";
    let photoUrl = null;
    let isEditing = false;
    let initialized = false;

    const breeds = {
        Dog: ["Shih Tzu", "Golden Retriever", "Beagle", "Poodle", "Aspin / Mixed Breed", "Other Dog Breed"],
        Cat: ["Persian", "Siamese", "Domestic Shorthair", "Puspin / Mixed Breed", "British Shorthair", "Other Cat Breed"],
        Rabbit: ["Holland Lop", "Lionhead", "Mini Rex", "Mixed Breed Rabbit", "Netherland Dwarf", "Other Rabbit Breed"],
        Bird: ["Lovebird", "Parakeet", "Cockatiel", "Canary", "African Grey", "Other Bird"]
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
        try {
            sessionStorage.setItem(key, JSON.stringify(state));
        } catch (e) {
            console.warn("Could not save to sessionStorage", e);
        }
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
        isEditing = true;
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
            if (state.photo) {
                photoUrl = state.photo;
            }
        } else {
            selectSpecies("Dog");
        }
        applyPhotoToEditor();
    }

    function cancelEditor() {
        isEditing = false;
        const state = readState();
        if (state && state.name) {
            showSaved(state);
        } else {
            const editor = el("gdEditor");
            const empty = el("gdEmpty");
            const saved = el("gdSaved");
            if (editor) editor.style.display = "none";
            if (empty) empty.style.display = "block";
            if (saved) saved.style.display = "none";
        }
        setMessage("");
    }

    function deletePet() {
        if (!confirm("Are you sure you want to delete your temporary guest pet?")) {
            return;
        }
        sessionStorage.removeItem(key);
        removePhoto();
        isEditing = false;

        const empty = el("gdEmpty");
        const editor = el("gdEditor");
        const saved = el("gdSaved");
        if (empty) empty.style.display = "block";
        if (editor) editor.style.display = "none";
        if (saved) saved.style.display = "none";

        const nameInput = el("gdPetName");
        if (nameInput) nameInput.value = "";
        setMessage("Guest pet removed. You can create a new temporary pet anytime.");
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

        const state = { name, species, breed, photo: photoUrl || "" };
        writeState(state);
        isEditing = false;
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

        if (state.photo) {
            photoUrl = state.photo;
        }
        applyPhotoToSaved();
    }

    function previewPhoto(input) {
        const file = input?.files?.[0];
        if (!file) return;

        const allowed = ["image/jpeg", "image/png", "image/webp"];
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

        const reader = new FileReader();
        reader.onload = (e) => {
            photoUrl = e.target.result;
            applyPhotoToEditor();
            applyPhotoToSaved();
            setMessage("");
        };
        reader.readAsDataURL(file);
    }

    function removePhoto() {
        photoUrl = null;
        const input = el("gdPhotoInput");
        if (input) input.value = "";
        applyPhotoToEditor();
        applyPhotoToSaved();

        const state = readState();
        if (state) {
            state.photo = "";
            writeState(state);
        }
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
        const root = el("guestDashboardRoot");
        if (!root) return;
        // Never reset while user is actively editing
        if (isEditing) return;

        const state = readState();
        if (state?.name) {
            showSaved(state);
        } else {
            const empty = el("gdEmpty");
            const editor = el("gdEditor");
            const saved = el("gdSaved");
            if (empty) empty.style.display = "block";
            if (editor) editor.style.display = "none";
            if (saved) saved.style.display = "none";
        }
        initialized = true;
    }

    // Initialize when DOM is ready or page is shown without constantly resetting on mutation
    window.addEventListener("pageshow", () => { isEditing = false; init(); });
    document.addEventListener("DOMContentLoaded", init);

    // Light check only if root appears dynamically after Blazor renders
    const checkInterval = setInterval(() => {
        if (el("guestDashboardRoot") && !initialized) {
            init();
            clearInterval(checkInterval);
        }
    }, 150);

    return { init, openEditor, cancelEditor, save, deletePet, selectSpecies, selectBreed, previewPhoto, removePhoto };
})();