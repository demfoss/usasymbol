/**
 * Universal Ranking Table JS
 * Handles: Search, Sort, View Toggle, Metric Toggle
 */
document.addEventListener('DOMContentLoaded', function () {
    initViewToggle();
    initSearch();
    initFilterChips();
    initSort();
    initSortBySelect();
    initMetricToggle();
    initRowClick();
    initRowHoverSync();
    initNotePopover();
});

// Active quick-filter chip value, shared between search and chip filtering
let activeChipValue = '';

// Sort state per table, shared between header clicks and the Sort-by select
const sortStateByTable = new WeakMap();

// ============================================
// VIEW TOGGLE (Cards / Table)
// ============================================
function initViewToggle() {
    const viewBtns = document.querySelectorAll('.viewBtn');
    const cardsView = document.getElementById('cardsView');
    const tableView = document.getElementById('tableView');

    if (!cardsView || !tableView) return;

    // Tailwind lg breakpoint
    const mq = window.matchMedia('(min-width: 1024px)');

    // One key, but separated prefs
    const STORAGE_KEY = 'rankingViewPrefs_v1';
    // { mobile: { view, touched }, desktop: { view, touched } }

    function defaultForBreakpoint() {
        // Table is denser than Cards (fewer scroll-pixels per row), so it's
        // the default at every breakpoint now that Rank+State stay frozen
        // during horizontal scroll on mobile.
        return 'table';
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
                mobile: { view: 'table', touched: false },
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
                const isActive = b.dataset.view === view;
                b.classList.toggle('is-active', isActive);
                b.setAttribute('aria-pressed', String(isActive));
            });
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
// SEARCH + QUICK FILTER CHIPS (across ALL ranking tables)
// Combined: a row/card must match both the free-text query AND the
// active chip (if any) to stay visible.
// ============================================
function applyFilters() {
    const searchInput = document.getElementById('tableSearch');
    const query = searchInput ? searchInput.value.toLowerCase().trim() : '';

    const tableEmptyState = document.getElementById('tableEmptyState');
    const cardsEmptyState = document.getElementById('cardsEmptyState');
    const resultsCounter = document.getElementById('resultsCounter');

    const allRows = document.querySelectorAll('.rankingTbody .tableRow');
    const cards = document.querySelectorAll('.rankingCard');

    function matches(el) {
        const searchText = (el.dataset.search || el.dataset.state || '').toLowerCase();
        const searchOk = searchText.includes(query);
        const chipOk = !activeChipValue || el.dataset.chipValue === undefined || el.dataset.chipValue === activeChipValue;
        return searchOk && chipOk;
    }

    let visibleCount = 0;

    allRows.forEach(row => {
        const ok = matches(row);
        row.classList.toggle('hidden', !ok);
        if (ok) visibleCount++;
    });

    cards.forEach(card => {
        card.classList.toggle('hidden', !matches(card));
    });

    if (tableEmptyState) tableEmptyState.classList.toggle('hidden', visibleCount > 0);
    if (cardsEmptyState) cardsEmptyState.classList.toggle('hidden', visibleCount > 0);

    if (resultsCounter) {
        const total = allRows.length || cards.length;
        resultsCounter.textContent =
            (query === '' && !activeChipValue) ? `Showing all ${total} entries` : `Found ${visibleCount} of ${total} entries`;
    }
}

function initSearch() {
    const searchInput = document.getElementById('tableSearch');
    if (!searchInput) return;

    searchInput.addEventListener('input', applyFilters);
}

function initFilterChips() {
    const chips = document.querySelectorAll('#quickFilterChips .filterChip');
    if (!chips.length) return;

    chips.forEach(chip => {
        chip.addEventListener('click', function () {
            activeChipValue = this.dataset.chipValue || '';

            chips.forEach(c => {
                const isActive = c === this;
                c.setAttribute('aria-pressed', String(isActive));
                c.classList.toggle('is-active', isActive);
            });

            applyFilters();
        });
    });
}

// ============================================
// SORT (per-table: sort rows inside the closest tbody)
// Shared by header clicks and the desktop "Sort by" select.
// ============================================
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

