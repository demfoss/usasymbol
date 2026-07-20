(function () {
    'use strict';

    var container = document.getElementById('usmap-container');
    if (!container) return;
    var hasBooted = false;

    function boot() {
        if (hasBooted) return;
        hasBooted = true;
        var wrapper = document.getElementById('usmap-wrapper');
        var tooltip = document.getElementById('map-tooltip');
        var tooltipState = document.getElementById('tt-state');
        var tooltipValue = document.getElementById('tt-value');
        var tooltipDot = document.getElementById('tt-status-dot');
        var dataEl = document.getElementById('choropleth-data');
        var filterGroupsEl = document.getElementById('map-filter-groups-data');
        var filtersHost = document.getElementById('map-filter-groups');
        var searchInput = document.getElementById('map-search-input');
        var searchResults = document.getElementById('map-search-results');
        var drawer = document.getElementById('map-drawer');
        var drawerBackdrop = document.getElementById('map-drawer-backdrop');
        var drawerClose = document.getElementById('map-drawer-close');
        var drawerFlag = document.getElementById('drawer-flag');
        var drawerState = document.getElementById('map-drawer-state');
        var drawerValue = document.getElementById('drawer-value');
        var drawerRank = document.getElementById('drawer-rank');
        var drawerSummary = document.getElementById('drawer-summary');
        var drawerDetails = document.getElementById('drawer-details');
        var drawerItems = document.getElementById('drawer-items');
        var drawerStateInfo = document.getElementById('drawer-state-info');
        var drawerDot = document.getElementById('drawer-status-dot');
        var drawerLink = document.getElementById('drawer-view-link');
        var smallStateButtons = Array.prototype.slice.call(document.querySelectorAll('.map-small-state'));
        var clearFiltersBtn = document.getElementById('map-clear-filters');

    if (!container || !wrapper || !tooltip || !dataEl) return;

    var stateData = parseJson(dataEl.textContent, {});
    var filterGroups = parseJson(filterGroupsEl && filterGroupsEl.textContent, []);
    var activeFilters = {};
    var activeLegendBucket = null;
    var legendEl = document.getElementById('map-legend');
    var legendStepButtons = legendEl ? Array.prototype.slice.call(legendEl.querySelectorAll('.map-legend-step')) : [];
    var metricLabel = (dataEl.dataset.metricLabel || '').trim();
    var iconOverlay = null; // cached after first filter interaction
    var searchableStates = [];
    var activeSearchIndex = -1;

    Object.keys(stateData).forEach(function (key) {
        var record = stateData[key];
        if (!record) return;
        record.key = key.toUpperCase();
        record.fill = record.f || '#e2e8f0';
        searchableStates.push({
            key: record.key,
            name: record.n || record.key
        });
    });

    searchableStates.sort(function (a, b) {
        return a.name.localeCompare(b.name);
    });

    // Value-based legend buckets (max 5). Quintile cut points are snapped to
    // distinct values so equal values never split across buckets and chip
    // ranges never overlap (no more "0 – 0" / "0 – 2" / "2 – 8").
    var legendBuckets = [];
    (function () {
        var sortedKeys = Object.keys(stateData)
            .filter(function (k) { return stateData[k] && stateData[k].nv != null; })
            .sort(function (a, b) { return stateData[a].nv - stateData[b].nv; });
        var n = sortedKeys.length;
        if (!n) return;

        var displayByValue = {};
        var distinct = [];
        sortedKeys.forEach(function (k) {
            var v = stateData[k].nv;
            if (!distinct.length || distinct[distinct.length - 1] !== v) distinct.push(v);
            if (!(v in displayByValue)) displayByValue[v] = stateData[k].v || String(v);
        });

        // Upper bound (inclusive) of each bucket, snapped to distinct values
        var cuts = [];
        if (distinct.length <= 5) {
            cuts = distinct.slice();
        } else {
            for (var b = 1; b <= 5; b++) {
                var v = stateData[sortedKeys[Math.min(n - 1, Math.ceil(b * n / 5) - 1)]].nv;
                if (!cuts.length || cuts[cuts.length - 1] < v) cuts.push(v);
            }
            var maxVal = distinct[distinct.length - 1];
            if (cuts[cuts.length - 1] < maxVal) cuts[cuts.length - 1] = maxVal;
        }

        var lo = distinct[0];
        cuts.forEach(function (hi) {
            legendBuckets.push({
                lo: lo,
                hi: hi,
                count: 0,
                label: lo === hi
                    ? displayByValue[lo]
                    : displayByValue[lo] + ' – ' + displayByValue[hi]
            });
            for (var j = 0; j < distinct.length; j++) {
                if (distinct[j] > hi) { lo = distinct[j]; break; }
            }
        });

        sortedKeys.forEach(function (k) {
            var v = stateData[k].nv;
            for (var i = 0; i < legendBuckets.length; i++) {
                if (v <= legendBuckets[i].hi) {
                    stateData[k].legendBucket = i;
                    legendBuckets[i].count++;
                    break;
                }
            }
        });
    })();

    function parseJson(raw, fallback) {
        if (!raw) return fallback;
        try { return JSON.parse(raw); } catch (error) { return fallback; }
    }

    function getStateRecord(key) {
        if (!key) return null;
        return stateData[String(key).toUpperCase()] || null;
    }

    function getStateKey(el) {
        if (!el || el.tagName !== 'path') return null;
        var cls = Array.prototype.slice.call(el.classList);
        return cls.find(function (name) { return name.length === 2 && /^[a-z]+$/.test(name); }) || null;
    }

    function getStatePath(key) {
        if (!key) return null;
        return container.querySelector('.' + String(key).toLowerCase());
    }

    function getStateUrl(key) {
        var record = getStateRecord(key);
        if (!record) return null;
        if (record.s) return '/states/' + record.s;
        if (record.n) return '/states/' + String(record.n).trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
        return null;
    }

    function getOptionColor(group, option) {
        if (!group) return '#0f766e';

        var matchingColor = null;
        var hasMultipleColors = false;

        for (var key in stateData) {
            if (!Object.prototype.hasOwnProperty.call(stateData, key)) continue;
            var record = stateData[key];
            var values = getFilterValues(record, group.key);
            if (values.indexOf(option) < 0) continue;

            var fill = record.fill || '#0f766e';
            if (matchingColor === null) {
                matchingColor = fill;
                continue;
            }

            if (matchingColor !== fill) {
                hasMultipleColors = true;
                if (!group.useColors) break;
            }
        }

        if (matchingColor && (!hasMultipleColors || group.useColors)) {
            return matchingColor;
        }

        return group.accentColor || matchingColor || '#0f766e';
    }

    function getFilterValues(record, filterKey) {
        if (!record || !record.filters) return [];
        var values = record.filters[filterKey];
        return Array.isArray(values) ? values : [];
    }

    function matchesFilters(record) {
        var groupsMatch = filterGroups.every(function (group) {
            var selected = activeFilters[group.key];
            if (!selected) return true;
            return getFilterValues(record, group.key).some(function (value) {
                return value === selected;
            });
        });
        if (!groupsMatch) return false;

        if (activeLegendBucket !== null) {
            return record.legendBucket === activeLegendBucket;
        }

        return true;
    }

    function setStateVisual(key, record, isMatch, activeColor) {
        var path = getStatePath(key);
        if (!path || !record) return;

        path.dataset.baseFill = record.fill;
        path.style.fill = isMatch ? (activeColor || record.fill) : '#ffffff';
        path.style.opacity = '1';
    }

    function toDetailParts(value) {
        var text = String(value || '').trim();
        var match = text.match(/^\[([^\]]+)\]\(([^)]+)\)$/);
        if (!match) return null;

        return {
            label: match[1],
            href: match[2]
        };
    }

    function renderPills(target, values) {
        target.textContent = '';
        values.forEach(function (value) {
            var pill = document.createElement('span');
            pill.className = 'map-detail-pill';
            var detailLink = toDetailParts(value);

            if (detailLink) {
                var link = document.createElement('a');
                link.className = 'map-detail-pill__link';
                link.href = detailLink.href;
                link.textContent = detailLink.label;
                pill.appendChild(link);
            } else {
                pill.textContent = value;
            }

            target.appendChild(pill);
        });
    }

    function renderDetailGroups(target, details) {
        target.textContent = '';

        if (!Array.isArray(details) || !details.length) {
            target.classList.add('hidden');
            return;
        }

        details.forEach(function (detail) {
            if (!detail || !detail.v) return;

            var group = document.createElement('div');
            group.className = 'map-drawer__detail-group';

            var label = document.createElement('div');
            label.className = 'map-drawer__detail-label';
            label.textContent = detail.l || 'Detail';

            var values = document.createElement('div');
            values.className = 'map-drawer__detail-values';
            renderPills(values, [detail.v]);

            group.appendChild(label);
            group.appendChild(values);
            target.appendChild(group);
        });

        target.classList.toggle('hidden', target.childElementCount === 0);
    }

    function renderItems(target, items) {
        target.textContent = '';

        if (!Array.isArray(items) || !items.length) {
            target.classList.add('hidden');
            return;
        }

        items.forEach(function (item) {
            if (!item || !item.v) return;

            var block = document.createElement('div');
            block.className = 'map-drawer__item';

            var title = document.createElement('div');
            title.className = 'map-drawer__item-title';
            title.textContent = item.v;

            var text = document.createElement('div');
            text.className = 'map-drawer__item-text';

            var values = (item.details || [])
                .filter(function (detail) { return detail && detail.v; })
                .map(function (detail) { return (detail.l ? detail.l + ': ' : '') + detail.v; });

            renderPills(text, values.length ? values : [item.v]);

            block.appendChild(title);
            block.appendChild(text);
            target.appendChild(block);
        });

        target.classList.toggle('hidden', target.childElementCount === 0);
    }

    function fillTooltip(record) {
        tooltipState.textContent = record.n || '';
        tooltipValue.textContent = metricLabel
            ? metricLabel + ': ' + (record.v || 'No value')
            : (record.v || 'No value');
        tooltipDot.style.background = record.fill;
    }

    function fillDrawer(record) {
        drawerState.textContent = record.n || '';
        drawerValue.textContent = record.v || (metricLabel || 'No value');
        drawerDot.style.background = record.fill;
        drawerValue.setAttribute('data-label', metricLabel || 'Value');

        if (drawerRank) {
            if (record.r) {
                drawerRank.textContent = 'Rank #' + record.r;
                drawerRank.classList.remove('hidden');
            } else {
                drawerRank.textContent = '';
                drawerRank.classList.add('hidden');
            }
        }

        if (drawerFlag) {
            if (record.flag) {
                drawerFlag.src = record.flag;
                drawerFlag.alt = (record.n || 'State') + ' flag';
                drawerFlag.classList.remove('hidden');
            } else {
                drawerFlag.removeAttribute('src');
                drawerFlag.alt = '';
                drawerFlag.classList.add('hidden');
            }
        }

        if (record.summary) {
            drawerSummary.textContent = record.summary;
            drawerSummary.classList.remove('hidden');
        } else {
            drawerSummary.textContent = '';
            drawerSummary.classList.add('hidden');
        }

        var allDetails = Array.isArray(record.details) ? record.details : [];
        var primaryDetails = allDetails.filter(function (d) { return d.g !== 'state'; });
        var stateInfoDetails = allDetails.filter(function (d) { return d.g === 'state'; });

        renderDetailGroups(drawerDetails, primaryDetails);
        renderItems(drawerItems, record.items);
        if (drawerStateInfo) renderDetailGroups(drawerStateInfo, stateInfoDetails);
        var stateUrl = getStateUrl(record.key);
        drawerLink.href = stateUrl || '#';
        drawerLink.classList.toggle('is-disabled', !stateUrl);
        drawerLink.setAttribute('aria-disabled', stateUrl ? 'false' : 'true');
        drawerLink.dataset.href = stateUrl || '';
    }

    function positionTooltip(clientX, clientY) {
        var rect = container.getBoundingClientRect();
        var width = tooltip.offsetWidth || 260;
        var height = tooltip.offsetHeight || 180;
        var left = clientX - rect.left + 18;
        var top = clientY - rect.top - height - 18;

        if (left + width > rect.width - 6) left = rect.width - width - 6;
        if (left < 6) left = 6;
        if (top < 6) top = clientY - rect.top + 18;
        if (top + height > rect.height - 6) top = rect.height - height - 6;

        tooltip.style.left = left + 'px';
        tooltip.style.top = top + 'px';
    }

    function showTooltip(key, clientX, clientY) {
        var record = getStateRecord(key);
        if (!record || !matchesFilters(record)) return;

        fillTooltip(record);
        tooltip.classList.remove('hidden');
        positionTooltip(clientX, clientY);
    }

    function hideTooltip() {
        tooltip.classList.add('hidden');
    }

    function openDrawer(key) {
        var record = getStateRecord(key);
        if (!record) return;

        fillDrawer(record);
        drawer.classList.add('is-open');
        drawer.setAttribute('aria-hidden', 'false');
        drawerBackdrop.classList.remove('hidden');
    }

    function closeDrawer() {
        drawer.classList.remove('is-open');
        drawer.setAttribute('aria-hidden', 'true');
        drawerBackdrop.classList.add('hidden');
    }

    function hideSearchResults() {
        if (!searchResults || !searchInput) return;
        searchResults.classList.add('hidden');
        searchInput.setAttribute('aria-expanded', 'false');
        activeSearchIndex = -1;
    }

    function showSearchResults() {
        if (!searchResults || !searchInput) return;
        searchResults.classList.remove('hidden');
        searchInput.setAttribute('aria-expanded', 'true');
    }

    function getVisibleSearchOptions() {
        if (!searchResults) return [];
        return Array.prototype.slice.call(searchResults.querySelectorAll('.map-search__option'));
    }

    function setActiveSearchOption(index) {
        var options = getVisibleSearchOptions();
        activeSearchIndex = options.length ? Math.max(0, Math.min(index, options.length - 1)) : -1;

        options.forEach(function (option, optionIndex) {
            var isActive = optionIndex === activeSearchIndex;
            option.classList.toggle('is-active', isActive);
            option.setAttribute('aria-selected', isActive ? 'true' : 'false');
            if (isActive) option.scrollIntoView({ block: 'nearest' });
        });
    }

    function selectSearchResult(item) {
        if (!item) return;
        if (searchInput) searchInput.value = item.name;
        hideSearchResults();
        hideTooltip();
        openDrawer(item.key);
    }

    function renderSearchResults(query) {
        if (!searchResults) return;

        var normalized = (query || '').trim().toLowerCase();
        var items = searchableStates.filter(function (item) {
            return !normalized || item.name.toLowerCase().indexOf(normalized) >= 0;
        }).slice(0, 60);

        searchResults.textContent = '';

        if (!items.length) {
            hideSearchResults();
            return;
        }

        items.forEach(function (item) {
            var option = document.createElement('button');
            option.type = 'button';
            option.className = 'map-search__option';
            option.setAttribute('role', 'option');
            option.setAttribute('aria-selected', 'false');
            option.textContent = item.name;
            option.addEventListener('click', function () {
                selectSearchResult(item);
            });
            searchResults.appendChild(option);
        });

        showSearchResults();
        setActiveSearchOption(0);
    }

    function applyFilters() {
        var selectedGroup = null;
        var selectedOption = null;

        filterGroups.forEach(function (group) {
            if (activeFilters[group.key]) {
                selectedGroup = group;
                selectedOption = activeFilters[group.key];
            }
        });

        Object.keys(stateData).forEach(function (key) {
            var record = stateData[key];
            var isMatch = matchesFilters(record);
            var activeColor = selectedGroup && selectedOption ? getOptionColor(selectedGroup, selectedOption) : record.fill;
            setStateVisual(key, record, isMatch, activeColor);
        });

        smallStateButtons.forEach(function (button) {
            var key = button.getAttribute('data-state-key');
            var record = getStateRecord(key);
            if (!record) return;

            var isMatch = matchesFilters(record);
            button.style.opacity = isMatch ? '1' : '0.3';
            button.style.background = selectedGroup && selectedOption && isMatch
                ? getOptionColor(selectedGroup, selectedOption)
                : (isMatch ? record.fill : '#ffffff');
            button.style.color = isMatch ? '#ffffff' : '#334155';
        });

        if (!iconOverlay) iconOverlay = container.querySelector('#icon-overlay');
        if (iconOverlay) {
            iconOverlay.querySelectorAll('g[data-postal]').forEach(function (g) {
                var postal = g.getAttribute('data-postal').toUpperCase();
                var record = stateData[postal];
                var isMatch = !record || matchesFilters(record);
                g.style.display = isMatch ? '' : 'none';
            });
        }

        var textOverlay = container.querySelector('#text-overlay');
        if (textOverlay) {
            textOverlay.querySelectorAll('g[data-postal]').forEach(function (g) {
                var postal = g.getAttribute('data-postal').toUpperCase();
                var record = stateData[postal];
                var isMatch = !record || matchesFilters(record);
                g.style.display = isMatch ? '' : 'none';
            });
        }

        syncLegendButtons();
        syncClearButton();
    }

    function syncClearButton() {
        if (!clearFiltersBtn) return;
        var hasActive = activeLegendBucket !== null ||
            filterGroups.some(function (g) { return !!activeFilters[g.key]; });
        clearFiltersBtn.classList.toggle('hidden', !hasActive);
    }

    function buildFilterCounts(group) {
        var counts = {};
        Object.keys(stateData).forEach(function (key) {
            getFilterValues(stateData[key], group.key).forEach(function (value) {
                counts[value] = (counts[value] || 0) + 1;
            });
        });
        return counts;
    }

    function renderFilters() {
        if (!filtersHost || !Array.isArray(filterGroups) || !filterGroups.length) return;

        filtersHost.textContent = '';

        filterGroups.forEach(function (group) {
            var counts = buildFilterCounts(group);
            var options = Object.keys(counts).sort(function (a, b) { return counts[b] - counts[a]; });
            if (!options.length) return;

            var wrap = document.createElement('div');
            wrap.className = 'map-filter-group';

            var label = document.createElement('div');
            label.className = 'map-filter-label';
            label.textContent = group.label || 'Filter';

            var list = document.createElement('div');
            list.className = 'map-filter-options';
            var collapseAfter = 8;
            var hasOverflow = options.length > collapseAfter;

            options.forEach(function (option) {
                var button = document.createElement('button');
                var count = document.createElement('span');
                button.type = 'button';
                button.className = 'map-filter-chip';
                if (hasOverflow && list.childElementCount >= collapseAfter) {
                    button.classList.add('map-filter-chip--overflow');
                }
                button.dataset.filterKey = group.key;
                button.dataset.filterValue = option;
                button.style.setProperty('--chip-fill', getOptionColor(group, option));
                button.innerHTML =
                    '<span class="map-filter-chip__label-wrap">' +
                        '<span class="map-filter-chip__dot"></span>' +
                        '<span class="map-filter-chip__label">' + option + '</span>' +
                    '</span>';

                count.className = 'map-filter-chip__count';
                count.textContent = String(counts[option]);
                button.appendChild(count);

                button.addEventListener('click', function () {
                    activeFilters[group.key] = activeFilters[group.key] === option ? null : option;
                    syncFilterButtons();
                    applyFilters();
                });

                list.appendChild(button);
            });

            wrap.appendChild(label);
            wrap.appendChild(list);

            if (hasOverflow) {
                wrap.classList.add('is-collapsed');

                var toggle = document.createElement('button');
                toggle.type = 'button';
                toggle.className = 'map-filter-more';
                toggle.textContent = 'Show all (' + options.length + ')';
                toggle.addEventListener('click', function () {
                    var expanded = wrap.classList.toggle('is-expanded');
                    wrap.classList.toggle('is-collapsed', !expanded);
                    toggle.textContent = expanded ? 'Show less' : 'Show all (' + options.length + ')';
                });

                wrap.appendChild(toggle);
            }

            filtersHost.appendChild(wrap);
        });

        syncFilterButtons();
    }

    function syncFilterButtons() {
        Array.prototype.forEach.call(filtersHost.querySelectorAll('.map-filter-chip:not(.map-filter-chip--range)'), function (button) {
            var key = button.dataset.filterKey;
            var value = button.dataset.filterValue;
            button.classList.toggle('is-active', activeFilters[key] === value);
        });
    }

    function syncLegendButtons() {
        if (!legendEl) return;
        legendEl.classList.toggle('has-active', activeLegendBucket !== null);
        legendStepButtons.forEach(function (button) {
            var idx = parseInt(button.getAttribute('data-step-index'), 10);
            var bucketIdx = isNaN(idx) ? -1 : parseInt(button.dataset.legendBucket || idx, 10);
            var isActive = activeLegendBucket !== null && bucketIdx === activeLegendBucket;
            button.classList.toggle('is-active', isActive);
            button.setAttribute('aria-pressed', isActive ? 'true' : 'false');
        });
        var chipsEl = document.getElementById('map-legend-chips');
        if (chipsEl) {
            Array.prototype.forEach.call(chipsEl.querySelectorAll('.map-filter-chip--range'), function (chip) {
                var idx = parseInt(chip.getAttribute('data-bucket-index'), 10);
                var isActive = activeLegendBucket !== null && idx === activeLegendBucket;
                chip.classList.toggle('is-active', isActive);
                chip.setAttribute('aria-pressed', isActive ? 'true' : 'false');
            });
        }
    }

    function initLegend() {
        if (!legendStepButtons.length || !legendBuckets.length) return;

        // The bottom legend renders one swatch per gradient step; buckets are
        // value-based and can be fewer, so each step maps to its nearest bucket.
        function bucketForStep(idx) {
            if (legendBuckets.length >= legendStepButtons.length) return Math.min(idx, legendBuckets.length - 1);
            return Math.min(legendBuckets.length - 1,
                Math.floor(idx * legendBuckets.length / legendStepButtons.length));
        }

        legendStepButtons.forEach(function (button) {
            var idx = parseInt(button.getAttribute('data-step-index'), 10);
            if (isNaN(idx) || idx < 0) return;
            var bucketIdx = bucketForStep(idx);
            button.dataset.legendBucket = String(bucketIdx);

            button.addEventListener('click', function () {
                activeLegendBucket = activeLegendBucket === bucketIdx ? null : bucketIdx;
                applyFilters();
            });
        });

        // Render gradient filter chips ABOVE the map when there are no categorical filters
        if (!filtersHost || filterGroups.length) return;

        var groupWrap = document.createElement('div');
        groupWrap.className = 'map-filter-group';

        var groupLabel = document.createElement('div');
        groupLabel.className = 'map-filter-label';
        groupLabel.textContent = 'Filter by ' + (metricLabel || 'value');
        groupWrap.appendChild(groupLabel);

        var chipsList = document.createElement('div');
        chipsList.className = 'map-filter-options';
        chipsList.id = 'map-legend-chips';

        legendBuckets.forEach(function (bucket, i) {
            if (!bucket.count) return;

            // Pick the swatch color of the legend step closest to this bucket
            var stepIdx = legendBuckets.length > 1
                ? Math.round(i * (legendStepButtons.length - 1) / (legendBuckets.length - 1))
                : legendStepButtons.length - 1;
            var swatchEl = legendStepButtons[stepIdx] && legendStepButtons[stepIdx].querySelector('.map-legend-step__swatch');
            var color = swatchEl ? swatchEl.style.background : '#94a3b8';

            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'map-filter-chip map-filter-chip--range';
            chip.style.setProperty('--chip-fill', color);
            chip.setAttribute('data-bucket-index', String(i));

            chip.setAttribute('aria-pressed', 'false');
            chip.innerHTML =
                '<span class="map-filter-chip__label-wrap">' +
                    '<span class="map-filter-chip__range-swatch" style="background:' + color + '"></span>' +
                    '<span class="map-filter-chip__label">' + bucket.label + '</span>' +
                '</span>';

            var countSpan = document.createElement('span');
            countSpan.className = 'map-filter-chip__count';
            countSpan.textContent = String(bucket.count);
            chip.appendChild(countSpan);

            chip.addEventListener('click', function () {
                activeLegendBucket = activeLegendBucket === i ? null : i;
                applyFilters();
            });

            chipsList.appendChild(chip);
        });

        groupWrap.appendChild(chipsList);
        filtersHost.appendChild(groupWrap);
    }

    function focusStateFromButton(key, button) {
        var target = button || getStatePath(key);
        if (!target) return;
        var rect = target.getBoundingClientRect();
        showTooltip(key, rect.left + rect.width / 2, rect.top + rect.height / 2);
    }

    function openStateFromButton(key, button) {
        focusStateFromButton(key, button);
        openDrawer(key);
    }

    container.addEventListener('mousemove', function (event) {
        var key = getStateKey(event.target);
        if (!key) {
            hideTooltip();
            return;
        }
        showTooltip(key, event.clientX, event.clientY);
    });

    wrapper.addEventListener('mouseleave', hideTooltip);

    container.addEventListener('click', function (event) {
        var key = getStateKey(event.target);
        if (!key) return;
        event.preventDefault();
        hideTooltip();
        openDrawer(key);
    });

    container.addEventListener('touchstart', function (event) {
        var key = getStateKey(event.target);
        if (!key) return;
        event.preventDefault();
        openDrawer(key);
    }, { passive: false });

    smallStateButtons.forEach(function (button) {
        button.addEventListener('mouseenter', function () {
            focusStateFromButton(button.getAttribute('data-state-key'), button);
        });
        button.addEventListener('mousemove', function () {
            focusStateFromButton(button.getAttribute('data-state-key'), button);
        });
        button.addEventListener('focus', function () {
            focusStateFromButton(button.getAttribute('data-state-key'), button);
        });
        button.addEventListener('blur', hideTooltip);
        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            openStateFromButton(button.getAttribute('data-state-key'), button);
        });
        button.addEventListener('touchstart', function (event) {
            event.preventDefault();
            event.stopPropagation();
            openStateFromButton(button.getAttribute('data-state-key'), button);
        }, { passive: false });
    });

    if (drawerClose) drawerClose.addEventListener('click', closeDrawer);
    if (drawerBackdrop) drawerBackdrop.addEventListener('click', closeDrawer);
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener('click', function () {
            activeLegendBucket = null;
            filterGroups.forEach(function (g) { activeFilters[g.key] = null; });
            syncFilterButtons();
            applyFilters();
        });
    }
    if (drawerLink) {
        drawerLink.addEventListener('click', function (event) {
            var href = drawerLink.dataset.href || drawerLink.getAttribute('href') || '';
            if (!href || href === '#') {
                event.preventDefault();
                return;
            }

            event.preventDefault();
            window.location.assign(href);
        });
    }
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') closeDrawer();
    });

    if (searchInput && searchResults) {
        searchInput.addEventListener('focus', function () {
            renderSearchResults(searchInput.value);
        });
        searchInput.addEventListener('input', function () {
            renderSearchResults(searchInput.value);
        });
        searchInput.addEventListener('keydown', function (event) {
            var options = getVisibleSearchOptions();
            if (!options.length && event.key !== 'Escape') return;

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                setActiveSearchOption(activeSearchIndex + 1);
                return;
            }

            if (event.key === 'ArrowUp') {
                event.preventDefault();
                setActiveSearchOption(activeSearchIndex <= 0 ? options.length - 1 : activeSearchIndex - 1);
                return;
            }

            if (event.key === 'Enter') {
                if (activeSearchIndex < 0 || !options[activeSearchIndex]) return;
                event.preventDefault();
                var selectedName = options[activeSearchIndex].textContent || '';
                var selectedItem = searchableStates.find(function (item) {
                    return item.name === selectedName;
                });
                selectSearchResult(selectedItem);
                return;
            }

            if (event.key === 'Escape') {
                hideSearchResults();
            }
        });
        document.addEventListener('click', function (event) {
            var target = event.target;
            if (target === searchInput || searchResults.contains(target)) return;
            hideSearchResults();
        });
    }

    var svg = container.querySelector('svg');
    if (svg) {
        svg.querySelectorAll('g.state path').forEach(function (path) {
            var key = getStateKey(path);
            var record = getStateRecord(key);
            if (!record) return;

            path.setAttribute('tabindex', '0');
            path.setAttribute('role', 'button');
            path.setAttribute('aria-label', record.n || key);

            path.addEventListener('focus', function (event) {
                var rect = event.target.getBoundingClientRect();
                showTooltip(key, rect.left + rect.width / 2, rect.top);
            });

            path.addEventListener('blur', hideTooltip);

            path.addEventListener('keydown', function (event) {
                if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    openDrawer(key);
                }
            });
        });
    }

    renderFilters();
    applyFilters();
    initLegend();
    } // end boot()

    if (container.querySelector('svg')) {
        boot();
        return;
    }

    if (container.dataset.svgSrc) {
        document.addEventListener('map-svg-ready', function () {
            if (container.querySelector('svg')) {
                boot();
            }
        }, { once: true });
        return;
    }

    boot();
})();
