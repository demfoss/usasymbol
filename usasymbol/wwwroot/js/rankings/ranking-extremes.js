(function () {
    "use strict";
    document.querySelectorAll("[data-extremes]").forEach(function (root) {
        var tabs = root.querySelectorAll("[data-extremes-tab]");
        var lists = root.querySelectorAll("[data-extreme-side]");
        var more = root.querySelector("[data-extremes-more]");
        var active = "highest";

        function render() {
            lists.forEach(function (list) { list.classList.toggle("is-active", list.dataset.extremeSide === active); });
            tabs.forEach(function (tab) { tab.setAttribute("aria-selected", String(tab.dataset.extremesTab === active)); });
        }
        tabs.forEach(function (tab) {
            tab.addEventListener("click", function () { active = tab.dataset.extremesTab; render(); });
        });
        if (more) {
            more.addEventListener("click", function () {
                root.classList.toggle("is-expanded");
                more.textContent = root.classList.contains("is-expanded") ? "Show 5" : more.dataset.label;
            });
            more.dataset.label = more.textContent;
        }
        render();

        // Highlight the visitor's own state when it appears in a list.
        function mark(slug) {
            root.querySelectorAll(".ranking-extreme-row[data-state]").forEach(function (row) {
                row.classList.toggle("is-mine", Boolean(slug) && row.dataset.state === slug);
            });
        }
        var select = document.querySelector("[data-kt-you]");
        if (select) {
            select.addEventListener("change", function () { mark(select.value); });
            window.setTimeout(function () { mark(select.value); }, 600);
        }
    });
})();
