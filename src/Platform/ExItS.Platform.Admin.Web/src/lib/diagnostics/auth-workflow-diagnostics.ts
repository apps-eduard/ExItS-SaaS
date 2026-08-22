import type { CredentialWorkflowFailureKind } from "@/api/auth/auth-errors";
import type { DiagnosticCategory } from "@/lib/diagnostics/diagnostic-types";

export function mapCredentialWorkflowCategory(
  kind: CredentialWorkflowFailureKind,
): DiagnosticCategory | null {
  switch (kind) {
    case "network":
      return "NETWORK_ERROR";
    case "service_unavailable":
      return "SERVICE_UNAVAILABLE";
    case "rate_limited":
      return "RATE_LIMITED";
    case "password_invalid":
    case "invalid_token":
    case "expired_token":
    case "invalid_display_name":
    case "invalid_email":
      return null;
    default:
      return "UNEXPECTED_CLIENT_ERROR";
  }
}

export function shouldShowCredentialWorkflowDiagnostic(
  kind: CredentialWorkflowFailureKind,
): boolean {
  return mapCredentialWorkflowCategory(kind) !== null;
}
