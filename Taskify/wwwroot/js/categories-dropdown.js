document.addEventListener('click', function (e) {
    const item = e.target.closest('.category-item');
    if (!item) return;

    const collapseTrigger = e.target.closest('[data-bs-toggle="collapse"]');
    if (collapseTrigger) {
        e.stopPropagation();
        return;
    }

    e.preventDefault();
    e.stopPropagation();

    const id = item.getAttribute('data-id');
    const name = item.getAttribute('data-name');

    const wrapper = item.closest('.dropdown');
    const hiddenInput = wrapper.querySelector('input[type="hidden"]');
    const searchInput = wrapper.querySelector('input[type="text"]');
    const labelText = wrapper.querySelector('.category-label-text') || wrapper.querySelector('.text-truncate');

    if (hiddenInput) hiddenInput.value = id;
    if (searchInput) searchInput.value = name;
    if (labelText) {
        if (labelText.tagName === 'INPUT') labelText.value = name;
        else labelText.textContent = name;
    }

    const form = item.closest('form');
    
    if (form && form.getAttribute('method')?.toLowerCase() === 'get') {
        form.submit();
    } else {
        const toggle = wrapper.querySelector('[data-bs-toggle="dropdown"]');
        if (toggle) {
            const bsDropdown = bootstrap.Dropdown.getOrCreateInstance(toggle);
            bsDropdown.hide();
        }
    }
});