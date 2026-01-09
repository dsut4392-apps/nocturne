/**
 * Remote functions for data migration from Nightscout
 */
import { getRequestEvent, query, command } from "$app/server";
import { z } from "zod";
import { error } from "@sveltejs/kit";

// Schema for test connection request
const testConnectionSchema = z.object({
  mode: z.enum(["Api", "MongoDb"]),
  nightscoutUrl: z.string().optional(),
  nightscoutApiSecret: z.string().optional(),
  mongoConnectionString: z.string().optional(),
  mongoDatabaseName: z.string().optional(),
});

// Schema for start migration request
const startMigrationSchema = z.object({
  mode: z.enum(["Api", "MongoDb"]),
  nightscoutUrl: z.string().optional(),
  nightscoutApiSecret: z.string().optional(),
  mongoConnectionString: z.string().optional(),
  mongoDatabaseName: z.string().optional(),
  collections: z.array(z.string()).default([]),
  startDate: z.string().datetime().optional(),
  endDate: z.string().datetime().optional(),
});

/**
 * Test a migration source connection
 */
export const testMigrationConnection = command(
  testConnectionSchema,
  async (request) => {
    const { locals } = getRequestEvent();
    const { apiClient } = locals;

    try {
      return await apiClient.migration.testConnection({
        mode: request.mode,
        nightscoutUrl: request.nightscoutUrl,
        nightscoutApiSecret: request.nightscoutApiSecret,
        mongoConnectionString: request.mongoConnectionString,
        mongoDatabaseName: request.mongoDatabaseName,
      });
    } catch (err) {
      console.error("Error testing migration connection:", err);
      throw error(500, "Failed to test migration connection");
    }
  }
);

/**
 * Start a new migration job
 */
export const startMigration = command(startMigrationSchema, async (request) => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  try {
    const result = await apiClient.migration.startMigration({
      mode: request.mode,
      nightscoutUrl: request.nightscoutUrl,
      nightscoutApiSecret: request.nightscoutApiSecret,
      mongoConnectionString: request.mongoConnectionString,
      mongoDatabaseName: request.mongoDatabaseName,
      collections: request.collections,
      startDate: request.startDate ? new Date(request.startDate) : undefined,
      endDate: request.endDate ? new Date(request.endDate) : undefined,
    });
    await getMigrationHistory.refresh();
    return result;
  } catch (err) {
    console.error("Error starting migration:", err);
    throw error(500, "Failed to start migration");
  }
});

/**
 * Get the status of a migration job
 */
export const getMigrationStatus = command(
  z.object({ jobId: z.string().uuid() }),
  async ({ jobId }) => {
    const { locals } = getRequestEvent();
    const { apiClient } = locals;

    try {
      return await apiClient.migration.getStatus(jobId);
    } catch (err) {
      console.error("Error getting migration status:", err);
      throw error(500, "Failed to get migration status");
    }
  }
);

/**
 * Cancel a running migration job
 */
export const cancelMigration = command(
  z.object({ jobId: z.string().uuid() }),
  async ({ jobId }) => {
    const { locals } = getRequestEvent();
    const { apiClient } = locals;

    try {
      await apiClient.migration.cancelMigration(jobId);
      await getMigrationHistory.refresh();
    } catch (err) {
      console.error("Error cancelling migration:", err);
      throw error(500, "Failed to cancel migration");
    }
  }
);

/**
 * Get migration job history
 */
export const getMigrationHistory = query(async () => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  try {
    return await apiClient.migration.getHistory();
  } catch (err) {
    console.error("Error getting migration history:", err);
    throw error(500, "Failed to get migration history");
  }
});
