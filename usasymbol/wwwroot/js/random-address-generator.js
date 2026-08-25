(function () {
    "use strict";

    var panel = document.getElementById("address-generator");
    if (!panel) return;

    var data = window.addressGeneratorData || {};
    var generateUrl = panel.getAttribute("data-generate-url");

    var stateSelect = document.getElementById("ag-state");
    var generateBtn = document.getElementById("ag-generate");
    var copyBtn = document.getElementById("ag-copy");
    var statusEl = document.getElementById("ag-status");
    var resultEl = document.getElementById("ag-result");

    var current = data.initial || null;

    function escapeHtml(value) {
        return String(value == null ? "" : value).replace(/[&<>"']/g, function (ch) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[ch];
        });
    }

    function setStatus(message, isError) {
        statusEl.textContent = message || "";
        statusEl.classList.toggle("is-error", !!isError);
    }

    function render(addr) {
        current = addr;
        resultEl.innerHTML =
            '<div class="tl-result">' +
                '<img class="tl-result-flag" src="' + escapeHtml(addr.flagUrl) + '" alt="' + escapeHtml(addr.stateName) + ' flag" loading="lazy" />' +
                '<div class="tl-result-body">' +
                    '<div class="tl-result-title">' + escapeHtml(addr.street) + '</div>' +
                    '<div class="tl-result-sub">' + escapeHtml(addr.city) + ', ' + escapeHtml(addr.stateCode) + ' ' + escapeHtml(addr.zip) + ' &middot; <a href="/states/' + escapeHtml(addr.stateSlug) + '">' + escapeHtml(addr.stateName) + '</a></div>' +
                '</div>' +
            '</div>';
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

    function generate() {
        var stateCode = stateSelect.value;
        generateBtn.disabled = true;
        setStatus("Generating…", false);

        var url = generateUrl + (stateCode ? "?state=" + encodeURIComponent(stateCode) : "");

        fetch(url, { headers: { "Accept": "application/json" } })
            .then(function (res) { return res.json(); })
            .then(function (addr) {
                render(addr);
                setStatus("", false);
            })
            .catch(function () {
                setStatus("Something went wrong. Please try again.", true);
            })
            .finally(function () {
                generateBtn.disabled = false;
            });
    }

    generateBtn.addEventListener("click", generate);
    stateSelect.addEventListener("change", generate);

    copyBtn.addEventListener("click", function () {
        if (!current) return;
        var text = current.street + "\n" + current.city + ", " + current.stateCode + " " + current.zip;
        copyText(text).then(function () {
            setStatus("Copied to clipboard.", false);
        });
    });

    if (current) render(current);
})();
