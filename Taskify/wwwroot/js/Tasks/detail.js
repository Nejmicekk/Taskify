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
                L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>'
                })
            ]
        });
        
        var marker = L.marker([lat, lng]).addTo(map);
        marker.bindPopup("<b>" + title + "</b><br>Zde se nachází problém.").openPopup();
        
        setTimeout(function() {
            map.invalidateSize();
        }, 200);
    }
    
    var deadlineInput = document.getElementById('new-deadline-display');
    if (deadlineInput) {
        new AirDatepicker('#new-deadline-display', {
            container: '#extendDeadlineModal',
            locale: datepickerLocaleCs,
            timepicker: true,
            minDate: new Date(),
            dateFormat: 'dd.MM.yyyy',
            timeFormat: 'HH:mm',
            autoClose: true,
            onSelect({date}) {
                if (date) {
                    document.getElementById('new-deadline-input').value = date.toISOString();
                }
            }
        });
    }
});