(function () {
    "use strict";

    var MIN_VOTES = 20;

    document.querySelectorAll("[data-ranking-reactions]").forEach(function (root) {
        var key = "ranking-reaction:" + root.dataset.category + "/" + root.dataset.slug;
        var endpoint = "/api/rankings/" + encodeURIComponent(root.dataset.category) + "/" +
            encodeURIComponent(root.dataset.slug) + "/engagement";
        var status = root.querySelector("[data-reaction-status]");
        var buttons = root.querySelectorAll("[data-reaction]");
        var saved = null;
        var data = null;
        var saving = false;
        try { saved = window.localStorage.getItem(key); } catch (_) { }

        window.dataLayer = window.dataLayer || [];

        function storeLocal(value) {
            try { if (value) window.localStorage.setItem(key, value); } catch (_) { }
        }

        function setStatus(text) { if (status) status.textContent = text; }

        function render() {
            var showResults = Boolean(saved && data && data.total >= MIN_VOTES);
            root.classList.toggle("is-voted", Boolean(saved));
            root.classList.toggle("has-results", showResults);
            buttons.forEach(function (button) {
                var value = button.dataset.reaction;
                var active = value === saved;
                var pct = showResults ? Math.round((data.counts[value] || 0) * 100 / data.total) : 0;
                button.classList.toggle("is-selected", active);
                button.setAttribute("aria-pressed", String(active));
                button.disabled = Boolean(saved);
                button.querySelector("[data-bar]").style.width = pct + "%";
                button.querySelector("[data-pct]").textContent = showResults ? pct + "%" : "";
                button.querySelector("[data-you]").hidden = !active;
                if (showResults) {
                    button.setAttribute("aria-label", button.textContent.replace(/\s+/g, " ").trim() + ", " + pct + " percent");
                }
            });
            if (!saved) setStatus("Tap one to see how others voted.");
            else if (showResults) setStatus(data.total.toLocaleString("en-US") + " reactions");
            else setStatus("Thanks! Results appear after more votes.");
        }

        function accept(result) {
            if (!result) return;
            if (result.counts) data = { counts: result.counts, total: result.total || 0 };
            if (result.reaction) { saved = result.reaction; storeLocal(saved); }
            render();
        }

        async function trackView() {
            var stored = false;
            try {
                var response = await fetch(endpoint + "/view", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (response.ok) { accept(await response.json()); stored = true; }
            } catch (_) { }

            window.dataLayer.push({
                event: "ranking_reactions_view",
                ranking_category: root.dataset.category,
                ranking_slug: root.dataset.slug,
                has_saved_reaction: Boolean(saved),
                stored_in_database: stored
            });
        }

        if ("IntersectionObserver" in window) {
            var observer = new IntersectionObserver(function (entries) {
                if (!entries.some(function (entry) { return entry.isIntersecting; })) return;
                observer.disconnect();
                trackView();
            }, { threshold: 0.25 });
            observer.observe(root);
        } else {
            trackView();
        }

        render();

        root.addEventListener("click", async function (event) {
            var button = event.target.closest("[data-reaction]");
            if (!button || saving || saved) return;
            var reaction = button.dataset.reaction;
            saving = true;
            buttons.forEach(function (item) { item.disabled = true; });
            setStatus("Saving…");

            try {
                var response = await fetch(endpoint + "/reaction", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: {
                        "Content-Type": "application/json",
                        "X-Requested-With": "XMLHttpRequest"
                    },
                    body: JSON.stringify({ reaction: reaction })
                });
                if (!response.ok) throw new Error("Reaction request failed");
                var result = await response.json();
                accept(result);
                window.dataLayer.push({
                    event: "ranking_reaction",
                    ranking_category: root.dataset.category,
                    ranking_slug: root.dataset.slug,
                    reaction: result.reaction,
                    stored_in_database: true
                });
            } catch (_) {
                buttons.forEach(function (item) { item.disabled = false; });
                setStatus("Couldn’t save your reaction. Please try again.");
                window.dataLayer.push({
                    event: "ranking_reaction_error",
                    ranking_category: root.dataset.category,
                    ranking_slug: root.dataset.slug
                });
            } finally {
                saving = false;
            }
        });
    });

    // Embed panel: live preview, size choice and a ready-to-paste snippet (iframe + text link outside it).
    var panel = document.getElementById("ranking-embed-panel");
    if (panel) {
        var toggle = document.querySelector("[data-embed-toggle]");
        var codeBox = panel.querySelector("[data-embed-code]");
        var sizeSelect = panel.querySelector("[data-embed-size]");
        var frame = panel.querySelector("[data-embed-preview-frame]");
        var openLink = panel.querySelector("[data-embed-open]");
        var status = panel.querySelector("[data-embed-status]");
        var format = panel.dataset.format;

        function escapeAttr(value) { return String(value).replace(/&/g, "&amp;").replace(/"/g, "&quot;"); }

        function snippet() {
            if (format === "image") {
                var imageUrl = panel.dataset.urlImage;
                var imageTitle = panel.dataset.titleImage;
                var imageSize = sizeSelect.value;
                var imageStyle = imageSize === "responsive" ? "max-width:100%;height:auto" : "max-width:100%;height:auto;width:" + imageSize + "px";
                // The caption link is plain HTML next to the image, so it counts as an ordinary backlink.
                var figure = '<figure style="margin:0">\n  <img src="' + imageUrl + '" alt="' + escapeAttr(imageTitle) + '" style="' + imageStyle + '">\n  <figcaption>' +
                    panel.dataset.sourceHtml.replace(/^<p>|<\/p>$/g, "") + "</figcaption>\n</figure>";
                return { code: figure, url: imageUrl, image: true, width: imageSize === "responsive" ? "100%" : imageSize, height: 0 };
            }
            var url = panel.dataset[format === "map" ? "urlMap" : "urlTable"];
            var title = panel.dataset[format === "map" ? "titleMap" : "titleTable"];
            var baseHeight = parseInt(panel.dataset[format === "map" ? "heightMap" : "heightTable"], 10);
            var size = sizeSelect.value;
            var width = size === "responsive" ? "100%" : size;
            // Narrower embeds wrap the legend and need more height; tables keep their height.
            var height = format === "map" && size !== "responsive" ? Math.round(baseHeight + (900 - parseInt(size, 10)) * 0.3) : baseHeight;
            var iframe = '<iframe src="' + url + '" title="' + escapeAttr(title) + '" width="' + width + '" height="' + height +
                '" loading="lazy" style="border:0;max-width:100%"' + (format === "map" ? " allowfullscreen" : "") + "></iframe>";
            return { code: iframe + "\n" + panel.dataset.sourceHtml, url: url, height: height, width: width };
        }

        function refresh() {
            var current = snippet();
            codeBox.value = current.code;
            if (openLink) openLink.href = current.url;
            // The preview loads the same-origin path so it also works on staging and localhost.
            var path = current.url.replace(/^https?:\/\/[^/]+/, "");
            if (current.image) {
                frame.innerHTML = "";
                var picture = document.createElement("img");
                picture.src = path;
                picture.alt = "Image preview";
                picture.style.width = "100%";
                picture.style.height = "auto";
                frame.appendChild(picture);
                return;
            }
            if (frame.querySelector("img")) frame.innerHTML = "";
            var preview = frame.querySelector("iframe");
            if (!preview) {
                preview = document.createElement("iframe");
                preview.title = "Embed preview";
                preview.loading = "lazy";
                frame.appendChild(preview);
            }
            if (preview.getAttribute("src") !== path) preview.setAttribute("src", path);
            preview.style.width = current.width === "100%" ? "100%" : current.width + "px";
            preview.style.height = Math.min(current.height, 560) + "px";
        }

        toggle.addEventListener("click", function () {
            var open = panel.hidden;
            panel.hidden = !open;
            toggle.setAttribute("aria-expanded", String(open));
            if (open) refresh();
        });
        sizeSelect.addEventListener("change", refresh);
        panel.querySelectorAll("[data-embed-format]").forEach(function (tab) {
            tab.addEventListener("click", function () {
                format = tab.dataset.embedFormat;
                panel.querySelectorAll("[data-embed-format]").forEach(function (item) {
                    item.setAttribute("aria-selected", String(item === tab));
                });
                refresh();
            });
        });
        panel.querySelector("[data-copy-embed]").addEventListener("click", async function () {
            var button = this;
            try {
                await navigator.clipboard.writeText(codeBox.value);
                button.textContent = "Copied";
                if (status) status.textContent = "Embed code copied to your clipboard.";
                window.setTimeout(function () { button.textContent = "Copy code"; }, 1800);
            } catch (_) {
                codeBox.focus({ preventScroll: true });
                codeBox.select();
                if (status) status.textContent = "Select and copy the highlighted code.";
            }
        });
    }

    // Generic "copy this text" buttons (citation under the table).
    document.querySelectorAll("[data-copy-text]").forEach(function (button) {
        button.addEventListener("click", async function () {
            var label = button.querySelector("span");
            var original = label.textContent;
            try {
                await navigator.clipboard.writeText(button.dataset.copyText);
                label.textContent = "Copied";
                window.setTimeout(function () { label.textContent = original; }, 1800);
            } catch (_) { window.prompt("Copy this citation:", button.dataset.copyText); }
        });
    });
})();
