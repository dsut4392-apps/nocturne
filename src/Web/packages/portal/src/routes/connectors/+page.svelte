<script lang="ts">
  import { wizardStore } from "$lib/stores/wizard.svelte";
  import {
    getConnectors,
    type ConnectorMetadata,
  } from "$lib/data/portal.remote";
  import { Button } from "@nocturne/app/ui/button";
  import { Badge } from "@nocturne/app/ui/badge";
  import { ChevronLeft, ChevronRight, Check, Settings2 } from "@lucide/svelte";
  import * as Card from "@nocturne/app/ui/card";
  import * as Dialog from "../../../../app/src/lib/components/ui/dialog";
  import { Input } from "@nocturne/app/ui/input";
  import { Label } from "@nocturne/app/ui/label";
  import { Switch } from "@nocturne/app/ui/switch";
  import * as Select from "../../../../app/src/lib/components/ui/select";

  import { useSearchParams } from "runed/kit";
  import { z } from "zod";

  const connectorsQuery = getConnectors({});

  let configuringConnector = $state<ConnectorMetadata | null>(null);
  let isConfigDialogOpen = $state(false);
  let configValues = $state<Record<string, string>>({});

  // Reset config values when dialog opens with new connector
  $effect(() => {
    if (isConfigDialogOpen && configuringConnector) {
      const initial: Record<string, string> =
        wizardStore.connectorConfigs.current[configuringConnector.type] || {};

      // Apply defaults
      configuringConnector.fields.forEach((field) => {
        if (
          initial[field.envVar] === undefined ||
          initial[field.envVar] === ""
        ) {
          if (field.default) {
            initial[field.envVar] = field.default;
          }
        }
      });
      configValues = { ...initial };
    }
  });

  const searchParams = useSearchParams(
    z.object({
      connectors: z.string().optional(),
    }),
  );

  // Initialize from URL
  $effect(() => {
    const urlConnectors = searchParams.connectors?.split(",").filter(Boolean);
    if (urlConnectors?.length) {
      urlConnectors.forEach((c) => {
        if (!wizardStore.selectedConnectors.includes(c)) {
          wizardStore.toggleConnector(c);
        }
      });
    }
  });

  // Sync Store -> URL
  $effect(() => {
    if (wizardStore.selectedConnectors.length > 0) {
      searchParams.connectors = wizardStore.selectedConnectors.join(",");
    } else {
      searchParams.connectors = undefined;
    }
  });

  function isSelected(type: string): boolean {
    return wizardStore.selectedConnectors.includes(type);
  }

  function handleToggleClick(connector: ConnectorMetadata) {
    wizardStore.toggleConnector(connector.type);
  }

  function openConfig(connector: ConnectorMetadata, e?: Event) {
    if (e) e.stopPropagation();
    configuringConnector = connector;
    isConfigDialogOpen = true;
  }

  function handleSaveConfig() {
    if (configuringConnector) {
      wizardStore.setConnectorConfig(configuringConnector.type, configValues);
      isConfigDialogOpen = false;
      configuringConnector = null;
    }
  }

  function handleCardClick(connector: ConnectorMetadata) {
    // If already selected and has config fields, open config
    if (isSelected(connector.type) && connector.fields.length > 0) {
      openConfig(connector);
    } else {
      // Otherwise just toggle
      wizardStore.toggleConnector(connector.type);
    }
  }

  function getCategoryVariant(
    category: string,
  ): "default" | "secondary" | "destructive" | "outline" {
    switch (category.toLowerCase()) {
      case "cgm":
        return "default";
      case "pump":
        return "secondary";
      default:
        return "outline";
    }
  }
</script>

