import { DOM, Api } from "../core/core.js";

/*
 * Generic functions
 */
function updateText(id, value = '') {
    const el = DOM.byId(id);
    if (el) el.textContent = value;
}

function getChartConfig(dataType, period) {
    const config = [
        { sub: '', x: 'Tempo' },
        { sub: `${dataType} por hora hoje`, x: 'Horas' },
        { sub: `${dataType} por dia esta semana`, x: 'Dias da Semana' },
        { sub: `${dataType} por dia este mês`, x: 'Dias do Mês' },
        { sub: `${dataType} por mês este ano`, x: 'Meses' },
        { sub: `${dataType} por mês (Ano Atual)`, x: 'Meses do Ano' }
    ];
    const idx = Number(period) || 0;
    return config[idx];
}

// Updates or creates a Chart.js instance
function updateChart(id, type, data, options)
{
    const canvas = DOM.byId(id);
    if (!canvas) return;
    
    const chart = window.Chart.getChart(canvas);
    // if no chart exists, create a new one
    if (!chart) {
        new Chart(canvas.getContext('2d'), {type, data, options});
        return;
    }
    
    chart.data.labels = data.labels;
    chart.data.datasets = data.datasets;
    chart.options.scales.x.title.text = options.scales.x.title.text;
    chart.update();
}

async function loadOrdersSummary() {
    const period = DOM.byId('selectOrdersPeriod')?.value;
    if (!period) return;
    
    const data = await Api.get('/Report/ReportStatisticsOrder/GetOrdersStats', { period });
    if (!data) return;
    
    updateText('totalOrderBar', data.totalOrders);
    updateText('totalIncomeBar', data.formattedTotalRevenue);
    updateText('averageIncomeBar', data.formattedAverageRevenue);
    updateText('totalBuyersBar', data.numberOfBuyers);
    
    renderOrderChart(data.orderChart, period);
    renderCategoriesChart(data.productCategories);
    renderTopProductsTable(data.topProducts);
}

function renderOrderChart(data, period) {
    const config = getChartConfig('Pedidos', period);
    updateText('orderChartSubtitle', config.sub);
    
    const chartData = {
        labels: data.map(d => d.label),
        datasets: [{
            label: 'Pedidos',
            data: data.map(d => d.count),
            backgroundColor: 'rgba(0,139,139,0.15)',
            borderColor: 'darkcyan',
            tension: 0.4,
            fill: true,
            pointRadius: 5,
            pointHoverRadius: 7,
            pointBackgroundColor: 'darkcyan',
        }]
    };
    
    const options = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: {
                title: {
                    display: true,
                    text: config.x,
                    color: '#6c757d',
                    font: { size: 12, weight: 'bold' },
                    padding: { top: 10 }
                },
                grid: { display: false }
            },
            y: {
                beginAtZero: true,
                grace: '10%',
                title: {
                    display: true,
                    text: 'Nº Pedidos',
                    color: '#6c757d',
                    font: { size: 12, weight: 'bold' },
                    padding: { bottom: 10 }
                },
                ticks: { stepSize: 1, precision: 0, color: '#6c757d' },
                grid: { color: 'rgba(0,0,0,0.04)' }
            }
        }
    };
    
    updateChart('orderChart', 'line', chartData, options);
}

function renderCategoriesChart(data) {
    const chartData = {
        labels: data.map(d => d.category),
        datasets: [{
            data: data.map(d => d.count),
            backgroundColor: ['#009697', '#007f80', 'rgba(0, 150, 151, 0.35)', '#dc3545', '#6c757d'],
            borderWidth: 2,
            borderColor: '#ffffff'
        }]
    };

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        cutout: '65%',
        plugins: { 
            legend: { 
                position: 'bottom',
                labels: {
                    boxWidth: 40,
                    padding: 15,
                    font: { size: 12 }
                }
            }
        }
    };
    
    updateChart('productCategoriesChart', 'doughnut', chartData, options);
}


function renderTopProductsTable(data) {
    const tbody = DOM.byId('topProductsTableBody');
    if (!tbody) return;

    if (!data || data.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="2" class="text-center text-muted py-3">
                    Sem vendas registadas.
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = data.map(p => `
        <tr>
            <td class="fw-semibold text-dark">${p.name}</td>
            <td class="text-center fw-bold text-color-ips">${p.quantity}</td>
        </tr>
    `).join('');
}

/*
 * Canteen
 */

async function loadTicketsSummary() {
    const period = DOM.byId('selectTicketsPeriod')?.value;
    if (!period) return;

    const data = await Api.get('/Report/ReportStatisticsTicket/GetTicketsStats', { period });
    if (!data) return;
    
    const findCat = name => data.byCategory?.find(c => c.category === name)?.count ?? 0;

    updateText('totalUsedTickets', data.totalUsedTickets);
    updateText('totalIncomeCanteen', data.formattedTotalRevenue);
    updateText('averageIncomeCanteen', data.formattedAverageRevenue);
    updateText('totalBuyersCanteen', data.numberOfBuyers);

    updateText('usedStudentTickets', findCat('Estudante'));
    updateText('usedExternalTickets', findCat('Externo'));
    updateText('usedWorkerTickets', findCat('Trabalhador IPS'));
    
    renderTicketChart(data.chart, period);
}

function renderTicketChart(data, period) {
    const config = getChartConfig('Refeições', period);
    updateText('ticketChartSubtitle', config.sub);

    const chartData = {
        labels: data.map(d => d.label),
        datasets: [{
            label: 'Refeições',
            data: data.map(d => d.count),
            borderColor: 'darkcyan',
            backgroundColor: 'rgba(0,139,139,0.15)',
            tension: 0.4,
            fill: true,
            pointRadius: 5,
            pointHoverRadius: 7,
            pointBackgroundColor: 'darkcyan',
        }]
    };

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: { 
                title: { 
                    display: true, 
                    text: config.x,
                    color: '#6c757d',
                    font: { size: 12, weight: 'bold' },
                    padding: { top: 10 }
                },
                grid: { display: false }
            },
            y: { 
                beginAtZero: true,
                grace: '10%',
                title: {
                    display: true,
                    text: 'Nº Senhas',
                    color: '#6c757d',
                    font: { size: 12, weight: 'bold' },
                    padding: { bottom: 10 }
                },
                ticks: { stepSize: 1, precision: 0, color: '#6c757d' },
                grid: { color: 'rgba(0,0,0,0.04)' }
            }
        }
    };
    
    updateChart('ticketsChart', 'line', chartData, options);
}

/*
 * Export
 */
const Statistics = { 
    init() {
        DOM.bind('selectTicketsPeriod', 'change', loadTicketsSummary, true);
        DOM.bind('selectOrdersPeriod', 'change', loadOrdersSummary, true);
    } 
};

DOM.bindDocumentLoad(Statistics.init);
export { Statistics };