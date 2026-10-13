/**
 * Project API Client Module
 * Dedicated service for /api/projects endpoints
 */
const ProjectApi = (() => {
    const BASE_URL = '/api/projects';

    /**
     * Fetch all projects with optional query filters
     * @param {Object} [filters] - { status, projectType, search }
     * @returns {Promise<Array>} List of WorkProject objects
     */
    async function getAll(filters = {}) {
        const params = new URLSearchParams();
        if (filters.status) params.append('status', filters.status);
        if (filters.projectType) params.append('projectType', filters.projectType);
        if (filters.search) params.append('search', filters.search);

        const qs = params.toString();
        const url = qs ? `${BASE_URL}?${qs}` : BASE_URL;

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) {
                const errText = await response.text();
                throw new Error(`GET ${url} failed [${response.status}]: ${errText || response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('[ProjectApi.getAll] Error:', error);
            throw error;
        }
    }

    /**
     * Fetch single project by ID with all relations
     * @param {number} id - Project ID
     * @returns {Promise<Object>} WorkProject object
     */
    async function getById(id) {
        const url = `${BASE_URL}/${id}`;
        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) {
                const errText = await response.text();
                throw new Error(`GET ${url} failed [${response.status}]: ${errText || response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error(`[ProjectApi.getById] ID ${id} Error:`, error);
            throw error;
        }
    }

    /**
     * Create a new project
     * @param {Object} payload - ProjectDto
     * @returns {Promise<Object>} Created WorkProject object
     */
    async function create(payload) {
        const url = BASE_URL;
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(payload)
            });
            if (!response.ok) {
                const errText = await response.text();
                throw new Error(`POST ${url} failed [${response.status}]: ${errText || response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('[ProjectApi.create] Error:', error);
            throw error;
        }
    }

    /**
     * Update an existing project
     * @param {number} id - Project ID
     * @param {Object} payload - ProjectDto
     * @returns {Promise<Object>} Updated WorkProject object
     */
    async function update(id, payload) {
        const url = `${BASE_URL}/${id}`;
        try {
            const response = await fetch(url, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(payload)
            });
            if (!response.ok) {
                const errText = await response.text();
                throw new Error(`PUT ${url} failed [${response.status}]: ${errText || response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error(`[ProjectApi.update] ID ${id} Error:`, error);
            throw error;
        }
    }

    /**
     * Delete a project by ID
     * @param {number} id - Project ID
     * @returns {Promise<Object>} Response confirmation message
     */
    async function deleteProject(id) {
        const url = `${BASE_URL}/${id}`;
        try {
            const response = await fetch(url, {
                method: 'DELETE',
                headers: { 'Accept': 'application/json' }
            });
            if (!response.ok) {
                const errText = await response.text();
                throw new Error(`DELETE ${url} failed [${response.status}]: ${errText || response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error(`[ProjectApi.delete] ID ${id} Error:`, error);
            throw error;
        }
    }

    return {
        getAll,
        getById,
        create,
        update,
        delete: deleteProject
    };
})();
