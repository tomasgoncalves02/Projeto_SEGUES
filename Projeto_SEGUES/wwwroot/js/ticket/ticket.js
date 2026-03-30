/**
 * Meal Ticket Visualization Module.
 * Manages the display of active meal tickets, including dynamic QR Code 
 * generation for canteen validation.
 */
import { DOM, Notifications } from "../core/core.js";

/**
 * Displays a modal containing a generated QR Code and the alphanumeric validation code.
 * @param {string} code - The unique 8-character ticket identifier.
 * @remarks
 * Utilizes the QRServer API to generate the visual code. 
 * Includes specific CSS styles for branding and a tip for users to 
 * maximize screen brightness to aid scanner readability.
 */
function showQr(code) {
    Notifications.show({
        title: 'Senha de Refeição',
        html: `<div class="p-3">
                   <img src="https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${code}" class="mb-3 border rounded p-2 shadow-sm" alt="QR Code">
                   <h2 class="fw-bold text-color-ips" style="letter-spacing: 6px;">${code}</h2>
                   <p class="text-muted small mt-2">
                       Apresente este código no refeitório para validação.<br />
                       Mantém o brilho do telemóvel alto para facilitar a leitura.
                   </p>
               </div>`
    });
}

/**
 * Ticket Module initialization and event management.
 */
const Ticket = {
    /**
     * Entry point for the module. Triggers the initial binding of events.
     */
    init() {
        DOM.delegate('showQr', 'click', function() {
            // Retrieves the unique ticket code from the element's data-code attribute
            showQr(this.dataset.code);
        });
    }
};

// Application Lifecycle Hooks
DOM.bindDocumentLoad(Ticket.init);

export { Ticket };