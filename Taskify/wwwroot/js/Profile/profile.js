var cropper;
var imageToCrop = document.getElementById('image-to-crop');
var cropModalElement = document.getElementById('cropModal');
var cropModal = new bootstrap.Modal(cropModalElement);

function enableEdit() {
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
            
            cropModal.show();
        }
        reader.readAsDataURL(input.files[0]);
        
        input.value = '';
    }
}

function copyProfileLink(btn) {
    const url = window.location.href;
    navigator.clipboard.writeText(url).then(() => {
        const originalHtml = btn.innerHTML;
        btn.innerHTML = '<i class="bi bi-check2 me-2"></i>Zkopírováno!';
        btn.classList.replace('btn-light', 'btn-success');
        btn.classList.add('text-white');

        setTimeout(() => {
            btn.innerHTML = originalHtml;
            btn.classList.replace('btn-success', 'btn-light');
            btn.classList.remove('text-white');
        }, 2000);
    });
}

document.addEventListener("DOMContentLoaded", function() {
    var summary = document.querySelector(".validation-summary-errors");

    if (summary && summary.innerText.trim().length > 0) {
        var errorModal = new bootstrap.Modal(document.getElementById('errorModal'));
        errorModal.show();
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

// žž se modal zavře, zničíme cropper (aby nezůstal v paměti)
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

            imgPreview.src = url;
            imgPreview.classList.remove('d-none');
            if (placeholder) placeholder.classList.add('d-none');
            
            var fileInput = document.getElementById('upload-photo');
            var dataTransfer = new DataTransfer();
            
            var file = new File([blob], "profile_cropped.jpg", { type: "image/jpeg" });
            dataTransfer.items.add(file);
            fileInput.files = dataTransfer.files;
            
            cropModal.hide();
            enableEdit();
        });
    }
});

document.addEventListener("DOMContentLoaded", function() {
    var summary = document.querySelector(".validation-summary-errors");
    if (summary && summary.innerText.trim().length > 0) {
        var errorModal = new bootstrap.Modal(document.getElementById('errorModal'));
        errorModal.show();
    }
});