document.addEventListener("DOMContentLoaded", function () {

    const searchInput = document.getElementById('categorySearch');
    const hiddenCatId = document.getElementById('hiddenCategoryId');
    const allLeaves = document.querySelectorAll('.category-leaf-li');
    const allLi = document.querySelectorAll('.category-dropdown-menu li');
    const allCollapses = document.querySelectorAll('.category-dropdown-menu ul.collapse');

    document.querySelector('.category-dropdown-menu').addEventListener('click', function(e) {
        e.stopPropagation();
    });

    searchInput.addEventListener('input', function () {
        const filter = this.value.toLowerCase();
        hiddenCatId.value = '';

        if (filter.length > 0) {
            allLi.forEach(li => li.style.display = 'none'); // Skrýt vše

            allLeaves.forEach(leaf => {
                if (leaf.getAttribute('data-search').toLowerCase().includes(filter)) {
                    let current = leaf;
                    while (current && current.tagName === 'LI') {
                        current.style.display = 'block';
                        current = current.parentElement.closest('li');
                    }
                    let parentUl = leaf.closest('ul.collapse');
                    while (parentUl) {
                        parentUl.classList.add('show');
                        let header = parentUl.previousElementSibling;
                        if(header) header.setAttribute('aria-expanded', 'true');
                        parentUl = parentUl.parentElement.closest('ul.collapse');
                    }
                }
            });
        } else {
            allLi.forEach(li => li.style.display = 'block');
            allCollapses.forEach(ul => {
                ul.classList.remove('show');
                let header = ul.previousElementSibling;
                if(header) header.setAttribute('aria-expanded', 'false');
            });
        }
    });

    document.querySelectorAll('.category-item').forEach(item => {
        item.addEventListener('click', function() {
            hiddenCatId.value = this.getAttribute('data-id');
            searchInput.value = this.getAttribute('data-name');
            var dropdown = bootstrap.Dropdown.getInstance(searchInput);
            if (dropdown) dropdown.hide();
        });
    });

    const localeCs = {
        days: ['Neděle', 'Pondělí', 'Úterý', 'Středa', 'Čtvrtek', 'Pátek', 'Sobota'],
        daysShort: ['Ne', 'Po', 'Út', 'St', 'Čt', 'Pá', 'So'],
        daysMin: ['Ne', 'Po', 'Út', 'St', 'Čt', 'Pá', 'So'],
        months: ['Leden', 'Únor', 'Březen', 'Duben', 'Květen', 'Červen', 'Červenec', 'Srpen', 'Září', 'Říjen', 'Listopad', 'Prosinec'],
        monthsShort: ['Led', 'Úno', 'Bře', 'Dub', 'Kvě', 'Čer', 'Čvc', 'Srp', 'Zář', 'Říj', 'Lis', 'Pro'],
        today: 'Dnes',
        clear: 'Vymazat',
        dateFormat: 'dd.MM.yyyy',
        timeFormat: 'HH:mm',
        firstDay: 1
    };
    
    let tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 7);

    new AirDatepicker('#deadline-display', {
        locale: localeCs,
        timepicker: true,
        minDate: new Date(),
        selectedDates: [tomorrow],
        autoClose: true,
        dateFormat: 'dd.MM.yyyy',
        timeFormat: 'HH:mm',
        altField: '#deadline-picker',
        altFieldDateFormat: 'yyyy-MM-dd',
        buttons: ['clear']
    });

    var map = L.map('map').setView([49.0371, 16.6192], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '&copy; OpenStreetMap' }).addTo(map);
    setTimeout(function() { map.invalidateSize(); }, 200);

    var marker = null;
    var latInput = document.getElementById('lat-input');
    var lngInput = document.getElementById('lng-input');
    var addressSearchInput = document.getElementById('address-search');

    function setLocation(lat, lng) {
        latInput.value = parseFloat(lat).toFixed(6).replace('.', ',');
        lngInput.value = parseFloat(lng).toFixed(6).replace('.', ',');
        if (marker) marker.setLatLng([lat, lng]);
        else marker = L.marker([lat, lng]).addTo(map);
    }

    if (latInput.value && latInput.value !== "0") {
        var lat = parseFloat(latInput.value.replace(',', '.'));
        var lng = parseFloat(lngInput.value.replace(',', '.'));
        setLocation(lat, lng);
        map.setView([lat, lng], 16);
    }

    map.on('click', function (e) {
        setLocation(e.latlng.lat, e.latlng.lng);
        fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${e.latlng.lat}&lon=${e.latlng.lng}`)
            .then(res => res.json())
            .then(data => {
                if (data && data.display_name) {
                    addressSearchInput.value = data.display_name;

                    const address = data.display_name;
                    const city = data.address.city || data.address.town || data.address.village || "";
                    document.getElementById('full-address-input').value = address;
                    document.getElementById('city-input').value = city;
                    document.getElementById('address-search').value = address;
                }
            });
    });

    document.getElementById('btn-search').addEventListener('click', function() {
        if (!addressSearchInput.value) return;
        fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(addressSearchInput.value)}&countrycodes=cz`)
            .then(res => res.json())
            .then(data => {
                if (data.length > 0) {
                    map.setView([data[0].lat, data[0].lon], 16);
                    setLocation(data[0].lat, data[0].lon);
                } else alert('Adresa nenalezena.');
            });
    });

    addressSearchInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); document.getElementById('btn-search').click(); }
    });
});