<div class="max-w-screen-lg mx-auto">
  <Button
    href="/setup?type={wizardStore.setupType}"
    variant="ghost"
    size="sm"
    class="mb-8 gap-1 text-muted-foreground"
  >
    <ChevronLeft size={16} />
    Back to setup
  </Button>

  <h1 class="text-3xl font-bold mb-2">Select Connectors</h1>
  <p class="text-muted-foreground mb-8">Choose which data sources to enable</p>

  <svelte:boundary
    onerror={(e) => console.error("Connectors boundary error:", e)}
  >
    {#await connectorsQuery}
      <div class="flex justify-center py-12">
        <div
          class="animate-spin w-8 h-8 border-2 border-primary border-t-transparent rounded-full"
        ></div>
      </div>
    {:then connectors}
      <div class="grid md:grid-cols-2 gap-4 mb-8">
        {#each connectors as connector}
          <Card.Root
            onclick={() => handleCardClick(connector)}
            class="flex flex-col h-full transition-all cursor-pointer {isSelected(
              connector.type,
            )
              ? 'border-primary ring-1 ring-primary/20 bg-primary/5'
              : 'hover:border-primary/50'}"
          >
            <Card.Header class="pb-3">
              <div class="flex justify-between items-start gap-4">
                <div class="flex gap-4">
                  <div
                    class="w-10 h-10 rounded-lg bg-muted/50 flex items-center justify-center text-lg shrink-0"
                  >
                    {connector.displayName.charAt(0)}
                  </div>
                  <div>
                    <Card.Title class="text-lg flex items-center gap-2">
                      {connector.displayName}
                    </Card.Title>
                    <div class="mt-1">
                      <Badge variant={getCategoryVariant(connector.category)}>
                        {connector.category}
                      </Badge>
                    </div>
                  </div>
                </div>

                <Button
                  variant={isSelected(connector.type) ? "default" : "outline"}
                  size="icon"
                  class="shrink-0 rounded-full w-8 h-8"
                  onclick={(e) => {
                    e.stopPropagation();
                    handleToggleClick(connector);
                  }}
                >
                  {#if isSelected(connector.type)}
                    <Check size={14} strokeWidth={3} />
                  {/if}
                  <span class="sr-only">
                    {isSelected(connector.type) ? "Deselect" : "Select"}
                    {connector.displayName}
                  </span>
                </Button>
              </div>
            </Card.Header>

            <Card.Content class="flex-1 pb-3">
              <Card.Description>
                {connector.description}
              </Card.Description>
            </Card.Content>

            {#if isSelected(connector.type) && connector.fields.length > 0}
              <Card.Footer class="pt-0">
                <Button
                  variant="secondary"
                  size="sm"
                  class="w-full gap-2"
                  onclick={(e) => openConfig(connector, e)}
                >
                  <Settings2 size={14} />
                  Configure
                </Button>
              </Card.Footer>
            {/if}
          </Card.Root>
        {/each}
      </div>

      <div
        class="flex items-center justify-between mt-8 sticky bottom-8 bg-background/80 backdrop-blur-sm p-4 rounded-xl border border-border/50 shadow-lg"
      >
        <p class="text-sm text-muted-foreground font-medium pl-2">
          {wizardStore.selectedConnectors.length} connector{wizardStore
            .selectedConnectors.length !== 1
            ? "s"
            : ""} selected
        </p>
        <Button href="/download" class="gap-2 shadow-sm" size="lg">
          Review & Download
          <ChevronRight size={20} />
        </Button>
      </div>
    {:catch error}
      <div
        class="p-4 rounded-lg bg-destructive/10 border border-destructive/50 text-destructive mb-6"
      >
        <p class="font-medium">Could not load connectors</p>
        <p class="text-sm opacity-80">{error.message}</p>
      </div>
    {/await}
  </svelte:boundary>
</div>

<Dialog.Root bind:open={isConfigDialogOpen}>
  <Dialog.Content class="sm:max-w-[425px]">
    {#if configuringConnector}
      <Dialog.Header>
        <Dialog.Title>Configure {configuringConnector.displayName}</Dialog.Title
        >
        <Dialog.Description>
          {configuringConnector.description}
        </Dialog.Description>
      </Dialog.Header>

      <div class="grid gap-6 py-4">
        {#each configuringConnector.fields as field}
          <div class="grid gap-2">
            <Label for={field.envVar} class="text-sm font-medium">
              {field.name}
              {#if field.required}
                <span class="text-destructive">*</span>
              {/if}
            </Label>

            {#if field.type === "boolean"}
              <div class="flex items-center space-x-2">
                <Switch
                  id={field.envVar}
                  checked={configValues[field.envVar] === "true"}
                  onCheckedChange={(v) =>
                    (configValues[field.envVar] = v.toString())}
                />
                <Label
                  for={field.envVar}
                  class="font-normal text-muted-foreground"
                >
                  {field.description}
                </Label>
              </div>
            {:else if field.type === "select" && field.options}
              <Select.Root
                type="single"
                value={configValues[field.envVar]}
                onValueChange={(v) => (configValues[field.envVar] = v)}
              >
                <Select.Trigger id={field.envVar}>
                  {#if configValues[field.envVar]}
                    {configValues[field.envVar]}
                  {:else}
                    <span class="text-muted-foreground">Select an option</span>
                  {/if}
                </Select.Trigger>
                <Select.Content>
                  {#each field.options as option}
                    <Select.Item value={option} label={option} />
                  {/each}
                </Select.Content>
              </Select.Root>
              <p class="text-[0.8rem] text-muted-foreground">
                {field.description}
              </p>
            {:else}
              <Input
                id={field.envVar}
                type={field.type === "password"
                  ? "password"
                  : field.type === "number"
                    ? "number"
                    : "text"}
                value={configValues[field.envVar] || ""}
                oninput={(e) =>
                  (configValues[field.envVar] = e.currentTarget.value)}
                placeholder={field.default || ""}
                required={field.required}
              />
              <p class="text-[0.8rem] text-muted-foreground">
                {field.description}
              </p>
            {/if}
          </div>
        {/each}

        {#if configuringConnector.fields.length === 0}
          <p class="text-sm text-muted-foreground italic">
            No configuration required for this connector.
          </p>
        {/if}
      </div>

      <Dialog.Footer>
        <Button variant="outline" onclick={() => (isConfigDialogOpen = false)}
          >Cancel</Button
        >
        <Button onclick={handleSaveConfig}>Save Changes</Button>
      </Dialog.Footer>
    {:else}
      <div class="p-4 flex justify-center py-8">
        <div
          class="animate-spin w-8 h-8 border-2 border-primary border-t-transparent rounded-full"
        ></div>
      </div>
    {/if}
  </Dialog.Content>
</Dialog.Root>
