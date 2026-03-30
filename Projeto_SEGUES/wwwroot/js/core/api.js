/*
 * API helper functions.
 * Centralized logic for Fetch and Send data operations with integrated security.
 */
import { DOM, Notifications } from "./core.js";

/**
 * Retrieves the Anti-Forgery Token (CSRF) from the hidden input field.
 * @returns {string} The validation token required for non-GET requests in ASP.NET Core.
 */
function getToken() {
    return DOM.byName('__RequestVerificationToken')[0]?.value || '';
}

/**
 * Appends query parameters to a given URL.
 * @param {string} url - The base endpoint URL.
 * @param {Object} params - Key-value pairs to be converted into query strings.
 * @returns {string} The complete URL with correctly formatted query parameters.
 */
function buildQuery(url, params) {
    if (!params || Object.keys(params).length === 0) return url;

    const query = new URLSearchParams(params).toString();
    return `${url}${url.includes('?') ? '&' : '?'}${query}`;
}

/**
 * Core request handler for the application's API calls.
 * @param {string} method - HTTP Verb (GET, POST, PUT, PATCH, DELETE).
 * @param {string} url - Target endpoint.
 * @param {Object} params - Data payload or query parameters.
 * @returns {Promise<Object|null>} The JSON response from the server or null on failure.
 */
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

        // Error Handling & Session Management
        if (!response.ok) {
            // Handle unauthorized access by redirecting to login
            if (response.status === 401) {
                Notifications.error("Sessão inválida. Redirecionando para a página de login...");
                window.location.href = '/Identity/Account/Login';
                return null;
            }
        }

        /* * Response Mapping Logic:
         * Ok (200) returns the result object.
         * NotFound (404) returns the .message property.
         * BadRequest (400) returns the .error property.
         */
        return await response.json();
    }
    catch (error) {
        Notifications.error(`Erro na comunicação com o servidor: [${method} ${url}] ` + error.message);
        return null;
    }
}

/**
 * Exported API object providing semantic methods for HTTP requests.
 */
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