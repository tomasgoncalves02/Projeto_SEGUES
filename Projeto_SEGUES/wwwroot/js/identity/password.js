/**
 * Password Visibility Toggle Utility Module.
 * Enhances UX in authentication forms by allowing users to reveal/hide their input.
 */
import { DOM } from "../core/dom.js";

/**
 * Switches the input type between 'password' and 'text'.
 * Also toggles the visual state of the associated Bootstrap Icon.
 * @param {HTMLElement} toggleBtn - The button element triggered by the user.
 * @param {HTMLInputElement} passwordInput - The specific input field to be toggled.
 */
function togglePasswordVisibility(toggleBtn, passwordInput) {
    const icon = DOM.byTag('i', toggleBtn)[0];
    const isPassword = passwordInput.type === 'password';

    // Change the input type to reveal/mask the characters
    passwordInput.type = isPassword ? 'text' : 'password';

    // Update the icon class (Bootstrap Icons) for visual feedback
    if (icon) {
        icon.classList.toggle('bi-eye');
        icon.classList.toggle('bi-eye-slash');
    }
}

/**
 * Validates the existence of elements and binds the click event for a specific password field.
 * @param {string} buttonId - ID of the toggle icon/button.
 * @param {string} inputId - ID of the password input field.
 */
function setupPasswordToggle(buttonId, inputId) {
    const toggleBtn = DOM.byId(buttonId);
    const passwordInput = DOM.byId(inputId);

    if (toggleBtn && passwordInput) {
        DOM.bind(buttonId, 'click', () => togglePasswordVisibility(toggleBtn, passwordInput));
    }
}

/**
 * Exported Password object for global identity-related UI logic.
 */
const Password = {
    /**
     * Initializes the toggle logic for standard password scenarios:
     * 1. Old Password (Change Password flow)
     * 2. New Password
     * 3. Confirmation Password
     */
    init() {
        setupPasswordToggle('toggleOldPassword', 'oldPasswordInput');
        setupPasswordToggle('togglePassword', 'passwordInput');
        setupPasswordToggle('toggleConfirmPassword', 'confirmPasswordInput');
    }
};

// Ensure the module initializes automatically on page load
DOM.bindDocumentLoad(Password.init);

export { Password };