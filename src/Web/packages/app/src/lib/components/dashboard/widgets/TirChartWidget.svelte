<script lang="ts">
  import WidgetCard from "./WidgetCard.svelte";
  import { getRealtimeStore } from "$lib/stores/realtime-store.svelte";
  import { getSettingsStore } from "$lib/stores/settings-store.svelte";

  const realtimeStore = getRealtimeStore();
  const settingsStore = getSettingsStore();

  // Get glucose target ranges from settings
  const targetLow = $derived(
    settingsStore.algorithm?.targetRange?.low ?? 70
  );
  const targetHigh = $derived(
    settingsStore.algorithm?.targetRange?.high ?? 180
  );

  // Calculate TIR from last 24h of entries
  const tirData = $derived.by(() => {
    const oneDayAgo = realtimeStore.now - 24 * 60 * 60 * 1000;
    const recentEntries = realtimeStore.entries.filter(
      (e) => (e.mills || 0) > oneDayAgo
    );

    if (recentEntries.length === 0) {
      return null;
    }

    const total = recentEntries.length;
    let severeLow = 0;
    let low = 0;
    let inRange = 0;
    let high = 0;
    let severeHigh = 0;

    for (const entry of recentEntries) {
      const sgv = entry.sgv ?? entry.mgdl ?? 0;
      if (sgv < 54) severeLow++;
      else if (sgv < targetLow) low++;
      else if (sgv <= targetHigh) inRange++;
      else if (sgv <= 250) high++;
      else severeHigh++;
    }

    return {
      severeLow: (severeLow / total) * 100,
      low: (low / total) * 100,
      inRange: (inRange / total) * 100,
      high: (high / total) * 100,
      severeHigh: (severeHigh / total) * 100,
      total,
    };
  });
</script>

<WidgetCard title="Time in Range" subtitle="Last 24h">
  {#if tirData}
    <!-- Stacked horizontal bar -->
    <div class="h-6 flex rounded overflow-hidden mb-2">
      {#if tirData.severeLow > 0}
        <div
          class="h-full"
          style="width: {tirData.severeLow}%; background-color: var(--glucose-very-low);"
          title="Severe Low: {tirData.severeLow.toFixed(1)}%"
        ></div>
      {/if}
      {#if tirData.low > 0}
        <div
          class="h-full"
          style="width: {tirData.low}%; background-color: var(--glucose-low);"
          title="Low: {tirData.low.toFixed(1)}%"
        ></div>
      {/if}
      {#if tirData.inRange > 0}
        <div
          class="h-full"
          style="width: {tirData.inRange}%; background-color: var(--glucose-in-range);"
          title="In Range: {tirData.inRange.toFixed(1)}%"
        ></div>
      {/if}
      {#if tirData.high > 0}
        <div
          class="h-full"
          style="width: {tirData.high}%; background-color: var(--glucose-high);"
          title="High: {tirData.high.toFixed(1)}%"
        ></div>
      {/if}
      {#if tirData.severeHigh > 0}
        <div
          class="h-full"
          style="width: {tirData.severeHigh}%; background-color: var(--glucose-very-high);"
          title="Severe High: {tirData.severeHigh.toFixed(1)}%"
        ></div>
      {/if}
    </div>

    <!-- TIR percentage prominently displayed -->
    <div class="flex items-baseline justify-between">
      <span class="text-2xl font-bold" style="color: var(--glucose-in-range);">
        {tirData.inRange.toFixed(0)}%
      </span>
      <span class="text-xs text-muted-foreground">
        {tirData.total} readings
      </span>
    </div>

    <!-- Low/High summary -->
    <div class="flex justify-between text-xs text-muted-foreground mt-1">
      <span style="color: var(--glucose-low);">
        ↓ {(tirData.severeLow + tirData.low).toFixed(0)}%
      </span>
      <span style="color: var(--glucose-high);">
        ↑ {(tirData.high + tirData.severeHigh).toFixed(0)}%
      </span>
    </div>
  {:else}
    <div class="flex flex-col items-center justify-center text-muted-foreground py-4">
      <p class="text-xs">No data available</p>
    </div>
  {/if}
</WidgetCard>
