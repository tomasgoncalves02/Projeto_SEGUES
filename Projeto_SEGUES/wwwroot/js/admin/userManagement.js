import { DOM, Notifications } from "../core/core.js";

/**
 * Função para confirmar a desativação de um utilizador
 */
function confirmDeactivateUser(e) {
    const btn = e.currentTarget;
    const { id, name } = btn.dataset;

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
 * Função para confirmar a reativação de um utilizador
 */
function confirmActivateUser(e) {
    const { id, name } = e.currentTarget.dataset;

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
 * Filtros da tabela de utilizadores (Search, Role e Category)
 */
function filterUsersTable() {
    const searchFilter = (DOM.byId('searchFilter')?.value ?? '').toLowerCase();
    const roleFilter = (DOM.byId('roleFilter')?.value ?? '').toLowerCase();
    const categoryFilter = (DOM.byId('categoryFilter')?.value ?? '').toLowerCase();

    DOM.byClass('user-row').forEach(row => {
        const userName = (row.dataset.name ?? '').toLowerCase();
        const userEmail = (row.dataset.email ?? '').toLowerCase();
        const userRole = (row.dataset.role ?? '').toLowerCase();
        const userCategory = (row.dataset.category ?? '').toLowerCase();

        const matchesSearch = userName.includes(searchFilter) || userEmail.includes(searchFilter);
        const matchesRole = roleFilter === '' || userRole === roleFilter;
        const matchesCategory = categoryFilter === '' || userCategory === categoryFilter;

        row.style.display = (matchesSearch && matchesRole && matchesCategory) ? '' : 'none';
    });
}

const UserManagement = {
    init() {
        DOM.bindAll('confirmDeactivateUser', 'click', confirmDeactivateUser);
        DOM.bindAll('confirmActivateUser', 'click', confirmActivateUser);
        DOM.bind('searchFilter', 'keyup', filterUsersTable);
        DOM.bind('roleFilter', 'change', filterUsersTable);
        DOM.bind('categoryFilter', 'change', filterUsersTable);
    }
};

DOM.bindDocumentLoad(UserManagement.init);
export { UserManagement };