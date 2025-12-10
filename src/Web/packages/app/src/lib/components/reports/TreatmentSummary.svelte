<script lang="ts">
  import { Syringe, Apple, Utensils } from "lucide-svelte";
  import {
    formatInsulinDisplay,
    formatCarbDisplay,
  } from "$lib/utils/formatting";
  import type { DayToDayDailyData } from "./types";
  import type { TreatmentSummary as TreatmentSummaryType } from "$lib/api";

  interface Props {
    dailyDataPoints: DayToDayDailyData[];
  }

  let { dailyDataPoints }: Props = $props();

  // Helper functions to access nested TreatmentSummary properties
  function getTotalInsulin(summary: TreatmentSummaryType | undefined): number {
    return (summary?.totals?.insulin?.bolus ?? 0) + (summary?.totals?.insulin?.basal ?? 0);
  }

  function getTotalCarbs(summary: TreatmentSummaryType | undefined): number {
    return summary?.totals?.food?.carbs ?? 0;
  }

  function getTotalProtein(summary: TreatmentSummaryType | undefined): number {
    return summary?.totals?.food?.protein ?? 0;
  }

  function getTotalFat(summary: TreatmentSummaryType | undefined): number {
    return summary?.totals?.food?.fat ?? 0;
  }
</script>

<div class="bg-white shadow-lg rounded-lg p-4 md:p-6 mb-6">
  <h2 class="text-xl font-semibold text-gray-700 mb-4">
    Overall Treatment Summary
  </h2>
  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    {#each dailyDataPoints as day}
      {@const totalInsulin = getTotalInsulin(day.treatmentSummary)}
      {@const totalCarbs = getTotalCarbs(day.treatmentSummary)}
      {@const totalProtein = getTotalProtein(day.treatmentSummary)}
      {@const totalFat = getTotalFat(day.treatmentSummary)}
      {#if day.treatmentSummary && (totalInsulin > 0 || totalCarbs > 0)}
        <div class="bg-gray-50 rounded-lg p-4">
          <h3 class="text-lg font-semibold text-gray-700 mb-3">
            {new Date(day.date).toLocaleDateString(undefined, {
              month: "short",
              day: "numeric",
            })}
          </h3>
          <div class="space-y-2 text-sm">
            {#if totalInsulin > 0}
              <div class="flex items-center gap-2">
                <Syringe class="w-4 h-4 text-blue-600" />
                <span>
                  Insulin: {formatInsulinDisplay(totalInsulin)}U
                </span>
              </div>
            {/if}
            {#if totalCarbs > 0}
              <div class="flex items-center gap-2">
                <Apple class="w-4 h-4 text-orange-600" />
                <span>
                  Carbs: {formatCarbDisplay(totalCarbs)}g
                </span>
              </div>
            {/if}
            {#if totalProtein > 0}
              <div class="flex items-center gap-2">
                <Utensils class="w-4 h-4 text-green-600" />
                <span>
                  Protein: {formatCarbDisplay(totalProtein)}g
                </span>
              </div>
            {/if}
            {#if totalFat > 0}
              <div class="flex items-center gap-2">
                <Utensils class="w-4 h-4 text-yellow-600" />
                <span>
                  Fat: {formatCarbDisplay(totalFat)}g
                </span>
              </div>
            {/if}
            <div class="text-xs text-gray-500 mt-2">
              {day.treatmentSummary.treatmentCount ?? 0} treatment events
            </div>
          </div>
        </div>
      {/if}
    {/each}
  </div>
</div>
