/**
 * DOM Helper Module.
 * Provides a standardized interface for element selection and event delegation.
 */
const DOM = {
    /**
     * Selects a single element by its unique ID.
     * @param {string} id - The ID of the element.
     * @returns {HTMLElement|null}
     */
    byId(id) {
        return document.getElementById(id);
    },

    /**
     * Selects all elements with a specific class and returns them as an Array.
     * @param {string} className - The CSS class name.
     * @param {HTMLElement|Document} root - The starting point for the search.
     * @returns {HTMLElement[]}
     */
    byClass(className, root = document) {
        return Array.from(root.getElementsByClassName(className));
    },

    /**
     * Selects elements by their HTML tag name.
     * @param {string} tagName - The tag (e.g., 'div', 'button').
     * @param {HTMLElement|Document} root - Search scope.
     * @returns {HTMLElement[]}
     */
    byTag(tagName, root = document) {
        return Array.from(root.getElementsByTagName(tagName));
    },

    /**
     * Selects elements by their 'name' attribute.
     * @param {string} name - The value of the name attribute.
     * @param {HTMLElement|Document} root - Search scope.
     * @returns {NodeList}
     */
    byName(name, root = document) {
        return root.getElementsByName(name);
    },

    /**
     * Selects the first element matching a CSS selector.
     * @param {string} selector - The CSS selector (e.g., '.class #id').
     * @param {HTMLElement|Document} root - Search scope.
     * @returns {HTMLElement|null}
     */
    bySelector(selector, root) {
        if (!root) root = document;
        return root.querySelector(selector);
    },

    /**
     * Selects all elements matching a CSS selector.
     * @param {string} selector - The CSS selector.
     * @param {HTMLElement|Document} root - Search scope.
     * @returns {NodeList}
     */
    bySelectorAll(selector, root) {
        if (!root) root = document;
        return root.querySelectorAll(selector);
    },

    /**
     * Binds a function to the DOMContentLoaded event.
     * @param {Function} fn - The callback to execute when the document is ready.
     */
    bindDocumentLoad(fn) {
        document.addEventListener('DOMContentLoaded', fn);
    },

    /**
     * Attaches an event listener to a single element by ID.
     * @param {string} id - Target element ID.
     * @param {string} event - Event type (e.g., 'click').
     * @param {Function} fn - Callback function.
     * @param {boolean} executeImmediately - If true, runs the function once upon binding.
     */
    bind(id, event, fn, executeImmediately = false) {
        const el = this.byId(id);
        if (!el) return;
        el.addEventListener(event, fn);
        if (executeImmediately) fn();
    },

    /**
     * Attaches an event listener to all elements currently sharing a class.
     * @param {string} className - Target class.
     * @param {string} event - Event type.
     * @param {Function} fn - Callback function.
     */
    bindAll(className, event, fn) {
        const elems = this.byClass(className);
        elems.forEach(el => el.addEventListener(event, fn));
    },

    /**
     * Delegates an event listener to a parent element.
     * @remarks 
     * Essential for dynamic content added via HTMX or AJAX, as it catches events 
     * from elements that did not exist during the initial page load.
     * @param {string} className - The target child class to match.
     * @param {string} event - Event type.
     * @param {Function} fn - Callback function.
     * @param {HTMLElement|Document} root - The persistent parent listener.
     */
    delegate(className, event, fn, root = document) {
        root.addEventListener(event, function (e) {
            const el = e.target.closest('.' + className);
            if (!el) return;
            fn.call(el, e);
        })
    },

    /**
     * Registers functions to be re-executed after an HTMX swap operation.
     * @remarks 
     * HTMX replaces DOM fragments, often losing event listeners or requiring 
     * re-initialization of components like tooltips or search bars.
     * @param {...Function} fns - Spread of functions to execute.
     */
    executeAfterHtmx(...fns) {
        for (const f of fns) {
            if (typeof f === 'function') document.addEventListener('htmx:afterSwap', f);
        }
    }
};

export { DOM };