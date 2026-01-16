var cropper;
var imageToCrop = document.getElementById('image-to-crop');
var cropModalElement = document.getElementById('cropModal');
var cropModal = new bootstrap.Modal(cropModalElement);

function enableEdit() {
    document.getElementById('input-bio').disabled = false;
    document.getElementById('input-phone').disabled = false;
    document.getElementById('input-email').disabled = false;
    document.getElementById('input-username').disabled = false;

    document.getElementById('edit-btn').classList.add('d-none');
    document.getElementById('save-btn').classList.remove('d-none');
    document.getElementById('cancel-btn').classList.remove('d-none');
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

document.addEventListener("DOMContentLoaded", function() {
    var summary = document.querySelector(".validation-summary-errors");

    if (summary && summary.innerText.trim().length > 0) {
        var errorModal = new bootstrap.Modal(document.getElementById('errorModal'));
        errorModal.show();
    }
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