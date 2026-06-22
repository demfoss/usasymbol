/**
 * Leaflet.heat — heatmap layer built on simpleheat
 * MIT License — Vladimir Agafonkin (Mapbox)
 * Bundled with simpleheat core for zero-dependency deployment.
 */
(function (factory) {
    if (typeof define === 'function' && define.amd) {
        define(['leaflet'], factory);
    } else if (typeof module !== 'undefined') {
        module.exports = factory(require('leaflet'));
    } else {
        window.L && factory(window.L);
    }
}(function (L) {
    'use strict';

    // ─── simpleheat core ────────────────────────────────────────────────────────
    function SimpleHeat(canvas) {
        this._canvas = canvas;
        this._ctx = canvas.getContext('2d');
        this._width = canvas.width;
        this._height = canvas.height;
        this._max = 1;
        this._data = [];
    }

    SimpleHeat.prototype = {
        defaultRadius: 25,
        defaultGradient: {
            0.4: '#0000ff',
            0.55: '#00ffff',
            0.7: '#00ff00',
            0.85: '#ffff00',
            1.0: '#ff0000'
        },

        data: function (d) { this._data = d; return this; },
        max: function (m) { this._max = m; return this; },
        clear: function () { this._data = []; return this; },

        radius: function (r, blur) {
            blur = (blur == null) ? 15 : blur;
            var c = this._circle = document.createElement('canvas');
            var ctx = c.getContext('2d');
            var r2 = this._r = r + blur;
            c.width = c.height = r2 * 2;
            // draw shadow-blurred dot
            ctx.shadowOffsetX = ctx.shadowOffsetY = 200;
            ctx.shadowBlur = blur;
            ctx.shadowColor = 'black';
            ctx.beginPath();
            ctx.arc(r2 - 200, r2 - 200, r, 0, Math.PI * 2, true);
            ctx.closePath();
            ctx.fill();
            return this;
        },

        gradient: function (grad) {
            var c = document.createElement('canvas');
            var ctx = c.getContext('2d');
            c.width = 1;
            c.height = 256;
            var g = ctx.createLinearGradient(0, 0, 0, 256);
            for (var stop in grad) { g.addColorStop(+stop, grad[stop]); }
            ctx.fillStyle = g;
            ctx.fillRect(0, 0, 1, 256);
            this._grad = ctx.getImageData(0, 0, 1, 256).data;
            return this;
        },

        resize: function () {
            this._width  = this._canvas.width;
            this._height = this._canvas.height;
        },

        draw: function (minOpacity) {
            if (!this._circle) this.radius(this.defaultRadius);
            if (!this._grad)   this.gradient(this.defaultGradient);

            var ctx = this._ctx;
            ctx.clearRect(0, 0, this._width, this._height);

            for (var i = 0, len = this._data.length, p; i < len; i++) {
                p = this._data[i];
                ctx.globalAlpha = Math.min(Math.max(p[2] / this._max, minOpacity || 0.05), 1);
                ctx.drawImage(this._circle, p[0] - this._r, p[1] - this._r);
            }

            var img = ctx.getImageData(0, 0, this._width, this._height);
            this._colorize(img.data, this._grad);
            ctx.putImageData(img, 0, 0);
            return this;
        },

        _colorize: function (px, grad) {
            for (var i = 0, len = px.length; i < len; i += 4) {
                var j = px[i + 3] * 4;
                if (j) {
                    px[i]     = grad[j];
                    px[i + 1] = grad[j + 1];
                    px[i + 2] = grad[j + 2];
                }
            }
        }
    };

    // ─── Gradient presets ────────────────────────────────────────────────────────
    var GRADIENTS = {
        hot:    { 0.2: '#000080', 0.4: '#0000ff', 0.55: '#00ffff', 0.7: '#00ff00', 0.85: '#ffff00', 1.0: '#ff0000' },
        cool:   { 0.2: '#003366', 0.45: '#006699', 0.65: '#33ccff', 0.85: '#99ffcc', 1.0: '#ffffff' },
        purple: { 0.2: '#1a0033', 0.45: '#6600cc', 0.65: '#cc00ff', 0.85: '#ff66ff', 1.0: '#ffffff' },
    };

    // ─── HeatLayer ───────────────────────────────────────────────────────────────
    var HeatLayer = (L.Layer || L.Class).extend({
        options: {
            minOpacity:  0.05,
            maxZoom:     18,
            radius:      18,
            blur:        22,
            max:         1.0,
            gradient:    null   // null → use 'hot' preset
        },

        initialize: function (latlngs, options) {
            this._latlngs = latlngs;
            L.setOptions(this, options);
        },

        setLatLngs: function (latlngs) { this._latlngs = latlngs; return this.redraw(); },

        setOptions: function (options) {
            L.setOptions(this, options);
            if (this._heat) this._applyHeatOptions();
            return this.redraw();
        },

        redraw: function () {
            if (this._heat && !this._frame && this._map && !this._map._animating)
                this._frame = L.Util.requestAnimFrame(this._redraw, this);
            return this;
        },

        onAdd: function (map) {
            this._map = map;
            if (!this._canvas) this._initCanvas();
            (this.options.pane ? this.getPane() : map.getPanes().overlayPane).appendChild(this._canvas);
            map.on('moveend', this._reset, this);
            if (map.options.zoomAnimation && L.Browser.any3d) map.on('zoomanim', this._animateZoom, this);
            this._reset();
        },

        onRemove: function (map) {
            (this.options.pane ? this.getPane() : map.getPanes().overlayPane).removeChild(this._canvas);
            map.off('moveend', this._reset, this);
            if (map.options.zoomAnimation) map.off('zoomanim', this._animateZoom, this);
        },

        addTo: function (map) { map.addLayer(this); return this; },

        _initCanvas: function () {
            var c = this._canvas = L.DomUtil.create('canvas', 'leaflet-layer leaflet-heatmap-layer');
            var originProp = L.DomUtil.testProp(['transformOrigin', 'WebkitTransformOrigin', 'msTransformOrigin']);
            c.style[originProp] = '50% 50%';
            var animated = this._map.options.zoomAnimation && L.Browser.any3d;
            L.DomUtil.addClass(c, 'leaflet-zoom-' + (animated ? 'animated' : 'hide'));
            this._heat = new SimpleHeat(c);
            this._applyHeatOptions();
        },

        _applyHeatOptions: function () {
            var grad = this.options.gradient || GRADIENTS[this.options.gradientPreset] || GRADIENTS.hot;
            this._heat.radius(this.options.radius, this.options.blur).gradient(grad);
        },

        _reset: function () {
            var tl = this._map.containerPointToLayerPoint([0, 0]);
            L.DomUtil.setPosition(this._canvas, tl);
            var size = this._map.getSize();
            this._canvas.width  = size.x;
            this._canvas.height = size.y;
            this._heat._width   = size.x;
            this._heat._height  = size.y;
            this._redraw();
        },

        _redraw: function () {
            if (!this._map) return;

            var r        = this._heat._r;
            var size     = this._map.getSize();
            var bounds   = new L.Bounds(L.point([-r, -r]), size.add([r, r]));
            var max      = this.options.max;
            var maxZoom  = this.options.maxZoom;
            var v        = 1 / Math.pow(2, Math.max(0, Math.min(maxZoom - this._map.getZoom(), 12)));
            var cellSize = Math.max(r / 2, 1);
            var grid     = {};
            var pPos     = this._map._getMapPanePos();
            var offX     = pPos.x % cellSize;
            var offY     = pPos.y % cellSize;

            this._latlngs.forEach(function (d) {
                var p   = this._map.latLngToContainerPoint(d);
                if (!bounds.contains(p)) return;
                var alt = (d.alt !== undefined ? d.alt : d[2] !== undefined ? +d[2] : 1) * v;
                var cx  = Math.floor((p.x - offX) / cellSize) + 2;
                var cy  = Math.floor((p.y - offY) / cellSize) + 2;
                var key = (cx << 16) | (cy & 0xffff);
                if (!grid[key]) {
                    grid[key] = [p.x, p.y, alt];
                } else {
                    var tot = grid[key][2] + alt;
                    grid[key][0] = (grid[key][0] * grid[key][2] + p.x * alt) / tot;
                    grid[key][1] = (grid[key][1] * grid[key][2] + p.y * alt) / tot;
                    grid[key][2] = tot;
                }
            }, this);

            var data = [];
            for (var k in grid) {
                var val = grid[k];
                data.push([Math.round(val[0]), Math.round(val[1]), Math.min(val[2], max)]);
            }

            this._heat.clear().data(data).draw(this.options.minOpacity);
            this._frame = null;
        },

        _animateZoom: function (e) {
            var scale  = this._map.getZoomScale(e.zoom);
            var offset = this._map._getCenterLayerPoint
                ? this._map._getCenterLayerPoint()._multiplyBy(-scale).subtract(this._map._getMapPanePos())
                : this._map._getCenterOffset(e.center)._multiplyBy(-scale).subtract(this._map._getMapPanePos());
            L.DomUtil.setTransform
                ? L.DomUtil.setTransform(this._canvas, offset, scale)
                : (this._canvas.style[L.DomUtil.TRANSFORM] = L.DomUtil.getTranslateString(offset) + ' scale(' + scale + ')');
        }
    });

    L.heatLayer = function (latlngs, options) { return new HeatLayer(latlngs, options); };

    return HeatLayer;
}));
