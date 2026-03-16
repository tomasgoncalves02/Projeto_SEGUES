import { DOM } from "../core/dom.js";

function togglePasswordVisibility(toggleBtn, passwordInput) {
    const icon = DOM.byTag('i', toggleBtn)[0];
    const isPassword = passwordInput.type === 'password';
    passwordInput.type = isPassword ? 'text' : 'password';
    if (icon) {
        icon.classList.toggle('bi-eye');
        icon.classList.toggle('bi-eye-slash');
    }
}

function setupPasswordToggle(buttonId, inputId) {
    const toggleBtn = DOM.byId(buttonId);
    const passwordInput = DOM.byId(inputId);

    if (toggleBtn && passwordInput) {
        DOM.bind(buttonId, 'click', () => togglePasswordVisibility(toggleBtn, passwordInput));
    }
}

const Password = {
    init() {
        setupPasswordToggle('toggleOldPassword', 'oldPasswordInput');
        setupPasswordToggle('togglePassword', 'passwordInput');
        setupPasswordToggle('toggleConfirmPassword', 'confirmPasswordInput');
    }
};

DOM.bindDocumentLoad(Password.init);
export { Password };