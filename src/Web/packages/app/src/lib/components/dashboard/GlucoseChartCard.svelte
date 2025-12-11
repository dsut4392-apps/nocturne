<script lang="ts">
  import type { Entry, Treatment } from "$lib/api";
  import {
    Card,
    CardContent,
    CardHeader,
    CardTitle,
  } from "$lib/components/ui/card";
  import { Badge } from "$lib/components/ui/badge";
  import * as ToggleGroup from "$lib/components/ui/toggle-group";
  import { getRealtimeStore } from "$lib/stores/realtime-store.svelte";
  import {
    Chart,
    Axis,
    Group,
    Polygon,
    Svg,
    Area,
    Spline,
    Rule,
    Points,
    Highlight,
    Text,
    ChartClipPath,
    Layer,
    Tooltip,
  } from "layerchart";
  import { chartConfig } from "$lib/constants";
  import { curveStepAfter, curveMonotoneX } from "d3";
  import { scaleTime, scaleLinear } from "d3-scale";
  import {
    getPredictions,
    type PredictionData,
  } from "$lib/data/predictions.remote";
  import {
    getChartData,
    type DashboardChartData,
  } from "$lib/data/chart-data.remote";
  import { glucoseUnits } from "$lib/stores/appearance-store.svelte";
  import { convertToDisplayUnits } from "$lib/utils/formatting";

  interface ComponentProps {
    entries?: Entry[];
    treatments?: Treatment[];
    demoMode?: boolean;
    dateRange?: {
      from: Date | string;
      to: Date | string;
    };
    /**
     * Default basal rate from profile (U/hr) - fallback if server data
     * unavailable
     */
    defaultBasalRate?: number;
    /** Insulin to carb ratio (g per 1U) */
    carbRatio?: number;
    /** Insulin Sensitivity Factor (mg/dL per unit) */
    isf?: number;
    /**
     * Show prediction lines (controlled by both widget toggle and algorithm
     * setting)
     */
    showPredictions?: boolean;
    /** Default focus hours for time range selector (from settings) */
    defaultFocusHours?: number;
    /** Prediction model from algorithm settings (ar2, linear, iob, cob, uam) */
    predictionModel?: string;
  }

  const realtimeStore = getRealtimeStore();
  let {
    entries = realtimeStore.entries,
    treatments = realtimeStore.treatments,
    demoMode = realtimeStore.demoMode,
    dateRange,
    defaultBasalRate = 1.0,
    carbRatio = 10,
    isf = 50,
    showPredictions = true,
    defaultFocusHours,
    predictionModel = "cone",
  }: ComponentProps = $props();

  // Prediction data state
  let predictionData = $state<PredictionData | null>(null);
  let predictionError = $state<string | null>(null);

  // Server-side chart data (IOB, COB, basal)
  let serverChartData = $state<DashboardChartData | null>(null);

  // Prediction display mode
  type PredictionDisplayMode =
    | "cone"
    | "lines"
    | "main"
    | "iob"
    | "zt"
    | "uam"
    | "cob";
  let predictionMode = $state<PredictionDisplayMode>("cone");

  // Sync prediction mode with algorithm settings model
  $effect(() => {
    const modelToMode: Record<string, PredictionDisplayMode> = {
      ar2: "cone",
      linear: "cone",
      iob: "iob",
      cob: "cob",
      uam: "uam",
      cone: "cone",
      lines: "lines",
    };
    predictionMode = modelToMode[predictionModel] ?? "cone";
  });

  // Suppress unused variable warnings
  void isf;
  void carbRatio;

  // Fetch predictions when enabled
  $effect(() => {
    if (showPredictions) {
      getPredictions({})
        .then((data) => {
          predictionData = data;
          predictionError = null;
        })
        .catch((err) => {
          console.error("Failed to fetch predictions:", err);
          predictionError = err.message;
          predictionData = null;
        });
    }
  });

  // Time range selection (in hours)
  type TimeRangeOption = "2" | "4" | "6" | "12" | "24";

  function getInitialTimeRange(hours?: number): TimeRangeOption {
    const validOptions: TimeRangeOption[] = ["2", "4", "6", "12", "24"];
    const hourStr = String(hours) as TimeRangeOption;
    return validOptions.includes(hourStr) ? hourStr : "6";
  }

  let selectedTimeRange = $state<TimeRangeOption>(
    getInitialTimeRange(defaultFocusHours)
  );

  const timeRangeOptions: { value: TimeRangeOption; label: string }[] = [
    { value: "2", label: "2h" },
    { value: "4", label: "4h" },
    { value: "6", label: "6h" },
    { value: "12", label: "12h" },
    { value: "24", label: "24h" },
  ];

  function normalizeDate(
    date: Date | string | undefined,
    fallback: Date
  ): Date {
    if (!date) return fallback;
    return date instanceof Date ? date : new Date(date);
  }

  const displayDemoMode = $derived(demoMode ?? realtimeStore.demoMode);

  const displayDateRange = $derived({
    from: dateRange
      ? normalizeDate(dateRange.from, new Date())
      : new Date(
          new Date().getTime() - parseInt(selectedTimeRange) * 60 * 60 * 1000
        ),
    to: dateRange ? normalizeDate(dateRange.to, new Date()) : new Date(),
  });

  // Fetch server-side chart data when date range changes
  $effect(() => {
    const startTime = displayDateRange.from.getTime();
    const endTime = displayDateRange.to.getTime();

    getChartData({ startTime, endTime, intervalMinutes: 5 })
      .then((data) => {
        serverChartData = data;
      })
      .catch((err) => {
        console.error("Failed to fetch chart data:", err);
        serverChartData = null;
      });
  });

  // Prediction buffer
  const predictionHours = 4;
  const chartXDomain = $derived({
    from: displayDateRange.from,
    to:
      showPredictions && predictionData
        ? new Date(
            displayDateRange.to.getTime() + predictionHours * 60 * 60 * 1000
          )
        : displayDateRange.to,
  });

  // Filter entries by date range
  const filteredEntries = $derived(
    entries.filter((e) => {
      const entryTime = e.mills ?? 0;
      return (
        entryTime >= displayDateRange.from.getTime() &&
        entryTime <= displayDateRange.to.getTime()
      );
    })
  );

  // Filter treatments by date range
  const filteredTreatments = $derived(
    treatments.filter((t) => {
      const treatmentTime =
        t.mills ?? (t.created_at ? new Date(t.created_at).getTime() : 0);
      return (
        treatmentTime >= displayDateRange.from.getTime() &&
        treatmentTime <= displayDateRange.to.getTime()
      );
    })
  );

  // Bolus treatments
  const bolusTreatments = $derived(
    filteredTreatments.filter(
      (t) =>
        t.insulin &&
        t.insulin > 0 &&
        (t.eventType?.includes("Bolus") ||
          t.eventType === "SMB" ||
          t.eventType === "Correction Bolus" ||
          t.eventType === "Meal Bolus" ||
          t.eventType === "Snack Bolus" ||
          t.eventType === "Bolus Wizard" ||
          t.eventType === "Combo Bolus")
    )
  );

  // Carb treatments
  const carbTreatments = $derived(
    filteredTreatments.filter((t) => t.carbs && t.carbs > 0)
  );

  function getTreatmentTime(t: Treatment): number {
    return t.mills ?? (t.created_at ? new Date(t.created_at).getTime() : 0);
  }

  // Thresholds (convert to display units)
  const units = $derived(glucoseUnits.current);
  const isMMOL = $derived(units === "mmol");
  const lowThreshold = $derived(
    convertToDisplayUnits(chartConfig.low.threshold ?? 55, units)
  );
  const highThreshold = $derived(
    convertToDisplayUnits(chartConfig.high.threshold ?? 180, units)
  );

  // Y domain for glucose (dynamic based on data, unit-aware)
  const glucoseYMin = $derived(isMMOL ? 2.2 : 40);
  const glucoseYMax = $derived.by(() => {
    const maxSgv = Math.max(...filteredEntries.map((e) => e.sgv ?? 0));
    const maxDisplayValue = convertToDisplayUnits(
      Math.min(400, Math.max(280, maxSgv) + 20),
      units
    );
    return maxDisplayValue;
  });

  // Glucose data for chart (convert to display units)
  const glucoseData = $derived(
    filteredEntries
      .filter((e) => e.sgv !== null && e.sgv !== undefined)
      .map((e) => ({
        time: new Date(e.mills ?? 0),
        sgv: convertToDisplayUnits(e.sgv ?? 0, units),
        color: getGlucoseColor(e.sgv ?? 0),
      }))
      .sort((a, b) => a.time.getTime() - b.time.getTime())
  );

  // Prediction curve data
  const predictionCurveData = $derived(
    predictionData?.curves.main.map((p) => ({
      time: new Date(p.timestamp),
      sgv: convertToDisplayUnits(p.value, units),
    })) ?? []
  );

  const iobPredictionData = $derived(
    predictionData?.curves.iobOnly.map((p) => ({
      time: new Date(p.timestamp),
      sgv: convertToDisplayUnits(p.value, units),
    })) ?? []
  );

  const uamPredictionData = $derived(
    predictionData?.curves.uam.map((p) => ({
      time: new Date(p.timestamp),
      sgv: convertToDisplayUnits(p.value, units),
    })) ?? []
  );

  const cobPredictionData = $derived(
    predictionData?.curves.cob.map((p) => ({
      time: new Date(p.timestamp),
      sgv: convertToDisplayUnits(p.value, units),
    })) ?? []
  );

  const zeroTempPredictionData = $derived(
    predictionData?.curves.zeroTemp.map((p) => ({
      time: new Date(p.timestamp),
      sgv: convertToDisplayUnits(p.value, units),
    })) ?? []
  );

  // Prediction cone data
  const predictionConeData = $derived.by(() => {
    if (!predictionData) return [];

    const curves = [
      predictionData.curves.main,
      predictionData.curves.iobOnly,
      predictionData.curves.zeroTemp,
      predictionData.curves.uam,
      predictionData.curves.cob,
    ].filter((c) => c && c.length > 0);

    if (curves.length === 0) return [];

    const primaryCurve = curves[0];
    return primaryCurve.map((point, i) => {
      const valuesAtTime = curves.map((c) => c[i]?.value ?? point.value);
      return {
        time: new Date(point.timestamp),
        min: convertToDisplayUnits(Math.min(...valuesAtTime), units),
        max: convertToDisplayUnits(Math.max(...valuesAtTime), units),
        mid: convertToDisplayUnits(
          (Math.min(...valuesAtTime) + Math.max(...valuesAtTime)) / 2,
          units
        ),
      };
    });
  });

  // Use server-side data for IOB and basal, with fallbacks
  const iobData = $derived(serverChartData?.iobSeries ?? []);
  const basalData = $derived(serverChartData?.basalSeries ?? []);
  const maxIOB = $derived(serverChartData?.maxIob ?? 3);
  const maxBasalRate = $derived(
    serverChartData?.maxBasalRate ?? defaultBasalRate * 2.5
  );
  const effectiveDefaultBasalRate = $derived(
    serverChartData?.defaultBasalRate ?? defaultBasalRate
  );

  function getGlucoseColor(sgv: number): string {
    const low = chartConfig.low.threshold ?? 55;
    const target = chartConfig.target.threshold ?? 80;
    const high = chartConfig.high.threshold ?? 180;
    const severeHigh = chartConfig.severeHigh.threshold ?? 250;

    if (sgv < low) return "var(--glucose-very-low)";
    if (sgv < target) return "var(--glucose-low)";
    if (sgv <= high) return "var(--glucose-in-range)";
    if (sgv <= severeHigh) return "var(--glucose-high)";
    return "var(--glucose-very-high)";
  }

  function formatTime(date: Date): string {
    return date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
  }

  // Bolus marker data
  const bolusMarkerData = $derived(
    bolusTreatments.map((t) => ({
      time: new Date(getTreatmentTime(t)),
      value: glucoseYMax - 10,
      insulin: t.insulin ?? 0,
    }))
  );

  // Carb marker data
  const carbMarkerData = $derived(
    carbTreatments.map((t) => ({
      time: new Date(getTreatmentTime(t)),
      value: glucoseYMin + 10,
      carbs: t.carbs ?? 0,
    }))
  );

  // ===== UNIFIED CHART CONFIGURATION =====
  // Chart layout: Glucose (67%), Basal (15%), IOB (~15%)
  const GLUCOSE_HEIGHT_PERCENT = 0.67; // 67% = 280px of 420px
  const BASAL_HEIGHT_PERCENT = 0.15; // 15% = ~63px
  const GAP_PERCENT = 0.015; // Small gap between panels

  // Unified Y-domain: 0-100 (percentage-based)
  const glucoseYStart = 0;
  const glucoseYEnd = GLUCOSE_HEIGHT_PERCENT * 100;
  const basalYStart = (GLUCOSE_HEIGHT_PERCENT + GAP_PERCENT) * 100;
  const basalYEnd =
    (GLUCOSE_HEIGHT_PERCENT + GAP_PERCENT + BASAL_HEIGHT_PERCENT) * 100;
  const iobYStart =
    (GLUCOSE_HEIGHT_PERCENT + GAP_PERCENT * 2 + BASAL_HEIGHT_PERCENT) * 100;
  const iobYEnd = 100;

  // Map glucose values to unified Y
  function mapGlucoseToY(sgv: number): number {
    const normalized = (sgv - glucoseYMin) / (glucoseYMax - glucoseYMin);
    // Invert: higher glucose = lower Y value in SVG
    return glucoseYEnd - normalized * (glucoseYEnd - glucoseYStart);
  }

  // Map basal values to unified Y
  function mapBasalToY(rate: number): number {
    const normalized = rate / maxBasalRate;
    return basalYEnd - normalized * (basalYEnd - basalYStart);
  }

  // Map IOB values to unified Y
  function mapIobToY(iob: number): number {
    const normalized = iob / maxIOB;
    return iobYEnd - normalized * (iobYEnd - iobYStart);
  }

  // Transformed data for unified chart
  const unifiedGlucoseData = $derived(
    glucoseData.map((d) => ({
      ...d,
      y: mapGlucoseToY(d.sgv),
    }))
  );

  const unifiedBasalData = $derived(
    basalData.map((d) => ({
      time: d.time,
      rate: d.rate,
      y: mapBasalToY(d.rate),
      y0: basalYEnd,
    }))
  );

  const unifiedIobData = $derived(
    iobData.map((d) => ({
      time: d.time,
      iob: d.value,
      y: mapIobToY(d.value),
      y0: iobYEnd,
    }))
  );

  // Unified threshold lines
  const unifiedHighThreshold = $derived(mapGlucoseToY(highThreshold));
  const unifiedLowThreshold = $derived(mapGlucoseToY(lowThreshold));
  const unifiedDefaultBasal = $derived(mapBasalToY(effectiveDefaultBasalRate));

  // Unified prediction data
  const unifiedPredictionConeData = $derived(
    predictionConeData.map((d) => ({
      ...d,
      yMin: mapGlucoseToY(d.min),
      yMax: mapGlucoseToY(d.max),
      yMid: mapGlucoseToY(d.mid),
    }))
  );

  const unifiedPredictionCurveData = $derived(
    predictionCurveData.map((d) => ({
      ...d,
      y: mapGlucoseToY(d.sgv),
    }))
  );

  const unifiedIobPredictionData = $derived(
    iobPredictionData.map((d) => ({
      ...d,
      y: mapGlucoseToY(d.sgv),
    }))
  );

  const unifiedUamPredictionData = $derived(
    uamPredictionData.map((d) => ({
      ...d,
      y: mapGlucoseToY(d.sgv),
    }))
  );

  const unifiedCobPredictionData = $derived(
    cobPredictionData.map((d) => ({
      ...d,
      y: mapGlucoseToY(d.sgv),
    }))
  );

  const unifiedZeroTempPredictionData = $derived(
    zeroTempPredictionData.map((d) => ({
      ...d,
      y: mapGlucoseToY(d.sgv),
    }))
  );

  // Unified bolus/carb marker positions
  const unifiedBolusMarkerData = $derived(
    bolusMarkerData.map((d) => ({
      ...d,
      y: mapGlucoseToY(glucoseYMax - 20),
    }))
  );

  const unifiedCarbMarkerData = $derived(
    carbMarkerData.map((d) => ({
      ...d,
      y: mapGlucoseToY(glucoseYMin + 20),
    }))
  );
