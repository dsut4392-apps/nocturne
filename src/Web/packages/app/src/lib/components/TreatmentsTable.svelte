<script lang="ts">
  import {
    Table,
    TableBody,
    TableCaption,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
  } from "$lib/components/ui/table";
  import { Button } from "$lib/components/ui/button";
  import { Edit, Trash2 } from "lucide-svelte";
  import { formatNotes } from "$lib/utils/formatting";
  import { getEventTypeStyle } from "$lib/constants/treatment-categories";
  import { Checkbox } from "$lib/components/ui/checkbox";
  import type { Treatment } from "$lib/api";
  import { formatDateTime } from "$lib/utils/formatting";

  interface Props {
    treatments: Treatment[];
    onEdit: (treatment: Treatment) => void;
    onDelete: (treatment: Treatment) => void;
    onBulkDelete?: (treatments: Treatment[]) => void;
  }

  let { treatments, onEdit, onDelete, onBulkDelete }: Props = $props();

  // Selection state
  let selectedTreatments = $state<Set<string>>(new Set());
  let isAllSelected = $derived(
    selectedTreatments.size === treatments.length && treatments.length > 0
  );
  let isSomeSelected = $derived(
    selectedTreatments.size > 0 && selectedTreatments.size < treatments.length
  );

  // Selection functions
  function toggleTreatmentSelection(treatmentId: string) {
    const newSelected = new Set(selectedTreatments);
    if (newSelected.has(treatmentId)) {
      newSelected.delete(treatmentId);
    } else {
      newSelected.add(treatmentId);
    }
    selectedTreatments = newSelected;
  }

  function toggleAllTreatments() {
    if (isAllSelected) {
      selectedTreatments = new Set();
    } else {
      selectedTreatments = new Set(
        treatments
          .map((t) => t._id)
          .filter((id): id is string => id !== undefined)
      );
    }
  }

  function handleBulkDelete() {
    const selectedTreatmentObjects = treatments.filter(
      (t) => t._id !== undefined && selectedTreatments.has(t._id)
    );
    if (onBulkDelete && selectedTreatmentObjects.length > 0) {
      onBulkDelete(selectedTreatmentObjects);
    }
  }
  const allColumns = [
    { key: "select", label: "Select" },
    { key: "time", label: "Time" },
    { key: "eventType", label: "Event Type" },
    { key: "bloodGlucose", label: "Blood Glucose" },
    { key: "insulin", label: "Insulin" },
    { key: "carbs", label: "Carbs/Food/Time" },
    { key: "protein", label: "Protein" },
    { key: "fat", label: "Fat" },
    { key: "duration", label: "Duration" },
    { key: "percent", label: "Percent" },
    { key: "basalValue", label: "Basal Value" },
    { key: "profile", label: "Profile" },
    { key: "enteredBy", label: "Entered By" },
    { key: "notes", label: "Notes" },
    { key: "actions", label: "Actions" },
  ];
  // Function to check if a column has any data
  function hasColumnData(columnKey: string, treatments: Treatment[]): boolean {
    if (
      columnKey === "select" ||
      columnKey === "time" ||
      columnKey === "actions"
    ) {
      return true; // Always show select, time and actions columns
    }

    return treatments.some((treatment) => {
      switch (columnKey) {
        case "eventType":
          return treatment.eventType && treatment.eventType.trim() !== "";
        case "bloodGlucose":
          return treatment.glucose !== undefined && treatment.glucose !== null;
        case "insulin":
          return treatment.insulin !== undefined && treatment.insulin !== null;
        case "carbs":
          return (
            (treatment.carbs !== undefined && treatment.carbs !== null) ||
            (treatment.absorptionTime !== undefined &&
              treatment.absorptionTime !== null)
          );
        case "protein":
          return treatment.protein !== undefined && treatment.protein !== null;
        case "fat":
          return treatment.fat !== undefined && treatment.fat !== null;
        case "duration":
          return (
            treatment.duration !== undefined && treatment.duration !== null
          );
        case "percent":
          return treatment.percent !== undefined && treatment.percent !== null;
        case "basalValue":
          return (
            (treatment.absolute !== undefined && treatment.absolute !== null) ||
            (treatment.rate !== undefined && treatment.rate !== null)
          );
        case "profile":
          return treatment.profile && treatment.profile.trim() !== "";
        case "enteredBy":
          return treatment.enteredBy && treatment.enteredBy.trim() !== "";
        case "notes":
          return (
            (treatment.notes && treatment.notes.trim() !== "") ||
            (treatment.reason && treatment.reason.trim() !== "")
          );
        default:
          return false;
      }
    });
  }

  // Filter columns to only show those with data
  const visibleColumns = $derived(
    allColumns.filter((column) => hasColumnData(column.key, treatments))
  );

  // Simple value formatters for protein, insulin, carbs, fat
  function formatTruthy(value: any): string {
    return value ? value : "-";
  }

  // Utility function to get event type styling
