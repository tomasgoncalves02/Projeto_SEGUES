/*
 * DOM helper functions
 */
const DOM = {
    byId(id, root = document) {
        return root.getElementById(id);
    },
    byClass(className, root = document) {
        return Array.from(root.getElementsByClassName(className));
    },
    byTag(tagName, root = document) {
        return Array.from(root.getElementsByTagName(tagName));
    },
    byName(name, root = document) {
        return root.getElementsByName(name);
    },
    bySelector(selector, root) {
        if (!root) root = document;
        return root.querySelector(selector);
    },
    bySelectorAll(selector, root) {
        if (!root) root = document;
        return root.querySelectorAll(selector);
    },
    bindDocumentLoad(fn) {
        document.addEventListener('DOMContentLoaded', fn);
    },
    // Binds an event listener to an element with the given id
    bind(id, event, fn, executeImmediately = false) {
        const el = this.byId(id);
        if (!el) return;
        el.addEventListener(event, fn);
        if (executeImmediately) fn();
    },
    bindAll(className, event, fn) {
        const elems = this.byClass(className);
        elems.forEach(el => el.addEventListener(event, fn));
    },
    // Delegates an event listener to an element with the given class.
    // Like bindAll but works also for future elements added to the DOM by HTMX
    delegate(className, event, fn, root = document) {
        root.addEventListener(event, function(e) {
            const el = e.target.closest('.' + className);
            if (!el) return;
            fn.call(el, e);
        })
    }
};

export { DOM };