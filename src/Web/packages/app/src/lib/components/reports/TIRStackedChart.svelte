<script lang="ts">
  import {
    DEFAULT_CONFIG,
    type ExtendedAnalysisConfig,
  } from "$lib/utils/glucose-analytics.ts";
  import { BarChart, Tooltip } from "layerchart";
  import { calculateTimeInRange as calculateTimeInRangeRemote } from "$lib/data/statistics.remote.ts";
  import type { Entry } from "$lib/api";
  let {
    entries,
    config,
  }: { entries: Entry[]; config?: ExtendedAnalysisConfig } = $props();

  const mergedConfig = {
    ...DEFAULT_CONFIG,
    ...config,
  };

  const timeInRangePromise = $derived(
    calculateTimeInRangeRemote({
      entries,
      config: mergedConfig,
    })
  );

  const timeInRange = $derived.by(() => {
    // Extract percentages from the backend response
    if (!timeInRangePromise) return { percentages: {} };

    return {
      percentages: {
        severeLow: timeInRangePromise.rangeStats?.severeLow?.percentage ?? 0,
        low: timeInRangePromise.rangeStats?.low?.percentage ?? 0,
        target: timeInRangePromise.rangeStats?.target?.percentage ?? 0,
        high: timeInRangePromise.rangeStats?.high?.percentage ?? 0,
        severeHigh: timeInRangePromise.rangeStats?.severeHigh?.percentage ?? 0,
      },
    };
  });

  const chartConfig = {
    severeLow: {
      key: "severeLow",
      label: "Severe Low",
      color: "var(--glucose-very-low)",
    },
    low: {
      key: "low",
      label: "Low",
      color: "var(--glucose-low)",
    },
    target: {
      key: "target",
      label: "Target",
      color: "var(--glucose-in-range)",
    },
    high: {
      key: "high",
      label: "High",
      color: "var(--glucose-high)",
    },
    severeHigh: {
      key: "severeHigh",
      label: "Severe High",
      color: "var(--glucose-very-high)",
    },
  };
</script>

<BarChart
  data={[timeInRange.percentages]}
  yDomain={[0, 100]}
  x={() => 1}
  yBaseline={undefined}
  yNice={false}
  series={Object.values(chartConfig).map((c) => ({
    key: c.key,
    color: c.color,
    label: c.label,
  }))}
  legend
  seriesLayout="stack"
  padding={{ top: 0, bottom: 24 }}
  props={{
    bars: {
      motion: { type: "tween", duration: 200 },
      strokeWidth: 0,
    },

    tooltip: {
      hideTotal: true,
      context: { mode: "bounds" },
      // header: { format: "none" },
    },
    xAxis: {
      hidden: true,
    },
    yAxis: {
      hidden: true,
    },
  }}
>
  {#snippet tooltip({ context: _ })}
    <Tooltip.Root>
      {#snippet children({ data })}
        <Tooltip.List>
          <Tooltip.Item label="Label:" value={data.label} />
          <!-- <Tooltip.Item label="Range:" value="{data.start} - {data.end}" /> -->
        </Tooltip.List>
      {/snippet}
    </Tooltip.Root>
  {/snippet}
</BarChart>
