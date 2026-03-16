import {DOM} from "../core/dom.js";

const TicketValidation = {
    init() {
        DOM.bindDocumentLoad(function() {
            const codeInput = DOM.byId('ticketCodeInput');
            if (!codeInput) return;
            codeInput.focus();
            codeInput.addEventListener('blur', function() {
                setTimeout(() => this.focus(), 100);
            });
        });
    }
};