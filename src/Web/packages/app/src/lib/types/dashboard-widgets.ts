/**
 * Dashboard Widget System
 *
 * Defines the configurable widgets that can appear above the glucose chart.
 * Widgets are small, self-contained display cards that show real-time data.
 */

/**
 * Unique identifier for each widget type
 */
export type WidgetId =
  | "bg-delta"
  | "last-updated"
  | "connection-status"
  | "meals"
  | "trackers"
  | "tir-chart"
  | "daily-summary";

/**
 * Widget size variants
 */
export type WidgetSize = "sm" | "md" | "lg";

/**
 * Widget definition with metadata
 */
export interface WidgetDefinition {
  id: WidgetId;
  name: string;
  description: string;
  /** Default enabled state */
  defaultEnabled: boolean;
  /** Icon name from lucide-svelte */
  icon: string;
  /** Category for grouping in settings */
  category: "glucose" | "meals" | "device" | "status";
}

/**
 * Widget instance configuration (user preferences)
 */
export interface WidgetConfig {
  id: WidgetId;
  enabled: boolean;
  /** Position in the grid (0-indexed) */
  position: number;
}

/**
 * Dashboard layout configuration
 */
export interface DashboardLayoutConfig {
  /** Ordered list of widget IDs for the top 3 slots */
  dashboardWidgets: WidgetId[];
  /** All widget configurations */
  widgets: Record<WidgetId, { enabled: boolean }>;
}

/**
 * Available widget definitions
 */
export const WIDGET_DEFINITIONS: WidgetDefinition[] = [
  {
    id: "bg-delta",
    name: "BG Delta",
    description: "Blood glucose change since last reading",
    defaultEnabled: true,
    icon: "TrendingUp",
    category: "glucose",
  },
  {
    id: "last-updated",
    name: "Last Updated",
    description: "Time since last glucose reading with device info",
    defaultEnabled: true,
    icon: "Clock",
    category: "device",
  },
  {
    id: "connection-status",
    name: "Connection Status",
    description: "Real-time data connection status",
    defaultEnabled: true,
    icon: "Wifi",
    category: "status",
  },
  {
    id: "meals",
    name: "Recent Meals",
    description: "Recent meal entries and carb intake",
    defaultEnabled: false,
    icon: "UtensilsCrossed",
    category: "meals",
  },
  {
    id: "trackers",
    name: "Trackers",
    description: "Active tracker status and progress",
    defaultEnabled: false,
    icon: "ListChecks",
    category: "status",
  },
  {
    id: "tir-chart",
    name: "Time in Range",
    description: "Stacked chart showing time in glucose ranges",
    defaultEnabled: false,
    icon: "BarChart3",
    category: "glucose",
  },
  {
    id: "daily-summary",
    name: "Daily Summary",
    description: "Today's glucose statistics overview",
    defaultEnabled: false,
    icon: "CalendarDays",
    category: "glucose",
  },
];

/**
 * Get widget definition by ID
 */
export function getWidgetDefinition(id: WidgetId): WidgetDefinition | undefined {
  return WIDGET_DEFINITIONS.find((w) => w.id === id);
}

/**
 * Default top 3 widgets
 */
export const DEFAULT_TOP_WIDGETS: WidgetId[] = [
  "bg-delta",
  "last-updated",
  "connection-status",
];

/**
 * Create default dashboard layout
 */
export function createDefaultDashboardLayout(): DashboardLayoutConfig {
  const widgets: Record<WidgetId, { enabled: boolean }> = {} as Record<WidgetId, { enabled: boolean }>;

  for (const def of WIDGET_DEFINITIONS) {
    widgets[def.id] = { enabled: def.defaultEnabled };
  }

  return {
    dashboardWidgets: [...DEFAULT_TOP_WIDGETS],
    widgets,
  };
}

/**
 * Get ordered list of enabled widgets for display
 */
export function getEnabledDashboardWidgets(
  layout: DashboardLayoutConfig | undefined
): WidgetId[] {
  if (!layout) {
    return DEFAULT_TOP_WIDGETS;
  }

  // Filter to only enabled widgets that exist in dashboardWidgets
  return layout.dashboardWidgets.filter(
    (id) => layout.widgets[id]?.enabled !== false
  );
}
