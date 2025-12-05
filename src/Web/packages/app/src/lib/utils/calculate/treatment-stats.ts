import type { TreatmentSummary as ApiTreatmentSummary, OverallAverages as ApiOverallAverages } from "$lib/api";

/**
 * Formats an insulin value for display.
 * @param insulin The insulin value.
 * @returns The formatted insulin string.
 */
export function formatInsulinDisplay(insulin: number | undefined): string {
  if (insulin === undefined || insulin === null) {
    return "N/A";
  }
  return insulin.toFixed(2);
}

/**
 * Formats a carb value for display.
 * @param carbs The carb value.
 * @returns The formatted carb string.
 */
export function formatCarbDisplay(carbs: number | undefined): string {
  if (carbs === undefined || carbs === null) {
    return "N/A";
  }
  return carbs.toFixed(0);
}

/**
 * Formats a percentage value for display.
 * @param value The percentage value.
 * @returns The formatted percentage string.
 */
export function formatPercentageDisplay(value: number | undefined): string {
  if (value === undefined || value === null) {
    return "N/A";
  }
  return value.toFixed(1);
}

/**
 * Get total insulin (bolus + basal) from a TreatmentSummary.
 * This is a simple helper that reads from the backend-calculated summary.
 * @param summary The treatment summary from the backend.
 * @returns The total insulin value.
 */
export function getTotalInsulin(summary: ApiTreatmentSummary | undefined | null): number {
  if (!summary?.totals?.insulin) {
    return 0;
  }
  return (summary.totals.insulin.bolus ?? 0) + (summary.totals.insulin.basal ?? 0);
}

export type TreatmentSummary = ApiTreatmentSummary;
export type OverallAverages = ApiOverallAverages;
