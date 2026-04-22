document.addEventListener("DOMContentLoaded", function () {
    const deadlineDisplay = document.getElementById('deadline-display');
    const deadlinePicker = document.getElementById('deadline-picker');

    if (deadlineDisplay && typeof AirDatepicker !== 'undefined') {
        const initialValue = deadlinePicker ? deadlinePicker.value : '';
        let selectedDate = null;
        
        if (initialValue) {
            selectedDate = new Date(initialValue);
        }

        const dp = new AirDatepicker('#deadline-display', {
            locale: typeof datepickerLocaleCs !== 'undefined' ? datepickerLocaleCs : {},
            timepicker: true,
            minDate: new Date(),
            autoClose: true,
            dateFormat: 'dd.MM.yyyy',
            timeFormat: 'HH:mm',
            onSelect({date}) {
                if (deadlinePicker) {
                    if (date) {
                        // Formátování pro C# LocalDateTime (ISO bez Z)
                        const offset = date.getTimezoneOffset();
                        const localDate = new Date(date.getTime() - (offset * 60 * 1000));
                        deadlinePicker.value = localDate.toISOString().slice(0, 16);
                    } else {
                        deadlinePicker.value = '';
                    }
                }
            },
            buttons: ['clear']
        });

        if (selectedDate && !isNaN(selectedDate.getTime())) {
            dp.selectDate(selectedDate);
        }
    }
    
    const searchInput = document.getElementById('categorySearch');
    const allLeaves = document.querySelectorAll('.category-leaf-li');
    const allLi = document.querySelectorAll('.category-dropdown-menu li');
    const allCollapses = document.querySelectorAll('.category-dropdown-menu ul.collapse');

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const filter = this.value.toLowerCase();
            if (filter.length > 0) {
                allLi.forEach(li => li.style.display = 'none');
                allLeaves.forEach(leaf => {
                    const searchData = leaf.getAttribute('data-search') || leaf.innerText;
                    if (searchData.toLowerCase().includes(filter)) {
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
    }
});
