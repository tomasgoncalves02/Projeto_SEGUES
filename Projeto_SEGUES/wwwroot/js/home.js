import {Notifications} from "./core/notifications";
import {DOM} from "./core/dom";

function pickMenu() {
    Notifications.show({
        title: 'Qual ementa deseja visualizar?',
        icon: 'question',
        showCancelButton: true,
        showDenyButton: true,
        confirmButtonText: 'Refeitório',
        denyButtonText: 'Bar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: 'var(--ips)',
        denyButtonColor: 'var(--ips)',
    }).then((result) => {
        if (result.isConfirmed) {
            window.open("https://software.movelife.net/pt-PT/Menus/PublicCC/Tj6o3O_vCFB2LmCmm9VUjw%3d%3d", "_blank");
        } else if (result.isDenied) {
            window.open("https://software.movelife.net/pt-PT/Menus/PublicCC/Tj6o3O_vCFDXvHU0nbgTmg%3d%3d?DaySelected=13%2F03%2F2026&capit=5&idzone=1616", "_blank");
        }
    });
}

const Home = {
    init() {
        DOM.bind('pickMenu', 'click', pickMenu);
    }
}

export {Home};