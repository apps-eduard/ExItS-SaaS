import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import { PlatformAntiforgeryDefaults } from "@/api/platform-antiforgery";

/**
 * Central authentication-lost transition for Platform Admin.
 * Registered by SessionProvider → markExpired (clears session + React Query).
 * Coalesces concurrent 401s into one transition.
 */

type AuthenticationLostHandler = () => void;

let authenticationLostHandler: AuthenticationLostHandler | null = null;
let authenticationLostNotified = false;

/** Public / credential-workflow paths must never trigger session-expiry redirect. */
const SUPPRESSED_AUTH_PATH_PREFIXES = [
  "/api/v1/platform/auth/login",
  "/api/v1/platform/auth/register",
  "/api/v1/platform/auth/activate-account",
  "/api/v1/platform/auth/forgot-password",
  "/api/v1/platform/auth/reset-password",
  "/api/v1/platform/auth/logout",
  PlatformAntiforgeryDefaults.tokenPath,
] as const;

export function setAuthenticationLostHandler(handler: AuthenticationLostHandler | null): void {
  authenticationLostHandler = handler;
}

export function resetAuthenticationLostLatch(): void {
  authenticationLostNotified = false;
}

export function isAntiforgeryErrorCode(errorCode: string | undefined): boolean {
  if (!errorCode) {
    return false;
  }
  return errorCode.toLowerCase().includes("antiforgery");
}

export function isAuthenticationLostFailure(
  status: number,
  errorCode: string | undefined,
): boolean {
  if (status === 403 || status === 419) {
    return false;
  }
  if (isAntiforgeryErrorCode(errorCode)) {
    return false;
  }
  // Credential / workflow failures — not an expired admin session.
  if (
    errorCode === AUTH_ERROR_CODES.loginFailed ||
    errorCode === AUTH_ERROR_CODES.credentialTokenInvalid ||
    errorCode === AUTH_ERROR_CODES.credentialTokenExpired ||
    errorCode === AUTH_ERROR_CODES.credentialLockedOut ||
    errorCode === AUTH_ERROR_CODES.accountNotEligible
  ) {
    return false;
  }
  if (
    errorCode === AUTH_ERROR_CODES.sessionInvalid ||
    errorCode === AUTH_ERROR_CODES.sessionExpired
  ) {
    return true;
  }
  return status === 401;
}

export function shouldSuppressAuthenticationLostForPath(path: string | undefined): boolean {
  if (!path) {
    return false;
  }
  const normalized = path.split("?")[0] ?? path;
  return SUPPRESSED_AUTH_PATH_PREFIXES.some(
    (prefix) => normalized === prefix || normalized.startsWith(`${prefix}/`),
  );
}

/**
 * Notify once that the authenticated session is gone.
 * Caller should clear antiforgery before or inside the registered handler.
 */
export function notifyAuthenticationLost(): void {
  if (authenticationLostNotified) {
    return;
  }
  authenticationLostNotified = true;
  authenticationLostHandler?.();
}

export function maybeNotifyAuthenticationLost(options: {
  status: number;
  errorCode?: string;
  path?: string;
  skipSessionExpiry?: boolean;
}): void {
  if (options.skipSessionExpiry) {
    return;
  }
  if (shouldSuppressAuthenticationLostForPath(options.path)) {
    return;
  }
  if (!isAuthenticationLostFailure(options.status, options.errorCode)) {
    return;
  }
  notifyAuthenticationLost();
}
