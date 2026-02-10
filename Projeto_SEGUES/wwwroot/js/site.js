/*
 * Events registration
 */
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

/*
 * Swal and Notifications Wrapper (matches C# TempDataExtensions
 */
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
        // Behavior
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
    return Swal.fire(config); // Return the promise
}

const Notifications = {
    // Success (Auto-close 3s, no button)
    success: function(message) {
        return showSwal({
            icon: 'success',
            title: 'Operação Concluída',
            text: message,
            timer: 3000,
            showConfirmButton: false,
            showCloseButton: true
        });
    },

    // Error (Sticky, footer link)
    error: function(message) {
        return showSwal({
            icon: 'error',
            title: 'Erro',
            text: message,
            allowOutsideClick: false,
            allowEscapeKey: false,
            footer: "Se o erro persistir, contacte o <a href='mailto:segues2026@gmail.com'>suporte</a>."
        });
    },

    // Warning (Sticky, requires click)
    warning: function(message) {
        return showSwal({
            icon: 'warning',
            title: 'Aviso',
            text: message,
            allowOutsideClick: false,
            allowEscapeKey: false
        });
    },

    // Info (Auto-close 4s)
    info: function(message) {
        return showSwal({
            icon: 'info',
            title: 'Informação',
            text: message,
            timer: 4000,
            showConfirmButton: false,
            showCloseButton: true
        });
    },

    // Confirmation (Returns Promise for logic)
    confirm: function(message) {
        return showSwal({
            icon: 'question',
            title: 'Confirma Operação?',
            text: message,
            showCancelButton: true,
            confirmButtonText: 'Sim',
            confirmButtomAriaLabel: 'Sim',
            cancelButtonText: 'Não',
            cancelButtonAriaLabel: 'Não',
            allowOutsideClick: false,
            allowEscapeKey: false
        });
    }
};

/*
 * Password Visibility Toggle
 */
function togglePasswordVisibility(toggleBtn, passwordInput) {
    const icon = toggleBtn.getElementsByTagName('i')[0];
    const isPassword = passwordInput.type === 'password';
    passwordInput.type = isPassword ? 'text' : 'password';
    if (icon) {
        icon.classList.toggle('bi-eye');
        icon.classList.toggle('bi-eye-slash');
    }
}

/*
 * Input Validation and Formatting
 */
function calculateTotal(quantityString, priceString) {
    if (!quantityString || !priceString) return "0.00";
    const quantity = Number(quantityString) || 0;
    const price = Number(priceString) || 0;
    return (quantity * price).toFixed(2);
}

/*
 * Code generation and verification
 */
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

// Display QR Code
function verQr(code) {
    showSwal({
        title: 'Senha de Refeição',
        html: `
                    <div class="p-3">
                        <img src="https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${code}"
                             class="mb-3 border rounded p-2 shadow-sm" alt="QR">
                        <h2 class="fw-bold text-ips" style="letter-spacing: 4px;">${code}</h2>
                        <p class="text-muted small mt-2">Mantém o brilho do telemóvel alto para facilitar a leitura.</p>
                    </div>
                `,
    });
/*
            html: '<div class="p-4 bg-light border border-2 rounded" style="border-color: #009697 !important;">' +
                '<img src="https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=' + code +'&size=150" alt="QR Code"> </div>' +
                '<br>'+
                '<div class="p-4 bg-light border border-2 rounded" style="border-color: #009697 !important;">' +
                '<h2 class="mb-0 fw-bold" style="letter-spacing: 2px; color: #009697;">' + code + '</h2>' +
                '</div><p class="mt-3 text-muted small">Apresente este código no refeitório para validação.</p>',*/
        
}