</script>

<section class="overflow-auto max-h-[70vh]">
  {#if selectedTreatments.size > 0}
    <div
      class="mb-4 p-3 bg-muted/50 rounded-md border flex items-center justify-between"
    >
      <span class="text-sm font-medium">
        {selectedTreatments.size} treatment{selectedTreatments.size !== 1
          ? "s"
          : ""} selected
      </span>
      <div class="flex gap-2">
        <Button
          variant="outline"
          size="sm"
          onclick={() => (selectedTreatments = new Set())}
        >
          Clear Selection
        </Button>
        <Button variant="destructive" size="sm" onclick={handleBulkDelete}>
          <Trash2 size={16} class="mr-2" />
          Delete Selected
        </Button>
      </div>
    </div>
  {/if}

  <Table class="relative">
    <TableCaption class="text-sm text-muted-foreground mt-2">
      {treatments.length} treatment{treatments.length !== 1 ? "s" : ""} found
    </TableCaption>
    <TableHeader>
      <TableRow class="bg-background border-b border-border">
        {#each visibleColumns as column}
          <TableHead
            class="px-4 py-3 text-left text-sm font-medium sticky top-0 bg-background z-10"
          >
            {#if column.key === "select"}
              <Checkbox
                checked={isAllSelected}
                indeterminate={isSomeSelected}
                onCheckedChange={toggleAllTreatments}
                aria-label="Select all treatments"
              />
            {:else}
              {column.label}
            {/if}
          </TableHead>
        {/each}
      </TableRow>
    </TableHeader>
    <TableBody class="">
      {#each treatments as treatment (treatment._id)}
        <TableRow class="border-t border-border hover:bg-muted/50">
          {#each visibleColumns as column}
            <TableCell
              class={`px-4 py-3 text-sm ${column.key === "carbs" ? "max-w-48 truncate" : ""} ${column.key === "notes" ? "max-w-64 truncate" : ""}`}
              title={column.key === "carbs"
                ? formatTruthy(treatment)
                : column.key === "notes"
                  ? formatNotes(treatment)
                  : undefined}
            >
              {#if column.key === "select"}
                <Checkbox
                  checked={treatment._id !== undefined &&
                    selectedTreatments.has(treatment._id)}
                  onCheckedChange={() => {
                    if (treatment._id !== undefined) {
                      toggleTreatmentSelection(treatment._id);
                    }
                  }}
                  aria-label={`Select treatment ${treatment._id}`}
                />
              {:else if column.key === "time"}
                {formatDateTime(treatment.created_at)}
              {:else if column.key === "eventType"}
                {#if treatment.eventType}
                  {@const style = getEventTypeStyle(treatment.eventType)}
                  <span
                    class="px-2 py-1 text-xs rounded-full border {style.bgClass} {style.colorClass} {style.borderClass}"
                    title={treatment.eventType}
                  >
                    {treatment.eventType}
                  </span>
                {:else}
                  <span class="text-muted-foreground">-</span>
                {/if}
              {:else if column.key === "bloodGlucose"}
                <!-- {formatBloodGlucose(treatment)} -->
              {:else if ["insulin", "carbs", "protein", "fat"].includes(column.key)}
                {formatTruthy(treatment.insulin)}
              {:else if column.key === "duration"}
                {treatment.duration !== undefined && treatment.duration !== null
                  ? `${treatment.duration.toFixed(0)} min`
                  : "-"}
              {:else if column.key === "percent"}
                {treatment.percent !== undefined && treatment.percent !== null
                  ? `${treatment.percent}%`
                  : "-"}
              {:else if column.key === "basalValue"}
                {treatment.absolute !== undefined && treatment.absolute !== null
                  ? formatTruthy(treatment.absolute)
                  : treatment.rate !== undefined && treatment.rate !== null
                    ? `${treatment.rate} U/hr`
                    : "-"}
              {:else if column.key === "profile"}
                {treatment.profile || "-"}
              {:else if column.key === "enteredBy"}
                {treatment.enteredBy || "-"}
              {:else if column.key === "notes"}
                {formatNotes(treatment)}
              {:else if column.key === "actions"}
                <div class="flex gap-2">
                  <Button
                    variant="ghost"
                    size="sm"
                    onclick={() => onEdit(treatment)}
                    class="h-8 w-8 p-0"
                    title="Edit treatment"
                  >
                    <Edit size={16} />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onclick={() => onDelete(treatment)}
                    class="h-8 w-8 p-0 text-destructive hover:text-destructive"
                    title="Delete treatment"
                  >
                    <Trash2 size={16} />
                  </Button>
                </div>
              {/if}
            </TableCell>
          {/each}
        </TableRow>
      {/each}
    </TableBody>
  </Table>
</section>
