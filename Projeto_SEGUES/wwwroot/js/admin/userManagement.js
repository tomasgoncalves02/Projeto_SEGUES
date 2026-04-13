import { DOM, Notifications } from "../core/core.js";

/**
 * Confirm user deactivation
 */
function confirmDeactivateUser(e) {
    e.preventDefault();
    const { id, name } = this.dataset;

    Notifications.confirm(
        '',
        `Tem a certeza que deseja desativar o utilizador <b>${name}</b>?<br>` +
        `<small class='text-muted'>O utilizador deixará de ter acesso à plataforma.</small>`
    ).then((result) => {
        if (result.isConfirmed) {
            DOM.byId(`deactivate-form-${id}`)?.submit();
        }
    });
}

/**
 * Confirm user activation
 */
function confirmActivateUser(e) {
    e.preventDefault();
    const { id, name } = this.dataset;

    Notifications.confirm(
        'Reativar Conta',
        `Deseja reativar a conta do utilizador <b>${name}</b>?`
    ).then((result) => {
        if (result.isConfirmed) {
            DOM.byId(`activate-form-${id}`)?.submit();
        }
    });
}

/**
 * Updates the user count badge in the UI.
 */
function updateUsersCount() {
    const rowCount = DOM.byClass('userRow')?.length || 0;
    const badge = DOM.byId('usersCountBadge');
    if (!badge) return;

    badge.textContent = rowCount.toString(10);
}

function syncExportData(e) {
    e.preventDefault();
    const form = DOM.byId('exportPdfForm');
    if (!form) return;

    // Sync filter values to hidden inputs in the export form
    DOM.byId('exportPdfSearch').value = DOM.byId('searchFilter')?.value || '';
    DOM.byId('exportPdfRole').value = DOM.byId('roleFilter')?.value || '';
    DOM.byId('exportPdfCategory').value = DOM.byId('categoryFilter')?.value || '';
    DOM.byId('exportPdfActiveOnly').value = DOM.byId('activeOnlySwitch')?.checked ? 'true' : 'false';

    form.submit();
}

const UserManagement = {
    init() {
        DOM.delegate('confirmDeactivateUser', 'click', confirmDeactivateUser);
        DOM.delegate('confirmActivateUser', 'click', confirmActivateUser);
        DOM.bind('exportPdfForm', 'submit', syncExportData);
    }
};

DOM.bindDocumentLoad(UserManagement.init);
DOM.executeAfterHtmx(updateUsersCount);

export { UserManagement };