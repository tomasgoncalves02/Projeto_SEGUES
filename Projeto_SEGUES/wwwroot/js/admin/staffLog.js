import { DOM, Notifications } from "../core/core.js";

/**
 * Parses staff log data from a data-attribute and displays it in a rich-text modal.
 * Translates specific backend paths to user-friendly operation names.
 */
function showStaffLogDetails(e) {
    e.preventDefault();
    // Extract data
    const {action, message, path, timestamp} = this.dataset;

    // Translate specific paths to user-friendly names
    let friendlyPath = path || "N/A";
    if (path) {
        if (path.includes("/AdminTicketManagement/Validate")) {
            friendlyPath = "Validação de Senhas";
        } else if (path.includes("/Order/UpdateStatus")) {
            friendlyPath = "Atualização do Pedido Bar";
        } else if (path.includes("/Order/ValidateCode")) {
            friendlyPath = "Entrega do Pedido Bar";
        }
    }

    Notifications.show({
        title: 'Detalhes da Atividade',
        html:
            `<div class="text-start p-1 p-md-2 mt-3">
                
                <div class="row g-2 g-md-3 mb-3">
                    <div class="col-12 col-md-6 mb-1">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body py-2 py-md-3 px-3">
                                <h6 class="text-color-ips fw-bold mb-1 fs-6 small">Operação</h6>
                                <p class="mb-0 fs-6 fw-bold text-dark lh-sm">${action || '---'}</p>
                            </div>
                        </div>
                    </div>
                    <div class="col-12 col-md-6 mb-1">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body py-2 py-md-3 px-3">
                                <h6 class="text-color-ips fw-bold mb-1 fs-6 small">Data e Hora</h6>
                                <p class="mb-0 fs-6 fw-bold text-dark lh-sm">${timestamp || '---'}</p>
                            </div>
                        </div>
                    </div>
                    
                    <div class="col-12 mb-1">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body py-2 py-md-3 px-3">
                                <h6 class="text-color-ips fw-bold mb-2 fs-6 small">Caminho / Área</h6>
                                <p class="mb-0 fs-6 text-muted font-monospace" style="word-break: break-all;">${friendlyPath}</p>
                            </div>
                        </div>
                    </div>
                    
                    <div class="col-12">
                        <div class="card bg-color-ips-very-light border-0 shadow-sm rounded-3 h-100">
                            <div class="card-body py-2 py-md-3 px-3">
                                <h6 class="text-color-ips fw-bold mb-2 fs-6 small">Mensagem Técnica</h6>
                                <p class="text-muted mb-0 fs-6">${message || 'Sem descrição disponível.'}</p>
                            </div>
                        </div>
                    </div>
            </div>`,
        confirmButtonText: 'Fechar Detalhes',
        customClass: {
            confirmButton: 'btn btn-ips w-100 py-2 mt-2 fw-bold shadow-sm'
        }
    });
}

function updateLogsCount() {
    const rowCount = DOM.byClass('showStaffLogDetails')?.length || 0;
    const badge = DOM.byId('logsCountBadge');
    if (!badge) return;
    badge.textContent = rowCount.toString(10);
}

function syncExportData(e) {
    e.preventDefault();
    const form = DOM.byId('exportPdfForm');
    if (!form) return;

    // Sync filter values to hidden inputs in the export form
    DOM.byId('exportPdfSearch').value = DOM.byId('searchFilter')?.value || '';
    DOM.byId('exportPdfAction').value = DOM.byId('actionFilter')?.value || '';
    DOM.byId('exportPdfDate').value = DOM.byId('dateFilter')?.value || '';

    form.submit();
}

/**
 * Staff Log Module initialization and rebinding logic.
 */
const StaffLog = {
    init() {
        DOM.delegate('showStaffLogDetails', 'click', showStaffLogDetails);
        DOM.bind('exportPdfForm', 'submit', syncExportData);
    }
};
    
// Lifecycle Hooks
DOM.bindDocumentLoad(StaffLog.init);
DOM.executeAfterHtmx(updateLogsCount);

export { StaffLog };