<script lang="ts">
    import type { GenerateRequest } from "$lib/data/portal.remote";
    import { Button } from "@nocturne/app/ui/button";
    import * as Card from "@nocturne/app/ui/card";
    import { ChevronLeft, Download, CheckCircle } from "@lucide/svelte";
    import { useSearchParams } from "runed/kit";
    import { z } from "zod";

    // Same schema as setup page to read URL params
    const DownloadParamsSchema = z.object({
        step: z.coerce.number().default(0),
        type: z.enum(["fresh", "migrate", "compatibility-proxy"]).optional(),
        // Database config
        useContainer: z.coerce.boolean().default(true),
        connectionString: z.string().optional(),
        // Optional services
        watchtower: z.coerce.boolean().default(true),
        includeDashboard: z.coerce.boolean().default(true),
        includeScalar: z.coerce.boolean().default(true),
        // Nightscout config (for migrate/proxy)
        nightscoutUrl: z.string().optional(),
        nightscoutApiSecret: z.string().optional(),
        enableDetailedLogging: z.coerce.boolean().default(false),
        // Migration-specific: MongoDB connection
        migrationMode: z.enum(["Api", "MongoDb"]).default("Api"),
        mongoConnectionString: z.string().optional(),
        mongoDatabaseName: z.string().optional(),
        // Connectors (comma-separated list)
        connectors: z.string().optional(),
    });

    const params = useSearchParams(DownloadParamsSchema, {
        updateURL: false, // Don't update URL on this page, just read
        noScroll: true,
    });

    // Parse connectors from URL
    const selectedConnectors = $derived(
        params.connectors?.split(",").filter(Boolean) ?? [],
    );

    let generating = $state(false);
    let error = $state<string | null>(null);

    async function handleDownload() {
        generating = true;
        error = null;

        try {
            const request: GenerateRequest = {
                setupType: params.type ?? "fresh",
                postgres: {
                    useContainer: params.useContainer,
                    connectionString: params.useContainer
                        ? undefined
                        : params.connectionString,
                },
                optionalServices: {
                    watchtower: params.watchtower,
                    includeDashboard: params.includeDashboard,
                    includeScalar: params.includeScalar,
                },
                connectors: selectedConnectors.map((type) => ({
                    type,
                    config: {}, // Connector configs would need separate handling
                })),
                migration:
                    params.type === "migrate"
                        ? {
                              mode: params.migrationMode,
                              nightscoutUrl: params.nightscoutUrl ?? "",
                              nightscoutApiSecret:
                                  params.nightscoutApiSecret ?? "",
                              mongoConnectionString:
                                  params.migrationMode === "MongoDb"
                                      ? params.mongoConnectionString
                                      : undefined,
                              mongoDatabaseName:
                                  params.migrationMode === "MongoDb"
                                      ? params.mongoDatabaseName
                                      : undefined,
                          }
                        : undefined,
                compatibilityProxy:
                    params.type === "compatibility-proxy"
                        ? {
                              nightscoutUrl: params.nightscoutUrl ?? "",
                              nightscoutApiSecret:
                                  params.nightscoutApiSecret ?? "",
                              enableDetailedLogging:
                                  params.enableDetailedLogging,
                          }
                        : undefined,
            };

            // Direct fetch call for file download - remote functions can't serialize blobs
            const response = await fetch("/api/generate", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(request),
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(
                    errorText ||
                        `Failed to generate config: ${response.statusText}`,
                );
            }

            const blob = await response.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = "nocturne-config.zip";
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        } catch (e) {
            error =
                e instanceof Error
                    ? e.message
                    : "Failed to generate configuration";
        } finally {
            generating = false;
        }
    }

    function getSetupTypeLabel(): string {
        switch (params.type) {
            case "fresh":
                return "Fresh Install";
            case "migrate":
                return "Migrate from Nightscout";
            case "compatibility-proxy":
                return "Compatibility Proxy";
            default:
                return params.type ?? "Unknown";
        }
    }

    function getBackUrl(): string {
        // Preserve all params when going back
        const searchParams = params.toURLSearchParams();
        searchParams.set("step", "2");
        return `/setup?${searchParams.toString()}`;
    }
</script>

