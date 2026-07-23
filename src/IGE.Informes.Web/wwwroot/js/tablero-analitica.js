const charts = {};
let chartJsLoaded = false;

async function ensureChartJsLoaded() {
    if (chartJsLoaded || window.Chart) {
        chartJsLoaded = true;
        return;
    }

    await new Promise((resolve, reject) => {
        const script = document.createElement("script");
        script.src = "/lib/chartjs/chart.umd.min.js";
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });

    chartJsLoaded = true;
}

export async function renderBarChart(canvasId, labels, data, color) {
    await ensureChartJsLoaded();
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        return;
    }

    if (charts[canvasId]) {
        charts[canvasId].destroy();
    }

    charts[canvasId] = new Chart(canvas, {
        type: "bar",
        data: {
            labels,
            datasets: [{
                data,
                backgroundColor: color,
            }],
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: false },
            },
            scales: {
                y: { beginAtZero: true, ticks: { precision: 0 } },
            },
        },
    });
}

export function destroyAllCharts() {
    for (const id of Object.keys(charts)) {
        charts[id].destroy();
        delete charts[id];
    }
}

export function downloadCsv(fileName, csvContent) {
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}
