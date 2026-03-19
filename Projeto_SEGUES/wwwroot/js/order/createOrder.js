import { Notifications, Api, DOM } from "../core/core.js";
import { MathUtils } from "../core/mathUtils.js";

function updateCartElement(id, value) {
    const el = DOM.byId(id);
    if (el) {
        el.innerText = value;
        el.style.display = parseFloat(value) > 0.0 ? "inline-block" : "none";
    }
}

async function addToCart(id, name) {
    const qty = DOM.byId('qty-' + id).value;
    if (!qty || qty <= 0)
    {
        Notifications.error("Quantidade inválida.");
        return;
    }
    const data = await Api.post(`/Order/CreateOrder/AddToCart?id=${id}&qty=${qty}`);
    if (!data || data.errorMessage) return;
    if (data.failMessage) {
        Notifications.error(data.failMessage);
        return;
    }
    updateCartElement('cartCount', data.count);
    updateCartElement('cartTotal', MathUtils.numberToCurrencyString(data.value));
    Notifications.success(data.successMessage);
}

async function removeFromCart(id, name) {
    Notifications.confirm(`Desejas remover ${name} do carrinho?`).then(async res => {
        if (res.isConfirmed) {
            const data = await Api.post(`/Order/CreateOrder/RemoveFromCart?id=${id}`);
            if (!data || data.errorMessage) return;
            if (data.failMessage) {
                Notifications.error(data.failMessage);
                return;
            }
            Notifications.success(data.successMessage);
            location.reload();
        }
    });
}

function confirmOrder() {
    Notifications.confirm("Tem a certeza que deseja efetuar este pedido?")
        .then((result) => {
            if (result.isConfirmed) {
                const form = DOM.byId("checkoutForm");
                if (form) form.submit();
            }
        });
}

const CreateOrder = {
    init() {
        DOM.bindAll('addToCart', 'click', async function() {
            await addToCart(this.dataset.id, this.dataset.name);
        });
        DOM.bindAll('removeFromCart', 'click', async function() {
            await removeFromCart(this.dataset.id, this.dataset.name);
        });
        DOM.bind('confirmOrder', 'click', confirmOrder);
    }
}

DOM.bindDocumentLoad(CreateOrder.init);
export { CreateOrder };