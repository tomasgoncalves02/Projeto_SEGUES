/**
 * String Utility Module.
 * Provides specialized methods for text manipulation and search optimization.
 */

/**
 * Normalizes a string by removing diacritics (accents) and converting it to lowercase.
 * Useful for implementing "fuzzy" search filters that are case-insensitive and accent-insensitive.
 * * @param {string} s - The original string to be normalized.
 * @returns {string} The processed string (e.g., "Café" becomes "cafe").
 */
function normalize(s) {
    if (!s) return "";

    // Step 1: Decompose combined characters (e.g., 'é' into 'e' + '´') using NFD normalization.
    // Step 2: Remove the combining marks (the accents) using a Regex range for Unicode diacritics.
    // Step 3: Convert the final result to lowercase for uniform comparison.
    return s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
}

/**
 * Exported StringUtils object for global utility access.
 */
const StringUtils = {
    normalize
};

export { StringUtils };