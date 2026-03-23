import { DOM } from "../core/dom.js";

function preventDoubleSubmission() {
    const form = DOM.byId('paymentForm');
    if (!form) return;

    // $(this).valid() is provided by Unobtrusive Validation
    if ($(this).valid()) {
        const submitBtn = DOM.bySelector('button[type="submit"]', form);
        if (!submitBtn) return;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>A redirecionar...';
    }
}

function disableKeyStroke(e) {
    const disabledKeys = ['e', 'E', '+', '-'];
    if (disabledKeys.includes(e.key)) {
        e.preventDefault();
    }
}

function formatAmount() {
    const amountInput = DOM.byId('amountInput');
    if (!amountInput) return;
    
    // Remove any non-numeric characters except for dot and comma, then replace comma with dot
    const value = amountInput.value.replace(/[^0-9.,]/g, '').replace(/,/g, '.');
    if (!isNaN(value) && value >= 5 && value <= 1000) {
        amountInput.value = value.toFixed(2);
    } else {
        amountInput.value = 5.00;
    }
}

const Payment = {
    init() {
        DOM.bind('paymentForm', 'submit', preventDoubleSubmission);
        DOM.bind('amountInput', 'keydown', disableKeyStroke);
        DOM.bind('amountInput', 'blur', formatAmount);
    }
}

DOM.bindDocumentLoad(Payment.init);
export { Payment };