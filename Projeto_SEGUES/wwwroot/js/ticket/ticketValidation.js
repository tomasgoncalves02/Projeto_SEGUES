import { DOM, Notifications } from "../core/core.js";

function scanTicket() {
    const qrContainerId = "qr-reader";
    const html5QrCode = new window.Html5Qrcode(qrContainerId);

    function onScanSuccess(decodedText, decodedResult) {
        decodedResult = null;
        DOM.byId('ticketCodeInput').value = decodedText;

        html5QrCode.stop().then(() => {
            DOM.bySelector('button[type="submit"]', DOM.byId('validationForm')).focus();
        }).catch((err) => {
            Notifications.error("Erro ao parar a câmara: " + err.message);
        });
    }

    html5QrCode.start(
        { facingMode: "environment" },
        {
            fps: 10,
            qrbox: { width: 200, height: 200 }
        },
        onScanSuccess,
        () => { /* Ignore errors */ }
    ).catch((err) => {
        Notifications.error("Verifique as permissões do navegador. Erro ao iniciar a câmara: " + err.message);
        
        const qrContainer = DOM.byId(qrContainerId);
        qrContainer.innerHTML = `
            <div class="alert alert-warning d-flex align-items-center text-start m-0 border-0 shadow-sm rounded-3" role="alert">
                <i class="bi bi-exclamation-triangle-fill fs-3 me-3 text-warning"></i>
                <div>
                    <strong>Câmara Inacessível</strong><br>
                    <small class="text-muted">Verifique as permissões do navegador ou insira o código manualmente.</small>
                </div>
            </div>
        `;
    });
}

const TicketValidation = {
    init() {
        DOM.bind('ticketCodeInput', 'blur', function() {
            setTimeout(() => DOM.byId('ticketCodeInput').focus(), 100);
        }, true);
        scanTicket();
    }
};

DOM.bindDocumentLoad(TicketValidation.init);
export { TicketValidation };