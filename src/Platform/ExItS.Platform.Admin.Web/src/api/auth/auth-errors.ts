import { PlatformApiError, PlatformNetworkError } from "@/api/platform-http";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";

export type SignInFailureKind =
  | "invalid_credentials"
  | "sign_in_denied"
  | "account_locked"
  | "account_disabled"
  | "rate_limited"
  | "service_unavailable"
  | "network"
  | "unknown";

export function isNetworkFailure(error: unknown): boolean {
  return (
    error instanceof PlatformNetworkError ||
    error instanceof TypeError ||
    (error instanceof Error && error.name === "TypeError")
  );
}

export function classifySignInFailure(error: unknown): SignInFailureKind {
  if (isNetworkFailure(error)) {
    return "network";
  }

  if (!(error instanceof PlatformApiError)) {
    return "unknown";
  }

  if (error.status === 429 || error.problem.errorCode === AUTH_ERROR_CODES.rateLimitExceeded) {
    return "rate_limited";
  }
  if (error.status === 502 || error.status === 503 || error.status === 504 || error.status === 500) {
    return "service_unavailable";
  }
  if (error.status === 403) {
    return "sign_in_denied";
  }

  const code = error.problem.errorCode;
  if (code === AUTH_ERROR_CODES.credentialLockedOut) {
    return "account_locked";
  }
  if (code === AUTH_ERROR_CODES.accountNotEligible) {
    return "account_disabled";
  }
  if (code === AUTH_ERROR_CODES.loginFailed || error.status === 401) {
    return "invalid_credentials";
  }

  return "unknown";
}

export type CredentialWorkflowFailureKind =
  | "invalid_token"
  | "expired_token"
  | "password_invalid"
  | "invalid_display_name"
  | "invalid_email"
  | "rate_limited"
  | "service_unavailable"
  | "network"
  | "unknown";

export function classifyCredentialWorkflowFailure(error: unknown): CredentialWorkflowFailureKind {
  if (isNetworkFailure(error)) {
    return "network";
  }

  if (!(error instanceof PlatformApiError)) {
    return "unknown";
  }

  if (error.status === 502 || error.status === 503 || error.status === 504) {
    return "service_unavailable";
  }

  const code = error.problem.errorCode;
  if (code === AUTH_ERROR_CODES.passwordInvalid) {
    return "password_invalid";
  }
  if (code === AUTH_ERROR_CODES.invalidDisplayName) {
    return "invalid_display_name";
  }
  if (code === AUTH_ERROR_CODES.invalidEmail) {
    return "invalid_email";
  }
  if (code === AUTH_ERROR_CODES.rateLimitExceeded) {
    return "rate_limited";
  }
  if (code === AUTH_ERROR_CODES.credentialTokenExpired) {
    return "expired_token";
  }
  if (code === AUTH_ERROR_CODES.credentialTokenInvalid) {
    return "invalid_token";
  }

  return "unknown";
}

export function isSessionInvalidError(error: unknown): boolean {
  if (!(error instanceof PlatformApiError)) {
    return false;
  }
  const code = error.problem.errorCode;
  return (
    code === AUTH_ERROR_CODES.sessionInvalid ||
    code === AUTH_ERROR_CODES.sessionExpired ||
    error.status === 401
  );
}
