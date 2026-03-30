/**
 * Ticket Transfer UI Controller.
 * Manages the multi-selection of meal tickets and the secure transfer 
 * process to another user, including recipient eligibility checks.
 */
import { Notifications, DOM, Api } from "../core/core.js";

/**
 * Toggles the checkbox state when a user clicks anywhere on a ticket row.
 * @param {Event} e - Click event.
 */
function toggleCheckbox(e) {
    if (e.target.classList.contains('ticket-checkbox')) return;

    const row = e.currentTarget || e.target.closest('.ticket-row');
    if (!row) return;

    const checkbox = DOM.bySelector('.ticket-checkbox', row);
    if (!checkbox) return;

    checkbox.checked = !checkbox.checked;
    updateSelectedCount();
}

/**
 * Updates the visual counter showing how many tickets are currently selected.
 */
function updateSelectedCount() {
    const count = DOM.bySelectorAll('.ticket-checkbox:checked').length;
    const el = DOM.byId('selectedCounter');
    if (el) el.innerText = count.toString();
}

/**
 * Handles the transfer form submission with server-side validation.
 * Includes a pre-validation check for recipient eligibility and a confirmation modal.
 * @param {Event} e - Submit event.
 */
async function handleTransfer(e) {
    e.preventDefault();

    const form = DOM.byId('transferForm') || e.target;
    if (!form) return;

    const el = DOM.byId('selectedCounter');
    const count = el ? parseInt(el.innerText) : 0;
    const email = DOM.byId('RecipientEmail')?.value.trim() || '';

    if (count === 0) {
        Notifications.error("Por favor, selecione pelo menos uma senha.");
        return;
    }

    try {
        // Disable button and add loading spinner
        const submitBtn = DOM.byId('transferFormSubmit');
        const originalBtnHtml = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>A validar...';

        const data = await Api.get(`/Ticket/Ticket/CheckTransferEligibility`, { email });
        if (!data) return;

        // Restore button
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnHtml;

        if (!data.success) {
            Notifications.error(data.message);
            return;
        }

        Notifications.confirm(null, `Tem a certeza que deseja enviar ${count} senha(s) para <b>${data.recipientName}</b> (${email})?`)
            .then(res => {
                if (res.isConfirmed) {
                    submitBtn.disabled = true;
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>A enviar...';
                    form.submit();
                }
            });
    } catch (e) {
        Notifications.error("Ocorreu um erro ao comunicar com o servidor. Verifique a sua ligação.");
    }
}

/**
 * TransferTicket Module initialization and event binding.
 */
const TransferTicket = {
    init() {
        DOM.bindAll('ticket-row', 'click', toggleCheckbox);
        DOM.bindAll('ticket-checkbox', 'change', updateSelectedCount);
        DOM.bind('transferForm', 'submit', handleTransfer);
    }
};

DOM.bindDocumentLoad(TransferTicket.init);
export { TransferTicket }