import { isNetworkFailure } from "@/api/auth/auth-errors";
import { PlatformApiError } from "@/api/platform-http";
import {
  capComponentStack,
  compactBrowserPlatform,
  createErrorReference,
  currentPathname,
  presentText,
} from "@/lib/diagnostics/diagnostic-redaction";
import type {
  DiagnosticCategory,
  DiagnosticEnvironment,
  DiagnosticRecord,
} from "@/lib/diagnostics/diagnostic-types";
import { DIAGNOSTIC_APPLICATION } from "@/lib/diagnostics/diagnostic-types";

const GENERIC_MESSAGE = "Unable to complete this operation.";
const NETWORK_MESSAGE = "Unable to complete this operation.";
const RENDER_MESSAGE = "The application could not continue.";

export type NormalizeDiagnosticInput = {
  error: unknown;
  operation?: string;
  category?: DiagnosticCategory;
  componentStack?: string;
  environment?: DiagnosticEnvironment;
};

function errorTypeName(error: unknown): string | undefined {
  if (error instanceof Error && presentText(error.name)) {
    return error.name;
  }
  return undefined;
}

function resolveCategory(error: unknown, explicit?: DiagnosticCategory): DiagnosticCategory {
  if (explicit) {
    return explicit;
  }
  if (error instanceof PlatformApiError) {
    return "API";
  }
  if (isNetworkFailure(error)) {
    return "NETWORK";
  }
  return "UNKNOWN";
}

function apiMessage(error: PlatformApiError): string {
  return presentText(error.problem.detail) ?? presentText(error.problem.title) ?? GENERIC_MESSAGE;
}

export function normalizeDiagnosticError(input: NormalizeDiagnosticInput): DiagnosticRecord {
  const environment = input.environment ?? {};
  const category = resolveCategory(input.error, input.category);
  const error = input.error;

  let message = GENERIC_MESSAGE;
  let httpStatus: number | undefined;
  let errorCode: string | undefined;
  let requestCorrelationId: string | undefined;
  let serverTraceId: string | undefined;
  let errorType = errorTypeName(error);

  if (error instanceof PlatformApiError) {
    message = apiMessage(error);
    httpStatus = error.status;
    errorCode = presentText(error.errorCode);
    requestCorrelationId = presentText(error.requestCorrelationId);
    serverTraceId = presentText(error.traceId);
    errorType = "PlatformApiError";
  } else if (category === "NETWORK" || isNetworkFailure(error)) {
    message = NETWORK_MESSAGE;
  } else if (category === "RENDER") {
    message = RENDER_MESSAGE;
  }

  return {
    application: DIAGNOSTIC_APPLICATION,
    errorReference: environment.createReference?.() ?? createErrorReference(),
    timestamp: environment.now?.() ?? new Date().toISOString(),
    category,
    message,
    route: environment.pathname ?? currentPathname(),
    operation: presentText(input.operation),
    errorType,
    httpStatus,
    errorCode,
    requestCorrelationId,
    serverTraceId,
    locale: presentText(environment.locale),
    theme: presentText(environment.theme),
    density: presentText(environment.density),
    browserPlatform: presentText(environment.browserPlatform) ?? compactBrowserPlatform(),
    componentStack: capComponentStack(input.componentStack),
  };
}
