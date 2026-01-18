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