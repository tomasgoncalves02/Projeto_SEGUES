import {DOM} from "../core/dom.js";
import {Notifications} from "../core/notifications";

function showQr(code) {
    Notifications.show({
        title: 'Senha de Refeição',
        html: `<div class="p-3 text-color-ips">
                 <img src="https://api.qrserver.com/v1/create-qr-code/?size=180x180&data=${code}" class="mb-3 border rounded p-2 shadow-sm" alt="QR Code">
                 <h2 class="fw-bold text-color-ips" style="letter-spacing: 6px;">${code}</h2>
                 <p class="text-muted small mt-2">
                    Apresente este código no refeitório para validação.<br />
                    Mantém o brilho do telemóvel alto para facilitar a leitura.
                 </p>
              </div>`,
        backdrop: 'var(--ips-shadow-soft)'
    });
}

const Ticket = {
    init() {
        DOM.bindAll('showQr', 'click', function() {
            showQr(this.dataset.code);
        });
    }
};
export {Ticket};