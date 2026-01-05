/**
 * Remote functions for metadata (widget definitions, etc.)
 */
import { getRequestEvent, query } from "$app/server";
import { error } from "@sveltejs/kit";
import type {
  WidgetDefinitionsMetadata,
  WidgetDefinition,
} from "$lib/api/generated/nocturne-api-client";

/**
 * Fetch widget definitions from the backend.
 * This is the single source of truth for widget metadata.
 */
export const fetchWidgetDefinitions = query(async (): Promise<WidgetDefinition[]> => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  try {
    const metadata = await apiClient.metadata.getWidgetDefinitions();
    return metadata.definitions ?? [];
  } catch (err) {
    console.error("Error fetching widget definitions:", err);
    throw error(500, "Failed to fetch widget definitions");
  }
});

/**
 * Fetch full widget definitions metadata from the backend.
 */
export const fetchWidgetDefinitionsMetadata = query(async (): Promise<WidgetDefinitionsMetadata> => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  try {
    return await apiClient.metadata.getWidgetDefinitions();
  } catch (err) {
    console.error("Error fetching widget definitions metadata:", err);
    throw error(500, "Failed to fetch widget definitions metadata");
  }
});
