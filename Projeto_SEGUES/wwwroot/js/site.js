/* ===================
 * Imports
 * =================== */
import { DOM, Api, Notifications } from "./core/core.js";


/**
 * Exibe Detalhes do Produto no Inventário (BarProductViewModel)
 */

function showProductDetails(id, name, description, price, stock) {
    const nameElem = document.getElementById('view-name');
    const descElem = document.getElementById('view-description');
    const priceElem = document.getElementById('view-price');
    const stockElem = document.getElementById('view-stock');

    if (nameElem) nameElem.innerText = name;
    if (descElem) descElem.innerText = description || "Sem descrição disponível.";
    if (priceElem) priceElem.innerText = formatCurrency(price.replace(',', '.'));

    if (stockElem) {
        stockElem.innerText = stock;
        stockElem.className = "fw-bold fs-4 " +
            (stock <= 0 ? "text-danger" : (stock < 5 ? "text-warning" : "text-success"));
    }

    const modalElem = document.getElementById('productModal');
    if (modalElem) {
        new bootstrap.Modal(modalElem).show();
    }
}



/**
 * Lógica de atualização de estado com validação de código para entrega
 */
function handleUpdate() {
    const statusSelect = document.getElementById('statusSelect');
    const orderIdInput = document.getElementById('orderId');

    const status = statusSelect.value;
    const orderId = orderIdInput.value;

    // CASO 1: SE FOR ENTREGUE (STATUS 4) -> ABRE POPUP
    if (status === "4") {
        Swal.fire({
            title: 'Validar Entrega',
            text: 'Introduza o código do cliente:',
            input: 'text',
            inputAttributes: { autocapitalize: 'characters' },
            showCancelButton: true,
            confirmButtonText: 'Validar',
            confirmButtonColor: 'var(--ips)',
            showLoaderOnConfirm: true,
            preConfirm: (code) => {
                return fetch(`/Order/OrderManagement/ValidateOrderCode?id=${orderId}&codeEntered=${code}`, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value }
                })
                    .then(response => {
                        if (!response.ok) return response.json().then(data => { throw new Error(data.message) });
                        return response.json();
                    })
                    .catch(error => { Swal.showValidationMessage(error.message) });
            }
        }).then((result) => {
            if (result.isConfirmed) {
                Notifications.success("Pedido entregue!");
                // Atualiza o badge para Entregue em vez de remover
                const row = document.querySelector(`tr[data-order-id="${orderId}"]`);
                if (row) {
                    const badge = row.querySelector('.badge');
                    if (badge) {
                        badge.className = 'badge bg-success';
                        badge.textContent = 'Entregue';
                    }
                }
                // Remove após 5 segundos
                setTimeout(() => {
                    row?.remove();
                }, 5000);
            }
        });
    }
    // CASO 2: OUTROS ESTADOS (PENDENTE, PREPARAÇÃO, PRONTO) -> UPDATE DIRETO
    else {
        // Fazemos um fetch para a Action UpdateStatus que já tinhas
        fetch(`/Order/OrderManagement/UpdateStatus`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: `id=${orderId}&newStatus=${status}`
        })
            .then(response => {
                if (response.ok) {
                    Notifications.success("Estado atualizado com sucesso!");
                    // Se usas HTMX para o painel lateral, podes recarregar apenas o painel
                    // Para simplificar agora, vamos recarregar a página:
                    setTimeout(() => window.location.reload(), 1000);
                } else {
                    Notifications.error("Erro ao atualizar o estado.");
                }
            });

    }
}



// ── Estatísticas: Resumo de Refeições ────────────────────────────────────────

let mealsChart;
let barChartB;
let productCategoryChart;

