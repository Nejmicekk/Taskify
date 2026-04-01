// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function copyToClipboard(text, btnElement) {
    if (!navigator.clipboard) {
        alert("Váš prohlížeč nepodporuje kopírování.");
        return;
    }

    navigator.clipboard.writeText(text).then(() => {
        if (!btnElement.dataset.originalHtml) {
            btnElement.dataset.originalHtml = btnElement.innerHTML;
            btnElement.dataset.originalClass = btnElement.className;
        }

        let newClass = btnElement.dataset.originalClass
            .replace("btn-outline-secondary", "btn-success")
            .replace("btn-secondary", "btn-success")
            .replace("btn-primary", "btn-success");
        if (!newClass.includes("btn-success")) newClass += " btn-success";

        btnElement.className = newClass;

        btnElement.innerHTML = '<i class="bi bi-check-lg"></i> Zkopírováno';

        setTimeout(() => {
            btnElement.className = btnElement.dataset.originalClass;
            btnElement.innerHTML = btnElement.dataset.originalHtml;
        }, 1000);

    }).catch(err => {
        console.error('Chyba při kopírování:', err);
    });
}

function showStatusAlert(message, isSuccess = true) {
    const container = document.getElementById('status-message-container');
    const alertClass = isSuccess ? 'success' : 'danger';
    
    const alertHtml = `
        <div class="alert alert-${alertClass} alert-dismissible fade show shadow-sm border-0 rounded-3" role="alert">
            <div class="d-flex align-items-center">
                <i class="bi ${isSuccess ? 'bi-check-circle-fill' : 'bi-exclamation-octagon-fill'} me-2"></i>
                <div>${message}</div>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;

    container.innerHTML = alertHtml;
    
    setTimeout(() => {
        const alertNode = container.querySelector('.alert');
        if (alertNode) {
            const bsAlert = new bootstrap.Alert(alertNode);
            bsAlert.close();
        }
    }, 5000);
}

let notificationOffset = 5;

function loadMoreNotifications(event) {
    if (event) {
        event.stopPropagation();
        event.preventDefault();
    }

    const btn = document.getElementById('loadMoreBtn');
    const originalText = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Načítání...';
    btn.disabled = true;

    fetch(`/Notifications/Index?handler=LoadMore&offset=${notificationOffset}&count=5`)
    .then(response => response.text())
    .then(html => {
        if (html.trim() === '') {
            document.getElementById('load-more-container').innerHTML = '<p class="small text-muted py-2 mb-0">Žádná další oznámení</p>';
        } else {
            const container = document.getElementById('loaded-notifications-container');
            container.insertAdjacentHTML('beforeend', html);
            notificationOffset += 5;
            btn.innerHTML = originalText;
            btn.disabled = false;
        }
    })
    .catch(err => {
        console.error('Error loading more notifications:', err);
        btn.innerHTML = originalText;
        btn.disabled = false;
    });
}

// Notification System
function markAsRead(id, event) {
    let targetUrl = null;
    if (event) {
        event.stopPropagation();
        event.preventDefault();
        // Get URL from the notification item element
        const item = event.currentTarget;
        if (item && item.dataset.url && item.dataset.url !== "") {
            targetUrl = item.dataset.url;
        }
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    fetch(`/Notifications/Index?handler=MarkAsRead&id=${id}`, {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update UI - remove highlight and badge
            const notifElement = document.getElementById(`notif-${id}`) || document.querySelector(`.notification-item[onclick*="markAsRead(${id}"]`);
            if (notifElement) {
                notifElement.classList.remove('bg-light-blue');
                const unreadDot = notifElement.querySelector('.bg-primary.rounded-circle');
                if (unreadDot) unreadDot.remove();
                const btn = notifElement.querySelector('button');
                if (btn) btn.remove();
            }
            updateBadge();
            
            // Redirect if we have a target URL
            if (targetUrl) {
                window.location.href = targetUrl;
            }
        }
    });
}

function markAllAsRead(event) {
    if (event) {
        event.stopPropagation();
        event.preventDefault();
    }

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Notifications/Index?handler=MarkAllAsRead', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            document.querySelectorAll('.bg-light-blue').forEach(el => el.classList.remove('bg-light-blue'));
            document.querySelectorAll('.notification-item button, .list-group-item button').forEach(el => el.remove());
            document.querySelectorAll('.notification-item .bg-primary.rounded-circle').forEach(el => el.remove());
            const badge = document.getElementById('notificationBadge');
            if (badge) badge.remove();
            
            const markAllBtn = document.querySelector('button[onclick*="markAllAsRead"]');
            if (markAllBtn) markAllBtn.remove();
        }
    });
}

function updateBadge() {
    const badge = document.getElementById('notificationBadge');
    if (!badge) return;

    let count = parseInt(badge.innerText);
    if (!isNaN(count)) {
        count--;
        if (count <= 0) {
            badge.remove();
            const markAllBtn = document.querySelector('button[onclick*="markAllAsRead"]');
            if (markAllBtn) markAllBtn.remove();
        } else {
            badge.innerText = count > 99 ? '99+' : count;
        }
    }
}