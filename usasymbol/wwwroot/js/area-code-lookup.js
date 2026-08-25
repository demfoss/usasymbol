(function () {
    "use strict";

    var panel = document.getElementById("area-code-lookup");
    if (!panel) return;

    var lookupUrl = panel.getAttribute("data-lookup-url");
    var form = document.getElementById("ac-form");
    var codeInput = document.getElementById("ac-code");
    var statusEl = document.getElementById("ac-status");
    var resultEl = document.getElementById("ac-result");

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function render(r) {
        var html =
            '<div class="tl-result">' +
                '<img class="tl-result-flag" src="' + escapeHtml(r.flagUrl) + '" alt="' + escapeHtml(r.primaryStateName) + ' flag" loading="lazy" />' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title">Area code ' + escapeHtml(r.areaCode) + '</div>' +
                    '<div class="tl-result-sub"><a href="/states/' + escapeHtml(r.primaryStateSlug) + '">' + escapeHtml(r.primaryStateName) + '</a></div>';

        if (r.cities && r.cities.length) {
            html += '<div class="tl-result-meta">' + r.cities.map(function (c) {
                return '<span class="tl-result-chip">' + escapeHtml(c) + '</span>';
            }).join("") + '</div>';
        }

        html += '</div></div>';

        if (r.spansMultipleStates && r.otherStateNames.length) {
            html += '<p class="tl-note">This area code is also used in ' + r.otherStateNames.map(escapeHtml).join(", ") + '.</p>';
        }

        resultEl.innerHTML = html;
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        var code = codeInput.value.trim();
        if (!/^\d{3}$/.test(code)) {
            setStatus("Enter a valid 3-digit area code.", true);
            resultEl.innerHTML = "";
            return;
        }

        setStatus("Looking up…", false);

        fetch(lookupUrl + "?code=" + encodeURIComponent(code), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found) {
                    setStatus("No matches for area code \"" + code + "\".", true);
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
