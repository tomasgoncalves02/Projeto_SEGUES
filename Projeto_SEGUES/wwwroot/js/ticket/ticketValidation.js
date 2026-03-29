/**
 * Ticket and Order QR Code Validation Module.
 * Integrates the device's camera using Html5Qrcode to scan and 
 * automatically process alphanumeric codes for service validation.
 */
import { DOM, Notifications } from "../core/core.js";

/**
 * Initializes and manages the QR Code scanner lifecycle.
 * @remarks
 * Uses the 'environment' facing mode (rear camera) to facilitate 
 * scanning customer screens. Handles success by stopping the camera 
 * and focusing on the submission button for a seamless workflow.
 */
function scanTicket() {
    const qrContainerId = "qr-reader";
    const html5QrCode = new window.Html5Qrcode(qrContainerId);

    /**
     * Callback triggered when a QR code is successfully decoded.
     * @param {string} decodedText - The alphanumeric code extracted from the QR.
     */
    function onScanSuccess(decodedText, decodedResult) {
        decodedResult = null;
        DOM.byId('ticketCodeInput').value = decodedText;       
        html5QrCode.stop().then(() => {           
            DOM.bySelector('button[type="submit"]', DOM.byId('validationForm')).focus();
        }).catch((err) => {
            Notifications.error("Erro ao parar a câmara: " + err.message);
        });
    }

    // Start the scanner with optimized settings for mobile use
    html5QrCode.start(
        { facingMode: "environment" },
        {
            fps: 10,
            qrbox: { width: 200, height: 200 }
        },
        onScanSuccess,
        () => { /* Ignore frame processing errors to prevent console spam */ }
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

/**
 * Ticket Validation Module initialization.
 */
const TicketValidation = {
    /**
     * Sets up the validation workflow.
     * Enforces an "Always Focused" policy on the input field for barcode scanner compatibility.
     */
    init() {
        // Focus persistence: Re-focus the input if it loses focus (blur)
        // This is crucial for environments using external physical scanners.
        DOM.bind('ticketCodeInput', 'blur', function () {
            setTimeout(() => DOM.byId('ticketCodeInput').focus(), 100);
        }, true);

        // Start the camera-based scanner
        scanTicket();
    }
};

// Application Lifecycle Hook
DOM.bindDocumentLoad(TicketValidation.init);

export { TicketValidation };