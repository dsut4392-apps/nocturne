<script lang="ts">
  import * as Dialog from "$lib/components/ui/dialog";
  import { Button } from "$lib/components/ui/button";
  import { Input } from "$lib/components/ui/input";
  import { Label } from "$lib/components/ui/label";
  import { Badge } from "$lib/components/ui/badge";
  import { Pencil, Trash2, Plus } from "lucide-svelte";
  import {
    TreatmentFoodInputMode,
    type TreatmentFood,
    type TreatmentFoodBreakdown,
    type TreatmentFoodRequest,
  } from "$lib/api";
  import {
    addTreatmentFood,
    deleteTreatmentFood,
    getTreatmentFoodBreakdown,
    updateTreatmentFood,
  } from "$lib/data/treatment-foods.remote";
  import TreatmentFoodSelectorDialog from "./TreatmentFoodSelectorDialog.svelte";

  interface Props {
    treatmentId?: string;
  }

  let { treatmentId }: Props = $props();

  let breakdown = $state<TreatmentFoodBreakdown | null>(null);
  let isLoading = $state(false);
  let loadError = $state<string | null>(null);
  let showAddFood = $state(false);
  let showAddOther = $state(false);
  let showEdit = $state(false);

  let editEntry = $state<TreatmentFood | null>(null);
  let editInputMode = $state<TreatmentFoodInputMode>(
    TreatmentFoodInputMode.Portions
  );
  let editPortions = $state(1);
  let editCarbs = $state<number | undefined>(undefined);
  let editOffset = $state<number | undefined>(0);
  let editNote = $state("");

  $effect(() => {
    if (!treatmentId) {
      breakdown = null;
      return;
    }
    void loadBreakdown(treatmentId);
  });

  $effect(() => {
    if (showAddOther) {
      editCarbs = undefined;
      editOffset = 0;
      editNote = "";
    }
  });

  async function loadBreakdown(id: string) {
    isLoading = true;
    loadError = null;
    try {
      breakdown = await getTreatmentFoodBreakdown(id);
    } catch (err) {
      console.error("Failed to load food breakdown:", err);
      loadError = "Unable to load food breakdown.";
    } finally {
      isLoading = false;
    }
  }

  async function handleAddFood(request: TreatmentFoodRequest) {
    if (!treatmentId) return;
    try {
      const updated = await addTreatmentFood({ treatmentId, request });
      breakdown = updated;
      showAddFood = false;
    } catch (err) {
      console.error("Failed to add food entry:", err);
    }
  }

  async function handleAddOther() {
    if (!treatmentId) return;
    const request: TreatmentFoodRequest = {
      foodId: undefined,
      carbs: editCarbs,
      timeOffsetMinutes: editOffset,
      note: editNote.trim() || undefined,
      inputMode: TreatmentFoodInputMode.Carbs,
    };
    try {
      const updated = await addTreatmentFood({ treatmentId, request });
      breakdown = updated;
      showAddOther = false;
      resetEditor();
    } catch (err) {
      console.error("Failed to add other entry:", err);
    }
  }

  function openEdit(entry: TreatmentFood) {
    editEntry = entry;
    editInputMode = entry.foodId
      ? TreatmentFoodInputMode.Portions
      : TreatmentFoodInputMode.Carbs;
    editPortions = entry.portions ?? 1;
    editCarbs = entry.carbs ?? undefined;
    editOffset = entry.timeOffsetMinutes ?? 0;
    editNote = entry.note ?? "";
    showEdit = true;
  }

  function resetEditor() {
    editEntry = null;
    editPortions = 1;
    editCarbs = undefined;
    editOffset = 0;
    editNote = "";
    editInputMode = TreatmentFoodInputMode.Portions;
    showEdit = false;
  }

  async function handleUpdateEntry() {
    if (!treatmentId || !editEntry?.id) return;

    const request: TreatmentFoodRequest = {
      foodId: editEntry.foodId ?? undefined,
      timeOffsetMinutes: editOffset,
      note: editNote.trim() || undefined,
      inputMode: editInputMode,
    };

    if (editEntry.foodId) {
      if (editInputMode === TreatmentFoodInputMode.Portions) {
        request.portions = editPortions;
      } else {
        request.carbs = editCarbs;
      }
    } else {
      request.carbs = editCarbs;
      request.inputMode = TreatmentFoodInputMode.Carbs;
    }

    try {
      const updated = await updateTreatmentFood({
        treatmentId,
        entryId: editEntry.id,
        request,
      });

      breakdown = updated;
      resetEditor();
    } catch (err) {
      console.error("Failed to update food entry:", err);
    }
  }

  async function handleDelete(entry: TreatmentFood) {
    if (!treatmentId || !entry.id) return;
    try {
      const updated = await deleteTreatmentFood({
        treatmentId,
        entryId: entry.id,
      });
      breakdown = updated;
    } catch (err) {
      console.error("Failed to delete food entry:", err);
    }
  }

  const isOtherEdit = $derived(editEntry && !editEntry.foodId);
