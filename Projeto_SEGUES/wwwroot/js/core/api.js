/*
 * API helper functions.
 * Fetch and Send data logic with anti-forgery token.
 */
import { DOM, Notifications } from "./core.js";

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
            headers['RequestVerificationToken'] = getToken();
        }
        const options = { method, headers };

        if (method === 'GET') {
            url = buildQuery(url, params);
        } else if (params && Object.keys(params).length > 0) {
            options.body = new URLSearchParams(params);
        }

        const response = await fetch(url, options);
        if (!response.ok) {
            // Handle unauthorized access by redirecting to login
            if (response.status === 401) {
                Notifications.error("Sessão inválida. Redirecionando para a página de login...");
                window.location.href = '/Identity/Account/Login';
                return null;
            }
        }
        // Ok returns the result object requested
        // NotFound returns .message
        // BadRequest returns .error
        return await response.json();
    }
    catch (error) {
        Notifications.error(`Erro na comunicação com o servidor: [${method} ${url}] ` + error.message);
        return null;
    }
}

const Api = {
    async get(url, params = {}) {
        return await request('GET', url, params);
    },
    async post(url, params = {}) {
        return await request('POST', url, params);
    },
    async put(url, params = {}) {
        return await request('PUT', url, params);
    },
    async patch(url, params = {}) {
        return await request('PATCH', url, params);
    },
    async delete(url, params = {}) {
        return await request('DELETE', url, params);
    }
};

export { Api };