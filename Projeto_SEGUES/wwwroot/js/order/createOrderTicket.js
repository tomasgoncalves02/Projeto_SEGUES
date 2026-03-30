/**
 * Ticket Purchase UI Controller.
 * Manages the meal ticket acquisition flow, including real-time total calculation 
 * and transactional confirmation dialogs.
 */
import { DOM, Notifications } from "../core/core.js";
import { MathUtils } from "../core/mathUtils.js";

/**
 * Validates the quantity and triggers a confirmation modal before purchasing tickets.
 * @param {Event} e - Form submission event.
 * @remarks
 * Uses a hard limit of 99 units to prevent accidental bulk purchases. 
 * Prevents default form submission to wait for the user's explicit confirmation via SweetAlert.
 */
function buyTicket(e) {
    e.preventDefault();
    const quantity = DOM.byId('quantityInput').value;

    // Validation: Ensures quantity is within institutional limits (1-99)
    if (quantity <= 0 || quantity > 99) {
        Notifications.error('Quantidade inválida. Por favor, insira uma quantidade entre 1 e 99.');
        return;
    }

    const totalValue = DOM.byId('totalValue').textContent;

    // Confirmation dialog with dynamic HTML content
    Notifications.confirm(null,
        `Deseja comprar <b>${quantity}</b> senha(s)?<br>Total a descontar: <b>${totalValue}</b>`
    ).then((result) => {
        if (result.isConfirmed) {
            // "this" context refers to the form being submitted
            this.submit();
        }
    });
}

/**
 * Updates the total price display based on the selected quantity and the user category's unit price.
 * @remarks
 * Uses MathUtils to ensure the currency formatting is consistent with the server-side locale.
 */
function updateTicketCart() {
    const totalDisplay = DOM.byId('totalValue');
    const unitPrice = this.dataset.unitprice;

    if (totalDisplay) {
        // Calculation performed in real-time as the user types or clicks
        totalDisplay.textContent = MathUtils.calculateTotal(this.value, unitPrice);
    }
}

/**
 * Exported CreateOrderTicket object for ticket-related storefront logic.
 */
const CreateOrderTicket = {
    /**
     * Binds input events for real-time calculation and form submission for validation.
     */
    init() {
        DOM.bind('purchaseTicketForm', 'submit', buyTicket);
        DOM.bind('quantityInput', 'input', updateTicketCart);
    }
}

// Automatic initialization when the document is ready
DOM.bindDocumentLoad(CreateOrderTicket.init);

export { CreateOrderTicket };