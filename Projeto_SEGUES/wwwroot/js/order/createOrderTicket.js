import { DOM, Notifications } from "../core/core.js";
import { MathUtils } from "../core/mathUtils.js";

function buyTicket(e) {
    e.preventDefault();
    const quantity = DOM.byId('quantityInput').value;
    const totalValue = DOM.byId('totalValue').textContent;
    Notifications.confirm(null,
        `Deseja comprar <b>${quantity}</b> senha(s)?<br>Total a descontar: <b>${totalValue}</b>`
    ).then((result) => {
        if (result.isConfirmed) {
            this.submit();
        }
    });
}

function updateTicketCart() {
    const totalDisplay = DOM.byId('totalValue');
    const unitPrice = this.dataset.unitprice;
    if (totalDisplay) {
        totalDisplay.textContent = MathUtils.calculateTotal(this.value, unitPrice);
    }
}

const CreateOrderTicket = {
    init() {
        DOM.bind('purchaseTicketForm', 'submit', buyTicket);
        DOM.bind('quantityInput', 'input', updateTicketCart);
    }
}

DOM.bindDocumentLoad(CreateOrderTicket.init);
export { CreateOrderTicket };