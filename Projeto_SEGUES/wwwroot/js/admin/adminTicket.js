import { DOM } from "../core/core.js";

function resetFilters() {
    const form = document.getElementById('filterForm');
    if (form) {
        form.reset();
        if (typeof htmx !== 'undefined') {
            htmx.trigger(form, 'change');
        }
    }
}

function exportPdfWithFilters() {
    const form = document.getElementById('filterForm');
    if (form) {
        const formData = new FormData(form);
        const params = new URLSearchParams(formData).toString();
        window.location.href = `/Admin/AdminTicketManagement/ExportTicketsPDF?${params}`;
    }
}
function handlePriceFormatting(e) {
    const decimalInputs = e.target.querySelectorAll('.decimal-input');

    decimalInputs.forEach(input => {
        if (input.value.includes(',')) {
            input.value = input.value.replace(',', '.');
        }
    });
}

const AdminTicket = {
    init() {
        window.resetFilters = resetFilters;
        window.exportPdfWithFilters = exportPdfWithFilters;
        const pricesForm = document.getElementById('pricesForm');

        if (pricesForm) {
            pricesForm.addEventListener('submit', function (e) {
                const inputs = pricesForm.querySelectorAll('.decimal-input');

                inputs.forEach(input => {
                    if (input.value) {
                        input.value = input.value.replace(',', '.');
                    }
                });
                console.log("Preços convertidos para ponto decimal.");
            });
        }
    }
};

DOM.bindDocumentLoad(AdminTicket.init);
export { AdminTicket };