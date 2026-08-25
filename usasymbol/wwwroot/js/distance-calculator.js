(function () {
    "use strict";

    var panel = document.getElementById("distance-calc");
    if (!panel) return;

    var calculateUrl = panel.getAttribute("data-calculate-url");
    var form = document.getElementById("dc-form");
    var fromInput = document.getElementById("dc-from");
    var toInput = document.getElementById("dc-to");
    var swapBtn = document.getElementById("dc-swap");
    var statusEl = document.getElementById("dc-status");
    var resultEl = document.getElementById("dc-result");

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function placeLabel(p) {
        return escapeHtml(p.city) + ', ' + escapeHtml(p.stateCode) + ' ' + escapeHtml(p.zip);
    }

    function render(data) {
        if (data.miles == null) {
            resultEl.innerHTML = '<p class="tl-note">Distance could not be calculated for one of these ZIP codes (missing coordinates).</p>';
            return;
        }

        resultEl.innerHTML =
            '<div class="tl-distance-result-grid">' +
                '<div class="tl-distance-figure"><div class="tl-distance-value">' + data.miles.toLocaleString() + '</div><div class="tl-distance-unit">Miles</div></div>' +
                '<div class="tl-distance-figure"><div class="tl-distance-value">' + data.kilometers.toLocaleString() + '</div><div class="tl-distance-unit">Kilometers</div></div>' +
            '</div>' +
            '<div class="tl-result-list" style="margin-top:16px;">' +
                '<div class="tl-result"><div class="tl-result-body"><div class="tl-result-title">' + placeLabel(data.from) + '</div><div class="tl-result-sub"><a href="/states/' + escapeHtml(data.from.stateSlug) + '">' + escapeHtml(data.from.stateName) + '</a></div></div></div>' +
                '<div class="tl-result"><div class="tl-result-body"><div class="tl-result-title">' + placeLabel(data.to) + '</div><div class="tl-result-sub"><a href="/states/' + escapeHtml(data.to.stateSlug) + '">' + escapeHtml(data.to.stateName) + '</a></div></div></div>' +
            '</div>';
    }

    swapBtn.addEventListener("click", function () {
        var tmp = fromInput.value;
        fromInput.value = toInput.value;
        toInput.value = tmp;
    });

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        var fromZip = fromInput.value.trim();
        var toZip = toInput.value.trim();

        if (!/^\d{3,5}$/.test(fromZip) || !/^\d{3,5}$/.test(toZip)) {
            setStatus("Enter two valid 5-digit ZIP codes.", true);
            resultEl.innerHTML = "";
            return;
        }

        setStatus("Calculating…", false);

        var params = new URLSearchParams({ fromZip: fromZip, toZip: toZip });

        fetch(calculateUrl + "?" + params.toString(), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found) {
                    setStatus("One or both ZIP codes could not be found.", true);
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
