(function () {
    "use strict";

    var panel = document.getElementById("pop-lookup");
    if (!panel) return;

    var lookupUrl = panel.getAttribute("data-lookup-url");
    var modeSelect = document.getElementById("pl-mode");
    var form = document.getElementById("pl-form");
    var zipField = document.getElementById("pl-zip-field");
    var cityField = document.getElementById("pl-city-field");
    var stateField = document.getElementById("pl-state-field");
    var zipInput = document.getElementById("pl-zip");
    var cityInput = document.getElementById("pl-city");
    var stateSelect = document.getElementById("pl-state");
    var statusEl = document.getElementById("pl-status");
    var resultEl = document.getElementById("pl-result");

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function render(data) {
        var place = data.place;
        var popText = data.population != null ? data.population.toLocaleString() : "Not available";
        var title = data.mode === "zip"
            ? popText + " people in ZIP " + escapeHtml(place.zip)
            : popText + " people across " + data.zipCount + " ZIP code" + (data.zipCount === 1 ? "" : "s") + " in " + escapeHtml(place.city);

        resultEl.innerHTML =
            '<div class="tl-result">' +
                '<img class="tl-result-flag" src="' + escapeHtml(place.flagUrl) + '" alt="' + escapeHtml(place.stateName) + ' flag" loading="lazy" />' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title">' + title + '</div>' +
                    '<div class="tl-result-sub">' + escapeHtml(place.city) + ', ' + escapeHtml(place.stateCode) + ' &middot; <a href="/states/' + escapeHtml(place.stateSlug) + '">' + escapeHtml(place.stateName) + '</a></div>' +
                '</div>' +
            '</div>' +
            (data.mode === "city" ? '<p class="tl-note">Estimated by summing U.S. Census population for every ZIP Code Tabulation Area matching this city — ZCTA boundaries don’t always match official city limits.</p>' : '');
    }

    modeSelect.addEventListener("change", function () {
        var isCity = modeSelect.value === "city";
        zipField.classList.toggle("hidden", isCity);
        cityField.classList.toggle("hidden", !isCity);
        stateField.classList.toggle("hidden", !isCity);
    });

    form.addEventListener("submit", function (e) {
        e.preventDefault();

        var params = new URLSearchParams();
        if (modeSelect.value === "zip") {
            var zip = zipInput.value.trim();
            if (!/^\d{3,5}$/.test(zip)) {
                setStatus("Enter a valid 5-digit ZIP code.", true);
                resultEl.innerHTML = "";
                return;
            }
            params.set("zip", zip);
        } else {
            var city = cityInput.value.trim();
            if (!city || !stateSelect.value) {
                setStatus("Enter a city and choose a state.", true);
                resultEl.innerHTML = "";
                return;
            }
            params.set("city", city);
            params.set("state", stateSelect.value);
        }

        setStatus("Looking up…", false);

        fetch(lookupUrl + "?" + params.toString(), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found) {
                    setStatus("No population data found.", true);
                    resultEl.innerHTML = "";
                    return;
                }
                setStatus("", false);
                render(data);
            })
            .catch(function () {
                setStatus("Something went wrong. Please try again.", true);
            });
    });
})();
