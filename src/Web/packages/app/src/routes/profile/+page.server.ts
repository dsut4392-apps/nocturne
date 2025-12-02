import type { PageServerLoad } from "./$types";
import type { Profile } from "$lib/api";
import { error } from "@sveltejs/kit";

export const load: PageServerLoad = async ({ locals }) => {
  try {
    // Fetch all profiles using the V3 API
    const response = await locals.apiClient.profile.getProfiles();
    const profiles = (response.data ?? []) as Profile[];

    // Sort profiles by mills (timestamp) descending to get most recent first
    const sortedProfiles = [...profiles].sort((a, b) => {
      const millsA = a.mills ?? 0;
      const millsB = b.mills ?? 0;
      return millsB - millsA;
    });

    // Get the current/active profile (most recent)
    const currentProfile = sortedProfiles.length > 0 ? sortedProfiles[0] : null;

    return {
      profiles: sortedProfiles,
      currentProfile,
      totalProfiles: profiles.length,
    };
  } catch (err) {
    console.error("Error loading profiles:", err);
    throw error(500, "Failed to load profiles");
  }
};
