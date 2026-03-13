document.addEventListener('DOMContentLoaded', function () {
    const mapContainer = document.getElementById('map-container');
    if (!mapContainer) return;

    dashboardMap = L.map('map-container').setView([49.0371, 16.6192], 14);
    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>'
    }).addTo(dashboardMap);

    const tasks = JSON.parse(mapContainer.dataset.tasks);

    tasks.forEach(t => {
        const template = document.getElementById('map-card-template').innerHTML;
        const wrapper = document.createElement('div');
        wrapper.innerHTML = template;

        wrapper.querySelector('.t-title').textContent = t.title;
        wrapper.querySelector('.t-xp').textContent = t.isGuest ? "?? XP" : `${t.xp} XP`;
        wrapper.querySelector('.t-category').textContent = t.category || 'Bez kategorie';

        const imgEl = wrapper.querySelector('.t-img');
        imgEl.src = t.isGuest ? '/images/placeholder-task.png' : (t.imageUrl || '/images/placeholder-task.png');

        const link = wrapper.querySelector('.t-link');
        if (t.isGuest) {
            link.textContent = "Přihlásit se";
            link.href = "/Identity/Account/Login";
            link.classList.replace('btn-primary', 'btn-primary');
        } else {
            link.href = `/Tasks/Details/${t.id}`;
        }

        L.marker([t.lat, t.lng]).addTo(dashboardMap).bindPopup(wrapper, {
            minWidth: 260,
            maxWidth: 260,
            className: 'custom-map-popup'
        });
    });
});

function toggleExpand(id) {
    const el = document.getElementById(id);
    const otherId = id === 'tasks-panel' ? 'map-panel' : 'tasks-panel';
    const otherEl = document.getElementById(otherId);

    const btn = el.querySelector('button');
    const isExpanding = !el.classList.contains('is-expanded-fullscreen');

    if (isExpanding) {
        el.classList.add('is-expanded-fullscreen');
        otherEl.classList.add('d-none');
        document.body.style.overflow = 'hidden';
        
        if (btn) {
            btn.innerHTML = '<i class="bi bi-arrows-angle-contract me-2"></i>Zmenšit mapu';
        }
    } else {
        el.classList.remove('is-expanded-fullscreen');
        otherEl.classList.remove('d-none');
        document.body.style.overflow = 'auto';
        
        if (btn) {
            btn.innerHTML = '<i class="bi bi-arrows-angle-expand me-2"></i>Zvětšit mapu';
        }
    }

    setTimeout(() => {
        if (typeof dashboardMap !== 'undefined' && dashboardMap) {
            dashboardMap.invalidateSize(true);
        }
    }, 350);
}