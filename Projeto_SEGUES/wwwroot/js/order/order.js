import { DOM, Notifications } from "../core/core.js";

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

const Order = { 
    init() {
        DOM.bindAll('showCode', 'click', function() {
            showCode(this.dataset.code);
        });
        DOM.bind("confirmOrderCancellation", "click", confirmOrderCancellation);
    }
};

DOM.bindDocumentLoad(Order.init);
export { Order };