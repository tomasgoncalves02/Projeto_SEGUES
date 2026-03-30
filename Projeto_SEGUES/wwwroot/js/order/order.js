/**
 * Active Order Management UI Controller.
 * Handles order code visualization, cancellation flows, and the secure delivery 
 * validation process using status transitions and code verification.
 */
import { DOM, Notifications, Api } from "../core/core.js";

/**
 * Displays the unique order redemption code in a high-visibility modal.
 */
function showCode(code) {
    Notifications.show({
        title: 'Código do Pedido',
        html: `<div class="p-3">
                   <h2 class="fw-bold text-color-ips" style="letter-spacing: 6px;">${code}</h2>
                   <p class="text-muted small mt-2">
                       Apresente este código no bar para validação.<br />
                       Mantém o brilho do telemóvel alto para facilitar a leitura.
                   </p>
               </div>`
    });
}

/**
 * Triggers a confirmation dialog for user-initiated order cancellation.
 */
function confirmOrderCancellation() {
    Notifications.confirm("Tem a certeza que quer cancelar este pedido?")
        .then(result => {
            if (result.isConfirmed) {
                const form = DOM.byId("orderCancellationForm");
                if (form) form.submit();
            }
        });
}

/**
 * Orchestrates the state transition of an order and updates HTMX targets.
 */
async function changeOrderState() {
    const orderId = Number(this.dataset.id);
    const newStatus = Number(this.dataset.status);

    const leftPanel = DOM.byId("side-panel-target");
    if (!leftPanel) return;

    const hxGet = leftPanel.getAttribute('hx-get').replace("{{}}", String(orderId));
    leftPanel.setAttribute('hx-get', hxGet);

    if (newStatus === 4) return validateDelivery(orderId);
    await performStatusUpdate(orderId, newStatus);
    document.body.dispatchEvent(new Event("orderUpdated"));
}

/**
 * Performs a standard status update via API.
 */
async function performStatusUpdate(orderId, newStatus) {
    const data = await Api.post(`/Order/OrderManagement/UpdateStatus`, { id: orderId, newStatus });
    if (!data || data.errorMessage) return;
    if (data.failMessage) {
        Notifications.error(data.failMessage);
        return;
    }
    Notifications.success(data.successMessage);
}

/**
 * Secure delivery validation using a 6-to-8 digit code entry.
 */
function validateDelivery(orderId) {
    let successMessage = "";
    Notifications.show({
        title: 'Validar Entrega',
        inputLabel: 'Introduza o código fornecido pelo cliente:',
        input: 'text',
        inputAttributes: { maxlength: 8, autofocus: true },
        customClass: { input: 'text-center code-input' },
        inputPlaceholder: 'XXXXXXXX',
        inputValidator: async (code) => {
            if (!code) return "O código é obrigatório.";

            const data = await Api.post(`/Order/OrderManagement/ValidateOrderCode`, { id: orderId, enteredCode: code });

            if (!data) return "Erro na validação. Tente novamente.";
            if (data.errorMessage) return data.errorMessage;
            if (data.failMessage) return data.failMessage;

            successMessage = data.successMessage;
            return null; // Validation passed
        },
        showCancelButton: true,
        allowOutsideClick: false,
        allowEscapeKey: false,
    }).then(() => {
        Notifications.success(successMessage);
        document.body.dispatchEvent(new Event("orderUpdated"));
    });
}

/**
 * Order Module initialization and event delegation.
 */
const Order = {
    init() {
        DOM.delegate('showCode', 'click', function() {
            showCode(this.dataset.code);
        });
        DOM.delegate("confirmOrderCancellation", "click", confirmOrderCancellation);
        DOM.delegate("changeOrderState", "click", changeOrderState);
    },
};

DOM.bindDocumentLoad(Order.init);

export { Order };