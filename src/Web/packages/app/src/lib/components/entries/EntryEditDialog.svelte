<script lang="ts">
  import type { Entry } from "$lib/api";
  import * as Dialog from "$lib/components/ui/dialog";
  import { Button } from "$lib/components/ui/button";
  import { Input } from "$lib/components/ui/input";
  import { Label } from "$lib/components/ui/label";
  import { Textarea } from "$lib/components/ui/textarea";

  interface Props {
    open: boolean;
    entry: Entry | null;
    isLoading?: boolean;
    onClose: () => void;
    onSave: (entry: Entry) => void;
  }

  let {
    open = $bindable(),
    entry,
    isLoading = false,
    onClose,
    onSave,
  }: Props = $props();

  let formData = $state<Partial<Entry>>({});

  $effect(() => {
    if (open && entry) {
      formData = { ...entry };
    }
  });

  function handleSubmit() {
    if (entry && formData) {
      onSave({ ...entry, ...formData } as Entry);
    }
  }
</script>

<Dialog.Root bind:open onOpenChange={(v) => !v && onClose()}>
  <Dialog.Content>
    <Dialog.Header>
      <Dialog.Title>Edit Entry</Dialog.Title>
    </Dialog.Header>

    <div class="grid gap-4 py-4">
      <div class="grid grid-cols-4 items-center gap-4">
        <Label class="text-right">Glucose (mg/dL)</Label>
        <Input type="number" bind:value={formData.sgv} class="col-span-3" />
      </div>
      <div class="grid grid-cols-4 items-center gap-4">
        <Label class="text-right">Notes</Label>
        <Textarea bind:value={formData.notes} class="col-span-3" />
      </div>
    </div>

    <Dialog.Footer>
      <Button variant="outline" onclick={onClose}>Cancel</Button>
      <Button onclick={handleSubmit} disabled={isLoading}>
        {isLoading ? "Saving..." : "Save"}
      </Button>
    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
