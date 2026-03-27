import { DOM, Notifications, Api } from "../core/core.js";

async function showOrderDetails(id) {
    // Create a modal with loading spinner
    Notifications.loading();
    // Fetch order details
    const data = await Api.get(`/Report/ReportOrder/GetOrderDetails/${id}`);
    
    if (!data) return;
    // BadRequest
    if (data.errorMessage) {
        Notifications.loadingError(`<div class="p-4 text-danger text-center">${data.errorMessage}</div>`);
        return;
    }
    // NotFound
    if (data.failMessage) {
        Notifications.loadingSuccessEmpty(`<div class="p-4 text-center text-muted">${data.failMessage}</div>`);
        return;
    }

    // Generate table rows
    const rows = data.products.map(p => `
        <tr class="align-middle">
            <td class="text-start">
                <div class="fw-bold">${p.name}</div>
                <div class="small text-muted" title="${p.categoryDescription}">${p.categoryName}</div>
            </td>
            <td class="fw-bold">${p.quantity}</td>
            <td class="text-color-ips fw-bold">${p.price}</td>
        </tr>
        `).join('');
    Notifications.loadingSuccess(
        'Detalhes do Pedido', `
        <div class="my-2">
            <p class="mb-2 text-muted fw-bold small fs-md-5">Código de Recolha</p>
            <h4 class="text-color-ips fw-bold bg-color-ips-light d-inline-block px-3 py-2 rounded border" style="letter-spacing: 2px;">${data.code}</h4>
        </div>
        <div class="table-responsive border rounded-3 shadow-sm">
            <table class="table table-hover mb-0">
                <thead class="bg-color-ips text-white">
                    <tr>
                        <th class="text-start ps-3">Produto</th>
                        <th>Qtd</th>
                        <th>Preço</th>
                    </tr>
                </thead>
                <tbody class="text-center">${rows}</tbody>
            </table>
        </div>
    `);
}

const ReportOrder = {
    init() {
        DOM.bindAll('showOrderDetails', 'click', async function() {
            await showOrderDetails(this.dataset.id);
        });
    }
};

DOM.bindDocumentLoad(ReportOrder.init);
export { ReportOrder };