window.shoppetHelp = (() => {
    function normalize(value) {
        return (value || "").toLowerCase().trim();
    }

    function search() {
        const input = document.getElementById("helpSearch");
        const query = normalize(input?.value);
        const cards = Array.from(document.querySelectorAll("[data-help-topic]"));
        let visible = 0;

        cards.forEach(card => {
            const haystack = normalize(
                [
                    card.getAttribute("data-title"),
                    card.getAttribute("data-tags"),
                    card.textContent
                ].join(" ")
            );

            const match = !query || haystack.includes(query);
            card.style.display = match ? "" : "none";
            if (match) visible++;
        });

        const empty = document.getElementById("helpEmpty");
        if (empty) empty.style.display = visible === 0 ? "block" : "none";

        const count = document.getElementById("helpResultCount");
        if (count) {
            count.textContent = query
                ? visible + (visible === 1 ? " result" : " results")
                : cards.length + " help topics";
        }
    }

    function clear() {
        const input = document.getElementById("helpSearch");
        if (input) {
            input.value = "";
            input.focus();
        }
        search();
    }

    function setQuery(value) {
        const input = document.getElementById("helpSearch");
        if (input) input.value = value || "";
        search();
        document.getElementById("helpResults")?.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    document.addEventListener("input", event => {
        if (event.target?.id === "helpSearch") search();
    });

    return { search, clear, setQuery };
})();