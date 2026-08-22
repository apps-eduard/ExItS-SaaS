import { PlatformApiError } from "@/api/platform-http";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";

export type SignInFailureKind =
  "invalid_credentials" | "account_locked" | "account_disabled" | "network" | "unknown";

export function isNetworkFailure(error: unknown): boolean {
  return error instanceof TypeError || (error instanceof Error && error.name === "TypeError");
}

export function classifySignInFailure(error: unknown): SignInFailureKind {
  if (isNetworkFailure(error)) {
    return "network";
  }

  if (!(error instanceof PlatformApiError)) {
    return "unknown";
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
  | "email_conflict"
  | "invalid_token"
  | "password_invalid"
  | "invalid_display_name"
  | "invalid_email"
  | "network"
  | "unknown";

export function classifyCredentialWorkflowFailure(error: unknown): CredentialWorkflowFailureKind {
  if (isNetworkFailure(error)) {
    return "network";
  }

  if (!(error instanceof PlatformApiError)) {
    return "unknown";
  }

  const code = error.problem.errorCode;
  if (code === AUTH_ERROR_CODES.emailConflict) {
    return "email_conflict";
  }
  if (code === AUTH_ERROR_CODES.passwordInvalid) {
    return "password_invalid";
  }
  if (code === AUTH_ERROR_CODES.invalidDisplayName) {
    return "invalid_display_name";
  }
  if (code === AUTH_ERROR_CODES.invalidEmail) {
    return "invalid_email";
  }
  if (
    code === AUTH_ERROR_CODES.credentialTokenInvalid ||
    code === AUTH_ERROR_CODES.credentialTokenExpired
  ) {
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
