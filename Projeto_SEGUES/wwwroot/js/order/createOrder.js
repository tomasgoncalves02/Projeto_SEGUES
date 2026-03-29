import { Notifications, Api, DOM } from "../core/core.js";
import { MathUtils } from "../core/mathUtils.js";

function updateCartElement(id, value, isCurrency = false) {
    const el = DOM.byId(id);
    if (el) {
        el.innerText = isCurrency ? MathUtils.numberToCurrencyString(value) : value;
        el.style.display = parseFloat(value) > 0 ? "inline-block" : "none";
    }
}

async function addToCart(id) {
    const qty = DOM.byId('qty-' + id).value;
    if (!qty || qty <= 0 || qty > 99)
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
    updateCartElement('cartTotal', data.value, true);
    Notifications.success(data.successMessage);
}

function toggleTimePicker() {
    let nowBtn = DOM.byId('now');
    let nowLabel = DOM.byId('nowLabel');
    let laterBtn = DOM.byId('later');
    let laterLabel = DOM.byId('laterLabel');
    let timePicker = DOM.byId('pickup-time-wrapper');
    let timePickerInput = DOM.byId('pickupTime');
    if (laterBtn.checked || !nowBtn.checked) {
        let now = new Date();
        timePickerInput.value = String(now.getHours()).padStart(2, '0') + ':' + String(now.getMinutes()).padStart(2, '0');
        timePicker.classList.remove('d-none');
        
        laterLabel.classList.add('btn-ips')
        laterLabel.classList.remove('btn-ips-outline-secondary', 'border-secondary-subtle')
        
        nowLabel.classList.remove('btn-ips')
        nowLabel.classList.add('btn-ips-outline-secondary', 'border-secondary-subtle')
    } else {
        timePicker.classList.add('d-none');
        
        laterLabel.classList.remove('btn-ips')
        laterLabel.classList.add('btn-ips-outline-secondary', 'border-secondary-subtle')
        
        nowLabel.classList.add('btn-ips')
        nowLabel.classList.remove('btn-ips-outline-secondary', 'border-secondary-subtle')
    }
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
    let nowBtn = DOM.byId('now');
    if (nowBtn.checked) {
        let now = new Date();
        DOM.byId('pickupTime').value = String(now.getHours()).padStart(2, '0') + ':' + String(now.getMinutes()).padStart(2, '0');
    }
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
        CreateOrder.rebind();
        DOM.bindAll('removeFromCart', 'click', async function() {
            await removeFromCart(this.dataset.id, this.dataset.name);
        });
        DOM.bind('confirmOrder', 'click', confirmOrder);
        DOM.bindAll('receiveNow', 'click', toggleTimePicker);
    },
    rebind() {
        DOM.bindAll('addToCart', 'click', async function() {
            await addToCart(this.dataset.id);
        });
    }
}

DOM.bindDocumentLoad(CreateOrder.init);
DOM.executeAfterHtmx(CreateOrder.rebind);
export { CreateOrder };