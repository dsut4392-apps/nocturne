import { getRequestEvent, query } from "$app/server";
import type { Profile } from "$lib/api";
import { z } from "zod";

/**
 * Get all profiles from the backend
 */
export const getProfiles = query(z.object({}), async () => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  const response = await apiClient.profile.getProfiles();

  // The V3 API wraps the data in a data array
  const profiles = (response.data ?? []) as Profile[];
  return profiles;
});

/**
 * Get a specific profile by ID
 */
export const getProfileById = query(
  z.object({
    id: z.string(),
  }),
  async (props) => {
    const { locals } = getRequestEvent();
    const { apiClient } = locals;

    const profile = await apiClient.profile.getProfileById(props.id);
    return profile;
  }
);

/**
 * Get the active/current profile
 * Returns the most recently created profile or the one with the default profile set
 */
export const getCurrentProfile = query(z.object({}), async () => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  const response = await apiClient.profile.getProfiles();
  const profiles = (response.data ?? []) as Profile[];

  if (profiles.length === 0) {
    return null;
  }

  // Sort by mills (timestamp) descending to get the most recent
  const sortedProfiles = [...profiles].sort((a, b) => {
    const millsA = a.mills ?? 0;
    const millsB = b.mills ?? 0;
    return millsB - millsA;
  });

  return sortedProfiles[0];
});

/**
 * Profile store names (basal, carbratio, sens, target) for displaying
 */
export type ProfileStoreName = keyof NonNullable<Profile["store"]>;

/**
 * Get profile store names from a profile
 */
export function getProfileStoreNames(profile: Profile): string[] {
  if (!profile.store) return [];
  return Object.keys(profile.store).map(String);
}

/**
 * Helper to format a time value entry for display
 */
export function formatTimeValue(time: string | undefined, value: number | undefined): string {
  if (!time || value === undefined) return "–";
  return `${time}: ${value}`;
}

/**
 * Convert profile time values to chart-friendly format
 */
export function timeValuesToChartData(
  timeValues: Array<{ time?: string; value?: number }> | undefined,
  label: string
): Array<{ time: string; value: number; label: string }> {
  if (!timeValues) return [];

  return timeValues
    .filter((tv) => tv.time !== undefined && tv.value !== undefined)
    .map((tv) => ({
      time: tv.time!,
      value: tv.value!,
      label,
    }));
}
