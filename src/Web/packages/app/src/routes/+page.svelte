<script lang="ts">
  import {
    CurrentBGDisplay,
    BGStatisticsCards,
    GlucoseChartCard,
    RecentEntriesCard,
    RecentTreatmentsCard,
    WidgetGrid,
  } from "$lib/components/dashboard";
  import { getRealtimeStore } from "$lib/stores/realtime-store.svelte";
  import { getSettingsStore } from "$lib/stores/settings-store.svelte";
  import {
    DEFAULT_TOP_WIDGETS,
    type WidgetId,
  } from "$lib/types/dashboard-widgets";

  const realtimeStore = getRealtimeStore();
  const settingsStore = getSettingsStore();

  // Dashboard widget visibility settings with defaults (show all if not configured)
  const widgets = $derived({
    glucoseChart:
      settingsStore.features?.dashboardWidgets?.glucoseChart ?? true,
    statistics: settingsStore.features?.dashboardWidgets?.statistics ?? true,
    treatments: settingsStore.features?.dashboardWidgets?.treatments ?? true,
    predictions: settingsStore.features?.dashboardWidgets?.predictions ?? true,
    dailyStats: settingsStore.features?.dashboardWidgets?.dailyStats ?? true,
  });

  // Get the configured top widgets or use defaults
  const dashboardWidgets = $derived<WidgetId[]>(
    (settingsStore.features?.dashboardWidgets?.dashboardWidgets as
      | WidgetId[]
      | undefined) ?? [...DEFAULT_TOP_WIDGETS]
  );

  // Get focusHours setting for chart default time range
  const focusHours = $derived(settingsStore.features?.display?.focusHours ?? 3);

  // Algorithm prediction settings - controls whether predictions are calculated
  const predictionEnabled = $derived(
    settingsStore.algorithm?.prediction?.enabled ?? true
  );
</script>

<div class="p-6 space-y-6">
  <CurrentBGDisplay />

  {#if widgets.statistics}
    <WidgetGrid widgets={dashboardWidgets} maxWidgets={3} />
  {/if}

  {#if widgets.glucoseChart}
    <GlucoseChartCard
      entries={realtimeStore.entries}
      treatments={realtimeStore.treatments}
      showPredictions={widgets.predictions && predictionEnabled}
      defaultFocusHours={focusHours}
    />
  {/if}

  {#if widgets.dailyStats}
    <RecentEntriesCard />
  {/if}

  {#if widgets.treatments}
    <RecentTreatmentsCard />
  {/if}
</div>
