<script lang="ts">
    import { Check } from "@lucide/svelte";

    interface Step {
        id: number;
        label: string;
        path: string;
    }

    interface Props {
        currentStep: number;
    }

    let { currentStep }: Props = $props();

    const steps: Step[] = [
        { id: 1, label: "Setup", path: "/setup" },
        { id: 2, label: "Connectors", path: "/connectors" },
        { id: 3, label: "Download", path: "/download" },
    ];
</script>

<nav aria-label="Setup progress" class="w-full">
    <ol class="flex items-center justify-between relative">
        {#each steps as step, index}
            {@const isCompleted = step.id < currentStep}
            {@const isCurrent = step.id === currentStep}
            {@const isUpcoming = step.id > currentStep}

            <li class="flex-1 relative flex flex-col items-center">
                <!-- Connector line (before) -->
                {#if index > 0}
                    <div
                        class="absolute top-5 right-1/2 w-full h-0.5 -translate-y-1/2 {isCompleted ||
                        isCurrent
                            ? 'bg-primary'
                            : 'bg-border'}"
                        style="z-index: 0;"
                    ></div>
                {/if}

                <!-- Step circle -->
                {#if isCompleted}
                    <a
                        href={step.path}
                        class="relative z-10 w-10 h-10 rounded-full bg-primary flex items-center justify-center text-primary-foreground transition-all hover:scale-110 hover:shadow-lg hover:shadow-primary/30"
                    >
                        <Check class="w-5 h-5" strokeWidth={3} />
                    </a>
                {:else if isCurrent}
                    <div
                        class="relative z-10 w-10 h-10 rounded-full bg-primary flex items-center justify-center text-primary-foreground ring-4 ring-primary/20 shadow-lg shadow-primary/30"
                    >
                        <span class="font-semibold">{step.id}</span>
                    </div>
                {:else}
                    <div
                        class="relative z-10 w-10 h-10 rounded-full bg-muted border-2 border-border flex items-center justify-center text-muted-foreground"
                    >
                        <span class="font-medium">{step.id}</span>
                    </div>
                {/if}

                <!-- Step label -->
                <span
                    class="mt-2 text-sm font-medium {isCurrent
                        ? 'text-foreground'
                        : isCompleted
                          ? 'text-primary'
                          : 'text-muted-foreground'}"
                >
                    {step.label}
                </span>
            </li>
        {/each}
    </ol>
</nav>
