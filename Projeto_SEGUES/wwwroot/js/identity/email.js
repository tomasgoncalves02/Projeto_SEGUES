import { DOM } from "../core/dom.js";

function autoSubmitVerificationCode() {
    const validationForm = DOM.byId('verificationCodeForm');
    const inputField = DOM.byId('verificationCodeInput');
    if (!validationForm || !inputField) return;
    
    // Remove any non-numeric characters
    inputField.value = inputField.value.replace(/[^0-9]/g, '');
    // Auto-submit when 6 digits are reached
    if (inputField.value.length === 6) {
        validationForm.submit();
    }
}

const Email = {
    init() {
        DOM.bind('verificationCodeInput', 'input', autoSubmitVerificationCode);
    }
}

DOM.bindDocumentLoad(Email.init);
export { Email };