function renderChart(data, period) {
    const config = {
        '1': { sub: 'Refeições por hora hoje', x: 'Horas' },
        '2': { sub: 'Refeições por dia esta semana', x: 'Dias da Semana' },
        '3': { sub: 'Refeições por dia este mês', x: 'Dias do Mês' },
        '4': { sub: 'Refeições por mês este ano', x: 'Meses' },
        '5': { sub: 'Refeições por mês (Ano Atual)', x: 'Meses do Ano' }
    };

    const currentConfig = config[period] || { sub: '', x: 'Tempo' };

    const canvas = document.getElementById('mealsChart');
    if (!canvas) return; 

    const ctx = canvas.getContext('2d');

    if (mealsChart) {
        mealsChart.destroy();
    }

    const subtitleElem = document.getElementById('chartSubtitle');
    if (subtitleElem) subtitleElem.textContent = currentConfig.sub;
    
    const chartLabels = data.map(d => d.label);
    const chartValues = data.map(d => d.count);

    mealsChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartLabels,
            datasets: [{
                label: 'Refeições',
                data: chartValues,
                borderColor: 'darkcyan',
                backgroundColor: 'rgba(0,139,139,0.15)', 
                borderWidth: 3,
                pointRadius: 5,
                pointHoverRadius: 7,
                pointBackgroundColor: 'darkcyan',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: currentConfig.x, 
                        color: '#6c757d',
                        font: {
                            size: 12,
                            weight: 'bold'
                        },
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
                        font: {
                            size: 12,
                            weight: 'bold'
                        },
                        padding: { bottom: 10 }
                    },
                    ticks: {
                        stepSize: 1,
                        precision: 0,
                        color: '#6c757d'
                    },
                    grid: {
                        color: 'rgba(0,0,0,0.04)'
                    }
                }
            }
        }
    });
}


function renderChartB(data, period) {
    const config = {
        '1': { sub: 'Pedidos por hora hoje', x: 'Horas' },
        '2': { sub: 'Pedidos por dia esta semana', x: 'Dias da Semana' },
        '3': { sub: 'Pedidos por dia este mês', x: 'Dias do Mês' },
        '4': { sub: 'Pedidos por mês este ano', x: 'Meses' },
        '5': { sub: 'Pedidos por mês (Ano Atual)', x: 'Meses do Ano' }
    };

    const currentConfig = config[period] || { sub: '', x: 'Tempo' };

    const canvas = document.getElementById('categoryChartB');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    if (barChartB) {
        barChartB.destroy();
    }

    const subtitleElem = document.getElementById('chartSubtitleB');
    if (subtitleElem) subtitleElem.textContent = currentConfig.sub;

    const chartLabels = data.map(d => d.label);
    const chartValues = data.map(d => d.count);

    barChartB = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartLabels,
            datasets: [{
                label: 'Pedidos',
                data: chartValues,
                borderColor: 'darkcyan',
                backgroundColor: 'rgba(0,139,139,0.15)',
                borderWidth: 3,
                pointRadius: 5,
                pointHoverRadius: 7,
                pointBackgroundColor: 'darkcyan',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    title: {
                        display: true,
                        text: currentConfig.x,
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
        }
    });
}
function renderDoughnutChart(data) {
    const canvas = document.getElementById('productCategoryChart');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    if (productCategoryChart) {
        productCategoryChart.destroy();
    }

    const safeData = data || [];
    const labels = safeData.map(d => d.category);
    const values = safeData.map(d => d.count);

    
    const bgColors = ['#009697', '#007f80', 'rgba(0, 150, 151, 0.35)', '#dc3545', '#6c757d'];

    productCategoryChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: values,
                backgroundColor: bgColors,
                borderWidth: 2, 
                borderColor: '#ffffff'
            }]
        },
        options: {
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
        }
    });
}

function clearDataB() {
    document.getElementById('idConsumptionB').textContent = '...';
    document.getElementById('idMoneyB').textContent = '...';
    document.getElementById('idAverageB').textContent = '...';
    document.getElementById('idBuyersB').textContent = '...';

    const tbody = document.getElementById('topProductsTableBody');
    if (tbody) {
        tbody.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-3">A carregar...</td></tr>';
    }

    if (productCategoryChart) {
        productCategoryChart.data.labels = [];
        productCategoryChart.data.datasets.forEach((dataset) => {
            dataset.data = [];
        });
        productCategoryChart.update();
    }

    if (barChartB) {
        barChartB.data.labels = [];
        barChartB.options.scales.x.title.text = '';
        barChartB.data.datasets.forEach((dataset) => {
            dataset.data = [];
        });
        barChartB.update();
    }
}





