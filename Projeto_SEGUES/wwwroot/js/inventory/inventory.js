import { DOM, Notifications } from "../core/core.js";

function showProductDetails() {
    let product = JSON.parse(this.dataset.product);
    Notifications.show({
        title: 'Detalhes do Produto',
        html:
            `<div class="text-start p-3">
                <h4 class="text-color-ips fw-bold">Nome do Produto</h4>
                <p class="mb-4">${product.name}</p>
                <h4 class="text-color-ips fw-bold">Descrição</h4>
                <p class="mb-4">${product.description}</p>
                <div class="row g-3">
                    <div class="col-12 col-md-6">
                        <div class="card bg-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-3">
                                <h4 class="text-color-ips fw-bold">Categoria</h4>
                                <p class="mb-4" title='${product.categoryDescription}'>${product.categoryName}</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-12 col-md-6">
                        <div class="card bg-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-3">
                                <h4 class="text-color-ips fw-bold">Preço</h4>
                                <p class="mb-4">${product.price}</p>
                            </div>
                        </div>
                    </div>
                </div>
                ${ product.stock && product.minStock ? 
                `<div class="row g-3 mt-1">
                    <div class="col-12 col-md-6">
                        <div class="card bg-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-3">
                                <h4 class="text-color-ips fw-bold">Stock</h4>
                                <p class="mb-4">${product.stock}</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-12 col-md-6">
                        <div class="card bg-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-3">
                                <h4 class="text-color-ips fw-bold">Stock Mínimo</h4>
                                <p class="mb-4">${product.minStock}</p>
                            </div>
                        </div>
                    </div>
                </div>` : '' }
            </div>`
        ,
        confirmButtonText: 'Fechar',
    });
}

function filterProductsTable() {
    const categoryFilter = (DOM.byId('categoryFilter')?.value ?? '').toLowerCase();
    const nameFilter = (DOM.byId('nameFilter')?.value ?? '').toLowerCase();
    
    let count = 0;
    DOM.byClass('productRow').forEach(row => {
        const rowName = (row.dataset.name ?? '').toLowerCase();
        const rowCategory = (row.dataset.categoryid ?? '').toLowerCase();
        
        const matches = rowName.includes(nameFilter) && rowCategory.includes(categoryFilter);
        row.style.display = matches ? '' : 'none';
        if (matches) count++;
    });
    DOM.byId('productsCountBadge').textContent = count;
}

function clearProductsTableFilters() {
    DOM.byId('categoryFilter').value = '';
    DOM.byId('nameFilter').value = '';
    filterProductsTable();
}

function confirmDeleteProduct(e) {
    const btn = e.currentTarget;
    const { id, name } = btn.dataset;

    Notifications.confirm(`Tem a certeza que deseja eliminar o produto "${name}"?`)
        .then((result) => {           
            if (result.isConfirmed) {
                DOM.byId(`delete-form-${id}`)?.submit();
            }
        });
}

function confirmReactivateProduct(e) {
    const { id, name } = e.currentTarget.dataset;

    Notifications.confirm(
        'Reativar Produto',
        `Deseja reativar o produto "<b>${name}</b>"? Ele voltará a ficar disponível.`
    ).then((result) => {
        if (result.isConfirmed) {
            DOM.byId(`reactivate-form-${id}`)?.submit();
        }
    });
}
function handleEditFormSubmit(e) {
    e.preventDefault();
    const form = this;

    Notifications.confirm(
        '',
        'Deseja guardar as alterações efetuadas neste produto?'
    ).then((result) => {
        if (result.isConfirmed) {
            form.submit();
        }
    });
}

const Inventory = {
    init() {
        DOM.delegate('showProductDetails', 'click', showProductDetails);
        DOM.bind('categoryFilter', 'change', filterProductsTable);
        DOM.bind('nameFilter', 'keyup', filterProductsTable);
        DOM.bind('clearProductsTableFilters', 'click', clearProductsTableFilters);
        DOM.bindAll('confirmDeleteProduct', 'click', confirmDeleteProduct);
        DOM.bindAll('confirmReactivateProduct', 'click', confirmReactivateProduct);
        DOM.bind('editForm', 'submit', handleEditFormSubmit);
    }
};

DOM.bindDocumentLoad(Inventory.init);
export { Inventory };