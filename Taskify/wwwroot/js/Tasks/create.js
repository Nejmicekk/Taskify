document.addEventListener("DOMContentLoaded", function () {
    const form = document.querySelector('form');
    const imageInput = document.getElementById('imageUploads');
    const imagePreview = document.getElementById('imagePreview');

    if (imageInput) {
        imageInput.addEventListener('change', function() {
            imagePreview.innerHTML = '';
            const files = Array.from(this.files);
            
            // Kontrola na GIF
            const hasGif = files.some(file => file.type === 'image/gif' || file.name.toLowerCase().endsWith('.gif'));
            if (hasGif) {
                alert('GIF soubory nejsou povoleny. Prosím vyberte jiné obrázky (JPG, PNG atd.).');
                this.value = ''; // Reset inputu
                return;
            }

            files.forEach((file, index) => {
                const reader = new FileReader();
                reader.onload = function(e) {
                    const col = document.createElement('div');
                    col.className = 'col';
                    col.innerHTML = `
                        <div class="position-relative">
                            <img src="${e.target.result}" class="img-thumbnail rounded-3 shadow-sm" style="width: 100%; height: 80px; object-fit: cover;">
                            ${index === 0 ? '<span class="badge bg-primary position-absolute top-0 start-0 m-1" style="font-size: 0.5rem">Hlavní</span>' : ''}
                        </div>
                    `;
                    imagePreview.appendChild(col);
                };
                reader.readAsDataURL(file);
            });
        });
    }

    const latInput = document.getElementById('lat-input');
    const cityInput = document.getElementById('city-input');
    const addressSearchInput = document.getElementById('address-search');
    
    const searchInput = document.getElementById('categorySearch');
    const hiddenCatId = document.getElementById('hiddenCategoryId');
    const allLeaves = document.querySelectorAll('.category-leaf-li');
    const allLi = document.querySelectorAll('.category-dropdown-menu li');
    const allCollapses = document.querySelectorAll('.category-dropdown-menu ul.collapse');

    searchInput.addEventListener('input', function () {
        const filter = this.value.toLowerCase();
        if (filter.length > 0) {
            allLi.forEach(li => li.style.display = 'none');
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
                        parentUl = parentUl.parentElement.closest('ul.collapse');
                    }
                }
            });
        } else {
            allLi.forEach(li => li.style.display = 'block');
            allCollapses.forEach(ul => ul.classList.remove('show'));
        }
    });

    let tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 7);

    if (document.getElementById('deadline-display')) {
        new AirDatepicker('#deadline-display', {
            locale: datepickerLocaleCs,
            timepicker: true,
            minDate: new Date(),
            selectedDates: [tomorrow],
            autoClose: true,
            dateFormat: 'dd.MM.yyyy',
            timeFormat: 'HH:mm',
            altField: '#deadline-picker',
            altFieldDateFormat: 'yyyy-MM-dd HH:mm',
            buttons: ['clear']
        });
    }
    
    var map = L.map('map').setView([49.0371, 16.6192], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { attribution: '&copy; OpenStreetMap' }).addTo(map);
    setTimeout(() => map.invalidateSize(), 200);

    var marker = null;
    function setLocation(lat, lng) {
        latInput.value = parseFloat(lat).toFixed(6).replace('.', ',');
        document.getElementById('lng-input').value = parseFloat(lng).toFixed(6).replace('.', ',');
        if (marker) marker.setLatLng([lat, lng]);
        else marker = L.marker([lat, lng]).addTo(map);
        document.getElementById('map').style.border = "none";
    }

    map.on('click', function (e) {
        setLocation(e.latlng.lat, e.latlng.lng);
        fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${e.latlng.lat}&lon=${e.latlng.lng}`)
            .then(res => res.json())
            .then(data => {
                if (data && data.address) {
                    const addr = data.address;
                    addressSearchInput.value = data.display_name;
                    document.getElementById('full-address-input').value = data.display_name;
                    document.getElementById('region-input').value = addr.state || "";
                    document.getElementById('city-input').value = addr.city || addr.town || addr.village || "";
                    document.getElementById('street-input').value = addr.road || "";
                    document.getElementById('street-number-input').value = addr.house_number || "";
                    document.getElementById('postcode-input').value = addr.postcode || "";
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

    addressSearchInput.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') { e.preventDefault(); document.getElementById('btn-search').click(); }
    });
});