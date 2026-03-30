/**
 * Notifications helper module.
 * Standardizes the display of SweetAlert2 (Swal) pop-ups across the application.
 */
import { DOM } from "./dom.js";

const Notifications = {
    /**
     * Initializes notifications by checking for server-side messages.
     * Searches for a DOM element with ID 'swal-data' containing a JSON payload
     * typically injected via TempData from ASP.NET Core controllers.
     */
    init() {
        const container = DOM.byId("swal-data");
        if (!container?.dataset.json?.length) return;

        try {
            Notifications.show(JSON.parse(container.dataset.json));
        } catch (e) {
            console.error("Invalid JSON data for Swal: ", e);
        }
    },

    /**
     * Core method to trigger a SweetAlert2 pop-up.
     * @param {Object} options - Configuration object following the Swal.fire() schema.
     * @returns {Promise} Resolves when the user interacts with the alert.
     */
    show(options) {
        return Swal.fire({
            icon: options.icon || 'info',
            title: options.title || '',
            text: options.text || '',
            html: options.html,
            footer: options.footer,
            timer: options.timer,
            // Behavior
            allowOutsideClick: options.allowOutsideClick ?? true,
            allowEscapeKey: options.allowEscapeKey ?? true,
            showCloseButton: options.showCloseButton ?? false,
            backdrop: options.backdrop || "var(--ips-shadow-soft)",
            didOpen: options.didOpen,
            preConfirm: options.preConfirm,
            showLoaderOnConfirm: options.showLoaderOnConfirm ?? false,
            customClass: options.customClass,
            // Input
            input: options.input,
            inputLabel: options.inputLabel,
            inputAttributes: options.inputAttributes,
            inputPlaceholder: options.inputPlaceholder,
            inputValidator: options.inputValidator,
            // Confirm Button
            showConfirmButton: options.showConfirmButton ?? true,
            confirmButtonText: options.confirmButtonText || 'OK',
            confirmButtonColor: options.confirmButtonColor || 'var(--ips)',
            confirmButtonAriaLabel: options.confirmButtonAriaLabel || options.confirmButtonText || 'OK',
            // Deny Button
            showDenyButton: options.showDenyButton ?? false,
            denyButtonText: options.denyButtonText || 'Não',
            denyButtonColor: options.denyButtonColor || 'var(--deny)',
            denyButtonAriaLabel: options.denyButtonAriaLabel || options.denyButtonText || 'Não',
            // Cancel Button
            showCancelButton: options.showCancelButton ?? false,
            cancelButtonText: options.cancelButtonText || 'Cancelar',
            cancelButtonColor: options.cancelButtonColor || 'var(--cancel)',
            cancelButtonAriaLabel: options.cancelButtonAriaLabel || options.cancelButtonText || 'Cancelar'
        });
    },

    /**
     * Success alert wrapper. Optimized for positive feedback.
     * Auto-closes after 3 seconds.
     */
    success(msg, html) {
        return this.show({
            icon: 'success',
            title: 'Operação Concluída',
            text: html ? '' : msg,
            html: html,
            timer: 3000,
            showConfirmButton: false,
            showCloseButton: true
        });
    },

    /**
     * Error alert wrapper. Requires explicit user action to dismiss.
     * Includes a persistent support link in the footer.
     */
    error(msg, html) {
        return this.show({
            icon: 'error',
            title: 'Erro',
            text: html ? '' : msg,
            html: html,
            allowOutsideClick: false,
            allowEscapeKey: false,
            footer: 'Se o erro persistir, contacte o <a href="mailto:segues2026@gmail.com">suporte</a>.'
        });
    },

    /**
     * Warning alert wrapper. Used for critical information that requires acknowledgment.
     */
    warning(msg, html) {
        return this.show({
            icon: 'warning',
            title: 'Aviso',
            text: html ? '' : msg,
            html: html,
            allowOutsideClick: false,
            allowEscapeKey: false
        });
    },

    /**
     * Info alert wrapper. Used for non-critical status updates.
     */
    info(msg, html, showConfirmButton = false, timer = 4000) {
        return this.show({
            icon: 'info',
            title: 'Informação',
            text: html ? '' : msg,
            html: html,
            timer: timer,
            showConfirmButton: showConfirmButton,
            showCloseButton: true
        });
    },

    /**
     * Confirmation dialog wrapper. 
     * @returns {Promise<SweetAlertResult>} Used to handle binary user decisions (Yes/No).
     */
    confirm(msg, html, title = 'Confirma Operação?') {
        return this.show({
            icon: 'question',
            title: title,
            text: html ? '' : msg,
            html: html,
            showCancelButton: true,
            confirmButtonText: 'Sim',
            confirmButtonAriaLabel: 'Sim',
            cancelButtonText: 'Não',
            cancelButtonAriaLabel: 'Não',
            allowOutsideClick: false,
            allowEscapeKey: false
        });
    },

    /**
     * Loading state wrapper. Disables interaction while background tasks process.
     */
    loading() {
        return this.show({
            icon: 'info',
            title: 'A processar...',
            text: '',
            html: '<p class="p-5 text-center">Por favor, aguarde.</p>',
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => {
                Swal.showLoading();
            }
        })
    },

    /**
     * Transitions a loading alert into an error state.
     */
    loadingError(html) {
        setTimeout(() => {
            Swal.hideLoading();
        }, 0);
        setTimeout(() => {
            Swal.update({
                icon: 'error',
                title: 'Erro',
                html: html,
                showConfirmButton: true,
                footer: 'Se o erro persistir, contacte o <a href="mailto:segues2026@gmail.com">suporte</a>.'
            });
        }, 0);
    },

    /**
     * Transitions a loading alert into a success state.
     */
    loadingSuccess(title, html) {
        setTimeout(() => {
            Swal.hideLoading();
        }, 0);
        setTimeout(() => {
            Swal.update({
                title: title,
                html: html,
                showConfirmButton: true
            });
        }, 0);
    },
    /**
      * Transitions a loading alert into a "No Results" state.
      * Specifically used for search operations that return an empty set.
      */
    loadingSuccessEmpty(html) {
        setTimeout(() => {
            Swal.hideLoading();
        }, 0);
        setTimeout(() => {
            Swal.update({
                title: 'Sem resultados',
                html: html,
                showConfirmButton: true
            });
        }, 0);
    }
};

DOM.bindDocumentLoad(Notifications.init);
export { Notifications };