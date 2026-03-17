import { DOM, Notifications } from "./core/core.js";

function pickMenu() {
    const canteenUrl = this.dataset.canteen;
    const barUrl = this.dataset.bar;

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
        if (result.isConfirmed && canteenUrl) {
            window.open(canteenUrl, "_blank");
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