<script>
  import {
    CurrentBGDisplay,
    BGStatisticsCards,
    GlucoseChartCard,
    RecentTreatmentsCard,
  } from "$lib/components/dashboard";
  import RecentEntriesCard from "$lib/components/dashboard/RecentEntriesCard.svelte";
</script>

<div class="p-6 space-y-6">
  <CurrentBGDisplay />

  <BGStatisticsCards />

  <GlucoseChartCard />

  <RecentEntriesCard />

  <RecentTreatmentsCard />
</div>
