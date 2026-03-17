import { DOM, Notifications } from "./core/core.js";

function pickMenu() {
    const btn = document.getElementById('pickMenu');
    const refeitorioUrl = btn.getAttribute('data-refeitorio');
    const barUrl = btn.getAttribute('data-bar');

    Notifications.show({
        title: 'Qual ementa deseja visualizar?',
        icon: 'question',
        showCancelButton: true,
        showDenyButton: true,
        confirmButtonText: 'Refeitório',
        denyButtonText: 'Bar',
        cancelButtonText: 'Cancelar',
        denyButtonColor: 'var(--ips)',
        cancelButtonColor: 'var(--deny)'
    }).then((result) => {
        if (result.isConfirmed && refeitorioUrl) {
            window.open(refeitorioUrl, "_blank");
        } else if (result.isDenied && barUrl) {
            window.open(barUrl, "_blank");
        }
    });
}

const Home = {
    init() {
        DOM.bind('pickMenu', 'click', pickMenu);
    }
}

DOM.bindDocumentLoad(Home.init);
export { Home };