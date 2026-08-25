import { PlatformApiError, PlatformNetworkError } from "@/api/platform-http";
import { isNetworkFailure } from "@/api/auth/auth-errors";
import {
  classifyHttpDiagnosticCategory,
  isRetryableCategory,
  networkErrorCode,
} from "@/lib/diagnostics/classify-http-error";
import {
  capComponentStack,
  compactBrowserPlatform,
  createErrorReference,
  currentPathname,
  presentText,
  readBrowserInfo,
  readNetworkOnline,
  safePathname,
} from "@/lib/diagnostics/diagnostic-redaction";
import type {
  DiagnosticCategory,
  DiagnosticEnvironment,
  DiagnosticRecord,
} from "@/lib/diagnostics/diagnostic-types";
import { DIAGNOSTIC_APPLICATION } from "@/lib/diagnostics/diagnostic-types";
import {
  getFrontendRuntimeStatus,
  isLocalValidationToolsEnabled,
  isPlatformApiSameOrigin,
  resolvePlatformApiBaseUrl,
} from "@/lib/env";

const GENERIC_MESSAGE = "Unable to complete this request.";
const NETWORK_MESSAGE = "Unable to connect to Platform API.";
const RENDER_MESSAGE = "Something went wrong.";

export type NormalizeDiagnosticInput = {
  error: unknown;
  operation?: string;
  category?: DiagnosticCategory;
  userMessage?: string;
  componentStack?: string;
  environment?: DiagnosticEnvironment;
};

function errorTypeName(error: unknown): string | undefined {
  if (error instanceof Error && presentText(error.name)) {
    return error.name;
  }
  return undefined;
}

function resolveEnvironmentLabel(): string {
  if (isLocalValidationToolsEnabled()) {
    return "Local Validation";
  }
  return import.meta.env.PROD ? "Production" : "Development";
}

function resolveApiMode(): string {
  if (isPlatformApiSameOrigin()) {
    return "same-origin";
  }
  // Empty base URL also means relative /api (Vite proxy) in DEV.
  if (resolvePlatformApiBaseUrl().length === 0) {
    return "same-origin";
  }
  return "direct";
}

function safeApiMessage(error: PlatformApiError): string {
  const category = classifyHttpDiagnosticCategory(error.status, error.errorCode);
  if (
    category === "SERVER_ERROR" ||
    category === "SERVICE_UNAVAILABLE" ||
    category === "UNEXPECTED_CLIENT_ERROR"
  ) {
    return GENERIC_MESSAGE;
  }
  if (category === "VALIDATION_ERROR" || category === "DOMAIN_ERROR") {
    return presentText(error.problem.detail) ?? presentText(error.problem.title) ?? GENERIC_MESSAGE;
  }
  return GENERIC_MESSAGE;
}

function resolveCategory(error: unknown, explicit?: DiagnosticCategory): DiagnosticCategory {
  if (explicit) {
    return explicit;
  }
  if (error instanceof PlatformNetworkError) {
    return error.networkFailureKind === "timeout" ? "TIMEOUT" : "NETWORK_ERROR";
  }
  if (error instanceof PlatformApiError) {
    return classifyHttpDiagnosticCategory(error.status, error.errorCode);
  }
  if (isNetworkFailure(error)) {
    return "NETWORK_ERROR";
  }
  return "UNEXPECTED_CLIENT_ERROR";
}

function resolveUserMessage(category: DiagnosticCategory, error: unknown, override?: string): string {
  if (presentText(override)) {
    return override!;
  }
  if (error instanceof PlatformApiError) {
    return safeApiMessage(error);
  }
  if (error instanceof PlatformNetworkError || category === "NETWORK_ERROR") {
    return NETWORK_MESSAGE;
  }
  if (category === "SERVICE_UNAVAILABLE") {
    return "The sign-in service is temporarily unavailable.";
  }
  if (category === "RATE_LIMITED") {
    return "Too many attempts. Please wait a few minutes and try again.";
  }
  if (category === "REACT_RENDER_ERROR") {
    return RENDER_MESSAGE;
  }
  return GENERIC_MESSAGE;
}

