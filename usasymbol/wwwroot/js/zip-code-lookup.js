(function () {
    "use strict";

    var panel = document.getElementById("zip-lookup");
    if (!panel) return;

    var lookupUrl = panel.getAttribute("data-lookup-url");
    var form = document.getElementById("zl-form");
    var zipInput = document.getElementById("zl-zip");
    var statusEl = document.getElementById("zl-status");
    var resultEl = document.getElementById("zl-result");

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function render(result) {
        resultEl.innerHTML =
            '<div class="tl-result">' +
                '<img class="tl-result-flag" src="' + escapeHtml(result.flagUrl) + '" alt="' + escapeHtml(result.stateName) + ' flag" loading="lazy" />' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title">' + escapeHtml(result.city) + ', ' + escapeHtml(result.stateCode) + ' ' + escapeHtml(result.zip) + '</div>' +
                    '<div class="tl-result-sub">' + escapeHtml(result.county ? result.county + ', ' : '') + '<a href="/states/' + escapeHtml(result.stateSlug) + '">' + escapeHtml(result.stateName) + '</a></div>' +
                '</div>' +
            '</div>';
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        var zip = zipInput.value.trim();
        if (!/^\d{3,5}$/.test(zip)) {
            setStatus("Enter a valid 5-digit ZIP code.", true);
            resultEl.innerHTML = "";
            return;
        }

        setStatus("Looking up…", false);

        fetch(lookupUrl + "?zip=" + encodeURIComponent(zip), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found) {
                    setStatus("No ZIP code found for \"" + zip + "\".", true);
                    resultEl.innerHTML = "";
                    return;
                }
                setStatus("", false);
                render(data.result);
            })
            .catch(function () {
                setStatus("Something went wrong. Please try again.", true);
            });
    });
})();
