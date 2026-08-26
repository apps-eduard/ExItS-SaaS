import { PlatformApiError } from "@/api/platform-http";

export const COMMERCIAL_MUTATION_KINDS = [
  "validation",
  "session_expired",
  "permission_denied",
  "not_found",
  "conflict",
  "domain_rule",
  "payment_required",
  "network",
  "unknown",
] as const;

export type CommercialMutationKind = (typeof COMMERCIAL_MUTATION_KINDS)[number];

export type CommercialMutationFailure = {
  kind: CommercialMutationKind;
  status?: number;
  errorCode?: string;
  message: string;
  correlationId?: string;
  traceId?: string;
};

const PAYMENT_REQUIRED_CODES = new Set([
  "application.payment.required_for_paid_activation",
]);

function looksLikePaymentRequired(error: PlatformApiError): boolean {
  const code = error.errorCode ?? "";
  if (PAYMENT_REQUIRED_CODES.has(code)) {
    return true;
  }
  return code.toLowerCase().includes("payment_required");
}

function looksLikeDomainRule(error: PlatformApiError): boolean {
  const code = (error.errorCode ?? "").toLowerCase();
  return (
    error.status === 422 ||
    code.includes("invalid_transition") ||
    code.includes("ineligible") ||
    code.includes("not_eligible") ||
    code.includes("domain")
  );
}

/**
 * User-safe mapping for future commercial UI. Does not expose stack traces.
 * Server problem.detail/title remain the operator-facing text when present.
 */
export function classifyCommercialMutationFailure(error: unknown): CommercialMutationFailure {
  if (error instanceof TypeError || (error instanceof Error && error.name === "TypeError")) {
    return { kind: "network", message: "The request could not reach the Platform API." };
  }

  if (!(error instanceof PlatformApiError)) {
    return { kind: "unknown", message: "The commercial request failed." };
  }

  const message = error.problem.detail ?? error.problem.title ?? error.message;
  const base = {
    status: error.status,
    errorCode: error.errorCode,
    message,
    correlationId: error.requestCorrelationId,
    traceId: error.traceId,
  };

  if (error.status === 401) {
    return { kind: "session_expired", ...base };
  }
  if (error.status === 403) {
    return { kind: "permission_denied", ...base };
  }
  if (error.status === 404) {
    return { kind: "not_found", ...base };
  }
  if (error.status === 409) {
    return { kind: "conflict", ...base };
  }
  if (looksLikePaymentRequired(error)) {
    return { kind: "payment_required", ...base };
  }
  if (error.status === 400 && looksLikeDomainRule(error)) {
    return { kind: "domain_rule", ...base };
  }
  if (error.status === 400) {
    return { kind: "validation", ...base };
  }
  if (looksLikeDomainRule(error)) {
    return { kind: "domain_rule", ...base };
  }

  return { kind: "unknown", ...base };
}
