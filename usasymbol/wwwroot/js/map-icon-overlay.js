(function () {
    'use strict';

    function ns(tag) {
        return document.createElementNS('http://www.w3.org/2000/svg', tag);
    }

    function placeIcon(parent, defs, postal, cx, cy, size, imgUrl) {
        var clipId = 'ico-' + postal.toLowerCase();
        var half = size / 2;
        var radius = Math.round(size * 0.22);

        var cp = ns('clipPath');
        cp.setAttribute('id', clipId);
        var cr = ns('rect');
        cr.setAttribute('x', cx - half);
        cr.setAttribute('y', cy - half);
        cr.setAttribute('width', size);
        cr.setAttribute('height', size);
        cr.setAttribute('rx', radius);
        cr.setAttribute('ry', radius);
        cp.appendChild(cr);
        defs.appendChild(cp);

        var g = ns('g');
        g.setAttribute('data-postal', postal.toLowerCase());

        var img = ns('image');
        img.setAttribute('href', imgUrl);
        img.setAttribute('x', cx - half);
        img.setAttribute('y', cy - half);
        img.setAttribute('width', size);
        img.setAttribute('height', size);
        img.setAttribute('clip-path', 'url(#' + clipId + ')');
        img.setAttribute('preserveAspectRatio', 'xMidYMid slice');

        img.addEventListener('error', function () {
            if (g.parentNode) g.parentNode.removeChild(g);
            var dead = defs.querySelector('#' + clipId);
            if (dead) defs.removeChild(dead);
        });

        g.appendChild(img);
        parent.appendChild(g);
    }

    function init() {
        var container = document.getElementById('usmap-container');
        var dataEl    = document.getElementById('choropleth-data');
        if (!container || !dataEl) return;

        var stateData;
        try { stateData = JSON.parse(dataEl.textContent || dataEl.innerText || '{}'); }
        catch (e) { return; }

        var imgByPostal = {};
        Object.keys(stateData).forEach(function (k) {
            var rec = stateData[k];
            if (rec && rec.img) imgByPostal[k.toUpperCase()] = rec.img;
        });

        if (!Object.keys(imgByPostal).length) return;

        var svg = container.querySelector('svg');
        if (!svg) return;

        // Read label positions from SVG <text> elements (already correctly placed per state)
        var labelPos = {};
        svg.querySelectorAll('text').forEach(function (el) {
            var code = (el.textContent || '').trim();
            if (code.length !== 2 || !/^[A-Z]{2}$/.test(code)) return;
            var x = parseFloat(el.getAttribute('x'));
            var y = parseFloat(el.getAttribute('y'));
            if (isNaN(x) || isNaN(y)) return;
            labelPos[code] = { cx: x, cy: y };
        });

        // Fallback: compute bbox centroids for states missing a text label
        svg.querySelectorAll('g.state path').forEach(function (path) {
            var cls = Array.prototype.slice.call(path.classList);
            var code = cls.find(function (c) { return c.length === 2 && /^[a-z]+$/.test(c); });
            if (!code) return;
            var postal = code.toUpperCase();
            if (labelPos[postal] || !imgByPostal[postal]) return;
            var bb;
            try { bb = path.getBBox(); } catch (e) { return; }
            if (!bb || bb.width === 0) return;
            labelPos[postal] = { cx: bb.x + bb.width / 2, cy: bb.y + bb.height / 2 };
        });

        var defs = svg.querySelector('defs');
        if (!defs) {
            defs = ns('defs');
            svg.insertBefore(defs, svg.firstChild);
        }

        var overlay = ns('g');
        overlay.setAttribute('id', 'icon-overlay');
        overlay.setAttribute('pointer-events', 'none');
        overlay.setAttribute('aria-hidden', 'true');
        svg.appendChild(overlay);

        // Determine a uniform icon radius from the SVG viewBox
        var vb = svg.viewBox && svg.viewBox.baseVal;
        var svgW = (vb && vb.width > 0) ? vb.width : 959;
        var size = Math.round(svgW * 0.06); // ~58 units on a 959-wide SVG

        Object.keys(imgByPostal).forEach(function (postal) {
            var pos = labelPos[postal];
            if (!pos) return;
            placeIcon(overlay, defs, postal, pos.cx, pos.cy, size, imgByPostal[postal]);
        });
    }

    var mapContainer = document.getElementById('usmap-container');
    if (!mapContainer) return;

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

    // Wait for SVG to be inserted before setting up icon overlay
    if (mapContainer.dataset.svgSrc) {
        document.addEventListener('map-svg-ready', scheduleInit, { once: true });
    } else {
        scheduleInit();
    }
})();
