function numberToCurrencyString(value) {
    let nbr = Number(value) || 0;
    return nbr.toFixed(2).toString().replace('.', ',') + ' €';
}

// Calculate total price
function calculateTotal(quantityString, priceString) {
    if (!quantityString || !priceString) return "0,00 €";
    const quantity = Number(quantityString) || 0;
    const price = Number(priceString) || 0;
    return numberToCurrencyString(quantity * price);
}

const MathUtils = { 
    numberToCurrencyString, 
    calculateTotal 
};

export { MathUtils };