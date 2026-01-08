<script lang="ts">
    import "../app.css";
    import { page } from "$app/state";
    import StepIndicator from "$lib/components/StepIndicator.svelte";

    let { children } = $props();

    // Determine current step from URL path
    const currentStep = $derived.by(() => {
        const pathname = page.url.pathname;
        if (pathname.startsWith("/setup")) return 1;
        if (pathname.startsWith("/connectors")) return 2;
        if (pathname.startsWith("/download")) return 3;
        return 0; // Not in wizard
    });

    const isInWizard = $derived(currentStep > 0);
</script>

<div
    class="min-h-screen bg-gradient-to-br from-background via-background to-primary/5"
>
    <header class="border-b border-border/40 backdrop-blur-sm">
        <div class="container mx-auto px-4 py-4 flex items-center gap-4">
            <a
                href="/"
                class="flex items-center gap-2 hover:opacity-80 transition-opacity"
            >
                <div
                    class="w-8 h-8 rounded-lg bg-primary/20 flex items-center justify-center"
                >
                    <span class="text-primary font-bold">N</span>
                </div>
                <span class="text-xl font-semibold">Nocturne Portal</span>
            </a>
        </div>
    </header>

    {#if isInWizard}
        <div class="border-b border-border/40 bg-card/30 backdrop-blur-sm">
            <div class="container mx-auto px-4 py-6">
                <div class="max-w-xl mx-auto">
                    <StepIndicator {currentStep} />
                </div>
            </div>
        </div>
    {/if}

    <main class="container mx-auto px-4 py-8">
        {@render children()}
    </main>

    <footer class="border-t border-border/40 mt-auto">
        <div
            class="container mx-auto px-4 py-6 text-center text-muted-foreground text-sm"
        >
            Nocturne &copy; 2026 - Open source diabetes management
        </div>
    </footer>
</div>
