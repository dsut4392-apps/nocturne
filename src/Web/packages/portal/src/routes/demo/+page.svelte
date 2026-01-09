<script lang="ts">
    import { Button } from "@nocturne/app/ui/button";
    import {
        ExternalLink,
        Activity,
        Code2,
        Database,
        LineChart,
    } from "@lucide/svelte";
    import {
        DEMO_ENABLED,
        DEMO_WEB_URL,
        getDemoScalarUrl,
    } from "$lib/config";

    const scalarUrl = getDemoScalarUrl();
</script>

<svelte:head>
    <title>Demo - Nocturne</title>
</svelte:head>

{#if DEMO_ENABLED}
    <div class="container mx-auto px-4 py-12">
        <div class="max-w-3xl mx-auto">
            <div class="text-center mb-12">
                <h1 class="text-4xl font-bold tracking-tight mb-4">
                    Try Nocturne
                </h1>
                <p class="text-lg text-muted-foreground">
                    Experience Nocturne with realistic demo data. Explore the interface,
                    check out the charts, and browse the API documentation.
                </p>
            </div>

            <!-- Demo Features -->
            <div class="grid gap-4 mb-10">
                <div class="p-5 rounded-xl border border-border/60 bg-card/50">
                    <div class="flex items-start gap-4">
                        <div
                            class="w-10 h-10 rounded-lg bg-green-500/15 flex items-center justify-center shrink-0"
                        >
                            <Activity class="w-5 h-5 text-green-500" />
                        </div>
                        <div>
                            <h3 class="font-semibold mb-1">Real-time glucose monitoring</h3>
                            <p class="text-sm text-muted-foreground">
                                See live glucose values update every 5 minutes with realistic patterns
                            </p>
                        </div>
                    </div>
                </div>

                <div class="p-5 rounded-xl border border-border/60 bg-card/50">
                    <div class="flex items-start gap-4">
                        <div
                            class="w-10 h-10 rounded-lg bg-blue-500/15 flex items-center justify-center shrink-0"
                        >
                            <LineChart class="w-5 h-5 text-blue-500" />
                        </div>
                        <div>
                            <h3 class="font-semibold mb-1">90 days of historical data</h3>
                            <p class="text-sm text-muted-foreground">
                                Explore trends, patterns, and analytics with a full data history
                            </p>
                        </div>
                    </div>
                </div>

                <div class="p-5 rounded-xl border border-border/60 bg-card/50">
                    <div class="flex items-start gap-4">
                        <div
                            class="w-10 h-10 rounded-lg bg-purple-500/15 flex items-center justify-center shrink-0"
                        >
                            <Database class="w-5 h-5 text-purple-500" />
                        </div>
                        <div>
                            <h3 class="font-semibold mb-1">Treatments and device data</h3>
                            <p class="text-sm text-muted-foreground">
                                Sample insulin doses, carbs, and device status entries included
                            </p>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Action Buttons -->
            <div class="flex flex-col sm:flex-row gap-4 justify-center mb-12">
                <Button
                    href={DEMO_WEB_URL}
                    target="_blank"
                    rel="noopener noreferrer"
                    size="lg"
                    class="gap-2"
                >
                    <Activity class="w-4 h-4" />
                    Launch Demo App
                    <ExternalLink class="w-4 h-4" />
                </Button>
                <Button
                    href={scalarUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    variant="outline"
                    size="lg"
                    class="gap-2"
                >
                    <Code2 class="w-4 h-4" />
                    Explore API Docs
                    <ExternalLink class="w-4 h-4" />
                </Button>
            </div>

            <!-- Note -->
            <div class="p-5 rounded-xl border border-amber-500/30 bg-amber-500/5">
                <p class="text-sm text-muted-foreground">
                    <span class="font-semibold text-amber-700 dark:text-amber-400">Note:</span>
                    This is a demo instance with synthetic data. Data resets periodically.
                    For your own instance with real data, use the
                    <a href="/setup" class="text-primary hover:underline">setup wizard</a>.
                </p>
            </div>
        </div>
    </div>
{:else}
    <div class="container mx-auto px-4 py-20">
        <div class="max-w-md mx-auto text-center">
            <h1 class="text-2xl font-bold mb-4">Demo Not Available</h1>
            <p class="text-muted-foreground mb-6">
                The demo instance is not currently enabled.
            </p>
            <Button href="/" variant="outline">
                Back to Home
            </Button>
        </div>
    </div>
{/if}
