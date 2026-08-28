import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";
import {
  AUTH_ACTIVATE_PATH,
  AUTH_FORGOT_PASSWORD_PATH,
  AUTH_LOGIN_PATH,
  AUTH_LOGOUT_PATH,
  AUTH_ME_PATH,
  AUTH_REGISTER_PATH,
  AUTH_RESET_PASSWORD_PATH,
  SESSION_EXPIRED_ERROR_CODE,
} from "@/api/platform/browser-session";

/**
 * Central authentication-lost transition for Pinoy Business POS Web.
 * Registered by SessionProvider → mark session expired (clears React Query + credentials).
 * Coalesces concurrent 401s into one transition.
 */

type AuthenticationLostHandler = () => void;

let authenticationLostHandler: AuthenticationLostHandler | null = null;
let authenticationLostNotified = false;

const AUTH_ERROR_CODES = {
  loginFailed: "application.auth.login_failed",
  sessionInvalid: "application.auth.session_invalid",
  sessionExpired: SESSION_EXPIRED_ERROR_CODE,
  accountNotEligible: "application.auth.account_not_eligible",
  credentialLockedOut: "application.credential.locked_out",
  passwordInvalid: "application.credential.password_invalid",
  credentialTokenInvalid: "application.auth.credential_token_invalid",
  credentialTokenExpired: "application.auth.credential_token_expired",
  accessTokenInvalid: "application.auth.access_token_invalid",
} as const;

/** Public / credential-workflow paths must never trigger session-expiry redirect. */
const SUPPRESSED_AUTH_PATH_PREFIXES = [
  AUTH_LOGIN_PATH,
  AUTH_REGISTER_PATH,
  AUTH_ACTIVATE_PATH,
  AUTH_FORGOT_PASSWORD_PATH,
  AUTH_RESET_PASSWORD_PATH,
  AUTH_LOGOUT_PATH,
  AUTH_ME_PATH,
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

const UNAUTHENTICATED_DEVELOPMENT_OPERATOR = "development-operator:unauthenticated";

/** Platform returns this actor when cookie session is missing on org-scoped APIs. */
export function isUnauthenticatedDevelopmentOperatorDetail(detail: string | undefined): boolean {
  return detail?.includes(UNAUTHENTICATED_DEVELOPMENT_OPERATOR) ?? false;
}

export function isAuthenticationLostFailure(
  status: number,
  errorCode: string | undefined,
  detail?: string,
): boolean {
  if (status === 403 && isUnauthenticatedDevelopmentOperatorDetail(detail)) {
    return true;
  }
  if (status === 403 || status === 419) {
    return false;
  }
  if (isAntiforgeryErrorCode(errorCode)) {
    return false;
  }
  if (
    errorCode === AUTH_ERROR_CODES.loginFailed ||
    errorCode === AUTH_ERROR_CODES.credentialTokenInvalid ||
    errorCode === AUTH_ERROR_CODES.credentialTokenExpired ||
    errorCode === AUTH_ERROR_CODES.credentialLockedOut ||
    errorCode === AUTH_ERROR_CODES.accountNotEligible ||
    errorCode === AUTH_ERROR_CODES.passwordInvalid
  ) {
    return false;
  }
  if (
    errorCode === AUTH_ERROR_CODES.sessionInvalid ||
    errorCode === AUTH_ERROR_CODES.sessionExpired ||
    errorCode === AUTH_ERROR_CODES.accessTokenInvalid
  ) {
    return true;
  }
  return status === 401;
}

export function isAuthenticationLostError(error: unknown): boolean {
  if (!error || typeof error !== "object") {
    return false;
  }
  const candidate = error as {
    status?: number;
    errorCode?: string;
    message?: string;
    problem?: { errorCode?: string; detail?: string };
  };
  const status = candidate.status;
  const errorCode = candidate.errorCode ?? candidate.problem?.errorCode;
  const detail = candidate.problem?.detail ?? candidate.message;
  if (typeof status !== "number") {
    return false;
  }
  return isAuthenticationLostFailure(status, errorCode, detail);
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
  detail?: string;
  path?: string;
  skipSessionExpiry?: boolean;
}): void {
  if (options.skipSessionExpiry) {
    return;
  }
  if (shouldSuppressAuthenticationLostForPath(options.path)) {
    return;
  }
  if (!isAuthenticationLostFailure(options.status, options.errorCode, options.detail)) {
    return;
  }
  notifyAuthenticationLost();
}
