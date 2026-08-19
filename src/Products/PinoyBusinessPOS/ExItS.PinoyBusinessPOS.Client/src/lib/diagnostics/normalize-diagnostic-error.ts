import { ApiClientError } from "@/api/http";
import type { DiagnosticCategory, DiagnosticRecord } from "@/lib/diagnostics/diagnostic-types";
import {
  compactBrowserPlatform,
  createErrorReference,
  currentPathname,
  redactIfSensitive,
  safePathname,
} from "@/lib/diagnostics/diagnostic-redaction";
import { getAppVersion } from "@/api/http";

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

function safeMessage(error: unknown): string {
  if (error instanceof ApiClientError) {
    return redactIfSensitive(error.message) ?? "API request failed.";
  }
  if (error instanceof Error) {
    return redactIfSensitive(error.message) ?? "Unexpected error.";
  }
  return "Unexpected error.";
}

export function normalizeDiagnosticError(
  error: unknown,
  environment: DiagnosticEnvironment,
): DiagnosticRecord {
  const apiError = error instanceof ApiClientError ? error : undefined;
  return {
    application: "ExItS Mobile Client",
    appVersion: environment.appVersion ?? getAppVersion(),
    errorReference: environment.createReference?.() ?? createErrorReference(),
    timestamp: environment.now?.() ?? new Date().toISOString(),
    category: categoryFor(error),
    message: safeMessage(error),
    route: safePathname(environment.pathname ?? currentPathname()),
    errorCode: redactIfSensitive(apiError?.errorCode),
    requestCorrelationId: apiError?.requestCorrelationId,
    locale: environment.locale,
    theme: environment.theme,
    browserPlatform: environment.browserPlatform ?? compactBrowserPlatform(),
  };
}
