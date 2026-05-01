/**
 * Universal Ranking Table JS
 * Handles: Search, Sort, View Toggle, Metric Toggle
 */
document.addEventListener('DOMContentLoaded', function () {
    initViewToggle();
    initSearch();
    initSort();
    initMetricToggle();
    initRowClick();
    initRowHoverSync();
});

// ============================================
// VIEW TOGGLE (Cards / Table)
// ============================================
function initViewToggle() {
    const viewBtns = document.querySelectorAll('.viewBtn');
    const cardsView = document.getElementById('cardsView');
    const tableView = document.getElementById('tableView');

    if (!cardsView || !tableView) return;

    const ACTIVE = ['bg-white', 'text-slate-900', 'shadow-sm'];
    const INACTIVE_ADD = ['text-slate-600'];
    const INACTIVE_REMOVE = ['bg-white', 'text-slate-900', 'shadow-sm'];

    // Tailwind lg breakpoint
    const mq = window.matchMedia('(min-width: 1024px)');

    // One key, but separated prefs
    const STORAGE_KEY = 'rankingViewPrefs_v1';
    // { mobile: { view, touched }, desktop: { view, touched } }

    function defaultForBreakpoint() {
        return mq.matches ? 'table' : 'cards';
    }

    function readPrefs() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            const p = raw ? JSON.parse(raw) : null;

            return {
                mobile: {
                    view: (p.mobile.view === 'table' ? 'table' : 'cards'),
                    touched: !!p.mobile.touched
                },
                desktop: {
                    view: (p.desktop.view === 'cards' ? 'cards' : 'table'),
                    touched: !!p.desktop.touched
                }
            };
        } catch {
            return {
                mobile: { view: 'cards', touched: false },
                desktop: { view: 'table', touched: false }
            };
        }
    }

    function writePrefs(prefs) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    }

    function modeKey() {
        return mq.matches ? 'desktop' : 'mobile';
    }

    function getViewToApply() {
        const prefs = readPrefs();
        const key = modeKey();

        // if user never touched toggle for this breakpoint -> use smart default
        if (!prefs[key].touched) return defaultForBreakpoint();

        // otherwise use saved view
        return prefs[key].view;
    }

    // initial
    setView(getViewToApply());

    // when crossing breakpoint -> re-apply relevant mode
    mq.addEventListener('change', () => {
        setView(getViewToApply());
    });

    // manual toggle -> mark touched + save view for that breakpoint
    if (viewBtns.length) {
        viewBtns.forEach(btn => {
            btn.addEventListener('click', function () {
                const view = this.dataset.view; // 'cards' | 'table'
                setView(view);

                const prefs = readPrefs();
                const key = modeKey();
                prefs[key] = { view, touched: true };
                writePrefs(prefs);
            });
        });
    }

    function setView(view) {
        // buttons
        if (viewBtns.length) {
            viewBtns.forEach(b => {
                b.classList.remove(...INACTIVE_REMOVE);
                b.classList.add(...INACTIVE_ADD);
                b.setAttribute('aria-pressed', 'false');
            });

            const activeBtn = document.querySelector(`.viewBtn[data-view="${view}"]`);
            if (activeBtn) {
                activeBtn.classList.remove(...INACTIVE_ADD);
                activeBtn.classList.add(...ACTIVE);
                activeBtn.setAttribute('aria-pressed', 'true');
            }
        }

        // views
        if (view === 'cards') {
            cardsView.classList.remove('hidden');
            tableView.classList.add('hidden');
        } else {
            tableView.classList.remove('hidden');
            cardsView.classList.add('hidden');
        }
    }
}

// ============================================
// SEARCH (across ALL ranking tables)
// ============================================
function initSearch() {
    const searchInput = document.getElementById('tableSearch');
    if (!searchInput) return;

    const tableEmptyState = document.getElementById('tableEmptyState');
    const cardsEmptyState = document.getElementById('cardsEmptyState');
    const resultsCounter = document.getElementById('resultsCounter');

    // All rows in all tbodies
    const allRows = document.querySelectorAll('.rankingTbody .tableRow');
    const cards = document.querySelectorAll('.rankingCard');

    searchInput.addEventListener('input', function () {
        const query = this.value.toLowerCase().trim();
        let visibleCount = 0;

        // Filter ALL table rows (all tables)
        allRows.forEach(row => {
            const searchText = (row.dataset.search || row.dataset.state || '').toLowerCase();
            const matches = searchText.includes(query);

            // keep existing pattern: hidden class
            row.classList.toggle('hidden', !matches);

            if (matches) visibleCount++;
        });

        // Filter cards
        cards.forEach(card => {
            const searchText = (card.dataset.search || card.dataset.state || '').toLowerCase();
            const matches = searchText.includes(query);
            card.classList.toggle('hidden', !matches);
        });

        // Empty states
        if (tableEmptyState) tableEmptyState.classList.toggle('hidden', visibleCount > 0);
        if (cardsEmptyState) cardsEmptyState.classList.toggle('hidden', visibleCount > 0);

        // Counter (counts table rows or cards)
        if (resultsCounter) {
            const total = allRows.length || cards.length;
            resultsCounter.textContent =
                query === '' ? `Showing all ${total} entries` : `Found ${visibleCount} of ${total} entries`;
        }
    });
}

