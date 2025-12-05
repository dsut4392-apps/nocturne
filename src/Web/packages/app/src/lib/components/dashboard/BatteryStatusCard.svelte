<script lang="ts">
  import {
    Card,
    CardContent,
    CardHeader,
    CardTitle,
  } from "$lib/components/ui/card";
  import { Badge } from "$lib/components/ui/badge";
  import { Button } from "$lib/components/ui/button";
  import { Separator } from "$lib/components/ui/separator";
  import {
    Battery,
    BatteryCharging,
    BatteryFull,
    BatteryLow,
    BatteryMedium,
    BatteryWarning,
    ChevronDown,
    ChevronUp,
    Zap,
  } from "lucide-svelte";
  import { timeAgo } from "$lib/utils";
  import { getBatteryCardData } from "$lib/data/battery.remote";

  // Props
  interface Props {
    showStatistics?: boolean;
    showVoltage?: boolean;
  }

  let { showStatistics = true, showVoltage = false }: Props = $props();

  // State
  let expanded = $state(false);

  // Fetch data using remote function
  const batteryDataPromise = $derived(
    getBatteryCardData({ recentMinutes: 30 })
  );

  // Get battery icon component based on level
  function getBatteryIconComponent(level: number | undefined) {
    if (!level) return BatteryWarning;
    if (level >= 95) return BatteryFull;
    if (level >= 50) return BatteryMedium;
    if (level >= 25) return BatteryLow;
    return BatteryWarning;
  }

  // Get status badge variant
  function getStatusBadgeVariant(
    status: string | undefined
  ): "destructive" | "secondary" | "default" {
    switch (status) {
      case "urgent":
        return "destructive";
      case "warn":
        return "secondary";
      default:
        return "default";
    }
  }

  // Get color class for battery status
  function getBatteryColorClass(status: string | undefined): string {
    switch (status) {
      case "urgent":
        return "text-red-500";
      case "warn":
        return "text-yellow-500";
      default:
        return "text-green-500";
    }
  }

  // Format duration in hours/minutes
  function formatDuration(minutes: number | undefined): string {
    if (!minutes) return "N/A";
    const hours = Math.floor(minutes / 60);
    const mins = Math.round(minutes % 60);
    if (hours === 0) return `${mins}m`;
    if (mins === 0) return `${hours}h`;
    return `${hours}h ${mins}m`;
  }

  // Extract device name from URI
  function extractDeviceName(device: string | undefined): string {
    if (!device) return "Unknown";
    if (device.includes("://")) {
      return device.split("://")[1] || device;
    }
    return device;
  }
</script>

