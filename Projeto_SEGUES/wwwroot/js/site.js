// Add Events
document.addEventListener("DOMContentLoaded", addEvents);

function addEvents() {
    // Swal
    const swalContainer = document.getElementById("swal-data");
    if (swalContainer && swalContainer.dataset.json && swalContainer.dataset.json.length > 0) {
        try {
            showSwal(JSON.parse(swalContainer.dataset.json));
        } catch (e) {
            console.error("Invalid JSON data for Swal:", e);
        }
    }
    
    // Password Visibility Toggle
    function setupPasswordToggle(buttonId, inputId) {
        const toggleBtn = document.getElementById( buttonId );
        const passwordInput = document.getElementById( inputId );
        if (toggleBtn && passwordInput) {
            toggleBtn.addEventListener('click', () => togglePasswordVisibility(toggleBtn, passwordInput));
        }
    }
    setupPasswordToggle('togglePassword', 'passwordInput');
    setupPasswordToggle('toggleConfirmPassword', 'confirmPasswordInput');
    
    // Verify Email Validation Code and Auto-Submit
    setupVerifyCode('verificationCodeInput');
}

// Swal
function showSwal(options) {
    if (!options || typeof options !== "object") {
        console.error("showSwal requires a valid options object.");
        return;
    }

    const config = {
        icon: options.icon || 'info',
        title: options.title || '',
        text: options.text || '',
        html: options.html || undefined,
        footer: options.footer || undefined,
        timer: options.timer || undefined,
        allowOutsideClick: options.allowOutsideClick ?? true,
        allowEscapeKey: options.allowEscapeKey ?? true,
        showCloseButton: options.showCloseButton ?? false,
        backdrop: options.backdrop || undefined,
        // Confirm Button
        showConfirmButton: options.showConfirmButton ?? true,
        confirmButtonText: options.confirmButtonText || 'OK',
        confirmButtonColor: options.confirmButtonColor || 'var(--ips)',
        confirmButtonAriaLabel: options.confirmButtonAriaLabel || 'OK',
        // Deny Button
        showDenyButton: options.showDenyButton ?? false,
        denyButtonText: options.denyButtonText || 'Não',
        denyButtonColor: options.denyButtonColor || 'var(--deny)',
        denyButtonAriaLabel: options.denyButtonAriaLabel || 'Não',
        // Cancel Button
        showCancelButton: options.showCancelButton ?? false,
        cancelButtonText: options.cancelButtonText || 'Cancelar',
        cancelButtonColor: options.cancelButtonColor || 'var(--cancel)',
        cancelButtonAriaLabel: options.cancelButtonAriaLabel || 'Cancelar'
    };
    Swal.fire(config);
}

// Password Visibility Toggle
function togglePasswordVisibility(toggleBtn, passwordInput) {
    const icon = toggleBtn.getElementsByTagName('i')[0];
    const isPassword = passwordInput.type === 'password';
    passwordInput.type = isPassword ? 'text' : 'password';
    if (icon) {
        icon.classList.toggle('bi-eye');
        icon.classList.toggle('bi-eye-slash');
    }
}

// Verify Email Validation Code and Auto-Submit
function setupVerifyCode(inputId) {
    const codeInput = document.getElementById(inputId);
    if (!codeInput) return;
    
    const validationForm = codeInput.closest("form");
    codeInput.addEventListener("input", function () {
        // Remove any non-numeric characters
        this.value = this.value.replace(/[^0-9]/g, '');
        // Auto-submit when 6 digits are reached
        if (this.value.length === 6 && validationForm) {
            validationForm.submit();
        }
    });
}

