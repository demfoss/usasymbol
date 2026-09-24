(() => {
    const map = document.querySelector('.hub-tile-map');
    if (!map) return;

    const name = document.getElementById('hub-state-name');
    const capital = document.getElementById('hub-state-capital');
    const population = document.getElementById('hub-state-population');
    const link = document.getElementById('hub-state-link');

    function preview(event) {
        const tile = event.target.closest('.hub-tile');
        if (!tile || tile.classList.contains('is-selected')) return;
        map.querySelector('.is-selected')?.classList.remove('is-selected');
        tile.classList.add('is-selected');
        name.textContent = tile.dataset.name;
        capital.textContent = tile.dataset.capital;
        population.textContent = tile.dataset.population;
        link.href = tile.href;
        link.querySelector('span').textContent = `Explore ${tile.dataset.name}`;
    }

    map.addEventListener('pointerover', preview);
    map.addEventListener('focusin', preview);
})();
