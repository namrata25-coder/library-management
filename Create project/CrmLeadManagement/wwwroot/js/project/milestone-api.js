/**
 * Milestone API Client Module
 * Dedicated service for /api/milestones endpoints
 */
const MilestoneApi = (() => {
    const BASE_URL = '/api/milestones';

    /**
     * Fetch all milestones with optional query filters
     * @param {Object} [filters] - { projectId, status, search }
     * @returns {Promise<Array>} List of MilestoneResponseDto objects
     */
    async function getAll(filters = {}) {
        const params = new URLSearchParams();
        if (filters.projectId) params.append('projectId', filters.projectId);
        if (filters.status) params.append('status', filters.status);
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
            console.error('[MilestoneApi.getAll] Error:', error);
            throw error;
        }
    }

    /**
     * Fetch a milestone by ID
     * @param {number} id - Milestone ID
     * @returns {Promise<Object>} MilestoneResponseDto
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
            console.error(`[MilestoneApi.getById] ID ${id} Error:`, error);
            throw error;
        }
    }

    /**
     * Create a new milestone
     * @param {Object} payload - MilestoneDto
     * @returns {Promise<Object>} Created milestone
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
            console.error('[MilestoneApi.create] Error:', error);
            throw error;
        }
    }

    /**
     * Update an existing milestone
     * @param {number} id - Milestone ID
     * @param {Object} payload - MilestoneDto
     * @returns {Promise<Object>} Updated milestone
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
            console.error(`[MilestoneApi.update] ID ${id} Error:`, error);
            throw error;
        }
    }

    /**
     * Delete a milestone by ID
     * @param {number} id - Milestone ID
     * @returns {Promise<Object>} Response confirmation message
     */
    async function deleteMilestone(id) {
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
            console.error(`[MilestoneApi.delete] ID ${id} Error:`, error);
            throw error;
        }
    }

    return {
        getAll,
        getById,
        create,
        update,
        delete: deleteMilestone
    };
})();
