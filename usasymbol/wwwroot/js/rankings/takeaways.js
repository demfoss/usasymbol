(() => {
    document.querySelectorAll('.kt-rank[data-kt-table]').forEach(root => {
        const select = root.querySelector('[data-kt-you]');
        const result = root.querySelector('[data-kt-result]');
        const compare = root.querySelector('[data-kt-compare]');
        const tableLink = root.querySelector('.kt-rank__table-link');
        const scale = root.querySelector('[data-kt-scale]');

        const anyWrap = root.querySelector('[data-kt-compare-any-wrap]');
        const anySelect = root.querySelector('[data-kt-compare-any]');
        
        function populateAny(current) {
            if (!anySelect) return;
            anySelect.innerHTML = '';
            const first = document.createElement('option');
            first.value = '';
            first.textContent = 'Compare with any state…';
            anySelect.appendChild(first);
            Array.from(select.options).filter(o => o.value && o.value !== current).forEach(o => {
                const item = document.createElement('option');
                item.value = o.value;
                item.textContent = o.text;
                anySelect.appendChild(item);
            });
            anyWrap.hidden = false;
        }
        anySelect?.addEventListener('change', () => {
            if (!anySelect.value || !select.value) return;
            const a = select.value, b = anySelect.value;
            location.href = '/compare/' + (a < b ? a + '-vs-' + b : b + '-vs-' + a);
        });

        function update() {
            const option = select.selectedOptions[0];
            result.hidden = !option?.value;
            if (result.hidden) return;
            root.querySelector('[data-kt-statement]').textContent = option.dataset.statement;
            const typical = option.dataset.typical === 'true';
            root.querySelector('[data-kt-context]').textContent = typical
                ? option.text + ' is the typical state, right at the median. ' + option.dataset.context
                : option.dataset.context;
            const typicalLine = root.querySelector('[data-kt-typical]');
            if (typicalLine) typicalLine.hidden = typical;
            const marker = root.querySelector('[data-kt-marker]');
            if (marker) marker.classList.toggle('is-typical', typical);
            tableLink.dataset.ktRow = option.value;
            compare.hidden = !option.dataset.compareUrl;
            if (!compare.hidden) {
                compare.href = option.dataset.compareUrl;
                compare.textContent = 'Compare with ' + option.dataset.compareName + ' →';
            }
            populateAny(option.value);
            if (scale) {
                root.querySelector('[data-kt-marker]').style.left = option.dataset.position + '%';
                root.querySelector('[data-kt-scale-name]').textContent = option.text;
                scale.setAttribute('aria-label', option.dataset.statement + '. ' + root.querySelector('.kt-rank__typical').textContent.trim() + ' ' + scale.dataset.ktScaleLeader + '.');
            }
        }
        const share = root.querySelector('[data-kt-share]');
        select.addEventListener('change', () => {
            const note = root.querySelector('[data-kt-geo]');
            if (note) note.hidden = true;
            update();
            if (select.value) history.replaceState(null, '', '#state-' + select.value);
        });
        // Deep link: /page#state-california preselects that state.
        const hash = location.hash.match(/^#state-([a-z0-9-]+)$/);
        if (hash && Array.from(select.options).some(o => o.value === hash[1])) select.value = hash[1];
        share?.addEventListener('click', async () => {
            const option = select.selectedOptions[0];
            if (!option?.value) return;
            const url = location.origin + location.pathname + '#state-' + option.value;
            const text = option.dataset.statement + '.';
            const label = share.querySelector('span');
            try {
                if (navigator.share) { await navigator.share({ title: document.title, text, url }); return; }
                await navigator.clipboard.writeText(text + ' ' + url);
                label.textContent = 'Link copied';
                setTimeout(() => { label.textContent = 'Share'; }, 1800);
            } catch (_) { }
        });
        // Restore a browser-preserved selection on reload and back navigation.
        window.addEventListener('pageshow', update);
        update();

        // Preselect the visitor's state when the CDN reports one (U.S. visitors only).
        // A hash link or an existing choice always wins, and the picker stays fully editable.
        async function preselectFromLocation() {
            if (select.value) return;
            let postal = null;
            // ?geo=OH forces a state in Development builds, for testing without a CDN.
            const forced = new URLSearchParams(location.search).get('geo');
            try { if (!forced) postal = sessionStorage.getItem('kt-visitor-postal'); } catch (_) { }
            if (postal === null) {
                try {
                    const response = await fetch('/api/visitor/state' + (forced ? '?debug=' + encodeURIComponent(forced) : ''), { credentials: 'same-origin' });
                    if (response.ok) postal = (await response.json()).postal || '';
                } catch (_) { postal = ''; }
                try { if (!forced) sessionStorage.setItem('kt-visitor-postal', postal || ''); } catch (_) { }
            }
            if (!postal || select.value) return;
            const match = Array.from(select.options).find(o => o.dataset.postal === postal);
            if (!match) return;
            select.value = match.value;
            update();
            const note = root.querySelector('[data-kt-geo]');
            if (note) note.hidden = false;
        }
        preselectFromLocation();

        function goToRow(slug) {
            const table = document.getElementById(root.dataset.ktTable);
            const row = table && Array.from(table.querySelectorAll('tr[data-state-slug]'))
                .find(item => item.dataset.stateSlug === slug);
            if (!row) return false;
            document.dispatchEvent(new CustomEvent('ranking:reveal-row', { detail: { row } }));
            requestAnimationFrame(() => {
                row.scrollIntoView({ behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth', block: 'center' });
                row.classList.remove('kt-row-flash');
                void row.offsetWidth;
                row.classList.add('kt-row-flash');
                row.setAttribute('tabindex', '-1');
                row.focus({ preventScroll: true });
            });
            return true;
        }

        root.addEventListener('click', event => {
            const link = event.target.closest('a[data-kt-row]');
            if (link && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey && goToRow(link.dataset.ktRow))
                event.preventDefault();

            const cite = event.target.closest('[data-kt-cite]');
            if (!cite) return;
            const citation = cite.dataset.ktCite;
            const label = cite.querySelector('span');
            const copied = () => {
                label.textContent = 'Copied';
                clearTimeout(cite.copyTimer);
                cite.copyTimer = setTimeout(() => { label.textContent = 'Cite this data'; }, 1800);
            };
            if (navigator.clipboard?.writeText) {
                navigator.clipboard.writeText(citation).then(copied, () => window.prompt('Copy this citation:', citation));
            } else {
                window.prompt('Copy this citation:', citation);
            }
        });
    });
})();
