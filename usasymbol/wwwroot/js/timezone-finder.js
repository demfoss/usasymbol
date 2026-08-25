(function () {
    "use strict";

    var panel = document.getElementById("tz-finder");
    if (!panel) return;

    var lookupUrl = panel.getAttribute("data-lookup-url");
    var modeSelect = document.getElementById("tz-mode");
    var form = document.getElementById("tz-form");
    var stateField = document.getElementById("tz-state-field");
    var zipField = document.getElementById("tz-zip-field");
    var stateSelect = document.getElementById("tz-state");
    var zipInput = document.getElementById("tz-zip");
    var statusEl = document.getElementById("tz-status");
    var resultEl = document.getElementById("tz-result");

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function offsetLabel(offset) {
        var sign = offset >= 0 ? "+" : "-";
        return "UTC" + sign + Math.abs(offset);
    }

    var clockInterval = null;

    function stopClock() {
        if (clockInterval) {
            clearInterval(clockInterval);
            clockInterval = null;
        }
    }

    function startClock(ianaZone) {
        stopClock();
        var clockEl = document.getElementById("tz-clock");
        if (!clockEl || !ianaZone) return;

        var formatter = new Intl.DateTimeFormat("en-US", {
            timeZone: ianaZone,
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
            hour12: true
        });
        var dateFormatter = new Intl.DateTimeFormat("en-US", {
            timeZone: ianaZone,
            weekday: "long",
            month: "long",
            day: "numeric"
        });

        function tick() {
            clockEl.textContent = formatter.format(new Date());
            var dateEl = document.getElementById("tz-clock-date");
            if (dateEl) dateEl.textContent = dateFormatter.format(new Date());
        }

        tick();
        clockInterval = setInterval(tick, 1000);
    }

    function render(r) {
        var html =
            '<div class="tl-result">' +
                '<img class="tl-result-flag" src="' + escapeHtml(r.flagUrl) + '" alt="' + escapeHtml(r.stateName) + ' flag" loading="lazy" />' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title">' + escapeHtml(r.zoneName) + '</div>' +
                    '<div class="tl-result-sub"><a href="/states/' + escapeHtml(r.stateSlug) + '">' + escapeHtml(r.stateName) + '</a></div>' +
                    '<div class="tl-result-meta">' +
                        '<span class="tl-result-chip">Standard: ' + escapeHtml(r.abbreviation) + ' (' + escapeHtml(offsetLabel(r.standardUtcOffset)) + ')</span>' +
                        (r.observesDst ? '<span class="tl-result-chip">Daylight: ' + escapeHtml(r.dstAbbreviation) + ' (' + escapeHtml(offsetLabel(r.standardUtcOffset + 1)) + ')</span>' : '<span class="tl-result-chip">No daylight saving time</span>') +
                    '</div>' +
                '</div>' +
            '</div>' +
            '<div class="tl-clock-panel">' +
                '<div class="tl-clock" id="tz-clock"></div>' +
                '<div class="tl-clock-date" id="tz-clock-date"></div>' +
            '</div>';

        if (r.spansMultipleZones) {
            html += '<p class="tl-note">' + escapeHtml(r.stateName) + ' spans more than one time zone. This shows the zone that covers most of the state — parts of it may observe a different time.</p>';
        }

        resultEl.innerHTML = html;
        startClock(r.ianaZone);
    }

    modeSelect.addEventListener("change", function () {
        var isZip = modeSelect.value === "zip";
        stateField.classList.toggle("hidden", isZip);
        zipField.classList.toggle("hidden", !isZip);
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
            if (!stateSelect.value) {
                setStatus("Choose a state.", true);
                resultEl.innerHTML = "";
                return;
            }
            params.set("state", stateSelect.value);
        }

        setStatus("Looking up…", false);

        fetch(lookupUrl + "?" + params.toString(), { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data.found) {
                    setStatus("No time zone data found.", true);
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
