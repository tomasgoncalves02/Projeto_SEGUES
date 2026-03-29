/**
 * Email and Identity Verification Utility Module.
 * Optimizes the user experience during security code validation (2FA/OTP).
 */
import { DOM } from "../core/dom.js";

/**
 * Validates the input content and automatically submits the form when 
 * the required code length is met.
 * @remarks
 * This function enforces a numeric-only policy and triggers a form submission 
 * exactly at 6 digits, reducing the number of clicks required by the user.
 */
function autoSubmitVerificationCode() {
    const validationForm = DOM.byId('verificationCodeForm');
    const inputField = DOM.byId('verificationCodeInput');

    // Safety check: ensure both the form and the input exist in the current DOM
    if (!validationForm || !inputField) return;

    // Data Sanitization: Remove any non-numeric characters in real-time
    inputField.value = inputField.value.replace(/[^0-9]/g, '');

    // Auto-submission Logic: Triggers when the standard 6-digit OTP length is reached
    if (inputField.value.length === 6) {
        validationForm.submit();
    }
}

/**
 * Exported Email object for identity-related frontend logic.
 */
const Email = {
    /**
     * Initializes event listeners for verification inputs.
     * Binds the 'input' event to the verification field for real-time tracking.
     */
    init() {
        DOM.bind('verificationCodeInput', 'input', autoSubmitVerificationCode);
    }
}

// Ensure the module initializes as soon as the DOM is ready
DOM.bindDocumentLoad(Email.init);

export { Email };