<script lang="ts">
  import {
    CurrentBGDisplay,
    BGStatisticsCards,
    GlucoseChartCard,
    RecentEntriesCard,
    RecentTreatmentsCard,
    BatteryStatusCard,
  } from "$lib/components/dashboard";
  import { getRealtimeStore } from "$lib/stores/realtime-store.svelte";

  const realtimeStore = getRealtimeStore();
</script>

<div class="p-6 space-y-6">
  <CurrentBGDisplay />

  <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
    <div class="md:col-span-3">
      <BGStatisticsCards />
    </div>
    <BatteryStatusCard />
  </div>

  <GlucoseChartCard
    entries={realtimeStore.entries}
    treatments={realtimeStore.treatments}
  />

  <RecentEntriesCard />

  <RecentTreatmentsCard />
</div>
