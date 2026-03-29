/**
 * Inventory Management UI Controller.
 * Handles product visualization, stock monitoring, and administrative action confirmations.
 */
import { DOM, Notifications } from "../core/core.js";

/**
 * Parses product data from a data-attribute and displays it in a rich-text modal.
 * @remarks
 * This function handles responsive classes (p-md-2, fs-md-5) to ensure product details 
 * are readable on both mobile devices and desktop management consoles.
 * Staff-only data (Stock/Minimum Stock) is rendered conditionally based on object availability.
 */
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
                ${product.stock && product.minStock ?
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
                </div>` : ''}
            </div>`
        ,
        confirmButtonText: 'Fechar',
    });
}

/**
 * Updates the visual badge indicating the total count of visible products.
 * Typically triggered after HTMX filtering operations.
 */
function updateProductsCount() {
    const rowCount = DOM.byClass('showProductDetails')?.length || 0;
    const badge = DOM.byId('productsCountBadge');
    if (!badge) return;

    badge.textContent = rowCount.toString(10);
}

/**
 * Triggers a confirmation dialog before deleting a product.
 * @param {Event} e - Click event.
 */
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

/**
 * Triggers a confirmation dialog before reactivating a previously deleted/disabled product.
 */
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

/**
 * Validates and confirms the submission of the product edit form.
 */
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

/**
 * Inventory Module initialization and rebinding logic.
 */
const Inventory = {
    init() {
        Inventory.rebind();
        DOM.bind('editForm', 'submit', handleEditFormSubmit);
    },
    /**
     * Reattaches event listeners to interactive elements.
     * Essential for maintaining functionality after dynamic table updates.
     */
    rebind() {
        DOM.bindAll('showProductDetails', 'click', showProductDetails);
        DOM.bindAll('confirmDeleteProduct', 'click', confirmDeleteProduct);
        DOM.bindAll('confirmReactivateProduct', 'click', confirmReactivateProduct);
    }
};

// Lifecycle Hooks
DOM.bindDocumentLoad(Inventory.init);
DOM.executeAfterHtmx(Inventory.rebind, updateProductsCount);

export { Inventory };