import type { Treatment } from "$lib/api";

/**
 * Identify anomalous treatments based on configurable thresholds
 */
export function identifyAnomalies(
  treatments: Treatment[],
  options: {
    highInsulinThreshold?: number;
    highCarbThreshold?: number;
    unusualTimeHours?: number[];
  } = {}
): Treatment[] {
  const {
    highInsulinThreshold = 15, // Units of insulin considered high
    highCarbThreshold = 100, // Grams of carbs considered high
    unusualTimeHours = [0, 1, 2, 3, 4, 5], // Hours considered unusual for boluses
  } = options;

  return treatments.filter((t) => {
    // High insulin bolus
    if (t.insulin && t.insulin > highInsulinThreshold) return true;

    // High carb entry
    if (t.carbs && t.carbs > highCarbThreshold) return true;

    // Unusual timing for boluses
    if (t.insulin && t.created_at) {
      const hour = new Date(t.created_at).getHours();
      if (unusualTimeHours.includes(hour)) return true;
    }

    return false;
  });
}

/**
 * Group treatments by time period for analysis
 */
export function groupTreatmentsByPeriod(
  treatments: Treatment[],
  period: "hour" | "day" | "week" | "month"
): Map<string, Treatment[]> {
  const groups = new Map<string, Treatment[]>();

  for (const treatment of treatments) {
    if (!treatment.created_at) continue;

    const date = new Date(treatment.created_at);
    let key: string;

    switch (period) {
      case "hour":
        key = date.toISOString().slice(0, 13); // YYYY-MM-DDTHH
        break;
      case "day":
        key = date.toISOString().slice(0, 10); // YYYY-MM-DD
        break;
      case "week":
        const weekStart = new Date(date);
        weekStart.setDate(date.getDate() - date.getDay());
        key = weekStart.toISOString().slice(0, 10);
        break;
      case "month":
        key = date.toISOString().slice(0, 7); // YYYY-MM
        break;
    }

    if (!groups.has(key)) {
      groups.set(key, []);
    }
    groups.get(key)!.push(treatment);
  }

  return groups;
}
