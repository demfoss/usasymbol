(function () {
    "use strict";

    var panel = document.getElementById("county-finder");
    if (!panel) return;

    var lookupUrl = panel.getAttribute("data-lookup-url");
    var modeSelect = document.getElementById("cf-mode");
    var form = document.getElementById("cf-form");
    var zipField = document.getElementById("cf-zip-field");
    var cityField = document.getElementById("cf-city-field");
    var stateField = document.getElementById("cf-state-field");
    var zipInput = document.getElementById("cf-zip");
    var cityInput = document.getElementById("cf-city");
    var stateSelect = document.getElementById("cf-state");
    var statusEl = document.getElementById("cf-status");
    var resultEl = document.getElementById("cf-result");

    var states = window.countyFinderStates || [];
    states.forEach(function (s) {
        var opt = document.createElement("option");
        opt.value = s.code;
        opt.textContent = s.name;
        stateSelect.appendChild(opt);
    });

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function resultRow(r) {
        var countyLabel = r.county ? r.countyUrl
            ? '<a href="' + escapeHtml(r.countyUrl) + '">' + escapeHtml(r.county) + '</a>'
            : escapeHtml(r.county)
            : "County not on file";

        return (
            '<div class="tl-result">' +
                '<img class="tl-result-flag" src="' + escapeHtml(r.flagUrl) + '" alt="' + escapeHtml(r.stateName) + ' flag" loading="lazy" />' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title">' + countyLabel + '</div>' +
                    '<div class="tl-result-sub">' + escapeHtml(r.city) + ', ' + escapeHtml(r.stateCode) + (r.zip ? ' ' + escapeHtml(r.zip) : '') + ' &middot; <a href="/states/' + escapeHtml(r.stateSlug) + '">' + escapeHtml(r.stateName) + '</a></div>' +
                '</div>' +
            '</div>'
        );
    }

    function render(results) {
        if (!results.length) {
            resultEl.innerHTML = "";
            return;
        }
        resultEl.innerHTML = '<div class="tl-result-list">' + results.map(resultRow).join("") + '</div>';
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
            if (!city) {
                setStatus("Enter a city name.", true);
                resultEl.innerHTML = "";
                return;
            }
            params.set("city", city);
            if (stateSelect.value) params.set("state", stateSelect.value);
        }

        setStatus("Searching…", false);

        fetch(lookupUrl + "?" + params.toString(), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found) {
                    setStatus("No matches found.", true);
                    resultEl.innerHTML = "";
                    return;
                }
                setStatus(data.results.length + " match" + (data.results.length === 1 ? "" : "es") + " found.", false);
                render(data.results);
            })
            .catch(function () {
                setStatus("Something went wrong. Please try again.", true);
            });
    });
})();
