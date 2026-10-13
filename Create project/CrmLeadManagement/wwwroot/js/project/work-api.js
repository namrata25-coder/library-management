/**
 * Work API Client Module
 * Dedicated service for /api/work endpoints
 */
const WorkApi = (() => {
    const BASE_URL = '/api/work';

    /**
     * Fetch high-level statistics and metrics for the Work module
     * @returns {Promise<Object>} WorkSummaryDto
     */
    async function getSummary() {
        const url = `${BASE_URL}/summary`;
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
            console.error('[WorkApi.getSummary] Error:', error);
            throw error;
        }
    }

    /**
     * Fetch aggregated report data across projects
     * @param {number|null} projectId - Optional filter by project ID
     * @returns {Promise<Array>} List of project reports
     */
    async function getReports(projectId = null) {
        const url = projectId ? `${BASE_URL}/reports?projectId=${encodeURIComponent(projectId)}` : `${BASE_URL}/reports`;
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
            console.error('[WorkApi.getReports] Error:', error);
            throw error;
        }
    }

    return {
        getSummary,
        getReports
    };
})();
