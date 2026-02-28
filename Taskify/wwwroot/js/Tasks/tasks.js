document.addEventListener("DOMContentLoaded", function() {
    const imagesContainers = document.querySelectorAll('.task-main-img');

    imagesContainers.forEach(img => {
        const rawData = img.getAttribute('data-images');
        if (!rawData) return;

        const images = JSON.parse(rawData);
        if (!images || images.length <= 1) return;

        let interval;
        let currentIndex = 0;

        img.closest('.task-item-wrapper').addEventListener('mouseenter', () => {
            interval = setInterval(() => {
                currentIndex = (currentIndex + 1) % images.length;
                img.style.opacity = '0.7';
                setTimeout(() => {
                    img.src = images[currentIndex];
                    img.style.opacity = '1';
                }, 150);
            }, 2000);
        });

        img.closest('.task-item-wrapper').addEventListener('mouseleave', () => {
            clearInterval(interval);
            img.src = images[0];
        });
    });
});