import { platformRequest, PlatformApiError } from "@/api/platform/platform-http";

export const AUTH_CREDENTIALS_PATH = "/api/v1/platform/auth/credentials";

/**
 * Credential shape for the *signed-in* user only. The POS client must never call the
 * Platform Admin `/users/{id}/credentials/password` surface from an Owner screen.
 */
export type PlatformCredentialStatus = {
  hasPassword: boolean;
  emailVerified: boolean;
  isLockedOut: boolean;
};

export type PlatformCredentialStatusResult =
  { ok: true; value: PlatformCredentialStatus } | { ok: false; status: number; errorCode?: string };

function readBoolean(raw: Record<string, unknown>, camel: string, pascal: string): boolean {
  return (raw[camel] ?? raw[pascal]) === true;
}

/**
 * Read whether the signed-in user can complete a password step-up.
 * Returns `ok: false` when the endpoint is unreachable so callers can decide their own
 * fail-closed copy instead of silently assuming a password exists.
 */
export async function getPlatformCredentialStatus(
  signal?: AbortSignal,
): Promise<PlatformCredentialStatusResult> {
  try {
    const payload = await platformRequest<Record<string, unknown>>({
      path: AUTH_CREDENTIALS_PATH,
      signal,
    });
    return {
      ok: true,
      value: {
        hasPassword: readBoolean(payload, "hasPassword", "HasPassword"),
        emailVerified: readBoolean(payload, "emailVerified", "EmailVerified"),
        isLockedOut: readBoolean(payload, "isLockedOut", "IsLockedOut"),
      },
    };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, errorCode: error.errorCode };
    }
    throw error;
  }
}
