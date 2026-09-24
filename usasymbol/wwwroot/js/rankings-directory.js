(() => {
    const filters = document.querySelector('.rd-filters');
    if (!filters) return;
    const desktop = window.matchMedia('(min-width: 901px)');
    const update = () => { filters.open = desktop.matches; };
    update();
    desktop.addEventListener('change', update);
})();
