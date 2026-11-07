import { PlatformApiError } from "./platform-http";
import { PosApiError } from "./pos-http";

const GUID_RE =
  /\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b/gi;
const API_PATH_RE = /\/api\/v1\/[^\s'"`]+/gi;
const ORG_ID_HINT_RE = /\borganizationId\b[=:]\s*[^\s,;]+/gi;

export function sanitizeUserFacingMessage(
  message: string | null | undefined,
  fallback: string,
): string {
  if (!message?.trim()) return fallback;
  let cleaned = message
    .replace(API_PATH_RE, "")
    .replace(GUID_RE, "")
    .replace(ORG_ID_HINT_RE, "")
    .replace(/Account class\s+'[^']+'\s+is not allowed[^.]*\.?/gi, "")
    .replace(/\s{2,}/g, " ")
    .trim();
  if (!cleaned || cleaned.length < 3) return fallback;
  // Prefer short, non-technical copy when the remainder still looks like a problem detail.
  if (/not allowed|forbidden|unauthorized|exception|stack/i.test(cleaned)) {
    return fallback;
  }
  return cleaned;
}

export function toUserFacingError(
  error: unknown,
  fallback = "Something went wrong. Please try again.",
): string {
  if (error instanceof PlatformApiError || error instanceof PosApiError) {
    if (error.status === 401) {
      return "Your session expired. Sign in again.";
    }
    if (error.status === 403) {
      return "You do not have permission for this action.";
    }
    return sanitizeUserFacingMessage(
      error.problem.detail ?? error.message,
      fallback,
    );
  }
  if (error instanceof Error) {
    return sanitizeUserFacingMessage(error.message, fallback);
  }
  return fallback;
}

export function isAuthDenialStatus(status: number | undefined): boolean {
  return status === 401 || status === 403;
}
