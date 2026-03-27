function normalize(s) {
    // Ignore accents and convert to lowercase
    return s.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
}

const StringUtils = { 
    normalize 
};

export { StringUtils };