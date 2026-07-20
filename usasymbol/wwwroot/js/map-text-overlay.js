(function () {
    'use strict';

    var dataEl = document.getElementById('choropleth-data');
    if (!dataEl || dataEl.dataset.textOverlay !== 'true') return;

    var mapContainer = document.getElementById('usmap-container');
    if (!mapContainer) return;

    function ns(tag) {
        return document.createElementNS('http://www.w3.org/2000/svg', tag);
    }

    function placeWord(parent, postal, cx, cy, word, fontSize) {
        var g = ns('g');
        g.setAttribute('data-postal', postal.toLowerCase());

        // Shadow/outline pass — thick dark stroke drawn first, then white fill on top
        var t = ns('text');
        t.setAttribute('x', cx);
        t.setAttribute('y', cy);
        t.setAttribute('text-anchor', 'middle');
        t.setAttribute('dominant-baseline', 'central');
        t.setAttribute('font-family', '"Manrope", "Inter", system-ui, sans-serif');
        t.setAttribute('font-weight', '800');
        t.setAttribute('font-size', fontSize);
        t.setAttribute('fill', '#ffffff');
        t.setAttribute('stroke', 'rgba(0,0,0,0.45)');
        t.setAttribute('stroke-width', Math.max(1, fontSize * 0.12));
        t.setAttribute('stroke-linejoin', 'round');
        t.setAttribute('paint-order', 'stroke fill');
        t.setAttribute('letter-spacing', '-0.01em');
        t.textContent = word;

        g.appendChild(t);
        parent.appendChild(g);
    }

    function init() {
        var stateData;
        try {
            stateData = JSON.parse(dataEl.textContent || dataEl.innerText || '{}');
        } catch (e) { return; }

        var hasWords = Object.keys(stateData).some(function (k) {
            var rec = stateData[k];
            return rec && rec.v && typeof rec.v === 'string' && rec.v.trim().length > 0;
        });
        if (!hasWords) return;

        var svg = mapContainer.querySelector('svg');
        if (!svg) return;

        var vb = svg.viewBox && svg.viewBox.baseVal;
        var svgW = (vb && vb.width > 0) ? vb.width : 959;

        // Read label centres from existing SVG <text> elements (postal code labels)
        var labelPos = {};
        svg.querySelectorAll('text').forEach(function (el) {
            var code = (el.textContent || '').trim();
            if (code.length !== 2 || !/^[A-Z]{2}$/.test(code)) return;
            var x = parseFloat(el.getAttribute('x'));
            var y = parseFloat(el.getAttribute('y'));
            if (!isNaN(x) && !isNaN(y)) labelPos[code] = { cx: x, cy: y, bboxW: null };
        });

        // Measure each state path's bounding-box width for font sizing
        svg.querySelectorAll('g.state path').forEach(function (path) {
            var cls = Array.prototype.slice.call(path.classList);
            var code = cls.find(function (c) { return c.length === 2 && /^[a-z]+$/.test(c); });
            if (!code) return;
            var postal = code.toUpperCase();
            try {
                var bb = path.getBBox();
                if (!bb || bb.width === 0) return;
                if (!labelPos[postal]) {
                    labelPos[postal] = { cx: bb.x + bb.width / 2, cy: bb.y + bb.height / 2, bboxW: bb.width };
                } else {
                    labelPos[postal].bboxW = bb.width;
                }
            } catch (e) {}
        });

        var overlay = ns('g');
        overlay.setAttribute('id', 'text-overlay');
        overlay.setAttribute('pointer-events', 'none');
        overlay.setAttribute('aria-hidden', 'true');
        svg.appendChild(overlay);

        Object.keys(stateData).forEach(function (k) {
            var rec = stateData[k];
            if (!rec || !rec.v) return;
            var postal = k.toUpperCase();
            var pos = labelPos[postal];
            if (!pos) return;

            var word = rec.v.trim();
            if (!word) return;

            // Font size: fill ~72% of the state width, clamped between 6 and 26 SVG units
            var stateW = pos.bboxW || svgW * 0.07;
            var charCount = word.replace(/\s+/g, '').length;
            var rawSize = (stateW * 0.72) / Math.max(charCount, 1) / 0.58;
            var fontSize = Math.max(6, Math.min(26, Math.round(rawSize)));

            placeWord(overlay, postal, pos.cx, pos.cy, word, fontSize);
        });
    }

    function scheduleInit() {
        if ('IntersectionObserver' in window) {
            var observer = new IntersectionObserver(function (entries, obs) {
                if (entries[0].isIntersecting) {
                    obs.disconnect();
                    requestAnimationFrame(init);
                }
            }, { rootMargin: '300px 0px' });
            observer.observe(mapContainer);
        } else {
            requestAnimationFrame(init);
        }
    }

    if (mapContainer.dataset.svgSrc) {
        document.addEventListener('map-svg-ready', scheduleInit, { once: true });
    } else {
        scheduleInit();
    }
})();
