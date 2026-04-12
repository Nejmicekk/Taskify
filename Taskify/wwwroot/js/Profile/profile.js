var cropper;
var imageToCrop = document.getElementById('image-to-crop');
var cropModalElement = document.getElementById('cropModal');
var cropModal = cropModalElement ? new bootstrap.Modal(cropModalElement) : null;

function enableEdit() {
    document.getElementById('input-name').disabled = false;
    document.getElementById('input-bio').disabled = false;
    document.getElementById('input-phone').disabled = false;
    document.getElementById('input-email').disabled = false;

    document.getElementById('edit-btn').classList.add('d-none');
    var saveActions = document.getElementById('save-actions');
    if (saveActions) saveActions.classList.remove('d-none');
}

function previewImage(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();

        reader.onload = function (e) {
            imageToCrop.src = e.target.result;
            if (cropModal) cropModal.show();
        }
        reader.readAsDataURL(input.files[0]);
        
        input.value = '';
    }
}

function copyProfileLink(btn) {
    const url = window.location.href;
    
    const showSuccess = () => {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="bi bi-check2 me-2"></i>Zkopírováno!';
        btn.classList.replace('btn-light', 'btn-success');
        btn.classList.add('text-white');
        setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.classList.replace('btn-success', 'btn-light');
            btn.classList.remove('text-white');
        }, 2000);
    };
    
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(url).then(showSuccess);
    } else {
        const textArea = document.createElement("textarea");
        textArea.value = url;
        textArea.style.position = "fixed";
        textArea.style.left = "-999999px";
        textArea.style.top = "-999999px";
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        try {
            document.execCommand('copy');
            showSuccess();
        } catch (err) {
            console.error('Nepodařilo se kopírovat:', err);
        }
        document.body.removeChild(textArea);
    }
}

function showAchievementDetail(name, desc, icon, date, rarity) {
    const overlay = document.getElementById('achievement-overlay');
    const content = document.getElementById('achievement-content');
    const iconImg = document.getElementById('overlay-icon');
    const nameEl = document.getElementById('overlay-name');
    const descEl = document.getElementById('overlay-desc');
    const dateEl = document.getElementById('overlay-date');
    const rarityEl = document.getElementById('overlay-rarity');

    if (!overlay || !content) return;

    iconImg.src = icon;
    iconImg.onerror = () => iconImg.src = '/images/achievements/placeholder.svg';
    nameEl.innerText = name;
    descEl.innerText = desc;
    dateEl.innerText = 'Získáno: ' + date;
    rarityEl.innerText = rarity;
    
    content.className = 'achievement-overlay-content p-5 text-center shadow-lg ' + 'rarity-' + rarity.toLowerCase();

    overlay.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeAchievementDetail() {
    const overlay = document.getElementById('achievement-overlay');
    if (overlay) overlay.classList.remove('active');
    document.body.style.overflow = 'auto';
}

function showFullImage(url, name) {
    if (!url) return;
    const overlay = document.getElementById('image-overlay');
    const imgEl = document.getElementById('full-profile-image');
    const nameEl = document.getElementById('full-image-name');

    if (!overlay) return;

    imgEl.src = url;
    nameEl.innerText = name;

    overlay.classList.add('active');
    document.body.style.overflow = 'hidden';
}

function closeFullImage() {
    const overlay = document.getElementById('image-overlay');
    if (overlay) overlay.classList.remove('active');
    document.body.style.overflow = 'auto';
}

document.addEventListener('keydown', function(e) {
    if (e.key === "Escape") {
        closeAchievementDetail();
        closeFullImage();
    }
});

document.addEventListener("DOMContentLoaded", function() {
    var summary = document.querySelector(".validation-summary-errors");

    if (summary && summary.innerText.trim().length > 0) {
        var errorModalElement = document.getElementById('errorModal');
        if (errorModalElement) {
            var errorModal = new bootstrap.Modal(errorModalElement);
            errorModal.show();
        }
    }

    var modals = document.querySelectorAll('.modal');
    modals.forEach(function(modal) {
        modal.addEventListener('hidden.bs.modal', function () {
            var backdrops = document.querySelectorAll('.modal-backdrop');
            backdrops.forEach(function(backdrop) {
                backdrop.remove();
            });
            document.body.classList.remove('modal-open');
            document.body.style.paddingRight = '';
            document.body.style.overflow = '';
        });
    });
});

if (cropModalElement) {
    cropModalElement.addEventListener('shown.bs.modal', function () {
        cropper = new Cropper(imageToCrop, {
            aspectRatio: 1,
            viewMode: 1,
            dragMode: 'move',
            autoCropArea: 1,
            restore: false,
            guides: false,
            center: false,
            highlight: false,
            cropBoxMovable: false,
            cropBoxResizable: false,
            toggleDragModeOnDblclick: false,
        });
    });

    cropModalElement.addEventListener('hidden.bs.modal', function () {
        if (cropper) {
            cropper.destroy();
            cropper = null;
        }
    });

    document.getElementById('crop-btn').addEventListener('click', function () {
        if (cropper) {
            cropper.getCroppedCanvas({
                width: 300,
                height: 300
            }).toBlob(function (blob) {
                var url = URL.createObjectURL(blob);
                var imgPreview = document.getElementById('img-preview');
                var placeholder = document.getElementById('initials-placeholder');

                if (imgPreview) {
                    imgPreview.src = url;
                    imgPreview.classList.remove('d-none');
                }
                if (placeholder) placeholder.classList.add('d-none');
                
                var fileInput = document.getElementById('upload-photo');
                var dataTransfer = new DataTransfer();
                
                var file = new File([blob], "profile_cropped.jpg", { type: "image/jpeg" });
                dataTransfer.items.add(file);
                if (fileInput) fileInput.files = dataTransfer.files;
                
                if (cropModal) cropModal.hide();
                enableEdit();
            });
        }
    });
}
