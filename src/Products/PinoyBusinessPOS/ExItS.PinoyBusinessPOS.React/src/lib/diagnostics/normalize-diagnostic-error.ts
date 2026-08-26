import { ApiClientError, getAppVersion } from "@/api/http";
import type { DiagnosticCategory, DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import { GENERIC_API_MESSAGE, GENERIC_RUNTIME_MESSAGE } from "@/lib/diagnostics/diagnostic-types";
import {
  allowlistedCorrelationId,
  allowlistedErrorCode,
  allowlistedHttpStatus,
  compactBrowserPlatform,
  createErrorReference,
  currentPathname,
  safePathname,
} from "@/lib/diagnostics/diagnostic-redaction";

export type DiagnosticEnvironment = {
  locale: string;
  theme: string;
  pathname?: string;
  now?: () => string;
  createReference?: () => string;
  browserPlatform?: string;
  appVersion?: string;
};

function categoryFor(error: unknown): DiagnosticCategory {
  if (error instanceof ApiClientError) {
    return "api";
  }
  return "runtime";
}

function controlledMessage(category: DiagnosticCategory): string {
  return category === "api" ? GENERIC_API_MESSAGE : GENERIC_RUNTIME_MESSAGE;
}

export function normalizeDiagnosticError(
  error: unknown,
  environment: DiagnosticEnvironment,
): DiagnosticRecord {
  const apiError = error instanceof ApiClientError ? error : undefined;
  const category = categoryFor(error);
  return {
    application: "ExItS Mobile Client",
    appVersion: environment.appVersion ?? getAppVersion(),
    errorReference: environment.createReference?.() ?? createErrorReference(),
    timestamp: environment.now?.() ?? new Date().toISOString(),
    category,
    message: controlledMessage(category),
    route: safePathname(environment.pathname ?? currentPathname()),
    httpStatus: allowlistedHttpStatus(apiError?.status),
    errorCode: allowlistedErrorCode(apiError?.errorCode),
    requestCorrelationId: allowlistedCorrelationId(apiError?.requestCorrelationId),
    locale: environment.locale,
    theme: environment.theme,
    browserPlatform: environment.browserPlatform ?? compactBrowserPlatform(),
  };
}
