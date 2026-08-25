(function () {
    "use strict";

    var panel = document.getElementById("park-finder");
    if (!panel) return;

    var findUrl = panel.getAttribute("data-find-url");
    var form = document.getElementById("np-form");
    var zipInput = document.getElementById("np-zip");
    var statusEl = document.getElementById("np-status");
    var resultEl = document.getElementById("np-result");

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function parkRow(p, rank) {
        return (
            '<div class="tl-result">' +
                '<div class="tl-result-rank">' + rank + '</div>' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title"><a href="' + escapeHtml(p.url) + '" style="color:inherit;text-decoration:none;">' + escapeHtml(p.name) + '</a></div>' +
                    '<div class="tl-result-sub">Near ' + escapeHtml(p.nearestCity || p.stateCode) + '</div>' +
                    '<div class="tl-result-meta">' +
                        '<span class="tl-result-chip">' + p.distanceMiles.toLocaleString() + ' mi away</span>' +
                        (p.googleMapsUrl ? '<a class="tl-result-chip" href="' + escapeHtml(p.googleMapsUrl) + '" target="_blank" rel="noopener"><i class="fa-solid fa-map-location-dot" aria-hidden="true"></i> View on map</a>' : '') +
                    '</div>' +
                '</div>' +
            '</div>'
        );
    }

    function render(data) {
        resultEl.innerHTML = '<div class="tl-result-list">' + data.results.map(function (p, i) { return parkRow(p, i + 1); }).join("") + '</div>';
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        var zip = zipInput.value.trim();
        if (!/^\d{3,5}$/.test(zip)) {
            setStatus("Enter a valid 5-digit ZIP code.", true);
            resultEl.innerHTML = "";
            return;
        }

        setStatus("Finding nearby parks…", false);

        fetch(findUrl + "?zip=" + encodeURIComponent(zip), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found || !data.results.length) {
                    setStatus("Couldn't find parks for that ZIP code.", true);
                    resultEl.innerHTML = "";
                    return;
                }
                setStatus("Closest " + data.results.length + " national parks to " + zip + ":", false);
                render(data);
            })
            .catch(function () {
                setStatus("Something went wrong. Please try again.", true);
            });
    });
})();
