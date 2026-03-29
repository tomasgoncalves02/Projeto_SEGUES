/**
 * Math and Currency Utility Module.
 * Provides standardized methods for financial calculations and localized string formatting.
 */

/**
 * Converts a numeric value into a localized currency string.
 * @param {number|string} value - The raw number to format.
 * @returns {string} Formatted string with two decimal places, comma separator, and Euro symbol (e.g., "10,50 €").
 */
function numberToCurrencyString(value) {
    let nbr = Number(value) || 0;
    // Standardizes the output to European Portuguese format (comma as decimal separator)
    return nbr.toFixed(2).toString().replace('.', ',') + ' €';
}

/**
 * Calculates the total monetary value for a line item and formats it.
 * @param {string|number} quantityString - The amount of items.
 * @param {string|number} priceString - The unit price of the item.
 * @returns {string} The formatted total currency string.
 */
function calculateTotal(quantityString, priceString) {
    if (!quantityString || !priceString) return "0,00 €";

    const quantity = Number(quantityString) || 0;
    const price = Number(priceString) || 0;

    return numberToCurrencyString(quantity * price);
}

/**
 * Exported MathUtils object for global utility access.
 */
const MathUtils = {
    numberToCurrencyString,
    calculateTotal
};

export { MathUtils };