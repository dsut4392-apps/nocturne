<script lang="ts">
  import "../app.css";
  import { page } from "$app/state";
  import { createRealtimeStore } from "$lib/stores/realtime-store.svelte";
  import { onMount } from "svelte";
  import * as Sidebar from "$lib/components/ui/sidebar";
  import { AppSidebar, MobileHeader } from "$lib/components/layout";
  import type { LayoutData } from "./$types";

  // WebSocket config - defaults, can be overridden in production
  const config = {
    url: typeof window !== "undefined" ? window.location.origin : "",
    reconnectAttempts: 10,
    reconnectDelay: 5000,
    maxReconnectDelay: 30000,
    pingTimeout: 60000,
    pingInterval: 25000,
  };

  // Check if we're on a reports sub-page (not the main /reports page)
  const isReportsSubpage = $derived(page.url.pathname.startsWith("/reports/"));

  const { data, children } = $props<{ data: LayoutData; children: any }>();

  const realtimeStore = createRealtimeStore(config);

  onMount(async () => {
    await realtimeStore.initialize();
  });
</script>

<Sidebar.Provider>
  <AppSidebar user={data.user} />
  <MobileHeader />
  <Sidebar.Inset>
    <!-- Desktop header - hidden on mobile and on reports subpages (which have their own header) -->
    {#if !isReportsSubpage}
      <header
        class="hidden md:flex h-14 shrink-0 items-center gap-2 border-b border-border px-4"
      >
        <Sidebar.Trigger class="-ml-1" />
        <div class="flex-1"></div>
      </header>
    {/if}
    <main class="flex-1 overflow-auto">
      <svelte:boundary>
        {@render children()}

        {#snippet pending()}
          <div class="flex items-center justify-center h-full">
            <div class="text-muted-foreground">Loading...</div>
          </div>
        {/snippet}
        {#snippet failed(e)}
          <div class="flex items-center justify-center h-full">
            <div class="text-destructive">
              Error loading entries: {e instanceof Error
                ? e.message
                : JSON.stringify(e)}
            </div>
          </div>
        {/snippet}
      </svelte:boundary>
    </main>
  </Sidebar.Inset>
</Sidebar.Provider>