{#await batteryDataPromise}
  <Card class="col-span-1">
    <CardHeader class="pb-2">
      <div class="flex items-center gap-2">
        <Battery class="h-4 w-4 text-muted-foreground" />
        <CardTitle class="text-sm font-medium">Device Battery</CardTitle>
      </div>
    </CardHeader>
    <CardContent>
      <div class="text-sm text-muted-foreground">Loading...</div>
    </CardContent>
  </Card>
{:then data}
  {@const currentStatus = data?.currentStatus}
  {@const statistics = data?.statistics ?? []}
  {@const hasDevices =
    currentStatus && Object.keys(currentStatus.devices ?? {}).length > 0}
  {@const deviceCount = currentStatus
    ? Object.keys(currentStatus.devices ?? {}).length
    : 0}

  <Card class="col-span-1">
    <CardHeader class="pb-2">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <Battery class="h-4 w-4 text-muted-foreground" />
          <CardTitle class="text-sm font-medium">Device Battery</CardTitle>
        </div>
        {#if hasDevices && deviceCount > 1}
          <Button
            variant="ghost"
            size="sm"
            class="h-6 px-2"
            onclick={() => (expanded = !expanded)}
          >
            {#if expanded}
              <ChevronUp class="h-4 w-4" />
            {:else}
              <ChevronDown class="h-4 w-4" />
            {/if}
          </Button>
        {/if}
      </div>
    </CardHeader>
    <CardContent>
      {#if !hasDevices}
        <div class="text-sm text-muted-foreground">
          No battery data available
        </div>
      {:else if currentStatus}
        <!-- Main battery display -->
        <div class="flex items-center gap-3">
          <div class="relative">
            {#if currentStatus.min?.isCharging}
              <BatteryCharging
                class="h-8 w-8 {getBatteryColorClass(currentStatus.status)}"
              />
            {:else}
              {@const IconComponent = getBatteryIconComponent(
                currentStatus.level
              )}
              <IconComponent
                class="h-8 w-8 {getBatteryColorClass(currentStatus.status)}"
              />
            {/if}
          </div>
          <div>
            <div class="text-2xl font-bold flex items-center gap-1">
              {currentStatus.display}
              {#if currentStatus.min?.isCharging}
                <Zap class="h-4 w-4 text-yellow-500" />
              {/if}
            </div>
            {#if currentStatus.min}
              <p class="text-xs text-muted-foreground">
                {extractDeviceName(currentStatus.min.device)}
                • {timeAgo(currentStatus.min.mills ?? 0)}
              </p>
            {/if}
          </div>
          <Badge
            variant={getStatusBadgeVariant(currentStatus.status)}
            class="ml-auto"
          >
            {currentStatus.status === "urgent"
              ? "Low"
              : currentStatus.status === "warn"
                ? "Warning"
                : "OK"}
          </Badge>
        </div>

        <!-- Expanded view for multiple devices -->
        {#if expanded && deviceCount > 1 && currentStatus.devices}
          <Separator class="my-3" />
          <div class="space-y-2">
            {#each Object.values(currentStatus.devices) as device}
              <div class="flex items-center justify-between text-sm">
                <div class="flex items-center gap-2">
                  {#if device.min?.isCharging}
                    <BatteryCharging class="h-4 w-4 text-yellow-500" />
                  {:else}
                    {@const DeviceIcon = getBatteryIconComponent(
                      device.min?.level
                    )}
                    <DeviceIcon
                      class="h-4 w-4 {getBatteryColorClass(
                        device.min?.notification
                      )}"
                    />
                  {/if}
                  <span>{device.name}</span>
                </div>
                <div class="flex items-center gap-2">
                  <span class="font-medium">{device.min?.display ?? "?"}</span>
                  {#if showVoltage && device.min?.voltage}
                    <span class="text-muted-foreground text-xs">
                      ({device.min.voltage.toFixed(2)}v)
                    </span>
                  {/if}
                </div>
              </div>
            {/each}
          </div>
        {/if}

        <!-- Statistics section -->
        {#if showStatistics && statistics.length > 0}
          <Separator class="my-3" />
          <div class="space-y-1">
            <h4
              class="text-xs font-medium text-muted-foreground uppercase tracking-wide"
            >
              Battery Stats (7 days)
            </h4>
            {#each statistics.slice(0, expanded ? statistics.length : 1) as stat}
              <div class="grid grid-cols-2 gap-2 text-xs">
                {#if stat.averageTimeBetweenChargesHours}
                  <div>
                    <span class="text-muted-foreground">Avg charge life:</span>
                    <span class="font-medium ml-1">
                      {formatDuration(stat.averageDischargeDurationMinutes)}
                    </span>
                  </div>
                {/if}
                {#if stat.chargeCycleCount && stat.chargeCycleCount > 0}
                  <div>
                    <span class="text-muted-foreground">Charge cycles:</span>
                    <span class="font-medium ml-1">
                      {stat.chargeCycleCount}
                    </span>
                  </div>
                {/if}
                {#if stat.averageLevel}
                  <div>
                    <span class="text-muted-foreground">Avg level:</span>
                    <span class="font-medium ml-1">
                      {stat.averageLevel.toFixed(0)}%
                    </span>
                  </div>
                {/if}
                {#if stat.timeBelow30Percent && stat.timeBelow30Percent > 0}
                  <div>
                    <span class="text-muted-foreground">Time &lt;30%:</span>
                    <span class="font-medium ml-1 text-yellow-600">
                      {stat.timeBelow30Percent.toFixed(1)}%
                    </span>
                  </div>
                {/if}
              </div>
            {/each}
          </div>
        {/if}
      {/if}
    </CardContent>
  </Card>
{:catch}
  <Card class="col-span-1">
    <CardHeader class="pb-2">
      <div class="flex items-center gap-2">
        <Battery class="h-4 w-4 text-muted-foreground" />
        <CardTitle class="text-sm font-medium">Device Battery</CardTitle>
      </div>
    </CardHeader>
    <CardContent>
      <div class="text-sm text-red-500">Failed to load battery data</div>
    </CardContent>
  </Card>
{/await}