</script>

<div class="rounded-lg border p-4 space-y-4">
  <div class="flex items-center justify-between">
    <div class="space-y-1">
      <div class="text-sm font-semibold">Food Breakdown</div>
      <div class="text-xs text-muted-foreground">
        Add foods to match carbs when it helps. Partial attribution is fine.
      </div>
    </div>
    <div class="flex items-center gap-2">
      <Button
        type="button"
        size="sm"
        variant="outline"
        onclick={() => (showAddOther = true)}
      >
        Add Other
      </Button>
      <Button type="button" size="sm" onclick={() => (showAddFood = true)}>
        <Plus class="mr-1 h-4 w-4" />
        Add Food
      </Button>
    </div>
  </div>

  {#if isLoading}
    <div class="text-sm text-muted-foreground">Loading breakdown...</div>
  {:else if loadError}
    <div class="text-sm text-destructive">{loadError}</div>
  {:else if breakdown}
    <div class="space-y-3">
      <div class="flex flex-wrap gap-2 text-xs">
        <Badge variant="secondary">
          Attributed {breakdown.attributedCarbs}g
        </Badge>
        <Badge variant="outline">
          Unspecified {breakdown.unspecifiedCarbs}g
        </Badge>
      </div>

      {#if !breakdown.foods || breakdown.foods.length === 0}
        <div
          class="rounded-md border border-dashed p-4 text-sm text-muted-foreground"
        >
          No foods added yet.
        </div>
      {:else}
        <div class="space-y-2">
          {#each breakdown.foods as entry (entry.id)}
            <div class="flex items-start justify-between rounded-md border p-3">
              <div class="space-y-1">
                <div class="font-medium">
                  {entry.foodName ?? "Other"}
                </div>
                <div class="text-xs text-muted-foreground">
                  {entry.foodId ? `${entry.portions} portions` : "Other"} - {entry.carbs}g
                  carbs
                  {entry.timeOffsetMinutes
                    ? ` - ${entry.timeOffsetMinutes} min`
                    : ""}
                  {entry.note ? ` - ${entry.note}` : ""}
                </div>
              </div>
              <div class="flex items-center gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  onclick={() => openEdit(entry)}
                >
                  <Pencil class="h-4 w-4" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  class="text-destructive"
                  onclick={() => handleDelete(entry)}
                >
                  <Trash2 class="h-4 w-4" />
                </Button>
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </div>
  {/if}
</div>

<TreatmentFoodSelectorDialog
  bind:open={showAddFood}
  onOpenChange={(value) => (showAddFood = value)}
  onSubmit={handleAddFood}
/>

<Dialog.Root
  bind:open={showAddOther}
  onOpenChange={(value) => (showAddOther = value)}
>
  <Dialog.Content class="max-w-md">
    <Dialog.Header>
      <Dialog.Title>Add Other</Dialog.Title>
      <Dialog.Description>
        Log carbs without a food entry. Notes are optional.
      </Dialog.Description>
    </Dialog.Header>

    <div class="space-y-4">
      <div class="space-y-2">
        <Label for="other-carbs">Carbs (g)</Label>
        <Input
          id="other-carbs"
          type="number"
          step="0.1"
          min="0"
          bind:value={editCarbs}
        />
      </div>
      <div class="space-y-2">
        <Label for="other-offset">Time offset (min)</Label>
        <Input
          id="other-offset"
          type="number"
          step="1"
          bind:value={editOffset}
        />
      </div>
      <div class="space-y-2">
        <Label for="other-note">Note</Label>
        <Input id="other-note" bind:value={editNote} />
      </div>
    </div>

    <Dialog.Footer class="gap-2">
      <Button
        type="button"
        variant="outline"
        onclick={() => (showAddOther = false)}
      >
        Cancel
      </Button>
      <Button type="button" onclick={handleAddOther}>Add Other</Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>

<Dialog.Root
  bind:open={showEdit}
  onOpenChange={(value) => !value && resetEditor()}
>
  <Dialog.Content class="max-w-md">
    <Dialog.Header>
      <Dialog.Title>Edit Food Entry</Dialog.Title>
      <Dialog.Description>
        Update portions or carbs. Values sync after saving.
      </Dialog.Description>
    </Dialog.Header>

    <div class="space-y-4">
      {#if editEntry}
        <div class="rounded-md border p-3 text-sm">
          <div class="font-medium">{editEntry.foodName ?? "Other"}</div>
          {#if editEntry.foodId}
            <div class="text-xs text-muted-foreground">
              {editEntry.carbsPerPortion ?? "-"}g per portion
            </div>
          {/if}
        </div>

        {#if !isOtherEdit}
          <div class="flex gap-2">
            <Button
              type="button"
              size="sm"
              variant={editInputMode === TreatmentFoodInputMode.Portions
                ? "default"
                : "outline"}
              onclick={() => (editInputMode = TreatmentFoodInputMode.Portions)}
            >
              Portions
            </Button>
            <Button
              type="button"
              size="sm"
              variant={editInputMode === TreatmentFoodInputMode.Carbs
                ? "default"
                : "outline"}
              onclick={() => (editInputMode = TreatmentFoodInputMode.Carbs)}
            >
              Carbs
            </Button>
          </div>
        {/if}

        <div class="grid gap-4 md:grid-cols-2">
          {#if !isOtherEdit}
            <div class="space-y-2">
              <Label for="edit-portions">Portions</Label>
              <Input
                id="edit-portions"
                type="number"
                step="0.1"
                min="0"
                bind:value={editPortions}
                disabled={editInputMode !== TreatmentFoodInputMode.Portions}
              />
            </div>
          {/if}
          <div class="space-y-2">
            <Label for="edit-carbs">Carbs (g)</Label>
            <Input
              id="edit-carbs"
              type="number"
              step="0.1"
              min="0"
              bind:value={editCarbs}
              disabled={!isOtherEdit &&
                editInputMode !== TreatmentFoodInputMode.Carbs}
            />
          </div>
        </div>

        <div class="grid gap-4 md:grid-cols-2">
          <div class="space-y-2">
            <Label for="edit-offset">Time offset (min)</Label>
            <Input
              id="edit-offset"
              type="number"
              step="1"
              bind:value={editOffset}
            />
          </div>
          <div class="space-y-2">
            <Label for="edit-note">Note</Label>
            <Input id="edit-note" bind:value={editNote} />
          </div>
        </div>
      {/if}
    </div>

    <Dialog.Footer class="gap-2">
      <Button type="button" variant="outline" onclick={resetEditor}>
        Cancel
      </Button>
      <Button type="button" onclick={handleUpdateEntry}>Save</Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
