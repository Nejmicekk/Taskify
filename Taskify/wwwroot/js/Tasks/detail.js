document.addEventListener('DOMContentLoaded', function () {
    var carouselEl = document.getElementById('taskGallery');
    if (carouselEl) {
        carouselEl.addEventListener('slid.bs.carousel', function (event) {
            var indexEl = document.getElementById('carousel-index');
            if (indexEl) {
                indexEl.innerText = event.to + 1;
            }
        });
    }
    
    var mapEl = document.getElementById('detailMap');
    if (mapEl) {
        var lat = parseFloat(mapEl.getAttribute('data-lat').replace(',', '.'));
        var lng = parseFloat(mapEl.getAttribute('data-lng').replace(',', '.'));
        var title = mapEl.getAttribute('data-title');
        
        var map = L.map('detailMap', {
            center: [lat, lng],
            zoom: 15,
            layers: [
                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: '&copy; OpenStreetMap contributors'
                })
            ]
        });
        
        var marker = L.marker([lat, lng]).addTo(map);
        marker.bindPopup("<b>" + title + "</b><br>Zde se nachází problém.").openPopup();
        
        setTimeout(function() {
            map.invalidateSize();
        }, 200);
    }
});