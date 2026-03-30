/**
 * Home Area UI Controller.
 * Manages general landing page interactions, such as the menu selection logic.
 */
import { DOM, Notifications } from "./core/core.js";

/**
 * Displays a selection modal to let the user choose which menu to view.
 * @remarks
 * Uses a customized SweetAlert2 (Notifications) dialog with three actions:
 * - Confirm: Opens the Canteen (Refeitório) URL.
 * - Deny: Opens the Bar URL.
 * - Cancel: Closes the modal.
 * Both links are opened in a new browser tab ("_blank").
 */
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

/**
 * Home Module initialization.
 */
const Home = {
    /**
     * Binds the pickMenu function to the appropriate button.
     */
    init() {
        DOM.bind('pickMenu', 'click', pickMenu);
    }
}

DOM.bindDocumentLoad(Home.init);
export { Home };