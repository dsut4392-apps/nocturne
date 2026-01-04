<script lang="ts">
  import WidgetCard from "./WidgetCard.svelte";
  import { getRealtimeStore } from "$lib/stores/realtime-store.svelte";
  import { glucoseUnits } from "$lib/stores/appearance-store.svelte";
  import { formatGlucoseDelta, getUnitLabel } from "$lib/utils/formatting";

  interface Props {
    /** Override bgDelta from props instead of realtime store */
    bgDelta?: number;
  }

  let { bgDelta }: Props = $props();

  const realtimeStore = getRealtimeStore();

  // Use realtime store values as fallback when props not provided
  const rawBgDelta = $derived(bgDelta ?? realtimeStore.bgDelta);
  const units = $derived(glucoseUnits.current);
  const displayBgDelta = $derived(formatGlucoseDelta(rawBgDelta, units));
  const unitLabel = $derived(getUnitLabel(units));
</script>

<WidgetCard title="BG Delta">
  <div class="text-2xl font-bold">
    {displayBgDelta}
  </div>
  <p class="text-xs text-muted-foreground">{unitLabel}</p>
</WidgetCard>