</script>

<Card class="bg-slate-950 border-slate-800">
  <CardHeader class="pb-2">
    <div class="flex items-center justify-between flex-wrap gap-2">
      <CardTitle class="flex items-center gap-2 text-slate-100">
        Blood Glucose
        {#if displayDemoMode}
          <Badge
            variant="outline"
            class="text-xs border-slate-700 text-slate-400"
          >
            Demo
          </Badge>
        {/if}
      </CardTitle>

      <div class="flex items-center gap-2">
        <!-- Prediction mode selector -->
        {#if showPredictions}
          <ToggleGroup.Root
            type="single"
            bind:value={predictionMode}
            class="bg-slate-900 rounded-lg p-0.5"
          >
            <ToggleGroup.Item
              value="cone"
              class="px-2 py-1 text-xs font-medium text-slate-400 data-[state=on]:bg-purple-700 data-[state=on]:text-slate-100 rounded-md transition-colors"
              title="Cone of probabilities"
            >
              Cone
            </ToggleGroup.Item>
            <ToggleGroup.Item
              value="lines"
              class="px-2 py-1 text-xs font-medium text-slate-400 data-[state=on]:bg-purple-700 data-[state=on]:text-slate-100 rounded-md transition-colors"
              title="All prediction lines"
            >
              Lines
            </ToggleGroup.Item>
            <ToggleGroup.Item
              value="iob"
              class="px-2 py-1 text-xs font-medium text-slate-400 data-[state=on]:bg-cyan-700 data-[state=on]:text-slate-100 rounded-md transition-colors"
              title="IOB only"
            >
              IOB
            </ToggleGroup.Item>
            <ToggleGroup.Item
              value="zt"
              class="px-2 py-1 text-xs font-medium text-slate-400 data-[state=on]:bg-orange-700 data-[state=on]:text-slate-100 rounded-md transition-colors"
              title="Zero Temp"
            >
              ZT
            </ToggleGroup.Item>
            <ToggleGroup.Item
              value="uam"
              class="px-2 py-1 text-xs font-medium text-slate-400 data-[state=on]:bg-green-700 data-[state=on]:text-slate-100 rounded-md transition-colors"
              title="UAM"
            >
              UAM
            </ToggleGroup.Item>
          </ToggleGroup.Root>
        {/if}

        <!-- Time range selector -->
        <ToggleGroup.Root
          type="single"
          bind:value={selectedTimeRange}
          class="bg-slate-900 rounded-lg p-0.5"
        >
          {#each timeRangeOptions as option}
            <ToggleGroup.Item
              value={option.value}
              class="px-3 py-1 text-xs font-medium text-slate-400 data-[state=on]:bg-slate-700 data-[state=on]:text-slate-100 rounded-md transition-colors"
            >
              {option.label}
            </ToggleGroup.Item>
          {/each}
        </ToggleGroup.Root>
      </div>
    </div>
  </CardHeader>

  <CardContent class="p-2">
    <div class="h-[420px] relative p-4">
      <Chart
        data={unifiedGlucoseData}
        x={(d) => d.time}
        y={(d) => d.y}
        xScale={scaleTime()}
        yScale={scaleLinear()}
        xDomain={[chartXDomain.from, chartXDomain.to]}
        yDomain={[0, 100]}
        padding={{ left: 12, bottom: 30, top: 8, right: 12 }}
        tooltip={{ mode: "quadtree-x" }}
      >
        <Svg>
          <ChartClipPath>
            <!-- High threshold line -->
            <Layer type={"svg"}>
              <Rule
                y={unifiedHighThreshold}
                class="stroke-[var(--glucose-high)]/50"
                stroke-dasharray="4,4"
              />

              <!-- Low threshold line -->
              <Rule
                y={unifiedLowThreshold}
                class="stroke-[var(--glucose-very-low)]/50"
                stroke-dasharray="4,4"
              />

              <Highlight points lines />
            </Layer>

            <!-- Glucose line -->
            <Layer type={"svg"}>
              <Spline
                data={unifiedGlucoseData}
                x={(d) => d.time}
                y={(d) => d.y}
                class="stroke-[var(--insulin)] stroke-2 fill-none"
              />

              <!-- Glucose points with color based on value -->
              {#each unifiedGlucoseData as point}
                <Points
                  data={[point]}
                  x={(d) => d.time}
                  y={(d) => d.y}
                  r={3}
                  fill={point.color}
                  class="opacity-90"
                />
              {/each}

              <!-- Prediction visualizations -->
              {#if showPredictions && predictionData}
                {#if predictionMode === "cone" && unifiedPredictionConeData.length > 0}
                  <Area
                    data={unifiedPredictionConeData}
                    x={(d) => d.time}
                    y0={(d) => d.yMax}
                    y1={(d) => d.yMin}
                    curve={curveMonotoneX}
                    class="fill-purple-500/20 stroke-none"
                  />
                  <Spline
                    data={unifiedPredictionConeData}
                    x={(d) => d.time}
                    y={(d) => d.yMid}
                    curve={curveMonotoneX}
                    class="stroke-purple-400 stroke-1 fill-none"
                    stroke-dasharray="4,2"
                  />
                {:else if predictionMode === "lines"}
                  {#if unifiedPredictionCurveData.length > 0}
                    <Spline
                      data={unifiedPredictionCurveData}
                      x={(d) => d.time}
                      y={(d) => d.y}
                      curve={curveMonotoneX}
                      class="stroke-purple-400 stroke-2 fill-none"
                      stroke-dasharray="6,3"
                    />
                  {/if}
                  {#if unifiedIobPredictionData.length > 0}
                    <Spline
                      data={unifiedIobPredictionData}
                      x={(d) => d.time}
                      y={(d) => d.y}
                      curve={curveMonotoneX}
                      class="stroke-cyan-400 stroke-1 fill-none opacity-80"
                      stroke-dasharray="4,2"
                    />
                  {/if}
                  {#if unifiedZeroTempPredictionData.length > 0}
                    <Spline
                      data={unifiedZeroTempPredictionData}
                      x={(d) => d.time}
                      y={(d) => d.y}
                      curve={curveMonotoneX}
                      class="stroke-orange-400 stroke-1 fill-none opacity-80"
                      stroke-dasharray="4,2"
                    />
                  {/if}
                  {#if unifiedUamPredictionData.length > 0}
                    <Spline
                      data={unifiedUamPredictionData}
                      x={(d) => d.time}
                      y={(d) => d.y}
                      curve={curveMonotoneX}
                      class="stroke-green-400 stroke-1 fill-none opacity-80"
                      stroke-dasharray="4,2"
                    />
                  {/if}
                  {#if unifiedCobPredictionData.length > 0}
                    <Spline
                      data={unifiedCobPredictionData}
                      x={(d) => d.time}
                      y={(d) => d.y}
                      curve={curveMonotoneX}
                      class="stroke-yellow-400 stroke-1 fill-none opacity-80"
                      stroke-dasharray="4,2"
                    />
                  {/if}
                {:else if predictionMode === "main" && unifiedPredictionCurveData.length > 0}
                  <Spline
                    data={unifiedPredictionCurveData}
                    x={(d) => d.time}
                    y={(d) => d.y}
                    curve={curveMonotoneX}
                    class="stroke-purple-400 stroke-2 fill-none"
                    stroke-dasharray="6,3"
                  />
                {:else if predictionMode === "iob" && unifiedIobPredictionData.length > 0}
                  <Spline
                    data={unifiedIobPredictionData}
                    x={(d) => d.time}
                    y={(d) => d.y}
                    curve={curveMonotoneX}
                    class="stroke-cyan-400 stroke-2 fill-none"
                    stroke-dasharray="6,3"
                  />
                {:else if predictionMode === "zt" && unifiedZeroTempPredictionData.length > 0}
                  <Spline
                    data={unifiedZeroTempPredictionData}
                    x={(d) => d.time}
                    y={(d) => d.y}
                    curve={curveMonotoneX}
                    class="stroke-orange-400 stroke-2 fill-none"
                    stroke-dasharray="6,3"
                  />
                {:else if predictionMode === "uam" && unifiedUamPredictionData.length > 0}
                  <Spline
                    data={unifiedUamPredictionData}
                    x={(d) => d.time}
                    y={(d) => d.y}
                    curve={curveMonotoneX}
                    class="stroke-green-400 stroke-2 fill-none"
                    stroke-dasharray="6,3"
                  />
                {:else if predictionMode === "cob" && unifiedCobPredictionData.length > 0}
                  <Spline
                    data={unifiedCobPredictionData}
                    x={(d) => d.time}
                    y={(d) => d.y}
                    curve={curveMonotoneX}
                    class="stroke-yellow-400 stroke-2 fill-none"
                    stroke-dasharray="6,3"
                  />
                {/if}
              {/if}
              {#if showPredictions && predictionError}
                <Text x={50} y={50} class="text-xs fill-red-400">
                  Prediction unavailable
                </Text>
              {/if}

              <!-- Bolus markers -->
              {#each unifiedBolusMarkerData as marker}
                <Group x={marker.time.getTime()} y={marker.y}>
                  <Polygon
                    points={[
                      { x: 0, y: -8 },
                      { x: -4, y: 0 },
                      { x: 4, y: 0 },
                    ]}
                    fill="var(--insulin-bolus)"
                    class="opacity-90"
                  />
                </Group>
              {/each}

              <!-- Carb markers -->
              {#each unifiedCarbMarkerData as marker}
                <Group x={marker.time.getTime()} y={marker.y}>
                  <Polygon
                    points={[
                      { x: 0, y: 8 },
                      { x: -4, y: 0 },
                      { x: 4, y: 0 },
                    ]}
                    fill="var(--carbs)"
                    class="opacity-90"
                  />
                </Group>
              {/each}

              <!-- ===== BASAL PANEL ===== -->
              <!-- Default basal reference line -->
              <Rule
                y={unifiedDefaultBasal}
                class="stroke-muted-foreground"
                stroke-dasharray="4,4"
              />

              <!-- Basal area -->
              {#if unifiedBasalData.length > 0}
                <Area
                  data={unifiedBasalData}
                  x={(d) => d.time}
                  y0={(d) => d.y0}
                  y1={(d) => d.y}
                  curve={curveStepAfter}
                  fill="var(--insulin-basal)"
                  class="stroke-[var(--insulin)] stroke-1"
                />
              {/if}

              <!-- ===== IOB PANEL ===== -->
              <!-- IOB area -->
              {#if unifiedIobData.length > 0 && unifiedIobData.some((d) => d.iob > 0.01)}
                <Area
                  data={unifiedIobData}
                  x={(d) => d.time}
                  y0={(d) => d.y0}
                  y1={(d) => d.y}
                  curve={curveMonotoneX}
                  fill="var(--iob-basal)"
                  class="stroke-[var(--insulin)] stroke-1"
                />
              {/if}
            </Layer>
          </ChartClipPath>
          <!-- X-Axis (bottom) -->
          <Axis
            placement="bottom"
            format={(v) => (v instanceof Date ? formatTime(v) : String(v))}
            tickLabelProps={{ class: "text-xs fill-muted-foreground" }}
          />

          <!-- Axis should be scaled for sgv values -->
          <Axis
            placement="left"
            scale={scaleLinear()}
            tickLabelProps={{ class: "text-xs fill-muted-foreground" }}
          />

          <!-- Glucose Y-Axis labels (left) -->
          <Text
            x={4}
            y={mapGlucoseToY(highThreshold)}
            textAnchor="start"
            dy={-4}
            class="text-[10px] fill-[var(--glucose-high)]"
          >
            {highThreshold}
          </Text>
          <Text
            x={4}
            y={mapGlucoseToY(lowThreshold)}
            textAnchor="start"
            dy={12}
            class="text-[10px] fill-[var(--glucose-very-low)]"
          >
            {lowThreshold}
          </Text>

          <!-- Basal label -->
          <Text
            x={4}
            y={basalYStart + 4}
            class="text-[8px] fill-muted-foreground font-medium"
          >
            BASAL
          </Text>
          <Text
            x={44}
            y={unifiedDefaultBasal}
            textAnchor="end"
            dy={-2}
            class="text-[7px] fill-muted-foreground"
          >
            {effectiveDefaultBasalRate.toFixed(1)}U/hr
          </Text>

          <!-- IOB label -->
          <Text
            x={4}
            y={iobYStart + 4}
            class="text-[8px] fill-muted-foreground font-medium"
          >
            IOB
          </Text>
          {#if maxIOB >= 1}
            <Text
              x={44}
              y={mapIobToY(1)}
              textAnchor="end"
              class="text-[6px] fill-[var(--insulin)]"
            >
              1U
            </Text>
          {/if}
        </Svg>

        <Tooltip.Root
          class="bg-card border border-border rounded-lg px-3 py-2 shadow-lg text-xs"
        >
          {#snippet children({ data })}
            <Tooltip.Header
              value={data?.time?.toLocaleTimeString([], {
                hour: "numeric",
                minute: "2-digit",
              })}
              format="time"
            />
            <Tooltip.List>
              {#if data?.sgv}
                <Tooltip.Item
                  label="Blood glucose"
                  value={data.sgv}
                  color="var(--glucose-in-range)"
                />
              {/if}
            </Tooltip.List>
          {/snippet}
        </Tooltip.Root>
      </Chart>
    </div>

    <!-- Legend -->
    <div
      class="flex flex-wrap justify-center gap-4 text-[10px] text-muted-foreground pt-2"
    >
      <div class="flex items-center gap-1">
        <div
          class="w-2 h-2 rounded-full"
          style="background: var(--glucose-in-range)"
        ></div>
        <span>In Range</span>
      </div>
      <div class="flex items-center gap-1">
        <div
          class="w-2 h-2 rounded-full"
          style="background: var(--glucose-high)"
        ></div>
        <span>High</span>
      </div>
      <div class="flex items-center gap-1">
        <div
          class="w-2 h-2 rounded-full"
          style="background: var(--glucose-very-low)"
        ></div>
        <span>Low</span>
      </div>
      <div class="flex items-center gap-1">
        <div
          class="w-3 h-2"
          style="background: var(--insulin-basal); border: 1px solid var(--insulin)"
        ></div>
        <span>Basal</span>
      </div>
      <div class="flex items-center gap-1">
        <div
          class="w-3 h-2"
          style="background: var(--iob-basal); border: 1px solid var(--insulin)"
        ></div>
        <span>IOB</span>
      </div>
      <div class="flex items-center gap-1">
        <div
          class="w-0 h-0 border-l-4 border-r-4 border-b-4 border-l-transparent border-r-transparent"
          style="border-bottom-color: var(--insulin-bolus)"
        ></div>
        <span>Bolus</span>
      </div>
      <div class="flex items-center gap-1">
        <div
          class="w-0 h-0 border-l-4 border-r-4 border-t-4 border-l-transparent border-r-transparent"
          style="border-top-color: var(--carbs)"
        ></div>
        <span>Carbs</span>
      </div>
    </div>
  </CardContent>
</Card>
