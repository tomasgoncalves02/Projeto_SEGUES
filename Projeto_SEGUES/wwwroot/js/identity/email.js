import {DOM} from "../core/dom.js";

const Email = {
    init() {
        DOM.bind('verificationCodeInput', 'input', function () {
            const validationForm = this.closest("form");
            
            // Remove any non-numeric characters
            this.value = this.value.replace(/[^0-9]/g, '');
            // Auto-submit when 6 digits are reached
            if (this.value.length === 6 && validationForm) {
                validationForm.submit();
            }
        });
    }
}

export {Email};