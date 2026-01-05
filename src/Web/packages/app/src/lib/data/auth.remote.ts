/**
 * Remote functions for authentication (logout, session management)
 */
import { getRequestEvent, command } from '$app/server';
import { error } from '@sveltejs/kit';
import type { LogoutResponse } from '$api';

/**
 * Logout the current user session
 * Calls the API to revoke the session and returns any provider logout URL
 */
export const logout = command(async (): Promise<LogoutResponse | null> => {
	const { locals } = getRequestEvent();
	const { apiClient } = locals;

	try {
		return await apiClient.oidc.logout();
	} catch (err) {
		console.error('Error during logout:', err);
		throw error(500, 'Failed to logout');
	}
});
