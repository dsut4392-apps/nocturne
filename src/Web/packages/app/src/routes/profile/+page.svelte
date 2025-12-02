<script lang="ts">
  import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
  } from "$lib/components/ui/card";
  import { Badge } from "$lib/components/ui/badge";
  import { Button } from "$lib/components/ui/button";
  import * as Tabs from "$lib/components/ui/tabs";
  import * as Table from "$lib/components/ui/table";
  import {
    User,
    Activity,
    Target,
    Droplet,
    ChevronRight,
    Settings,
    TrendingUp,
    History,
  } from "lucide-svelte";
  import type { Profile, TimeValue } from "$lib/api";

  interface Props {
    data: {
      profiles: Profile[];
      currentProfile: Profile | null;
      totalProfiles: number;
    };
  }

  let { data }: Props = $props();

  // Currently selected profile for detail view
  let selectedProfileId = $state<string | null>(
    data.currentProfile?._id ?? null
  );

  // Derived: get selected profile
  let selectedProfile = $derived(
    data.profiles.find((p) => p._id === selectedProfileId) ?? null
  );

  // Derived: get selected profile's store names
  let profileStoreNames = $derived(
    selectedProfile?.store ? Object.keys(selectedProfile.store).map(String) : []
  );

  // Derived: get selected profile's default store
  let defaultStoreName = $derived(selectedProfile?.defaultProfile ?? "");
  let defaultStore = $derived(
    defaultStoreName && selectedProfile?.store
      ? selectedProfile.store[defaultStoreName]
      : null
  );

  function formatDate(dateString: string | undefined): string {
    if (!dateString) return "Unknown";
    try {
      return new Date(dateString).toLocaleDateString(undefined, {
        year: "numeric",
        month: "long",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    } catch {
      return dateString;
    }
  }
</script>

<svelte:head>
  <title>Profile - Nocturne</title>
  <meta
    name="description"
    content="Manage your diabetes therapy profile settings"
  />
</svelte:head>

<div class="container mx-auto p-6 max-w-5xl space-y-6">
  <!-- Header -->
  <div class="flex items-start justify-between">
    <div class="flex items-center gap-3">
      <div
        class="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10"
      >
        <User class="h-6 w-6 text-primary" />
      </div>
      <div>
        <h1 class="text-3xl font-bold tracking-tight">Profile</h1>
        <p class="text-muted-foreground">
          Your therapy settings and insulin parameters
        </p>
      </div>
    </div>
    <Badge variant="secondary" class="gap-1">
      <History class="h-3 w-3" />
      {data.totalProfiles} profile{data.totalProfiles !== 1 ? "s" : ""}
    </Badge>
  </div>

  {#if !data.currentProfile}
    <!-- Empty State -->
    <Card class="border-dashed">
      <CardContent class="py-12">
        <div class="text-center space-y-4">
          <div
            class="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-muted"
          >
            <User class="h-8 w-8 text-muted-foreground" />
          </div>
          <div>
            <h3 class="text-lg font-semibold">No Profile Found</h3>
            <p class="text-sm text-muted-foreground max-w-md mx-auto mt-1">
              Profiles are typically uploaded from your diabetes management app
              (like AAPS, Loop, or xDrip+). They contain your basal rates,
              insulin sensitivity factors, and carb ratios.
            </p>
          </div>
          <Button variant="outline" href="/settings/services">
            <Settings class="h-4 w-4 mr-2" />
            Configure Data Sources
          </Button>
        </div>
      </CardContent>
    </Card>
  {:else}
    <!-- Profile Selector (if multiple profiles) -->
    {#if data.profiles.length > 1}
      <Card>
        <CardHeader class="pb-3">
          <CardTitle class="text-lg flex items-center gap-2">
            <History class="h-5 w-5" />
            Profile History
          </CardTitle>
          <CardDescription>
            Select a profile to view its settings
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {#each data.profiles as profile}
              <button
                class="flex items-center gap-3 p-3 rounded-lg border text-left transition-colors
                       {selectedProfileId === profile._id
                  ? 'border-primary bg-primary/5'
                  : 'hover:bg-accent/50'}"
                onclick={() => (selectedProfileId = profile._id ?? null)}
              >
                <div
                  class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg
                         {selectedProfileId === profile._id
                    ? 'bg-primary/10'
                    : 'bg-muted'}"
                >
                  <User
                    class="h-5 w-5 {selectedProfileId === profile._id
                      ? 'text-primary'
                      : 'text-muted-foreground'}"
                  />
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="font-medium truncate">
                      {profile.defaultProfile ?? "Unnamed Profile"}
                    </span>
                    {#if profile._id === data.currentProfile?._id}
                      <Badge
                        variant="default"
                        class="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100 text-xs"
                      >
                        Active
                      </Badge>
                    {/if}
                  </div>
                  <p class="text-xs text-muted-foreground truncate">
                    {formatDate(profile.created_at)}
                  </p>
                </div>
                <ChevronRight
                  class="h-4 w-4 text-muted-foreground shrink-0 {selectedProfileId ===
                  profile._id
                    ? 'text-primary'
                    : ''}"
                />
              </button>
            {/each}
          </div>
        </CardContent>
      </Card>
    {/if}

    <!-- Selected Profile Details -->
    {#if selectedProfile}
      <!-- Profile Overview Card -->
      <Card>
        <CardHeader>
          <div class="flex items-start justify-between">
            <div>
              <CardTitle class="flex items-center gap-2">
                {selectedProfile.defaultProfile ?? "Profile"}
                {#if selectedProfile._id === data.currentProfile?._id}
                  <Badge
                    variant="default"
                    class="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-100"
                  >
                    Active
                  </Badge>
                {/if}
              </CardTitle>
              <CardDescription>
                Created {formatDate(selectedProfile.created_at)}
              </CardDescription>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
            <div class="space-y-1">
              <span class="text-xs text-muted-foreground">Units</span>
              <p class="font-medium">
                {selectedProfile.units ?? "mg/dL"}
              </p>
            </div>
            <div class="space-y-1">
              <span class="text-xs text-muted-foreground">Timezone</span>
              <p class="font-medium">
                {defaultStore?.timezone ?? "Not set"}
              </p>
            </div>
            <div class="space-y-1">
              <span class="text-xs text-muted-foreground">DIA</span>
              <p class="font-medium">
                {defaultStore?.dia ?? "–"} hours
              </p>
            </div>
            <div class="space-y-1">
              <span class="text-xs text-muted-foreground">Carbs/hr</span>
              <p class="font-medium">
                {defaultStore?.carbs_hr ?? "–"} g/hr
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <!-- Profile Stores -->
      {#if profileStoreNames.length > 0}
        <Tabs.Root value={defaultStoreName || profileStoreNames[0]}>
          <Tabs.List class="w-full justify-start">
            {#each profileStoreNames as storeName}
              <Tabs.Trigger value={storeName} class="gap-2">
                <User class="h-4 w-4" />
                {storeName}
                {#if storeName === defaultStoreName}
                  <Badge variant="secondary" class="text-xs ml-1">
                    Default
                  </Badge>
                {/if}
              </Tabs.Trigger>
            {/each}
          </Tabs.List>

          {#each profileStoreNames as storeName}
            {@const store = selectedProfile.store?.[storeName]}
            <Tabs.Content value={storeName} class="mt-4 space-y-4">
              {#if store}
                <!-- Time-based Settings Grid -->
                <div class="grid gap-4 md:grid-cols-2">
                  <!-- Basal Rates -->
                  {#if store.basal && store.basal.length > 0}
                    {@render ProfileTimeValueCard({
                      title: "Basal Rates",
                      description: "Background insulin delivery rates",
                      unit: "U/hr",
                      icon: Activity,
                      values: store.basal,
                      colorClass: "text-blue-600",
                    })}
                  {/if}

                  <!-- Carb Ratios -->
                  {#if store.carbratio && store.carbratio.length > 0}
                    {@render ProfileTimeValueCard({
                      title: "Carb Ratios (I:C)",
                      description: "Grams of carbs per unit of insulin",
                      unit: "g/U",
                      icon: Droplet,
                      values: store.carbratio,
                      colorClass: "text-green-600",
                    })}
                  {/if}

                  <!-- Insulin Sensitivity -->
                  {#if store.sens && store.sens.length > 0}
                    {@render ProfileTimeValueCard({
                      title: "Insulin Sensitivity (ISF)",
                      description: "BG drop per unit of insulin",
                      unit:
                        selectedProfile.units === "mmol"
                          ? "mmol/L/U"
                          : "mg/dL/U",
                      icon: TrendingUp,
                      values: store.sens,
                      colorClass: "text-purple-600",
                    })}
                  {/if}

                  <!-- Target Range -->
                  {#if (store.target_low && store.target_low.length > 0) || (store.target_high && store.target_high.length > 0)}
                    <Card>
                      <CardHeader class="pb-3">
                        <div class="flex items-center gap-3">
                          <div
                            class="flex h-10 w-10 items-center justify-center rounded-lg bg-amber-500/10"
                          >
                            <Target class="h-5 w-5 text-amber-600" />
                          </div>
                          <div>
                            <CardTitle class="text-base">
                              Target Range
                            </CardTitle>
                            <CardDescription class="text-xs">
                              Desired blood glucose range
                            </CardDescription>
                          </div>
                        </div>
                      </CardHeader>
                      <CardContent>
                        <Table.Root>
                          <Table.Header>
                            <Table.Row>
                              <Table.Head>Time</Table.Head>
                              <Table.Head class="text-right">Low</Table.Head>
                              <Table.Head class="text-right">High</Table.Head>
                            </Table.Row>
                          </Table.Header>
                          <Table.Body>
                            {@const lowValues = store.target_low ?? []}
                            {@const highValues = store.target_high ?? []}
                            {@const maxLen = Math.max(
                              lowValues.length,
                              highValues.length
                            )}
                            {#each Array(maxLen) as _, i}
                              <Table.Row>
                                <Table.Cell class="font-mono text-sm">
                                  {lowValues[i]?.time ??
                                    highValues[i]?.time ??
                                    "–"}
                                </Table.Cell>
                                <Table.Cell class="text-right font-mono">
                                  {lowValues[i]?.value ?? "–"}
                                </Table.Cell>
                                <Table.Cell class="text-right font-mono">
                                  {highValues[i]?.value ?? "–"}
                                </Table.Cell>
                              </Table.Row>
                            {/each}
                          </Table.Body>
                        </Table.Root>
                      </CardContent>
                    </Card>
                  {/if}
                </div>

                <!-- Additional Store Metadata -->
                <Card class="bg-muted/30">
                  <CardHeader class="pb-3">
                    <CardTitle class="text-sm font-medium">
                      Profile Settings
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
                      <div>
                        <span class="text-muted-foreground">DIA</span>
                        <p class="font-medium">{store.dia ?? "–"} hours</p>
                      </div>
                      <div>
                        <span class="text-muted-foreground">Carbs/hr</span>
                        <p class="font-medium">{store.carbs_hr ?? "–"} g/hr</p>
                      </div>
                      <div>
                        <span class="text-muted-foreground">Timezone</span>
                        <p class="font-medium">{store.timezone ?? "–"}</p>
                      </div>
                      <div>
                        <span class="text-muted-foreground">Units</span>
                        <p class="font-medium">{store.units ?? "–"}</p>
                      </div>
                    </div>
                  </CardContent>
                </Card>
              {:else}
                <div class="text-center py-8 text-muted-foreground">
                  <p>No data available for this profile store.</p>
                </div>
              {/if}
            </Tabs.Content>
          {/each}
        </Tabs.Root>
      {:else}
        <Card class="border-dashed">
          <CardContent class="py-8">
            <div class="text-center text-muted-foreground">
              <p>
                This profile doesn't contain any therapy settings (basal, carb
                ratios, etc.)
              </p>
            </div>
          </CardContent>
        </Card>
      {/if}
    {/if}
  {/if}
</div>

<!-- Profile Time Value Card Component -->
{#snippet ProfileTimeValueCard({
  title,
  description,
  unit,
  icon: Icon,
  values,
  colorClass,
}: {
  title: string;
  description: string;
  unit: string;
  icon: typeof Activity;
  values: TimeValue[];
  colorClass: string;
})}
  <Card>
    <CardHeader class="pb-3">
      <div class="flex items-center gap-3">
        <div
          class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10"
        >
          <Icon class="h-5 w-5 {colorClass}" />
        </div>
        <div>
          <CardTitle class="text-base">{title}</CardTitle>
          <CardDescription class="text-xs">{description}</CardDescription>
        </div>
      </div>
    </CardHeader>
    <CardContent>
      <Table.Root>
        <Table.Header>
          <Table.Row>
            <Table.Head>Time</Table.Head>
            <Table.Head class="text-right">{unit}</Table.Head>
          </Table.Row>
        </Table.Header>
        <Table.Body>
          {#each values as timeValue}
            <Table.Row>
              <Table.Cell class="font-mono text-sm">
                {timeValue.time ?? "–"}
              </Table.Cell>
              <Table.Cell class="text-right font-mono">
                {timeValue.value ?? "–"}
              </Table.Cell>
            </Table.Row>
          {/each}
        </Table.Body>
      </Table.Root>
    </CardContent>
  </Card>
{/snippet}
