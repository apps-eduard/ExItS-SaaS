import { AUTH_LOGIN_PATH } from "@/api/platform/browser-session";
import { PlatformApiError } from "@/api/platform/platform-http";
import { redactDiagnosticText } from "@/diagnostics/diagnostic-redaction";
import { normalizePosError } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";
import type { MessageKey } from "@/i18n/messages";

export const AUTH_LOGIN_FAILURE_STAGE = "platform.auth.login" as const;

export type AuthLoginFailureDiagnostic = {
  failureStage: typeof AUTH_LOGIN_FAILURE_STAGE;
  httpMethod: "POST";
  path: string;
  status?: number;
  errorCode?: string;
  title?: string;
  detail?: string;
  traceId?: string;
  requestCorrelationId?: string;
};

const INVALID_CREDENTIAL_ERROR_CODES = new Set([
  "application.auth.login_failed",
  "application.credential.password_invalid",
]);

function sanitizeDetail(value: string | undefined): string | undefined {
  if (!value) {
    return undefined;
  }
  const sanitized = redactDiagnosticText(value).trim();
  return sanitized.length > 0 ? sanitized : undefined;
}

export function buildAuthLoginFailure(error: unknown): AuthLoginFailureDiagnostic {
  const base: AuthLoginFailureDiagnostic = {
    failureStage: AUTH_LOGIN_FAILURE_STAGE,
    httpMethod: "POST",
    path: AUTH_LOGIN_PATH,
  };

  if (error instanceof PlatformApiError) {
    return {
      ...base,
      status: error.status,
      errorCode: error.errorCode,
      title: error.problem.title,
      detail: sanitizeDetail(error.problem.detail ?? error.message),
      traceId: error.traceId,
      requestCorrelationId: error.requestCorrelationId,
    };
  }

  if (error instanceof Error) {
    return {
      ...base,
      detail: sanitizeDetail(error.message),
    };
  }

  return base;
}

export function isInvalidCredentialsFailure(failure: AuthLoginFailureDiagnostic): boolean {
  if (failure.status !== 401) {
    return false;
  }
  if (!failure.errorCode) {
    return true;
  }
  return INVALID_CREDENTIAL_ERROR_CODES.has(failure.errorCode);
}

function isLikelyNetworkFailure(failure: AuthLoginFailureDiagnostic): boolean {
  if (failure.status !== undefined) {
    return false;
  }
  const detail = (failure.detail ?? "").toLowerCase();
  return (
    detail.includes("failed to fetch") ||
    detail.includes("networkerror") ||
    detail.includes("load failed") ||
    detail.includes("network request failed")
  );
}

export function resolveAuthLoginFriendlyMessageKey(
  failure: AuthLoginFailureDiagnostic,
): MessageKey {
  if (isLikelyNetworkFailure(failure)) {
    return "signIn.networkError";
  }

  if (failure.status === 403) {
    return "signIn.denied";
  }

  return "signIn.failed";
}

export function resolveAuthLoginFailurePresentation(
  failure: AuthLoginFailureDiagnostic,
  t: (key: MessageKey) => string,
): { title: string; detail: string; friendlyMessage: string } {
  const titleKey = resolveAuthLoginFriendlyMessageKey(failure);
  const title = t(titleKey);
  const friendlyMessage = title;

  if (titleKey === "signIn.networkError") {
    return { title, detail: title, friendlyMessage };
  }

  if (failure.status === undefined && failure.detail) {
    return {
      title,
      detail: sanitizeDetail(failure.detail) ?? title,
      friendlyMessage,
    };
  }

  if (titleKey === "signIn.denied") {
    return {
      title,
      detail: sanitizeDetail(failure.detail) ?? title,
      friendlyMessage,
    };
  }

  if (isInvalidCredentialsFailure(failure)) {
    return { title, detail: title, friendlyMessage };
  }

  return {
    title,
    detail: sanitizeDetail(failure.detail) ?? sanitizeDetail(failure.title) ?? title,
    friendlyMessage,
  };
}

export function authLoginFailureToPosErrorReport(
  failure: AuthLoginFailureDiagnostic,
  friendlyMessage: string,
): PosErrorReportInput {
  const error =
    failure.status !== undefined
      ? new PlatformApiError(
          failure.status,
          {
            errorCode: failure.errorCode,
            title: failure.title,
            detail: failure.detail,
            traceId: failure.traceId,
          },
          failure.requestCorrelationId,
        )
      : new Error(sanitizeDetail(failure.detail) ?? friendlyMessage);

  return normalizePosError({
    source: "session",
    error,
    operation: failure.failureStage,
    httpMethod: failure.httpMethod,
    path: failure.path,
    screen: "/sign-in",
    pathname: "/sign-in",
    friendlyMessage,
    status: failure.status,
    errorCode: failure.errorCode,
    traceId: failure.traceId,
  });
}
