document.addEventListener('DOMContentLoaded', function () {
    Chart.defaults.font.family = "'Plus Jakarta Sans', sans-serif";
    Chart.defaults.color = "#6c757d";
    
    const data = window.statsData;
    if (!data) return;
    
    const ctxTrend = document.getElementById('trendChart');
    if (ctxTrend) {
        new Chart(ctxTrend, {
            type: 'line',
            data: {
                labels: data.trendLabels,
                datasets: [{
                    label: 'Nové úkoly',
                    data: data.trendValues,
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    fill: true,
                    tension: 0.4,
                    borderWidth: 3,
                    pointBackgroundColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { 
                    y: { beginAtZero: true, grid: { borderDash: [5, 5] } },
                    x: { grid: { display: false } }
                }
            }
        });
    }
    
    const ctxStatus = document.getElementById('statusChart');
    if (ctxStatus) {
        new Chart(ctxStatus, {
            type: 'doughnut',
            data: {
                labels: data.statusLabels,
                datasets: [{
                    data: data.statusValues,
                    backgroundColor: ['#0d6efd', '#198754', '#ffc107', '#dc3545', '#6610f2', '#0dcaf0'],
                    borderWidth: 0,
                    cutout: '75%'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } }
            }
        });
    }
    
    const ctxTime = document.getElementById('timeChart');
    if (ctxTime) {
        const hourLabels = Array.from({length: 24}, (_, i) => i + ':00');
        new Chart(ctxTime, {
            type: 'bar',
            data: {
                labels: hourLabels,
                datasets: [{
                    label: 'Počet dokončení',
                    data: data.completionHours,
                    backgroundColor: 'rgba(25, 135, 84, 0.6)',
                    borderRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { 
                    y: { beginAtZero: true, ticks: { stepSize: 1 } }, 
                    x: { grid: { display: false } } 
                }
            }
        });
    }
    
    const ctxWeekly = document.getElementById('weeklyChart');
    if (ctxWeekly) {
        const dayLabels = ['Pondělí', 'Úterý', 'Středa', 'Čtvrtek', 'Pátek', 'Sobota', 'Neděle'];
        new Chart(ctxWeekly, {
            type: 'bar',
            data: {
                labels: dayLabels,
                datasets: [{
                    label: 'Aktivita',
                    data: data.weeklyActivity,
                    backgroundColor: 'rgba(111, 66, 193, 0.6)',
                    borderColor: '#6f42c1',
                    borderWidth: 1,
                    borderRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { 
                    y: { beginAtZero: true, ticks: { stepSize: 1 } },
                    x: { grid: { display: false } }
                }
            }
        });
    }
});
