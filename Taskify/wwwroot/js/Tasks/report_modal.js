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