document.addEventListener('DOMContentLoaded', function () {
    const mapElement = document.getElementById('explore-map');
    if (!mapElement) return;
    
    const map = L.map('explore-map').setView([49.0371, 16.6192], 14);
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png').addTo(map);
    
    const tasksData = JSON.parse(mapElement.dataset.tasks);
    let markers = [];
    
    function renderMarkers(filteredIds = null) {
        markers.forEach(m => map.removeLayer(m));
        markers = [];

        tasksData.forEach(t => {
            if (filteredIds && !filteredIds.includes(t.id)) return;

            const m = L.marker([t.lat, t.lng])
                .addTo(map)
                .bindPopup(`<b>${t.title}</b><br><a href="/Tasks/Details?id=${t.id}">Zobrazit detail</a>`);
            markers.push(m);
        });
    }
    
    function applyFilters() {
        const query = document.getElementById('searchQuery').value.toLowerCase();
        const categoryId = document.getElementById('categoryFilter').value;
        const visibleIds = [];
        let count = 0;

        document.querySelectorAll('.task-item-wrapper').forEach(el => {
            const taskTitle = el.getAttribute('data-title');
            const taskCat = el.getAttribute('data-category');
            const taskId = parseInt(el.querySelector('a')?.getAttribute('href')?.split('=')[1]);

            const matchSearch = taskTitle.includes(query);
            const matchCat = categoryId === "" || taskCat === categoryId;

            if (matchSearch && matchCat) {
                el.classList.remove('d-none');
                if (taskId) visibleIds.push(taskId);
                count++;
            } else {
                el.classList.add('d-none');
            }
        });

        document.getElementById('task-count').innerText = count;
        renderMarkers(visibleIds);
    }
    
    document.getElementById('btn-apply-filters').addEventListener('click', applyFilters);
    
    document.getElementById('searchQuery').addEventListener('keyup', function(e) {
        if (e.key === 'Enter') applyFilters();
    });
    
    renderMarkers();
});