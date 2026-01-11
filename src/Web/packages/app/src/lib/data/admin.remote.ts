/**
 * Remote functions for admin password reset management
 */
import { getRequestEvent, query, command } from "$app/server";
import { z } from "zod";
import { error } from "@sveltejs/kit";
import type {
  PasswordResetRequestListResponse,
  SetTemporaryPasswordResponse,
  HandlePasswordResetResponse,
  SetTemporaryPasswordRequest,
} from "$api";
import { SetTemporaryPasswordRequestSchema } from "$lib/api/generated/schemas";

export const getPendingPasswordResets = query(async () => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  try {
    return (await apiClient.localAuth.getPendingPasswordResets()) as PasswordResetRequestListResponse;
  } catch (err) {
    console.error("Error loading password resets:", err);
    throw error(500, "Failed to load password resets");
  }
});

export const setTemporaryPassword = command(
  SetTemporaryPasswordRequestSchema,
  async (request) => {
    const { locals } = getRequestEvent();
    const { apiClient } = locals;

    try {
      const result = (await apiClient.localAuth.setTemporaryPassword(
        request as SetTemporaryPasswordRequest
      )) as SetTemporaryPasswordResponse;
      await getPendingPasswordResets().refresh();
      return result;
    } catch (err) {
      console.error("Error setting temporary password:", err);
      throw error(500, "Failed to set temporary password");
    }
  }
);

export const handlePasswordReset = command(z.string(), async (requestId) => {
  const { locals } = getRequestEvent();
  const { apiClient } = locals;

  try {
    const result = (await apiClient.localAuth.handlePasswordReset(
      requestId
    )) as HandlePasswordResetResponse;
    await getPendingPasswordResets().refresh();
    return result;
  } catch (err) {
    console.error("Error handling password reset request:", err);
    throw error(500, "Failed to handle password reset request");
  }
});
