import { Notifications, DOM } from "../core/core.js";

function updateSelectedCount() {
    const count = document.querySelectorAll('.ticket-checkbox:checked').length;
    const el = DOM.byId('selectedCount');
    if (el) el.innerText = count;
}

async function handleTransfer(e) {
    e.preventDefault();
    const form = e.target;
    const count = document.querySelectorAll('.ticket-checkbox:checked').length;
    const email = form.querySelector('input[name="RecipientEmail"]').value;

    if (count === 0) {
        Notifications.error("Por favor, selecione pelo menos uma senha.");
        return;
    }

    try {
        const response = await fetch(`/Ticket/Ticket/CheckTransferEligibility?email=${encodeURIComponent(email)}`);
        const data = await response.json();

        if (!data.success) {
            Notifications.error(data.message);
            return;
        }

        Notifications.confirm(null, `Enviar ${count} senhas para ${data.recipientName}?`)
            .then(res => { if (res.isConfirmed) form.submit(); });

    } catch (e) {
        Notifications.error("Erro ao validar destinatário.");
    }
}

const init = () => {
    const form = DOM.byId('transferForm');
    if (!form) return;

    if (form.dataset.error) Notifications.error(form.dataset.error);
    if (form.dataset.success) Notifications.success(form.dataset.success);

    DOM.bind('transferForm', 'submit', handleTransfer);

    document.addEventListener('change', (e) => {
        if (e.target.classList.contains('ticket-checkbox')) updateSelectedCount();
    });
};

DOM.bindDocumentLoad(init);