// ============================================
// SORT (per-table: sort rows inside the closest tbody)
// ============================================
function initSort() {
    // now potentially multiple tables / header sets
    const headers = document.querySelectorAll('th[data-sort]');
    if (!headers.length) return;

    // Track sort state per table
    const sortStateByTable = new WeakMap(); // tableEl -> { column, direction }

    headers.forEach(th => {
        th.addEventListener('click', function () {
            const table = th.closest('table');
            if (!table) return;

            const tbody = table.querySelector('.rankingTbody');
            if (!tbody) return;

            const column = th.dataset.sort;

            const currentSort = sortStateByTable.get(table) || { column: null, direction: 'asc' };

            // Toggle direction
            if (currentSort.column === column) {
                currentSort.direction = currentSort.direction === 'asc' ? 'desc' : 'asc';
            } else {
                currentSort.column = column;
                currentSort.direction = 'asc';
            }

            sortStateByTable.set(table, currentSort);

            // Update header icons ONLY within this table
            table.querySelectorAll('th[data-sort]').forEach(h => {
                const icon = h.querySelector('i');
                if (icon) icon.className = 'fas fa-sort text-slate-400 text-xs';
            });

            const activeIcon = th.querySelector('i');
            if (activeIcon) {
                activeIcon.className =
                    currentSort.direction === 'asc'
                        ? 'fas fa-sort-up text-blue-600 text-xs'
                        : 'fas fa-sort-down text-blue-600 text-xs';
            }

            // Sort rows inside this tbody only
            const rows = Array.from(tbody.querySelectorAll('tr.tableRow'));

            rows.sort((a, b) => {
                const aVal = getCellValue(a, column, table);
                const bVal = getCellValue(b, column, table);

                // Try numeric sort
                const aNum = parseFloat(String(aVal).replace(/[^0-9.-]/g, ''));
                const bNum = parseFloat(String(bVal).replace(/[^0-9.-]/g, ''));

                if (!isNaN(aNum) && !isNaN(bNum)) {
                    return currentSort.direction === 'asc' ? aNum - bNum : bNum - aNum;
                }

                // String sort
                return currentSort.direction === 'asc'
                    ? String(aVal).localeCompare(String(bVal))
                    : String(bVal).localeCompare(String(aVal));
            });

            // Re-append sorted rows
            rows.forEach(row => tbody.appendChild(row));

            // Update zebra striping (within this tbody)
            rows.forEach((row, index) => {
                row.classList.remove('bg-white', 'bg-slate-50/50');
                row.classList.add(index % 2 === 0 ? 'bg-white' : 'bg-slate-50/50');
            });
        });
    });

    function getCellValue(row, column, table) {
        // data attribute first
        if (row.dataset[column]) return row.dataset[column];

        // cell by data-column
        const cell = row.querySelector(`td[data-column="${column}"]`);
        if (cell) return cell.textContent.trim();

        // fallback: index by headers within this table
        const tableHeaders = table.querySelectorAll('th[data-sort]');
        let colIndex = -1;
        tableHeaders.forEach((h, i) => {
            if (h.dataset.sort === column) colIndex = i;
        });

        if (colIndex >= 0) {
            const cells = row.querySelectorAll('td');
            if (cells[colIndex]) return cells[colIndex].textContent.trim();
        }

        return '';
    }
}

function initMetricToggle() {
    const metricBtns = document.querySelectorAll('.metricBtn');
    if (metricBtns.length === 0) return;

    metricBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const selectedMetric = this.dataset.metric;

            // Update button styles
            metricBtns.forEach(b => {
                b.classList.remove('bg-blue-600', 'text-white');
                b.classList.add('bg-white', 'text-slate-700');
            });
            this.classList.remove('bg-white', 'text-slate-700');
            this.classList.add('bg-blue-600', 'text-white');

            // Toggle table columns
            document.querySelectorAll('[data-toggleable="true"]').forEach(el => {
                const column = el.dataset.column;
                if (column === selectedMetric) {
                    el.classList.remove('hidden');
                } else {
                    el.classList.add('hidden');
                }
            });

            // Sort by the newly selected metric
            document.querySelectorAll('.rankingTable').forEach(tbl => {
                const th = tbl.querySelector(`th[data-sort="${selectedMetric}"]`);
                if (th) th.dispatchEvent(new MouseEvent('click', { bubbles: true }));
            });

            // Toggle card metric groups
            document.querySelectorAll('.metric-group[data-toggleable="true"]').forEach(el => {
                const column = el.dataset.column;
                if (column === selectedMetric) {
                    el.classList.remove('hidden');
                } else {
                    el.classList.add('hidden');
                }
            });
        });
    });
}

// ============================================
// ROW CLICK (Table): whole row is clickable (ALL tables)
// Priority: symbol url (if exists) else state url
// ============================================
function initRowClick() {
    const rows = document.querySelectorAll('.rankingTbody .tableRow');
    if (!rows.length) return;

    rows.forEach(row => {
        row.addEventListener('click', (e) => {
            // If user clicked an actual link/button inside the row � do nothing (let it work)
            if (e.target.closest('a, button, input, select, textarea, details, summary')) return;

            const symbolUrl = row.dataset.symbolUrl;
            const stateUrl = row.dataset.stateUrl;

            const url = (symbolUrl && symbolUrl.trim().length > 0) ? symbolUrl : stateUrl;
            if (url) window.location.href = url;
        });
    });
}

function initRowHoverSync() {
    const rows = document.querySelectorAll('.rankingTbody .tableRow[data-state-slug]');
    if (!rows.length) return;

    rows.forEach(row => {
        row.addEventListener('mouseenter', () => {
            const slug = row.dataset.stateSlug;
            if (slug && window.mapFns && typeof window.mapFns.highlightBySlug === 'function') {
                window.mapFns.highlightBySlug(slug);
            }
        });

        row.addEventListener('mouseleave', () => {
            if (window.mapFns && typeof window.mapFns.clearHighlight === 'function') {
                window.mapFns.clearHighlight();
            }
        });
    });
}
