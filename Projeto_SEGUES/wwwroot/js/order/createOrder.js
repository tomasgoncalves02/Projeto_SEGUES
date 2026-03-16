import { Notifications, Api, DOM } from "../core/core.js";

function updateCartElement(id, value) {
    const el = DOM.byId(id);
    if (el) {
        el.innerText = value;
        el.style.display = value > 0 ? "block" : "none";
    }
}

async function addToCart(id, name) {
    const qty = DOM.byId('qty-' + id).value;
    if (!qty || qty <= 0)
    {
        Notifications.error("Quantidade inválida.");
        return;
    }
    try {
        const data = await Api.post(`/Order/CreateOrder/AddToCart?id=${id}&qty=${qty}`);
        if (!data.success) {
            Notifications.error(`Erro ao adicionar ${qty}x ${name} ao carrinho: ${data.message}`);
            return;
        }
        updateCartElement('cart-count', data.count);
        updateCartElement('cart-total', data.value);
        Notifications.success(`${qty}x ${name} adicionado ao carrinho!`);
    } catch (e) { 
        Notifications.error(`Erro ao adicionar ${qty}x ${name} ao carrinho: ${e.message}`);
    }
}

function removeFromCart(id, name) {
    Notifications.confirm(`Desejas remover ${name} do carrinho?`).then(async res => {
        if (res.isConfirmed) {
            try {
                const data = await Api.post(`/Order/CreateOrder/RemoveFromCart?id=${id}`);

                if (data.success) {
                    location.reload();
                } else {
                    Notifications.error(`Erro ao remover ${name} do carrinho: ${data.message}`);
                }
            } catch (err) {
                Notifications.error(`Erro ao remover ${name} do carrinho: ${e.message}`);
            }
        }
    });
}

export { addToCart, removeFromCart };