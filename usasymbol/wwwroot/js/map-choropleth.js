(function () {
    'use strict';

    var container = document.getElementById('usmap-container');
    var tooltip = document.getElementById('map-tooltip');
    var ttImage = document.getElementById('tt-image');
    var ttState = document.getElementById('tt-state');
    var ttDetails = document.getElementById('tt-details');
    var ttRankRow = document.getElementById('tt-rank-row');
    var ttRank = document.getElementById('tt-rank');
    var ttValueLabel = document.getElementById('tt-value-label');
    var ttValue = document.getElementById('tt-value');
    var ttValueBlock = ttValue ? ttValue.parentElement : null;
    var dataEl = document.getElementById('choropleth-data');

    if (!container || !tooltip || !dataEl) return;

    var stateData = JSON.parse(dataEl.textContent || dataEl.innerText || '{}');
    var metricLabel = (dataEl.dataset.metricLabel || '').trim();
    var activeKey = null;
    var activeSource = null;
    var stateRows = Array.prototype.slice.call(document.querySelectorAll('.rankingTbody .tableRow[data-state-slug]'));

    var slugToKey = {};
    Object.keys(stateData).forEach(function (key) {
        var record = stateData[key];
        if (record && record.s) {
            slugToKey[String(record.s).toLowerCase()] = key;
        }
    });

    function getStateRecord(key) {
        if (!key) return null;
        return stateData[String(key).toUpperCase()] || null;
    }

    function getStateUrl(key) {
        var record = getStateRecord(key);
        if (!record || !record.s) return null;
        var rows = getRowsForKey(key);
        if (rows.length && rows[0].dataset.symbolUrl) {
            return rows[0].dataset.symbolUrl;
        }
        return '/states/' + record.s;
    }

    function getStateKey(el) {
        if (!el || el.tagName !== 'path') return null;
        var cls = Array.prototype.slice.call(el.classList);
        return cls.find(function (c) { return c.length === 2 && /^[a-z]+$/.test(c); }) || null;
    }

    function getStateKeyBySlug(slug) {
        if (!slug) return null;
        return slugToKey[String(slug).toLowerCase()] || null;
    }

    function getStatePath(key) {
        if (!key) return null;
        return container.querySelector('.' + String(key).toLowerCase());
    }

    function getRowsForKey(key) {
        var record = getStateRecord(key);
        if (!record || !record.s) return [];
        var slug = String(record.s).toLowerCase();
        return stateRows.filter(function (row) {
            return (row.dataset.stateSlug || '').toLowerCase() === slug;
        });
    }

    function positionTooltip(clientX, clientY) {
        var rect = container.getBoundingClientRect();
        var x = clientX - rect.left;
        var y = clientY - rect.top;

        var ttW = tooltip.offsetWidth || 180;
        var ttH = tooltip.offsetHeight || 72;

        var left = x + 16;
        var top = y - ttH - 10;

        if (left + ttW > rect.width - 4) left = x - ttW - 16;
        if (top < 4) top = y + 16;
        if (top + ttH > rect.height - 4) top = rect.height - ttH - 4;

        tooltip.style.left = left + 'px';
        tooltip.style.top = top + 'px';
    }

    function applyRowHighlight(key) {
        getRowsForKey(key).forEach(function (row) {
            row.classList.add('ring-2', 'ring-slate-300', 'bg-slate-50');
        });
    }

    function removeRowHighlight(key) {
        getRowsForKey(key).forEach(function (row) {
            row.classList.remove('ring-2', 'ring-slate-300', 'bg-slate-50');
        });
    }

    function highlightState(key) {
        var path = getStatePath(key);
        if (path) {
            path.style.opacity = '1';
            path.style.strokeWidth = '2';
            path.style.filter = 'brightness(0.92) saturate(1.05)';
        }
        applyRowHighlight(key);
    }

    function unhighlightState(key) {
        var path = getStatePath(key);
        if (path) {
            path.style.opacity = '';
            path.style.strokeWidth = '';
            path.style.filter = '';
        }
        removeRowHighlight(key);
    }

    function showTooltip(key, clientX, clientY, source) {
        var d = getStateRecord(key);
        if (!d) return;

        ttState.textContent = d.n || '';

        if (ttImage) {
            var image = d.img || getFirstItemImage(d.items);
            if (image) {
                ttImage.src = image;
                ttImage.classList.remove('hidden');
            } else {
                ttImage.removeAttribute('src');
                ttImage.classList.add('hidden');
            }
        }

        var hasItems = Array.isArray(d.items) && d.items.length > 1;
        if (ttDetails) {
            if (hasItems) {
                renderItems(ttDetails, d.items);
            } else {
                renderDetails(ttDetails, d.details);
            }
        }

        var val = d.v || '';
        ttValueLabel.textContent = metricLabel || 'Value';

        if (ttValueBlock) {
            ttValueBlock.classList.toggle('hidden', hasItems);
        }

        if (d.r != null && !hasItems) {
            ttRank.textContent = '#' + d.r;
            ttRankRow.classList.remove('hidden');
            ttValue.textContent = val ? val + (metricLabel ? ' ' + metricLabel : '') : '';
        } else {
            ttRank.textContent = '';
            ttRankRow.classList.add('hidden');
            ttValue.textContent = hasItems ? '' : val + (metricLabel && val ? ' ' + metricLabel : '');
        }

        if (!hasItems && !ttValue.textContent) {
            ttValue.textContent = 'No value';
        }

        tooltip.classList.remove('hidden');
        positionTooltip(clientX, clientY);

        if (activeKey && activeKey !== key) {
            unhighlightState(activeKey);
        }

        highlightState(key);
        activeKey = key;
        activeSource = source || 'map';
    }

    function renderDetails(target, details) {
        target.textContent = '';

        if (!Array.isArray(details) || !details.length) {
            target.classList.add('hidden');
            return;
        }

        details.forEach(function (detail) {
            if (!detail || !detail.v) return;

            var row = document.createElement('p');
            row.style.margin = '0';
            row.style.fontSize = '10px';
            row.style.lineHeight = '1.3';
            row.style.fontWeight = '500';
            row.style.color = '#cbd5e1';

            var label = detail.l ? String(detail.l) + ': ' : '';
            row.textContent = label + detail.v;
            target.appendChild(row);
        });

        target.classList.toggle('hidden', target.childElementCount === 0);
    }

    function getFirstItemImage(items) {
        if (!Array.isArray(items)) return '';
        for (var i = 0; i < items.length; i += 1) {
            if (items[i] && items[i].img) return items[i].img;
        }
        return '';
    }

    function renderItems(target, items) {
        target.textContent = '';

        if (!Array.isArray(items) || !items.length) {
            target.classList.add('hidden');
            return;
        }

        items.forEach(function (item, index) {
            if (!item || !item.v) return;

            var block = document.createElement('div');
            block.style.margin = index === 0 ? '0' : '6px 0 0 0';
            block.style.paddingTop = index === 0 ? '0' : '6px';
            block.style.borderTop = index === 0 ? '0' : '1px solid rgba(148,163,184,.28)';

            var title = document.createElement('p');
            title.style.margin = '0';
            title.style.fontSize = '11px';
            title.style.lineHeight = '1.25';
            title.style.fontWeight = '800';
            title.style.color = '#f8fafc';
            title.textContent = item.v;
            block.appendChild(title);

            var detailText = formatItemDetails(item.details);
            if (detailText) {
                var meta = document.createElement('p');
                meta.style.margin = '2px 0 0 0';
                meta.style.fontSize = '10px';
                meta.style.lineHeight = '1.3';
                meta.style.fontWeight = '500';
                meta.style.color = '#cbd5e1';
                meta.textContent = detailText;
                block.appendChild(meta);
            }

            target.appendChild(block);
        });

        target.classList.toggle('hidden', target.childElementCount === 0);
    }

    function formatItemDetails(details) {
        if (!Array.isArray(details) || !details.length) return '';

        return details
            .filter(function (detail) { return detail && detail.v; })
            .map(function (detail) {
                return (detail.l ? String(detail.l) + ': ' : '') + detail.v;
            })
            .join(' • ');
    }

    function hideTooltip(force) {
        tooltip.classList.add('hidden');

        if (activeKey && (force || activeSource !== 'table')) {
            unhighlightState(activeKey);
            activeKey = null;
            activeSource = null;
        }
    }

    function focusStateBySlug(slug) {
        var key = getStateKeyBySlug(slug);
        if (!key) return;

        var path = getStatePath(key);
        if (!path) return;

        var rect = path.getBoundingClientRect();
        showTooltip(key, rect.left + rect.width / 2, rect.top + rect.height / 2, 'table');
    }

    function clearTableFocus() {
        if (!activeKey) return;
        unhighlightState(activeKey);
        activeKey = null;
        activeSource = null;
        tooltip.classList.add('hidden');
    }

    function navigateToState(key) {
        var url = getStateUrl(key);
        if (url) window.location.assign(url);
    }

    window.mapFns = {
        highlightBySlug: focusStateBySlug,
        clearHighlight: clearTableFocus,
        getStateKeyBySlug: getStateKeyBySlug
    };

    container.addEventListener('mousemove', function (e) {
        var key = getStateKey(e.target);
        if (key) {
            showTooltip(key, e.clientX, e.clientY, 'map');
        } else {
            hideTooltip(true);
        }
    });

    container.addEventListener('mouseleave', function () {
        hideTooltip(true);
    });

    container.addEventListener('touchstart', function (e) {
        var key = getStateKey(e.target);
        if (!key) return;
        e.preventDefault();
        var t = e.touches[0];
        if (activeKey === key) {
            navigateToState(key);
        } else {
            showTooltip(key, t.clientX, t.clientY, 'map');
        }
    }, { passive: false });

    container.addEventListener('click', function (e) {
        var key = getStateKey(e.target);
        if (key) navigateToState(key);
    });

    document.addEventListener('touchstart', function (e) {
        if (!container.contains(e.target)) hideTooltip(true);
    }, { passive: true });

    var svg = container.querySelector('svg');
    if (svg) {
        svg.querySelectorAll('g.state path').forEach(function (path) {
            var key = getStateKey(path);
            var record = getStateRecord(key);

            path.setAttribute('tabindex', '0');
            path.setAttribute('role', 'link');
            if (record && record.n) {
                path.setAttribute('aria-label', record.n);
            }

            path.addEventListener('focus', function (e) {
                var focusedKey = getStateKey(e.target);
                if (focusedKey) {
                    var rect = e.target.getBoundingClientRect();
                    showTooltip(focusedKey, rect.left + rect.width / 2, rect.top, 'map');
                }
            });
            path.addEventListener('blur', function () {
                hideTooltip(true);
            });
            path.addEventListener('keydown', function (e) {
                var focusedKey = getStateKey(e.target);
                if (!focusedKey) return;
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    navigateToState(focusedKey);
                }
            });
        });
    }
})();