function applySort(table, th, direction) {
    const tbody = table.querySelector('.rankingTbody');
    if (!tbody) return;

    const column = th.dataset.sort;
    sortStateByTable.set(table, { column, direction });

    // Update header icons ONLY within this table
    table.querySelectorAll('th[data-sort]').forEach(h => {
        h.setAttribute('aria-sort', 'none');
        const icon = h.querySelector('i');
        if (icon) icon.className = 'ranking-sort-icon fas fa-sort';
    });

    const activeIcon = th.querySelector('i');
    if (activeIcon) {
        activeIcon.className =
            direction === 'asc'
                ? 'ranking-sort-icon is-active fas fa-sort-up'
                : 'ranking-sort-icon is-active fas fa-sort-down';
    }
    th.setAttribute('aria-sort', direction === 'asc' ? 'ascending' : 'descending');

    // Sort rows inside this tbody only
    const rows = Array.from(tbody.querySelectorAll('tr.tableRow'));

    rows.sort((a, b) => {
        const aVal = getCellValue(a, column, table);
        const bVal = getCellValue(b, column, table);

        // Try numeric sort
        const aNum = parseFloat(String(aVal).replace(/[^0-9.-]/g, ''));
        const bNum = parseFloat(String(bVal).replace(/[^0-9.-]/g, ''));

        if (!isNaN(aNum) && !isNaN(bNum)) {
            return direction === 'asc' ? aNum - bNum : bNum - aNum;
        }

        // String sort
        return direction === 'asc'
            ? String(aVal).localeCompare(String(bVal))
            : String(bVal).localeCompare(String(aVal));
    });

    // Re-append sorted rows
    rows.forEach(row => tbody.appendChild(row));

    // Keep the "Sort by" select in sync when sorting comes from a header click
    const sortBySelect = document.getElementById('sortBySelect');
    const firstTable = document.querySelector('.rankingTable');
    if (sortBySelect && table === firstTable) {
        sortBySelect.value = column;
    }
}

function initSort() {
    const headers = document.querySelectorAll('th[data-sort]');
    if (!headers.length) return;

    headers.forEach(th => {
        function sortFromHeader() {
            const table = th.closest('table');
            if (!table) return;

            const column = th.dataset.sort;
            const currentSort = sortStateByTable.get(table) || { column: null, direction: 'asc' };
            const direction = currentSort.column === column
                ? (currentSort.direction === 'asc' ? 'desc' : 'asc')
                : 'asc';

            applySort(table, th, direction);
        }

        th.addEventListener('click', sortFromHeader);
        th.addEventListener('keydown', function (event) {
            if (event.key !== 'Enter' && event.key !== ' ') return;
            event.preventDefault();
            sortFromHeader();
        });
    });
}

// ============================================
// SORT BY (desktop select, targets the first table on the page)
// ============================================
function initSortBySelect() {
    const select = document.getElementById('sortBySelect');
    if (!select) return;

    const table = document.querySelector('.rankingTable');
    if (!table) return;

    select.addEventListener('change', function () {
        const th = table.querySelector(`th[data-sort="${this.value}"]`);
        if (th) applySort(table, th, 'asc');
    });
}

function initMetricToggle() {
    const metricBtns = document.querySelectorAll('.metricBtn');
    if (metricBtns.length === 0) return;

    metricBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            const selectedMetric = this.dataset.metric;

            // Update button state
            metricBtns.forEach(b => {
                const isActive = b === this;
                b.classList.toggle('is-active', isActive);
                b.setAttribute('aria-pressed', String(isActive));
            });

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

// ============================================
// NOTE POPOVER (Table): click-to-reveal full note text
// ============================================
function initNotePopover() {
    const triggers = document.querySelectorAll('.noteTrigger');
    if (!triggers.length) return;

    let popover = null;
    let activeTrigger = null;

    function closePopover() {
        if (popover) {
            popover.remove();
            popover = null;
        }
        if (activeTrigger) {
            activeTrigger.setAttribute('aria-expanded', 'false');
            activeTrigger = null;
        }
    }

    function openPopover(trigger) {
        closePopover();

        popover = document.createElement('div');
        popover.className = 'noteTriggerPopover fixed z-50 max-w-xs rounded-lg border border-slate-200 bg-white p-3 text-[13px] leading-5 text-slate-700 shadow-lg';
        popover.setAttribute('role', 'dialog');
        popover.textContent = trigger.dataset.note || '';
        document.body.appendChild(popover);

        const triggerRect = trigger.getBoundingClientRect();
        const popoverRect = popover.getBoundingClientRect();

        let top = triggerRect.bottom + 8;
        if (top + popoverRect.height > window.innerHeight - 8) {
            top = triggerRect.top - popoverRect.height - 8;
        }

        let left = triggerRect.left;
        if (left + popoverRect.width > window.innerWidth - 8) {
            left = window.innerWidth - popoverRect.width - 8;
        }

        popover.style.top = `${Math.max(8, top)}px`;
        popover.style.left = `${Math.max(8, left)}px`;

        trigger.setAttribute('aria-expanded', 'true');
        activeTrigger = trigger;
    }

    triggers.forEach(trigger => {
        trigger.setAttribute('aria-expanded', 'false');
        trigger.addEventListener('click', (e) => {
            e.stopPropagation();
            if (activeTrigger === trigger) {
                closePopover();
            } else {
                openPopover(trigger);
            }
        });
    });

    document.addEventListener('click', (e) => {
        if (popover && !popover.contains(e.target) && e.target !== activeTrigger) {
            closePopover();
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closePopover();
    });

    window.addEventListener('scroll', closePopover, true);
    window.addEventListener('resize', closePopover);
}
