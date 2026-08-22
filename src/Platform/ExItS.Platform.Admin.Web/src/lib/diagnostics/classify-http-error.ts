import type { DiagnosticCategory } from "@/lib/diagnostics/diagnostic-types";

const DOMAIN_ERROR_CODES = [
  "application.",
  "platform.display_name.",
  "platform.email.",
  "platform.credential.",
  "platform.auth.",
  "platform.organization.",
  "platform.subscription.",
  "platform.payment.",
  "platform.catalog.",
  "platform.plan.",
  "platform.entitlement.",
  "platform.rate_limit.",
];

function isDomainErrorCode(errorCode: string | undefined): boolean {
  if (!errorCode) {
    return false;
  }
  return DOMAIN_ERROR_CODES.some((prefix) => errorCode.startsWith(prefix));
}

export function classifyHttpDiagnosticCategory(
  status: number,
  errorCode?: string,
): DiagnosticCategory {
  if (errorCode?.includes("antiforgery")) {
    return "SECURITY_REQUEST_ERROR";
  }

  if (status === 400) {
    return isDomainErrorCode(errorCode) ? "DOMAIN_ERROR" : "VALIDATION_ERROR";
  }
  if (status === 401) {
    return isDomainErrorCode(errorCode) ? "DOMAIN_ERROR" : "AUTHENTICATION_REQUIRED";
  }
  if (status === 403) {
    return "FORBIDDEN";
  }
  if (status === 404) {
    return "NOT_FOUND";
  }
  if (status === 409) {
    return "CONFLICT";
  }
  if (status === 419) {
    return "SECURITY_REQUEST_ERROR";
  }
  if (status === 429) {
    return "RATE_LIMITED";
  }
  if (status === 502 || status === 503 || status === 504) {
    return "SERVICE_UNAVAILABLE";
  }
  if (status >= 500) {
    return "SERVER_ERROR";
  }

  return isDomainErrorCode(errorCode) ? "DOMAIN_ERROR" : "VALIDATION_ERROR";
}

export function isRetryableCategory(category: DiagnosticCategory): boolean {
  return (
    category === "NETWORK_ERROR" ||
    category === "SERVICE_UNAVAILABLE" ||
    category === "TIMEOUT" ||
    category === "RATE_LIMITED" ||
    category === "SERVER_ERROR"
  );
}

export function networkErrorCode(category: DiagnosticCategory): string {
  switch (category) {
    case "NETWORK_ERROR":
      return "NETWORK_UNAVAILABLE";
    case "SERVICE_UNAVAILABLE":
      return "SERVICE_UNAVAILABLE";
    case "TIMEOUT":
      return "REQUEST_TIMEOUT";
    case "RATE_LIMITED":
      return "platform.rate_limit.exceeded";
    default:
      return category;
  }
}
