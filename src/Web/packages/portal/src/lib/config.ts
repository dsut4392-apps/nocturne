/**
 * Portal Configuration
 *
 * Environment variables for the portal, including demo instance settings.
 * Uses Vite's import.meta.env for public environment variables.
 */

/**
 * Whether the demo instance is enabled
 */
export const DEMO_ENABLED = import.meta.env.VITE_DEMO_ENABLED === "true";

/**
 * URL to the demo Nocturne API (includes /scalar for API docs)
 */
export const DEMO_API_URL = import.meta.env.VITE_DEMO_API_URL || "";

/**
 * URL to the demo Nocturne Web application
 */
export const DEMO_WEB_URL = import.meta.env.VITE_DEMO_WEB_URL || "";

/**
 * Get the Scalar API documentation URL for the demo instance
 */
export function getDemoScalarUrl(): string {
  if (!DEMO_API_URL) return "";
  return `${DEMO_API_URL}/scalar`;
}
