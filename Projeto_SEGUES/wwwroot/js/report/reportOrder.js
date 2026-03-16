import { DOM, Notifications, Api } from "../core/core.js";

async function showOrderDetails(id) {
    // Create a modal with loading spinner
    Notifications.loading();
    // Fetch order details
    try {
        const data = await Api.get(`/Report/ReportOrder/GetOrderDetails/${id}`);

        if (!data || !data.products || data.products.length === 0) {
            Notifications.loadingSuccessEmpty('<div class="p-4 text-center text-muted">Nenhum detalhe encontrado para este pedido.</div>');
            return;
        }

        // Generate table rows
        const rows = data.products.map(p => `
            <tr>
                <td class="text-start fw-bold">${p.name}</td>
                <td class="fw-bold text-center">${p.quantity}</td>
                <td class="text-color-ips fw-bold">${p.price.toFixed(2)}€</td>
            </tr>
        `).join('');
        Notifications.loadingSuccess(
            '<h2 class="fw-bold mb-0">Detalhes do Pedido</h2>',
            `<div class="text-start mb-3 mt-4">
                <p class="mb-0 text-muted fw-bold">Código de Recolha</p>
                <h4 class="text-color-ips fw-bold" style="letter-spacing: 2px;">${data.code}</h4>
            </div>
            <div class="table-responsive border rounded-3 shadow-sm">
                <table class="table table-hover mb-0">
                    <thead class="bg-color-ips text-white small">
                        <tr>
                            <th class="text-start">Produto</th>
                            <th>Qtd</th>
                            <th>Preço</th>
                        </tr>
                    </thead>
                    <tbody class="text-center">${rows}</tbody>
                </table>
            </div>`
        );
    } catch (err) {
        console.error("Erro ao carregar detalhes:", err);
        Notifications.loadingError('<div class="p-4 text-danger text-center">Não foi possível carregar os detalhes do pedido. Verifique as permissões.</div>');
    }
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