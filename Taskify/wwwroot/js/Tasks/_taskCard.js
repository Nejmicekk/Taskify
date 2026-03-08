document.addEventListener("DOMContentLoaded", function() {
    const imagesContainers = document.querySelectorAll('.task-main-img');

    imagesContainers.forEach(img => {
        const rawData = img.getAttribute('data-images');
        if (!rawData) return;

        const images = JSON.parse(rawData);
        if (!images || images.length <= 1) return;

        images.forEach(src => {
            const cacheImage = new Image();
            cacheImage.src = src;
        });

        let interval;
        let currentIndex = 0;
        const cardElement = img.closest('.card');

        if (cardElement) {
            img.style.transition = 'opacity 0.2s ease-in-out';
            cardElement.addEventListener('mouseenter', () => {
                interval = setInterval(() => {
                    currentIndex = (currentIndex + 1) % images.length;
                    img.style.opacity = '0.3';
                    setTimeout(() => {
                        img.src = images[currentIndex];
                        img.decode().then(() => {
                            img.style.opacity = '1';
                        }).catch(() => {
                            img.style.opacity = '1';
                        });
                    }, 200);
                }, 2000);
            });

            cardElement.addEventListener('mouseleave', () => {
                clearInterval(interval);
                currentIndex = 0;
                img.src = images[0];
                img.style.opacity = '1';
            });
        }
    });
});

function openReportModal(taskId) {
    document.getElementById('reportTaskId').value = taskId;
    var myModal = new bootstrap.Modal(document.getElementById('reportModal'));
    myModal.show();
}

async function submitReport() {
    const taskId = document.getElementById('reportTaskId').value;
    const reason = parseInt(document.getElementById('reportReason').value);
    const description = document.getElementById('reportDescription').value;
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    const data = {
        taskId: parseInt(taskId),
        reason: reason,
        description: description
    };
    
    const response = await fetch('/Reports/Report', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify(data)
    });

    if (!response.ok) {
        const errorText = await response.text();
        console.error("Server error:", errorText);
        alert("Server vrátil chybu: " + response.status);
        return;
    }

    const result = await response.json();
    if (result.success) {
        showStatusAlert(result.message, true);
    } else {
        showStatusAlert(result.message, false);
    }
    const modalEl = document.getElementById('reportModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) {
        modal.hide();
    }
}