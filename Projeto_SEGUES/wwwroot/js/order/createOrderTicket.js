// Calculate total price
function calculateTotal(quantityString, priceString) {
    if (!quantityString || !priceString) return "0,00 €";
    const quantity = Number(quantityString) || 0;
    const price = Number(priceString) || 0;
    return (quantity * price).toFixed(2).toString().replace('.', ',') + ' €';
}

export { calculateTotal };