function clearDataR() {
    document.getElementById('idMeals').textContent = '...';
    document.getElementById('idMoney').textContent = '...';
    document.getElementById('idAverage').textContent = '...';
    document.getElementById('idBuyers').textContent = '...';
    document.getElementById('idStudent').textContent = '...';
    document.getElementById('idExternal').textContent = '...';
    document.getElementById('idWorker').textContent = '...';



    if (mealsChart) {

        mealsChart.data.labels = [];
        mealsChart.options.scales.x.title.text = '';
        mealsChart.data.datasets.forEach((dataset) => {
            dataset.data = [];
        });


        mealsChart.update();
    }

        
}




async function loadMealsSummary() {

    clearDataR();

    const periodSelect = document.getElementById('selectTicketsPeriod');
    const period = periodSelect?.value;

    
    try {
        const response = await fetch(`/Statistics/StatisticsTicket/GetTicketsStats?period=${encodeURIComponent(period)}`);

       
        if (!response.ok) {
            throw new Error(`Erro do Servidor: ${response.status}`);
        }

        const d = await response.json();
        const cat = d.byCategory ?? [];
        const find = name => (cat.find(c => c.category === name)?.count ?? 0);

        document.getElementById('idMeals').textContent = d.totalMeals ?? 0;
        document.getElementById('idMoney').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.totalRevenue ?? 0);
        document.getElementById('idAverage').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.averageRevenue ?? 0);
        document.getElementById('idBuyers').textContent = d.newBuyers ?? 0;

        document.getElementById('idStudent').textContent = find('Estudante');
        document.getElementById('idExternal').textContent = find('Externo');
        document.getElementById('idWorker').textContent = find('Trabalhador IPS');

        renderChart(d.chart, period);
    } catch (error) {
        console.error("Erro ao carregar estatísticas do refeitório:", error);
    }
}


async function loadBarSummary() {

    clearDataB();

    const periodSelect = document.getElementById('selectOrdersPeriod');
    const period = periodSelect?.value;



    try {

        const d = await fetch(`/Statistics/StatisticsBar/GetBarStats?period=${encodeURIComponent(period)}`)
            .then(r => r.json());
        const cat = d.byCategory ?? [];
        const find = name => (cat.find(c => c.category === name)?.count ?? 0);



        document.getElementById('idConsumptionB').textContent = d.totalConsumptions ?? 0;
        document.getElementById('idMoneyB').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.totalRevenue ?? 0);
        document.getElementById('idAverageB').textContent =
            new Intl.NumberFormat('pt-PT', { style: 'currency', currency: 'EUR' }).format(d.averageRevenue ?? 0);
        document.getElementById('idBuyersB').textContent = d.newBuyers ?? 0;

        renderChartB(d.chart, period);
        renderDoughnutChart(d.productCategories);

        if (typeof renderDoughnutChart === 'function') renderDoughnutChart(d.productCategories);
        renderTopProductsTable(d.topProducts);

    } catch (error) {
        console.error("Erro ao carregar estatísticas do bar:", error);
    }
}

function renderTopProductsTable(data) {
    const tbody = document.getElementById('topProductsTableBody');
    if (!tbody) return;

    tbody.innerHTML = ''; 

    const safeData = data || [];

   
    if (safeData.length === 0) {
        tbody.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-3">Sem vendas registadas neste período.</td></tr>';
        return;
    }

    
    safeData.forEach(item => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td class="fw-semibold text-dark">${item.name}</td>
            <td class="text-center fw-bold" style="color: var(--ips); font-size: 1.1rem;">${item.quantity}</td>
        `;
        tbody.appendChild(tr);
    });
}












