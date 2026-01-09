import { query, command, getRequestEvent } from "$app/server";
import { z } from "zod";

// Schema definitions
const emptySchema = z.object({});

const generateRequestSchema = z.object({
  setupType: z.enum(["fresh", "migrate", "compatibility-proxy"]),
  migration: z.object({
    nightscoutUrl: z.string(),
    nightscoutApiSecret: z.string(),
  }).optional(),
  compatibilityProxy: z.object({
    nightscoutUrl: z.string(),
    nightscoutApiSecret: z.string(),
    enableDetailedLogging: z.boolean().optional(),
  }).optional(),
  postgres: z.object({
    useContainer: z.boolean(),
    connectionString: z.string().optional(),
  }),
  optionalServices: z.object({
    watchtower: z.boolean(),
    includeDashboard: z.boolean(),
    includeScalar: z.boolean(),
  }),
  connectors: z.array(z.object({
    type: z.string(),
    config: z.record(z.string(), z.string()),
  })),
});

// Infer types from schemas - no casting
export type GenerateRequest = z.infer<typeof generateRequestSchema>;

// Response schemas for type safety
const connectorFieldSchema = z.object({
  name: z.string(),
  envVar: z.string(),
  type: z.enum(["string", "password", "boolean", "select", "number"]),
  required: z.boolean(),
  description: z.string(),
  default: z.string().nullable().optional(),
  options: z.array(z.string()).nullable().optional(),
});

const connectorMetadataSchema = z.object({
  type: z.string(),
  displayName: z.string(),
  category: z.string(),
  description: z.string(),
  icon: z.string(),
  fields: z.array(connectorFieldSchema),
});

const connectorsResponseSchema = z.object({
  connectors: z.array(connectorMetadataSchema),
});

export type ConnectorField = z.infer<typeof connectorFieldSchema>;
export type ConnectorMetadata = z.infer<typeof connectorMetadataSchema>;

// Remote functions
export const getConnectors = query(emptySchema, async () => {
  const { fetch } = getRequestEvent();
  const response = await fetch("/api/connectors");
  if (!response.ok) {
    throw new Error(`Failed to fetch connectors: ${response.statusText}`);
  }
  const data: unknown = await response.json();
  const parsed = connectorsResponseSchema.parse(data);
  return parsed.connectors;
});

export const generateConfig = command(generateRequestSchema, async (request) => {
  const { fetch } = getRequestEvent();
  const response = await fetch("/api/generate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Failed to generate config: ${error}`);
  }

  return response.blob();
});

// Nightscout connection test schemas
const testNightscoutSchema = z.object({
  url: z.string(),
  apiSecret: z.string().optional(),
});

const nightscoutConnectionResultSchema = z.object({
  name: z.string().optional(),
  version: z.string().optional(),
  units: z.string().optional(),
  latestSgv: z.number().optional(),
  latestTime: z.string().optional(),
});

export type NightscoutConnectionResult = z.infer<typeof nightscoutConnectionResultSchema>;

// SHA1 hash function for API secret (Nightscout requires hashed secrets)
async function hashApiSecret(secret: string): Promise<string> {
  const encoder = new TextEncoder();
  const data = encoder.encode(secret);
  const hashBuffer = await crypto.subtle.digest('SHA-1', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
}

export const testNightscoutConnection = command(testNightscoutSchema, async ({ url, apiSecret }) => {
  // Normalize URL
  let baseUrl = url.trim();
  if (!baseUrl.startsWith("http")) {
    baseUrl = "https://" + baseUrl;
  }
  baseUrl = baseUrl.replace(/\/$/, "");

  // First fetch status endpoint (public, for site info)
  const statusRes = await fetch(`${baseUrl}/api/v1/status.json`, {
    headers: { Accept: "application/json" },
    signal: AbortSignal.timeout(10000),
  });

  if (!statusRes.ok) {
    throw new Error(`Server returned ${statusRes.status} - could not connect to Nightscout`);
  }

  const status = await statusRes.json();

  // If API secret provided, verify it using the verifyauth endpoint with hashed secret
  if (apiSecret) {
    const hashedSecret = await hashApiSecret(apiSecret);

    const authRes = await fetch(`${baseUrl}/api/v1/verifyauth`, {
      headers: {
        Accept: "application/json",
        "api-secret": hashedSecret,
      },
      signal: AbortSignal.timeout(10000),
    });

    if (!authRes.ok) {
      throw new Error("Invalid API secret - authentication failed");
    }

    const authData = await authRes.json();
    console.log(authData);
    // verifyauth returns { status, message: { canRead, canWrite, isAdmin, message, rolefound, permissions } }
    // The actual auth info is nested inside authData.message
    const authInfo = authData.message || authData;
    if (!authInfo.canRead) {
      throw new Error("Invalid API secret - no read permissions");
    }
  }

  // Try to get latest glucose reading
  let latestSgv: number | undefined;
  let latestTime: string | undefined;
  try {
    const entriesRes = await fetch(
      `${baseUrl}/api/v1/entries.json?count=1`,
      { headers: { Accept: "application/json" }, signal: AbortSignal.timeout(5000) },
    );
    if (entriesRes.ok) {
      const entries = await entriesRes.json();
      if (entries.length > 0) {
        latestSgv = entries[0].sgv;
        latestTime = new Date(entries[0].date).toISOString();
      }
    }
  } catch {
    // Ignore entries fetch errors
  }

  return {
    name: status.settings?.customTitle || status.name || "Nightscout",
    version: status.version,
    units: status.settings?.units,
    latestSgv,
    latestTime,
  } satisfies NightscoutConnectionResult;
});

