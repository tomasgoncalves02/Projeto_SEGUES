/*
 * Notifications helper functions.
 * Swal pop-ups.
 */
import {DOM} from "./dom.js";

const Notifications = {
    // Initialize and show Swal pop-ups
    init() {
        const container = DOM.byId("swal-data");
        if (!container?.dataset.json?.length) return;
        
        try {
            this.show(JSON.parse(container.dataset.json));
        } catch (e) {
            console.error("Invalid JSON data for Swal: ", e);
        }
    },
    // Show Swal pop-up with custom options
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
        });
    },
    // Wrapper for success pop-up (Auto-close 3s, no button)
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
    // Wrapper for error pop-up (Sticky, footer link)
    error(msg, html) {
        return this.show({
            icon: 'error',
            title: 'Erro',
            text: html ? '' : msg,
            html: html,
            allowOutsideClick: false,
            allowEscapeKey: false,
            footer: "Se o erro persistir, contacte o <a href='mailto:segues2026@gmail.com'>suporte</a>."
        });
    },
    // Wrapper for warning pop-up (Sticky, requires click)
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
    // Wrapper for info pop-up (Auto-close 4s)
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
    // Wrapper for confirm pop-up (Returns Promise for logic)
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
    }
};
export {Notifications};