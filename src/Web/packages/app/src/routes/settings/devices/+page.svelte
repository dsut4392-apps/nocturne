<script lang="ts">
  import { getSettingsStore } from "$lib/stores/settings-store.svelte";
  import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
  } from "$lib/components/ui/card";
  import { Badge } from "$lib/components/ui/badge";
  import { Switch } from "$lib/components/ui/switch";
  import { Label } from "$lib/components/ui/label";
  import { Separator } from "$lib/components/ui/separator";
  import { AlertCircle, Loader2 } from "lucide-svelte";
  import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
  } from "$lib/components/ui/select";
  import {
    Smartphone,
    Activity,
    Bluetooth,
    Battery,
    Droplet,
    Radio,
    Watch,
  } from "lucide-svelte";
  import SettingsPageSkeleton from "$lib/components/settings/SettingsPageSkeleton.svelte";
  import {
    getRecentDeviceStatuses,
    type DisplayDevice,
  } from "$lib/data/devices.remote";

  const store = getSettingsStore();

  function getStatusBadge(status: DisplayDevice["status"]) {
    switch (status) {
      case "active":
        return { variant: "default" as const, text: "Active" };
      case "stale":
        return { variant: "secondary" as const, text: "Stale" };
      default:
        return { variant: "secondary" as const, text: "Unknown" };
    }
  }

  function formatLastSeen(date: Date): string {
    const diff = Date.now() - date.getTime();
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return "Just now";
    if (minutes < 60) return `${minutes}m ago`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours}h ago`;
    return date.toLocaleDateString();
  }
</script>

<svelte:head>
  <title>Devices - Settings - Nocturne</title>
</svelte:head>

<div class="container mx-auto p-6 max-w-3xl space-y-6">
  <!-- Header -->
  <div>
    <h1 class="text-2xl font-bold tracking-tight">Devices</h1>
    <p class="text-muted-foreground">
      Manage your CGM, insulin pump, and other connected devices
    </p>
  </div>

  {#if store.isLoading}
    <SettingsPageSkeleton cardCount={3} />
  {:else if store.hasError}
    <Card class="border-destructive">
      <CardContent class="flex items-center gap-3 pt-6">
        <AlertCircle class="h-5 w-5 text-destructive" />
        <div>
          <p class="font-medium">Failed to load settings</p>
          <p class="text-sm text-muted-foreground">{store.error}</p>
        </div>
      </CardContent>
    </Card>
  {:else if store.devices}
    <!-- Active Devices -->
    <Card>
      <CardHeader>
        <CardTitle>Active Devices</CardTitle>
        <CardDescription>
          Devices actively reporting data to Nocturne
        </CardDescription>
      </CardHeader>
      <CardContent class="space-y-4">
        {#await getRecentDeviceStatuses()}
          <div class="flex items-center justify-center py-8">
            <Loader2 class="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        {:then devices}
          {#if devices.length === 0}
            <div class="text-center py-8 text-muted-foreground">
              <Bluetooth class="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p class="font-medium">No active devices found</p>
              <p class="text-sm">Connect a CGM or pump app to see it here</p>
            </div>
          {:else}
            {#each devices as device (device.id)}
              <div
                class="flex flex-col sm:flex-row sm:items-center justify-between p-4 rounded-lg border gap-4"
              >
                <div class="flex items-start sm:items-center gap-4">
                  <div
                    class="flex h-12 w-12 shrink-0 items-center justify-center rounded-lg bg-primary/10"
                  >
                    {#if device.type === "cgm"}
                      <Activity class="h-6 w-6 text-primary" />
                    {:else if device.type === "pump"}
                      <Droplet class="h-6 w-6 text-primary" />
                    {:else if device.type === "loop"}
                      <Radio class="h-6 w-6 text-primary" />
                    {:else if device.type === "uploader"}
                      <Smartphone class="h-6 w-6 text-primary" />
                    {:else}
                      <Watch class="h-6 w-6 text-primary" />
                    {/if}
                  </div>
                  <div>
                    <div class="flex flex-wrap items-center gap-2 mb-1">
                      <span class="font-medium">{device.name}</span>
                      <Badge variant={getStatusBadge(device.status).variant}>
                        {getStatusBadge(device.status).text}
                      </Badge>
                    </div>

                    <div
                      class="flex flex-wrap gap-x-4 gap-y-1 text-sm text-muted-foreground"
                    >
                      {#if device.batteryLevel != null}
                        <span class="flex items-center gap-1">
                          <Battery class="h-3 w-3" />
                          {device.batteryLevel}%
                        </span>
                      {/if}

                      {#each device.details as detail}
                        <span class="flex items-center gap-1">
                          <span class="opacity-70">{detail.label}:</span>
                          <span>{detail.value}{detail.unit ?? ""}</span>
                        </span>
                      {/each}
                    </div>

                    <div class="text-xs text-muted-foreground mt-1">
                      Last seen: {formatLastSeen(device.lastSeen)}
                    </div>
                  </div>
                </div>
              </div>
            {/each}
          {/if}
        {:catch error}
          <div class="text-center py-8 text-muted-foreground">
            <AlertCircle
              class="h-12 w-12 mx-auto mb-4 opacity-50 text-destructive"
            />
            <p class="font-medium text-destructive">Failed to load devices</p>
            <p class="text-sm">{error.message}</p>
          </div>
        {/await}
      </CardContent>
    </Card>

    <!-- Device Settings -->
    <Card>
      <CardHeader>
        <CardTitle>Device Settings</CardTitle>
        <CardDescription>
          Configure how devices connect and sync data
        </CardDescription>
      </CardHeader>
      <CardContent class="space-y-6">
        <div class="flex items-center justify-between">
          <div class="space-y-0.5">
            <Label>Auto-connect on startup</Label>
            <p class="text-sm text-muted-foreground">
              Automatically reconnect to known devices when the app starts
            </p>
          </div>
          <Switch
            checked={store.devices.autoConnect ?? true}
            onCheckedChange={(checked) => {
              if (store.devices) {
                store.devices.autoConnect = checked;
                store.markChanged();
              }
            }}
          />
        </div>

        <Separator />

        <div class="flex items-center justify-between">
          <div class="space-y-0.5">
            <Label>Cloud upload</Label>
            <p class="text-sm text-muted-foreground">
              Upload device data to your Nightscout site
            </p>
          </div>
          <Switch
            checked={store.devices.uploadEnabled ?? true}
            onCheckedChange={(checked) => {
              if (store.devices) {
                store.devices.uploadEnabled = checked;
                store.markChanged();
              }
            }}
          />
        </div>

        <Separator />

        <div class="flex items-center justify-between">
          <div class="space-y-0.5">
            <Label>Show raw sensor data</Label>
            <p class="text-sm text-muted-foreground">
              Display unfiltered readings alongside calibrated values
            </p>
          </div>
          <Switch
            checked={store.devices.showRawData ?? false}
            onCheckedChange={(checked) => {
              if (store.devices) {
                store.devices.showRawData = checked;
                store.markChanged();
              }
            }}
          />
        </div>
      </CardContent>
    </Card>

    <!-- CGM Settings -->
    <Card>
      <CardHeader>
        <CardTitle>CGM Configuration</CardTitle>
        <CardDescription>
          Settings specific to continuous glucose monitors
        </CardDescription>
      </CardHeader>
      <CardContent class="space-y-4">
        <div class="space-y-2">
          <Label>Data source priority</Label>
          <Select
            type="single"
            value={store.devices.cgmConfiguration?.dataSourcePriority ?? "cgm"}
            onValueChange={(value) => {
              if (store.devices?.cgmConfiguration) {
                store.devices.cgmConfiguration.dataSourcePriority = value;
                store.markChanged();
              }
            }}
          >
            <SelectTrigger>
              <span>
                {#if store.devices.cgmConfiguration?.dataSourcePriority === "meter"}
                  Meter readings preferred
                {:else if store.devices.cgmConfiguration?.dataSourcePriority === "average"}
                  Average both sources
                {:else}
                  CGM readings preferred
                {/if}
              </span>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="cgm">CGM readings preferred</SelectItem>
              <SelectItem value="meter">Meter readings preferred</SelectItem>
              <SelectItem value="average">Average both sources</SelectItem>
            </SelectContent>
          </Select>
          <p class="text-sm text-muted-foreground">
            Choose which data source to prioritize when multiple are available
          </p>
        </div>

        <Separator />

        <div class="space-y-2">
          <Label>Sensor warmup period</Label>
          <Select
            type="single"
            value={`${store.devices.cgmConfiguration?.sensorWarmupHours ?? 2}h`}
            onValueChange={(value) => {
              if (store.devices?.cgmConfiguration) {
                const hours = parseInt(value.replace("h", ""));
                if (!isNaN(hours)) {
                  store.devices.cgmConfiguration.sensorWarmupHours = hours;
                  store.markChanged();
                }
              }
            }}
          >
            <SelectTrigger>
              <span>
                {store.devices.cgmConfiguration?.sensorWarmupHours ?? 2} hours
              </span>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="1h">1 hour</SelectItem>
              <SelectItem value="2h">2 hours</SelectItem>
              <SelectItem value="4h">4 hours</SelectItem>
            </SelectContent>
          </Select>
          <p class="text-sm text-muted-foreground">
            Time to wait before using readings from a new sensor
          </p>
        </div>
      </CardContent>
    </Card>
  {/if}
</div>
