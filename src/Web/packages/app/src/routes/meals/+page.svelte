<script lang="ts">
  import DateRangePicker from "$lib/components/ui/date-range-picker.svelte";
  import { Button } from "$lib/components/ui/button";
  import { Badge } from "$lib/components/ui/badge";
  import * as Card from "$lib/components/ui/card";
  import { Calendar, Utensils } from "lucide-svelte";
  import type { MealTreatment, Treatment } from "$lib/api";
  import { formatDateTime } from "$lib/utils/formatting";
  import { getMealTreatments } from "$lib/data/treatment-foods.remote";
  import {
    updateTreatment,
    deleteTreatment,
  } from "$lib/data/treatments.remote";
  import { toast } from "svelte-sonner";
  import { invalidateAll } from "$app/navigation";
  import { TreatmentEditDialog } from "$lib/components/treatments";

  let dateRange = $state<{ from?: string; to?: string }>({});
  let filterMode = $state<"all" | "unattributed">("all");

  let showEditDialog = $state(false);
  let treatmentToEdit = $state<Treatment | null>(null);
  let isSaving = $state(false);

  function handleDateChange(params: { from?: string; to?: string }) {
    dateRange = { from: params.from, to: params.to };
  }

  const queryParams = $derived({
    from: dateRange.from,
    to: dateRange.to,
    attributed: filterMode === "unattributed" ? false : undefined,
  });

  const mealsQuery = $derived(getMealTreatments(queryParams));
  const meals = $derived<MealTreatment[]>(mealsQuery.current ?? []);

  function openEdit(treatment: Treatment) {
    treatmentToEdit = treatment;
    showEditDialog = true;
  }

  function handleEditClose() {
    showEditDialog = false;
    treatmentToEdit = null;
  }

  async function handleEditSave(updatedTreatment: Treatment) {
    isSaving = true;
    try {
      await updateTreatment({ ...updatedTreatment });
      toast.success("Treatment updated");
      showEditDialog = false;
      treatmentToEdit = null;
      getMealTreatments(queryParams).refresh();
      invalidateAll();
    } catch (err) {
      console.error("Update error:", err);
      toast.error("Failed to update treatment");
    } finally {
      isSaving = false;
    }
  }

  async function handleEditDelete(treatmentId: string) {
    isSaving = true;
    try {
      await deleteTreatment(treatmentId);
      toast.success("Treatment deleted");
      showEditDialog = false;
      treatmentToEdit = null;
      getMealTreatments(queryParams).refresh();
      invalidateAll();
    } catch (err) {
      console.error("Delete error:", err);
      toast.error("Failed to delete treatment");
    } finally {
      isSaving = false;
    }
  }
</script>

<svelte:head>
  <title>Meals - Nocturne</title>
  <meta
    name="description"
    content="Review carb treatments and add food breakdowns for better meal documentation"
  />
</svelte:head>

<div class="container mx-auto space-y-6 px-4 py-6">
  <div class="space-y-2 text-center">
    <div
      class="flex items-center justify-center gap-2 text-sm text-muted-foreground"
    >
      <Calendar class="h-4 w-4" />
      <span>Meals</span>
    </div>
    <h1 class="text-3xl font-bold">Meal Attribution</h1>
    <p class="text-muted-foreground">
      Pair carb treatments with foods when you want more detail.
    </p>
  </div>

  <DateRangePicker
    title="Meal range"
    defaultDays={1}
    onDateChange={handleDateChange}
  />

  <div class="flex items-center gap-2">
    <Button
      type="button"
      variant={filterMode === "all" ? "default" : "outline"}
      onclick={() => (filterMode = "all")}
    >
      All
    </Button>
    <Button
      type="button"
      variant={filterMode === "unattributed" ? "default" : "outline"}
      onclick={() => (filterMode = "unattributed")}
    >
      Unattributed only
    </Button>
  </div>

  <Card.Root>
    <Card.Content class="p-0">
      {#if meals.length === 0}
        <div class="p-6 text-center text-sm text-muted-foreground">
          No meals found in this range.
        </div>
      {:else}
        <div class="divide-y">
          {#each meals as meal (meal.treatment?._id)}
            <div
              class="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between"
            >
              <div class="space-y-2">
                <div class="flex items-center gap-2">
                  <Utensils class="h-4 w-4 text-amber-500" />
                  <div class="font-medium">
                    {meal.treatment?.eventType ?? "Meal"}
                  </div>
                  <Badge variant={meal.isAttributed ? "secondary" : "outline"}>
                    {meal.isAttributed ? "Attributed" : "Unattributed"}
                  </Badge>
                </div>
                <div class="text-xs text-muted-foreground">
                  {formatDateTime(meal.treatment?.created_at)}
                </div>
                <div class="flex flex-wrap gap-2 text-xs text-muted-foreground">
                  <span>{meal.attributedCarbs}g attributed</span>
                  <span>-</span>
                  <span>{meal.unspecifiedCarbs}g unspecified</span>
                </div>
              </div>
              <div class="flex items-center gap-2">
                {#if meal.treatment}
                  <Button
                    type="button"
                    variant="outline"
                    onclick={() => meal.treatment && openEdit(meal.treatment)}
                  >
                    Edit breakdown
                  </Button>
                {/if}
              </div>
            </div>
          {/each}
        </div>
      {/if}
    </Card.Content>
  </Card.Root>
</div>

<TreatmentEditDialog
  bind:open={showEditDialog}
  treatment={treatmentToEdit}
  availableEventTypes={[]}
  isLoading={isSaving}
  onClose={handleEditClose}
  onSave={handleEditSave}
  onDelete={handleEditDelete}
/>
