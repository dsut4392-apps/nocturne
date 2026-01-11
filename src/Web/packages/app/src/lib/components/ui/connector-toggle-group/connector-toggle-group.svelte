<script lang="ts" module>
  import type { Snippet } from "svelte";
  import type { HTMLAttributes } from "svelte/elements";

  export interface ConnectorData {
    type: string;
    displayName: string;
    description: string;
    category: string;
    fields?: unknown[];
  }
</script>

<script lang="ts">
  import { ToggleGroup as ToggleGroupPrimitive } from "bits-ui";
  import { cn } from "$lib/utils";

  let {
    ref = $bindable(null),
    value = $bindable<string[]>([]),
    class: className,
    onValueChange,
    children,
    ...restProps
  }: {
    ref?: HTMLDivElement | null;
    value?: string[];
    class?: string;
    onValueChange?: (value: string[]) => void;
    children?: Snippet;
  } & Omit<HTMLAttributes<HTMLDivElement>, "class"> = $props();

  function handleValueChange(newValue: string[]) {
    value = newValue;
    onValueChange?.(newValue);
  }
</script>

<ToggleGroupPrimitive.Root
  bind:ref
  {value}
  onValueChange={handleValueChange}
  type="multiple"
  data-slot="connector-toggle-group"
  class={cn("flex flex-col gap-3", className)}
  {...restProps}
>
  {@render children?.()}
</ToggleGroupPrimitive.Root>
