document.addEventListener('DOMContentLoaded', function () {
    const mapContainer = document.getElementById('map-container');
    if (!mapContainer) return;

    dashboardMap = L.map('map-container').setView([49.0371, 16.6192], 14);
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png').addTo(dashboardMap);
    
    const tasks = JSON.parse(mapContainer.dataset.tasks);

    tasks.forEach(t => {
        var popupContent = `
            <div class="p-2">
                <h6 class="fw-bold mb-1">${t.title}</h6>
                <p class="text-muted small mb-2">${t.category || 'Bez kategorie'}</p>
                <div class="d-flex justify-content-between align-items-center">
                    <span class="badge bg-success">${t.xp} XP</span>
                    <a href="/Tasks/Details?id=${t.id}" class="btn btn-link btn-sm p-0">Detail</a>
                </div>
            </div>`;
        L.marker([t.lat, t.lng]).addTo(dashboardMap).bindPopup(popupContent);
    });
});

function toggleExpand(id) {
    const el = document.getElementById(id);
    const otherId = id === 'tasks-panel' ? 'map-panel' : 'tasks-panel';
    const otherEl = document.getElementById(otherId);

    const isExpanding = !el.classList.contains('is-expanded-fullscreen');

    if (isExpanding) {
        el.classList.add('is-expanded-fullscreen');
        otherEl.classList.add('d-none');
        document.body.style.overflow = 'hidden';
    } else {
        el.classList.remove('is-expanded-fullscreen');
        otherEl.classList.remove('d-none');
        document.body.style.overflow = 'auto';
    }

    setTimeout(() => {
        if (dashboardMap) {
            dashboardMap.invalidateSize();
        }
    }, 350);
}