function resolveHttpStatusLabel(status: number | undefined, error: unknown): string {
  if (typeof status === "number") {
    return String(status);
  }
  if (error instanceof PlatformNetworkError || isNetworkFailure(error)) {
    return "Not received";
  }
  return "Not available";
}

function resolveErrorCode(category: DiagnosticCategory, error: unknown): string | undefined {
  if (error instanceof PlatformApiError) {
    return presentText(error.errorCode);
  }
  if (error instanceof PlatformNetworkError || category === "NETWORK_ERROR") {
    return networkErrorCode(category);
  }
  if (category === "SERVICE_UNAVAILABLE") {
    return networkErrorCode(category);
  }
  if (category === "RATE_LIMITED") {
    return networkErrorCode(category);
  }
  return undefined;
}

export function normalizeDiagnosticError(input: NormalizeDiagnosticInput): DiagnosticRecord {
  const environment = input.environment ?? {};
  const runtime = getFrontendRuntimeStatus();
  const category = resolveCategory(input.error, input.category);
  const error = input.error;
  const browser = readBrowserInfo();

  let httpStatus: number | undefined;
  let traceId: string | undefined;
  let correlationId: string | undefined;
  let httpMethod: string | undefined;
  let apiPath: string | undefined;
  let networkFailureKind: string | undefined;
  let errorType = errorTypeName(error);

  if (error instanceof PlatformApiError) {
    httpStatus = error.status;
    traceId = presentText(error.traceId);
    correlationId = presentText(error.requestCorrelationId);
    httpMethod = presentText(error.method);
    apiPath = presentText(error.path);
    errorType = "PlatformApiError";
  } else if (error instanceof PlatformNetworkError) {
    httpMethod = error.method;
    apiPath = error.path;
    correlationId = error.requestCorrelationId;
    networkFailureKind = error.networkFailureKind;
    errorType = "PlatformNetworkError";
  } else if (isNetworkFailure(error)) {
    networkFailureKind = "fetch_failed";
    errorType = "TypeError";
  }

  const userMessage = resolveUserMessage(category, error, input.userMessage);

  return {
    application: DIAGNOSTIC_APPLICATION,
    errorReference: environment.createReference?.() ?? createErrorReference(),
    timestampUtc: environment.now?.() ?? new Date().toISOString(),
    buildSha: environment.buildSha ?? runtime.buildSha,
    environment: environment.environment ?? resolveEnvironmentLabel(),
    frontendMode: environment.frontendMode ?? runtime.frontendMode,
    localValidationEnabled:
      environment.localValidationEnabled ?? runtime.localValidationToolsEnabled,
    apiMode: environment.apiMode ?? resolveApiMode(),
    pagePath: safePathname(environment.pathname) ?? currentPathname(),
    operation: presentText(input.operation),
    category,
    userMessage,
    httpMethod,
    apiPath,
    httpStatus,
    httpStatusLabel: resolveHttpStatusLabel(httpStatus, error),
    errorCode: resolveErrorCode(category, error),
    traceId,
    correlationId,
    networkOnline: environment.networkOnline ?? readNetworkOnline(),
    networkFailureKind,
    browserName: environment.browserName ?? browser.name,
    browserVersion: environment.browserVersion ?? browser.version,
    retryable: isRetryableCategory(category),
    errorType,
    componentStack: capComponentStack(input.componentStack),
  };
}

export function buildDiagnosticEnvironmentFromPreferences(input: {
  locale?: string;
  theme?: string;
  density?: string;
}): DiagnosticEnvironment {
  const browser = readBrowserInfo();
  return {
    pathname: currentPathname(),
    locale: input.locale,
    theme: input.theme,
    density: input.density,
    browserPlatform: compactBrowserPlatform(),
    browserName: browser.name,
    browserVersion: browser.version,
    networkOnline: readNetworkOnline(),
  };
}
