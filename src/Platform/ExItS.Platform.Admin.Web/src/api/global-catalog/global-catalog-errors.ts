import { PlatformApiError } from "@/api/platform-http";

export type GlobalCatalogMutationFailureKind =
  | "validation"
  | "session_expired"
  | "permission_denied"
  | "not_found"
  | "conflict"
  | "domain_rule"
  | "network"
  | "unknown";

export type GlobalCatalogMutationFailure = {
  kind: GlobalCatalogMutationFailureKind;
  status?: number;
  errorCode?: string;
  message: string;
  correlationId?: string;
  traceId?: string;
};

export function classifyGlobalCatalogMutationFailure(
  error: unknown,
): GlobalCatalogMutationFailure {
  if (error instanceof TypeError || (error instanceof Error && error.name === "TypeError")) {
    return { kind: "network", message: "The request could not reach the Platform API." };
  }

  if (!(error instanceof PlatformApiError)) {
    return { kind: "unknown", message: "The global catalog request failed." };
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
  if (error.status === 400) {
    return { kind: "validation", ...base };
  }
  if (error.status === 422) {
    return { kind: "domain_rule", ...base };
  }

  return { kind: "unknown", ...base };
}
