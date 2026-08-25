(function () {
    "use strict";

    var tbody = document.getElementById("abbr-tbody");
    var search = document.getElementById("abbr-search");
    var noMatch = document.getElementById("abbr-no-match");
    var table = document.getElementById("abbr-table");
    if (!tbody || !search) return;

    var states = window.stateAbbreviationData || [];

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function rowMarkup(state) {
        return (
            '<tr>' +
                '<td><span class="tl-abbr-row-state"><img src="' + escapeHtml(state.flagUrl) + '" alt="' + escapeHtml(state.name) + ' flag" loading="lazy" width="24" height="17" /> <a href="/states/' + escapeHtml(state.slug) + '">' + escapeHtml(state.name) + '</a></span></td>' +
                '<td><span class="tl-abbr-code">' + escapeHtml(state.code) + '</span></td>' +
                '<td><button type="button" class="tl-abbr-copy" data-copy="' + escapeHtml(state.code) + '"><i class="fa-regular fa-copy" aria-hidden="true"></i> Copy</button></td>' +
            '</tr>'
        );
    }

    function render(list) {
        tbody.innerHTML = list.map(rowMarkup).join("");
        var hasResults = list.length > 0;
        table.classList.toggle("hidden", !hasResults);
        noMatch.classList.toggle("hidden", hasResults);
    }

    function copyText(text) {
        if (navigator.clipboard && navigator.clipboard.writeText) {
            return navigator.clipboard.writeText(text);
        }
        var temp = document.createElement("textarea");
        temp.value = text;
        temp.style.position = "fixed";
        temp.style.opacity = "0";
        document.body.appendChild(temp);
        temp.select();
        try { document.execCommand("copy"); } catch (e) { /* no-op */ }
        document.body.removeChild(temp);
        return Promise.resolve();
    }

    tbody.addEventListener("click", function (e) {
        var btn = e.target.closest(".tl-abbr-copy");
        if (!btn) return;
        var text = btn.getAttribute("data-copy") || "";
        copyText(text).then(function () {
            btn.classList.add("is-copied");
            setTimeout(function () { btn.classList.remove("is-copied"); }, 1200);
        });
    });

    search.addEventListener("input", function () {
        var q = search.value.trim().toLowerCase();
        if (!q) { render(states); return; }

        var filtered = states.filter(function (s) {
            return s.name.toLowerCase().indexOf(q) !== -1 || s.code.toLowerCase().indexOf(q) !== -1;
        });
        render(filtered);
    });

    render(states);
})();
