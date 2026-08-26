import { PosApiError } from "@/api/pos/pos-http";
import { PlatformApiError } from "@/api/platform/platform-http";
import type { PosErrorReportInput, PosErrorSource } from "@/diagnostics/pos-error-report";
import { readPosBuildLabel } from "@/diagnostics/pos-build-info";
import { redactDiagnosticText, safeDiagnosticError, safeDiagnosticLocation } from "@/diagnostics/diagnostic-redaction";

export type NormalizePosErrorInput = {
  error: unknown;
  source?: PosErrorSource;
  operation?: string;
  friendlyMessage?: string;
  screen?: string;
  pathname?: string;
  httpMethod?: string;
  path?: string;
  status?: number;
  errorCode?: string;
  traceId?: string;
  accountClass?: string;
  organizationPublicId?: string;
  organizationName?: string;
  branchPublicId?: string;
  branchName?: string;
  platformRuntime?: string;
  componentStack?: string | null;
};

function resolveNetworkStatus(error: unknown): number | undefined {
  if (error instanceof TypeError) {
    return undefined;
  }
  return undefined;
}

function resolveFromApiError(
  error: PlatformApiError | PosApiError,
  base: Omit<NormalizePosErrorInput, "error">,
): PosErrorReportInput {
  return {
    source: base.source ?? "api",
    occurredAt: new Date().toISOString(),
    screen: base.screen,
    pathname: base.pathname,
    operation: base.operation,
    friendlyMessage: base.friendlyMessage,
    httpMethod: base.httpMethod,
    path: base.path,
    status: error.status,
    errorCode: error.errorCode,
    traceId:
      error instanceof PlatformApiError
        ? error.traceId
        : error.problem.traceId ?? error.requestCorrelationId,
    correlationId: error.requestCorrelationId,
    accountClass: base.accountClass,
    organizationPublicId: base.organizationPublicId,
    organizationName: base.organizationName,
    branchPublicId: base.branchPublicId,
    branchName: base.branchName,
    posBuild: readPosBuildLabel(),
    platformRuntime: base.platformRuntime,
    online: typeof navigator !== "undefined" ? navigator.onLine : undefined,
    error,
    componentStack: base.componentStack,
    mode: import.meta.env.MODE,
  };
}

export function normalizePosError(input: NormalizePosErrorInput): PosErrorReportInput {
  const location = safeDiagnosticLocation(
    typeof window !== "undefined" ? window.location.href : null,
    input.pathname,
  );
  const normalizedError = safeDiagnosticError(input.error);

  if (input.error instanceof PlatformApiError || input.error instanceof PosApiError) {
    return resolveFromApiError(input.error, input);
  }

  const message =
    input.friendlyMessage ??
    (normalizedError.message !== "Unknown non-Error value (details omitted for privacy)"
      ? normalizedError.message
      : "Something went wrong while loading your workspace.");

  return {
    source: input.source ?? "network",
    occurredAt: new Date().toISOString(),
    screen: input.screen ?? location.pathname,
    pathname: location.pathname,
    operation: input.operation,
    friendlyMessage: message,
    httpMethod: input.httpMethod,
    path: input.path,
    status: input.status ?? resolveNetworkStatus(input.error),
    errorCode: input.errorCode,
    traceId: input.traceId,
    accountClass: input.accountClass,
    organizationPublicId: input.organizationPublicId,
    organizationName: input.organizationName,
    branchPublicId: input.branchPublicId,
    branchName: input.branchName,
    posBuild: readPosBuildLabel(),
    platformRuntime: input.platformRuntime,
    online: typeof navigator !== "undefined" ? navigator.onLine : undefined,
    error:
      input.error instanceof Error
        ? new Error(redactDiagnosticText(input.error.message))
        : new Error(redactDiagnosticText(message)),
    componentStack: input.componentStack,
    mode: import.meta.env.MODE,
  };
}

export function normalizeReactClientError(input: {
  source: PosErrorReportInput["source"];
  error: unknown;
  componentStack?: string | null;
  pathname?: string;
  friendlyMessage?: string;
}): PosErrorReportInput {
  const location = safeDiagnosticLocation(
    typeof window !== "undefined" ? window.location.href : null,
    input.pathname,
  );
  return {
    source: input.source,
    occurredAt: new Date().toISOString(),
    screen: location.pathname,
    pathname: location.pathname,
    operation: "react render",
    friendlyMessage: input.friendlyMessage ?? "Something went wrong.",
    posBuild: readPosBuildLabel(),
    online: typeof navigator !== "undefined" ? navigator.onLine : undefined,
    error: input.error,
    componentStack: input.componentStack,
    mode: import.meta.env.MODE,
  };
}
