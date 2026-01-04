<script lang="ts">
  import type { WidgetId } from "$lib/types/dashboard-widgets";
  import { DEFAULT_TOP_WIDGETS } from "$lib/types/dashboard-widgets";
  import BgDeltaWidget from "./widgets/BgDeltaWidget.svelte";
  import LastUpdatedWidget from "./widgets/LastUpdatedWidget.svelte";
  import ConnectionStatusWidget from "./widgets/ConnectionStatusWidget.svelte";
  import MealsWidget from "./widgets/MealsWidget.svelte";
  import TrackersWidget from "./widgets/TrackersWidget.svelte";
  import TirChartWidget from "./widgets/TirChartWidget.svelte";
  import DailySummaryWidget from "./widgets/DailySummaryWidget.svelte";

  interface Props {
    /** Ordered list of widget IDs to display */
    widgets?: WidgetId[];
    /** Maximum number of widgets to show (default 3) */
    maxWidgets?: number;
  }

  let { widgets = DEFAULT_TOP_WIDGETS, maxWidgets = 3 }: Props = $props();

  // Limit to max widgets
  const displayWidgets = $derived(widgets.slice(0, maxWidgets));

  // Widget component map
  const widgetComponents: Record<WidgetId, typeof BgDeltaWidget> = {
    "bg-delta": BgDeltaWidget,
    "last-updated": LastUpdatedWidget,
    "connection-status": ConnectionStatusWidget,
    meals: MealsWidget,
    trackers: TrackersWidget,
    "tir-chart": TirChartWidget,
    "daily-summary": DailySummaryWidget,
  };
</script>

<div class="grid grid-cols-1 md:grid-cols-3 gap-4">
  {#each displayWidgets as widgetId (widgetId)}
    {@const WidgetComponent = widgetComponents[widgetId]}
    {#if WidgetComponent}
      <WidgetComponent />
    {/if}
  {/each}
</div>
