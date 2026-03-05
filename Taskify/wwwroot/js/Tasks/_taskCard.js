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