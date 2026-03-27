import { DOM, Notifications, StringUtils } from "../core/core.js";

function showProductDetails() {
    let product = JSON.parse(this.dataset.product);
    Notifications.show({
        title: 'Detalhes do Produto',
        html:
            `<div class="text-start p-1 p-md-2">
                <h6 class="text-color-ips fw-bold mb-1 mb-md-2 fs-5">Nome do Produto</h6>
                <p class="mb-2 mb-md-4 fs-7 fs-md-5">${product.name}</p>
                <h6 class="text-color-ips fw-bold mb-1 mb-md-2 fs-5">Descrição</h6>
                <p class="mb-2 mb-md-4 fs-7 fs-md-5">${product.description}</p>
                <div class="row g-2 g-md-3">
                    <div class="col-6">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-2 py-md-3 px-2">
                                <h6 class="text-color-ips fw-bold mb-1 fs-5">Categoria</h6>
                                <p class="mb-1 mb-md-2 fs-7 fs-md-5 lh-sm" title='${product.categoryDescription}'>${product.categoryName}</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-6">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-2 py-md-3 px-2">
                                <h6 class="text-color-ips fw-bold mb-1 fs-5">Preço</h6>
                                <p class="mb-1 mb-md-2 fs-7 fs-md-5 lh-sm">${product.price}</p>
                            </div>
                        </div>
                    </div>
                </div>
                ${ product.stock && product.minStock ? 
                `<div class="row g-2 g-md-3 mt-1 mt-md-2">
                    <div class="col-6">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-2 py-md-3 px-2">
                                <h6 class="text-color-ips fw-bold mb-1 fs-5">Stock</h6>
                                <p class="mb-1 mb-md-2 fs-7 fs-md-5 lh-sm">${product.stock}</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-6">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body text-center py-2 py-md-3 px-2">
                                <h6 class="text-color-ips fw-bold mb-1 fs-5">Stock Mínimo</h6>
                                <p class="mb-1 mb-md-2 fs-7 fs-md-5 lh-sm">${product.minStock}</p>
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
    // Filter values
    const categoryFilter = (DOM.byId('categoryFilter').value ?? '').toLowerCase();
    const nameFilter = StringUtils.normalize(DOM.byId('nameFilter').value ?? '');
    const maxPriceVal = DOM.byId('maxPriceFilter').value;
    const maxPrice = maxPriceVal ? parseFloat(maxPriceVal) : null;
    const stockFilter = DOM.byId('stockFilter').value ?? '';
    const activeOnly = DOM.byId('activeOnlyFilter').checked ?? false;
    
    let count = 0;
    DOM.byClass('productRow').forEach(row => {
        // Data attributes
        const rowName = StringUtils.normalize(row.dataset.name ?? '');
        const rowCategory = (row.dataset.categoryid ?? '').toLowerCase();
        const rowPrice = parseFloat(row.dataset.price ?? '0');
        const rowStock = parseInt(row.dataset.stock ?? '0', 10);
        const rowMinStock = parseInt(row.dataset.minstock ?? '0', 10);
        const rowIsActive = row.dataset.isactive === 'true';

        let matches = true;
        
        // Name and Category
        if (!rowName.includes(nameFilter) || !rowCategory.includes(categoryFilter)) {
            matches = false;
        }

        // Max Price
        if (matches && maxPrice !== null && !isNaN(maxPrice)) {
            if (rowPrice > maxPrice) matches = false;
        }

        // Stock
        if (matches && stockFilter !== '') {
            if (stockFilter === 'inStock' && rowStock <= 0) matches = false;
            if (stockFilter === 'lowStock' && (rowStock >= rowMinStock || rowStock === 0)) matches = false;
            if (stockFilter === 'outOfStock' && rowStock > 0) matches = false;
        }
        
        // Active only
        if (matches && activeOnly && !rowIsActive) {
            matches = false;
        }
        
        row.style.display = matches ? '' : 'none';
        if (matches) count++;
    });
    DOM.byId('productsCountBadge').textContent = count;
}

function clearProductsTableFilters() {
    DOM.byId('categoryFilter').value = '';
    DOM.byId('nameFilter').value = '';
    DOM.byId('maxPriceFilter').value = '';
    DOM.byId('stockFilter').value = '';
    DOM.byId('activeOnlyFilter').checked = false;
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
        DOM.bind('nameFilter', 'keyup', filterProductsTable);
        DOM.bind('categoryFilter', 'change', filterProductsTable);
        DOM.bind('maxPriceFilter', 'keyup', filterProductsTable);
        DOM.bind('maxPriceFilter', 'change', filterProductsTable);
        DOM.bind('stockFilter', 'change', filterProductsTable);
        DOM.bind('activeOnlyFilter', 'change', filterProductsTable);
        DOM.bind('clearProductsTableFilters', 'click', clearProductsTableFilters);
        DOM.bindAll('confirmDeleteProduct', 'click', confirmDeleteProduct);
        DOM.bindAll('confirmReactivateProduct', 'click', confirmReactivateProduct);
        DOM.bind('editForm', 'submit', handleEditFormSubmit);
    }
};

DOM.bindDocumentLoad(Inventory.init);
export { Inventory };