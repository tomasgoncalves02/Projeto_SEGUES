import { DOM } from "../core/dom.js";

function updateTicketsCount() {
    const rowCount = DOM.byClass('ticketRow')?.length || 0;
    const badge = DOM.byId('ticketCountBadge');
    if (!badge) return;

    badge.textContent = rowCount.toString(10);
}

function syncExportData(e) {
    e.preventDefault();
    const form = DOM.byId('exportPdfForm');
    if (!form) return;

    // Sync filter values to hidden inputs in the export form
    DOM.byId('exportPdfSearch').value = DOM.byId('searchFilter')?.value || '';
    DOM.byId('exportPdfDate').value = DOM.byId('dateFilter')?.value || '';
    DOM.byId('exportPdfState').value = DOM.byId('stateFilter')?.value || '';
    DOM.byId('exportPdfFlow').value = DOM.byId('flowFilter')?.value || '';

    form.submit();
}

const ReportTicket = {
    init() {
        DOM.bind('exportPdfForm', 'submit', syncExportData);
    }
};

DOM.bindDocumentLoad(ReportTicket.init);
DOM.executeAfterHtmx(updateTicketsCount);

export { ReportTicket };