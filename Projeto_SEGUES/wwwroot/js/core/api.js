/*
 * API helper functions.
 * Fetch and Send data logic with anti-forgery token.
 */
import {DOM} from "./dom.js";

function getToken() {
    return DOM.byName('__RequestVerificationToken')[0]?.value || '';
}

function buildQuery(url, params) {
    if (!params || Object.keys(params).length === 0) return url;

    const query = new URLSearchParams(params).toString();
    return `${url}${url.includes('?') ? '&' : '?'}${query}`;
}

async function request(method, url, params = {}) {
    try {
        const headers = {
            'Content-Type': 'application/x-www-form-urlencoded'
        };
        if (method !== 'GET') {
            headers['RequestVerificationToken'] = this.getToken();
        }
        const options = { method, headers };

        if (method === 'GET') {
            url = this.buildQuery(url, params);
        } else if (params && Object.keys(params).length > 0) {
            options.body = new URLSearchParams(params);
        }

        const response = await fetch(url, options);

        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return await response.json();
    }
    catch (error) {
        console.error(`Api Error [${method} ${url}]:`, error);
        throw error;
    }
}

const Api = {
    async get(url, params = {}) {
        return await this.request('GET', url, params);
    },
    async post(url, params = {}) {
        return await this.request('POST', url, params);
    },
    async put(url, params = {}) {
        return await this.request('PUT', url, params);
    },
    async patch(url, params = {}) {
        return await this.request('PATCH', url, params);
    },
    async delete(url, params = {}) {
        return await this.request('DELETE', url, params);
    }
};

export {Api};