/**
 * Payment and Balance Top-up UI Controller.
 * Manages transaction integrity by preventing duplicate submissions and 
 * enforcing strict numerical input validation for payment gateways.
 */
import { DOM } from "../core/dom.js";

/**
 * Prevents multiple form submissions to avoid duplicate charges.
 * @remarks
 * Integrates with jQuery Unobtrusive Validation. If the form is valid, 
 * it disables the submit button and injects a loading spinner. 
 * A 10ms delay is used to ensure the browser successfully initiates the POST 
 * request before the button is disabled.
 */
function preventDoubleSubmission() {
    const form = DOM.byId('paymentForm');
    if (!form) return;

    // Check ASP.NET Core Client-side Validation state
    if ($(form).valid()) {
        const submitBtn = DOM.bySelector('button[type="submit"]', form);
        if (!submitBtn) return;

        // Visual feedback and submission lock
        setTimeout(() => {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>A redirecionar...';
        }, 10);
    }
}

/**
 * Filters keystrokes to ensure only valid currency numbers are entered.
 * @param {KeyboardEvent} e - The keydown event.
 * @remarks
 * Blocks scientific notation characters ('e', 'E') and operators ('+', '-') 
 * that are technically valid in <input type="number"> but invalid for simple currency amounts.
 */
function disableKeyStroke(e) {
    const disabledKeys = ['e', 'E', '+', '-'];
    if (disabledKeys.includes(e.key)) {
        e.preventDefault();
    }
}

/**
 * Normalizes the amount input on blur (losing focus).
 * @remarks
 * Enforces business rules: minimum 5.00€ and maximum 1000.00€. 
 * Automatically formats the input to two decimal places for consistency.
 */
function formatAmount() {
    const amountInput = DOM.byId('amountInput');
    if (!amountInput) return;

    const value = parseFloat(amountInput.value);

    // Validate range and apply two-decimal formatting
    if (!isNaN(value) && value >= 5 && value <= 1000) {
        amountInput.value = value.toFixed(2);
    } else {
        // Fallback to institutional minimum if input is invalid or out of range
        amountInput.value = 5.00;
    }
}

/**
 * Exported Payment object for financial transaction UI logic.
 */
const Payment = {
    /**
     * Binds event listeners for form submission, keystroke filtering, and blur formatting.
     */
    init() {
        DOM.bind('paymentForm', 'submit', preventDoubleSubmission);
        DOM.bind('amountInput', 'keydown', disableKeyStroke);
        DOM.bind('amountInput', 'blur', formatAmount);
    }
}

// Ensure the module initializes automatically when the DOM is ready
DOM.bindDocumentLoad(Payment.init);

export { Payment };