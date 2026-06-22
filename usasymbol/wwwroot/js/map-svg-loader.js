(function () {
    'use strict';

    var container = document.getElementById('usmap-container');
    if (!container || !container.dataset.svgSrc) return;

    fetch(container.dataset.svgSrc)
        .then(function (r) { return r.text(); })
        .then(function (txt) {
            container.innerHTML = txt;
            document.dispatchEvent(new CustomEvent('map-svg-ready'));
        })
        .catch(function () {
            document.dispatchEvent(new CustomEvent('map-svg-ready'));
        });
})();
