(function () {
    "use strict";

    var panel = document.getElementById("zip-generator");
    if (!panel) return;

    var data = window.zipGeneratorData || {};
    var generateUrl = panel.getAttribute("data-generate-url");

    var countInput = document.getElementById("zg-count");
    var stateSelect = document.getElementById("zg-state");
    var generateBtn = document.getElementById("zg-generate");
    var downloadBtn = document.getElementById("zg-download");
    var statusEl = document.getElementById("zg-status");
    var grid = document.getElementById("zg-grid");

    var currentResults = Array.isArray(data.initialResults) ? data.initialResults : [];

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function cardMarkup(entry) {
        var lineText = entry.zip + ", " + entry.city + ", " + entry.stateCode;
        return (
            '<div class="zg-card">' +
                '<button type="button" class="zg-copy" data-copy="' + escapeHtml(lineText) + '" aria-label="Copy ' + escapeHtml(lineText) + '" title="Copy">' +
                    '<i class="fa-regular fa-copy" aria-hidden="true"></i>' +
                '</button>' +
                '<div class="zg-card-top">' +
                    '<span class="zg-zip">' + escapeHtml(entry.zip) + '</span>' +
                    '<img class="zg-flag" src="' + escapeHtml(entry.flagUrl) + '" alt="' + escapeHtml(entry.stateName) + ' flag" loading="lazy" width="28" height="20" />' +
                '</div>' +
                '<span class="zg-city">' + escapeHtml(entry.city) + '</span>' +
                '<span class="zg-state"><a href="/states/' + escapeHtml(entry.stateSlug) + '">' + escapeHtml(entry.stateName) + '</a></span>' +
            '</div>'
        );
    }

    function render(results) {
        currentResults = results || [];

        if (!currentResults.length) {
            grid.innerHTML = '<div class="zg-empty">No ZIP codes found. Try a different state.</div>';
            return;
        }

        grid.innerHTML = currentResults.map(cardMarkup).join("");
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function clampCount(value) {
        var n = parseInt(value, 10);
        if (isNaN(n)) n = 20;
        if (n < 1) n = 1;
        if (n > 100) n = 100;
        return n;
    }

    function generate() {
        var count = clampCount(countInput.value);
        countInput.value = count;
        var stateCode = stateSelect.value;

        generateBtn.disabled = true;
        setStatus("Generating…", false);

        var url = generateUrl + "?count=" + encodeURIComponent(count) + (stateCode ? "&state=" + encodeURIComponent(stateCode) : "");

        fetch(url, { headers: { "Accept": "application/json" } })
            .then(function (res) {
                if (!res.ok) throw new Error("Request failed");
                return res.json();
            })
            .then(function (results) {
                render(results);
                setStatus(results.length + " ZIP code" + (results.length === 1 ? "" : "s") + " generated.", false);
            })
            .catch(function () {
                setStatus("Something went wrong generating ZIP codes. Please try again.", true);
            })
            .finally(function () {
                generateBtn.disabled = false;
            });
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

    function downloadTxt() {
        if (!currentResults.length) return;

        var lines = currentResults.map(function (entry) {
            return entry.zip + ", " + entry.city + ", " + entry.stateCode;
        });
        var blob = new Blob([lines.join("\r\n") + "\r\n"], { type: "text/plain;charset=utf-8" });
        var url = URL.createObjectURL(blob);

        var link = document.createElement("a");
        link.href = url;
        link.download = "zip-codes.txt";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

    grid.addEventListener("click", function (e) {
        var btn = e.target.closest(".zg-copy");
        if (!btn) return;

        var text = btn.getAttribute("data-copy") || "";
        copyText(text).then(function () {
            btn.classList.add("is-copied");
            var icon = btn.querySelector("i");
            if (icon) icon.className = "fa-solid fa-check";
            setTimeout(function () {
                btn.classList.remove("is-copied");
                if (icon) icon.className = "fa-regular fa-copy";
            }, 1500);
        });
    });

    generateBtn.addEventListener("click", generate);
    stateSelect.addEventListener("change", generate);
    downloadBtn.addEventListener("click", downloadTxt);
    countInput.addEventListener("keydown", function (e) {
        if (e.key === "Enter") generate();
    });

    render(currentResults);
})();
