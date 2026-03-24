import { DOM, Notifications, Api } from "../core/core.js";

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

function confirmOrderCancellation() {
    Notifications.confirm("Tens a certeza que queres cancelar este pedido?")
        .then(result => {
            if (result.isConfirmed) {
                const form = DOM.byId("orderCancellationForm");
                if (form) form.submit();
            }
        });
}

async function changeOrderState() {
    const orderId = Number(this.dataset.id);
    const newStatus = Number(this.dataset.status);
    
    const leftPanel = DOM.byId("side-panel-target");
    if (!leftPanel) return;
    // replace {{}} with orderId
    const hxGet = leftPanel.getAttribute('hx-get').replace("{{}}", String(orderId));
    leftPanel.setAttribute('hx-get', hxGet);
    
    // If transitioning to DELIVERED (Status 4), use the validation flow instead of a direct update
    if (newStatus === 4) return validateDelivery(orderId);
    await performStatusUpdate(orderId, newStatus);
    document.body.dispatchEvent(new Event("orderUpdated"));
}

async function performStatusUpdate(orderId, newStatus) {
    const data = await Api.post(`/Order/OrderManagement/UpdateStatus`, { id: orderId, newStatus });
    if (!data || data.errorMessage) return;
    if (data.failMessage)
    {
        Notifications.error(data.failMessage);
        return ;
    }
    Notifications.success(data.successMessage);
}

function validateDelivery(orderId) {
    let successMessage = "";
    Notifications.show({
        title: 'Validar Entrega',
        inputLabel: 'Introduza o código fornecido pelo cliente:',
        input: 'text',
        inputAttributes: { maxlength: 8, autofocus : true },
        customClass: { input: 'text-center code-input' },
        inputPlaceholder: 'XXXXXXXX',
        inputValidator: async (code) => {
            if (!code) return "O código é obrigatório.";
            const data = await Api.post(`/Order/OrderManagement/ValidateOrderCode`, { id: orderId, enteredCode: code });
            if (!data) return "Erro na validação. Tente novamente.";
            if (data.errorMessage) return data.errorMessage;
            if (data.failMessage) return data.failMessage;
            successMessage = data.successMessage;
            return null; // Validation successful
        },
        showCancelButton: true,
        allowOutsideClick: false,
        allowEscapeKey: false,
    }).then(() => { 
        Notifications.success(successMessage);
        document.body.dispatchEvent(new Event("orderUpdated"));
    });
}

const Order = { 
    init() {
        DOM.bindAll('showCode', 'click', function() {
            showCode(this.dataset.code);
        });
        DOM.bind("confirmOrderCancellation", "click", confirmOrderCancellation);
        DOM.delegate("changeOrderState", "click", changeOrderState);
    }
};

DOM.bindDocumentLoad(Order.init);
export { Order };