<div class="max-w-2xl mx-auto">
    <Button
        href={getBackUrl()}
        variant="ghost"
        size="sm"
        class="mb-8 gap-1 text-muted-foreground"
    >
        <ChevronLeft size={16} />
        Back to connectors
    </Button>

    <h1 class="text-3xl font-bold mb-2">Review Configuration</h1>
    <p class="text-muted-foreground mb-8">
        Verify your settings and download deployment files
    </p>

    <div class="space-y-6 mb-8">
        <Card.Root>
            <Card.Header>
                <Card.Title>Setup Type</Card.Title>
            </Card.Header>
            <Card.Content>
                <div class="flex items-center gap-3">
                    <CheckCircle size={20} class="text-primary" />
                    <span class="font-medium">{getSetupTypeLabel()}</span>
                </div>
            </Card.Content>
        </Card.Root>

        {#if params.type !== "fresh" && params.nightscoutUrl}
            <Card.Root>
                <Card.Header>
                    <Card.Title>Nightscout Instance</Card.Title>
                </Card.Header>
                <Card.Content>
                    <p class="text-muted-foreground">
                        ✓ {params.nightscoutUrl}
                    </p>
                </Card.Content>
            </Card.Root>
        {/if}

        <Card.Root>
            <Card.Header>
                <Card.Title>Database</Card.Title>
            </Card.Header>
            <Card.Content>
                <p class="text-muted-foreground">
                    {params.useContainer
                        ? "✓ Using included PostgreSQL container"
                        : "✓ External database configured"}
                </p>
            </Card.Content>
        </Card.Root>

        <Card.Root>
            <Card.Header>
                <Card.Title>Connectors</Card.Title>
            </Card.Header>
            <Card.Content>
                {#if selectedConnectors.length === 0}
                    <p class="text-muted-foreground">No connectors selected</p>
                {:else}
                    <ul class="space-y-2">
                        {#each selectedConnectors as connector}
                            <li
                                class="flex items-center gap-2 text-muted-foreground"
                            >
                                <CheckCircle size={16} class="text-green-400" />
                                {connector}
                            </li>
                        {/each}
                    </ul>
                {/if}
            </Card.Content>
        </Card.Root>

        <Card.Root>
            <Card.Header>
                <Card.Title>Optional Services</Card.Title>
            </Card.Header>
            <Card.Content>
                <ul class="space-y-1 text-muted-foreground">
                    <li>
                        {params.watchtower ? "✓" : "○"} Watchtower auto-updates
                    </li>
                    <li>
                        {params.includeDashboard ? "✓" : "○"} Aspire Dashboard
                    </li>
                    <li>
                        {params.includeScalar ? "✓" : "○"} Scalar API docs
                    </li>
                </ul>
            </Card.Content>
        </Card.Root>
    </div>

    {#if error}
        <div
            class="p-4 rounded-lg bg-red-500/10 border border-red-500/50 text-red-400 mb-6"
        >
            <p class="font-medium">Generation failed</p>
            <p class="text-sm opacity-80">{error}</p>
        </div>
    {/if}

    <div class="flex justify-center">
        <Button
            onclick={handleDownload}
            disabled={generating}
            size="lg"
            class="gap-3"
        >
            {#if generating}
                <div
                    class="animate-spin w-5 h-5 border-2 border-current border-t-transparent rounded-full"
                ></div>
                Generating...
            {:else}
                <Download size={24} />
                Download Configuration
            {/if}
        </Button>
    </div>

    <Card.Root class="mt-8 bg-muted/30">
        <Card.Header>
            <Card.Title class="text-base">After downloading:</Card.Title>
        </Card.Header>
        <Card.Content>
            <ol
                class="list-decimal list-inside space-y-2 text-muted-foreground text-sm"
            >
                <li>Extract the ZIP file to your server</li>
                <li>
                    Fill in any empty values in <code class="text-primary"
                        >.env</code
                    >
                </li>
                <li>
                    Run <code class="text-primary">docker compose up -d</code>
                </li>
                <li>
                    Access Nocturne at <code class="text-primary"
                        >http://localhost:1337</code
                    >
                </li>
            </ol>
        </Card.Content>
    </Card.Root>
</div>
