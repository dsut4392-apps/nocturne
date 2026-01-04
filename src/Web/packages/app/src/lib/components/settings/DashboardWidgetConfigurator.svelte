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
  import {
    WIDGET_DEFINITIONS,
    DEFAULT_TOP_WIDGETS,
    type WidgetId,
    type WidgetDefinition,
  } from "$lib/types/dashboard-widgets";
  import {
    GripVertical,
    LayoutGrid,
    Plus,
    X,
    TrendingUp,
    Clock,
    Wifi,
    UtensilsCrossed,
    ListChecks,
    BarChart3,
    CalendarDays,
  } from "lucide-svelte";
  import type { Component } from "svelte";

  interface Props {
    /** Currently selected widget IDs (ordered) */
    value: WidgetId[];
    /** Callback when widgets change */
    onchange?: (widgets: WidgetId[]) => void;
    /** Maximum number of widgets allowed */
    maxWidgets?: number;
  }

  let { value = [...DEFAULT_TOP_WIDGETS], onchange, maxWidgets = 3 }: Props = $props();

  // Local state for drag operations
  let draggedIndex: number | null = $state(null);
  let dragOverIndex: number | null = $state(null);

  // Icon mapping
  const iconMap: Record<string, Component> = {
    TrendingUp,
    Clock,
    Wifi,
    UtensilsCrossed,
    ListChecks,
    BarChart3,
    CalendarDays,
  };

  // Get icon component for a widget
  function getIcon(iconName: string): Component {
    return iconMap[iconName] || LayoutGrid;
  }

  // Available widgets (not currently selected)
  const availableWidgets = $derived(
    WIDGET_DEFINITIONS.filter((w) => !value.includes(w.id))
  );

  // Selected widget definitions in order
  const selectedWidgets = $derived(
    value
      .map((id) => WIDGET_DEFINITIONS.find((w) => w.id === id))
      .filter((w): w is WidgetDefinition => w !== undefined)
  );

  // Can add more widgets?
  const canAddMore = $derived(value.length < maxWidgets);

  // Drag handlers for reordering
  function handleDragStart(event: DragEvent, index: number) {
    draggedIndex = index;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = "move";
      event.dataTransfer.setData("text/plain", String(index));
    }
  }

  function handleDragOver(event: DragEvent, index: number) {
    event.preventDefault();
    if (draggedIndex !== null && draggedIndex !== index) {
      dragOverIndex = index;
    }
  }

  function handleDragLeave() {
    dragOverIndex = null;
  }

  function handleDrop(event: DragEvent, targetIndex: number) {
    event.preventDefault();

    if (draggedIndex !== null && draggedIndex !== targetIndex) {
      const newValue = [...value];
      const [removed] = newValue.splice(draggedIndex, 1);
      newValue.splice(targetIndex, 0, removed);
      onchange?.(newValue);
    }

    draggedIndex = null;
    dragOverIndex = null;
  }

  function handleDragEnd() {
    draggedIndex = null;
    dragOverIndex = null;
  }

  // Add widget
  function addWidget(id: WidgetId) {
    if (value.length < maxWidgets) {
      onchange?.([...value, id]);
    }
  }

  // Remove widget
  function removeWidget(index: number) {
    const newValue = [...value];
    newValue.splice(index, 1);
    onchange?.(newValue);
  }

  // Category colors
  function getCategoryColor(category: WidgetDefinition["category"]): string {
    switch (category) {
      case "glucose":
        return "bg-green-500/20 text-green-400";
      case "meals":
        return "bg-yellow-500/20 text-yellow-400";
      case "device":
        return "bg-blue-500/20 text-blue-400";
      case "status":
        return "bg-purple-500/20 text-purple-400";
      default:
        return "bg-gray-500/20 text-gray-400";
    }
  }
</script>

<Card>
  <CardHeader>
    <CardTitle class="flex items-center gap-2">
      <LayoutGrid class="h-5 w-5" />
      Dashboard Widgets
    </CardTitle>
    <CardDescription>
      Customize the {maxWidgets} widgets shown above the glucose chart. Drag to reorder.
    </CardDescription>
  </CardHeader>
  <CardContent class="space-y-4">
    <!-- Selected widgets (draggable) -->
    <div class="space-y-2">
      <label class="text-sm font-medium">Active Widgets</label>
      <div class="space-y-2">
        {#each selectedWidgets as widget, index (widget.id)}
          {@const Icon = getIcon(widget.icon)}
          <div
            class="flex items-center gap-2 p-3 rounded-lg border bg-card transition-all
              {dragOverIndex === index ? 'border-primary bg-accent' : 'border-border'}
              {draggedIndex === index ? 'opacity-50' : ''}"
            draggable="true"
            ondragstart={(e) => handleDragStart(e, index)}
            ondragover={(e) => handleDragOver(e, index)}
            ondragleave={handleDragLeave}
            ondrop={(e) => handleDrop(e, index)}
            ondragend={handleDragEnd}
            role="listitem"
          >
            <GripVertical class="h-4 w-4 text-muted-foreground cursor-grab" />
            <Badge variant="outline" class="w-6 h-6 p-0 justify-center">
              {index + 1}
            </Badge>
            <Icon class="h-4 w-4 text-muted-foreground" />
            <div class="flex-1 min-w-0">
              <div class="font-medium text-sm">{widget.name}</div>
              <div class="text-xs text-muted-foreground truncate">
                {widget.description}
              </div>
            </div>
            <Badge variant="secondary" class="text-xs {getCategoryColor(widget.category)}">
              {widget.category}
            </Badge>
            <Button
              variant="ghost"
              size="sm"
              class="h-8 w-8 p-0 text-muted-foreground hover:text-destructive"
              onclick={() => removeWidget(index)}
            >
              <X class="h-4 w-4" />
            </Button>
          </div>
        {/each}

        {#if selectedWidgets.length === 0}
          <div class="text-center py-8 text-muted-foreground border border-dashed rounded-lg">
            <p class="text-sm">No widgets selected</p>
            <p class="text-xs">Add widgets from the list below</p>
          </div>
        {/if}
      </div>
    </div>

    <!-- Available widgets to add -->
    {#if availableWidgets.length > 0}
      <div class="space-y-2">
        <label class="text-sm font-medium text-muted-foreground">
          Available Widgets {#if !canAddMore}(max {maxWidgets} reached){/if}
        </label>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
          {#each availableWidgets as widget (widget.id)}
            {@const Icon = getIcon(widget.icon)}
            <button
              type="button"
              class="flex items-center gap-2 p-2 rounded-lg border border-dashed text-left transition-colors
                {canAddMore
                  ? 'hover:border-primary hover:bg-accent cursor-pointer'
                  : 'opacity-50 cursor-not-allowed'}"
              onclick={() => canAddMore && addWidget(widget.id)}
              disabled={!canAddMore}
            >
              <Plus class="h-4 w-4 text-muted-foreground" />
              <Icon class="h-4 w-4 text-muted-foreground" />
              <div class="flex-1 min-w-0">
                <div class="font-medium text-sm">{widget.name}</div>
              </div>
              <Badge variant="outline" class="text-xs {getCategoryColor(widget.category)}">
                {widget.category}
              </Badge>
            </button>
          {/each}
        </div>
      </div>
    {/if}

    <p class="text-xs text-muted-foreground">
      Changes are saved automatically when you leave this page.
    </p>
  </CardContent>
</Card>
