import { PlatformAntiforgeryDefaults } from "@/api/platform/antiforgery";

/**
 * Classifies workspace bind / POS operational failures for UX.
 * Server remains authoritative — this only maps completed outcomes to user language.
 */

export type WorkspaceBindFailureKind =
  | "product_access_denied"
  | "subscription_suspended"
  | "session_expired"
  | "staff_org_lock"
  | "branch_not_accessible"
  | "open_shift_blocks_branch_switch"
  | "profile_required"
  | "antiforgery"
  | "service_unavailable"
  | "generic";

export type WorkspaceBindFailure = {
  kind: WorkspaceBindFailureKind;
  /** User-facing detail (never engineering bearer text). */
  detailKey:
    | "accessDenied.detail"
    | "accessDenied.sessionExpired"
    | "accessDenied.staffOrgLock"
    | "accessDenied.branchNotAccessible"
    | "accessDenied.openShiftBlocksBranchSwitch"
    | "accessDenied.profileRequired"
    | "accessDenied.antiforgery"
    | "accessDenied.serviceUnavailable"
    | "accessDenied.generic"
    | "commercial.productUnavailable"
    | "commercial.subscriptionSuspended"
    | "commercial.accessUnavailable";
  /** Keep technical detail for console / diagnostics only. */
  technicalDetail: string | null;
};

const BEARER_INACTIVE_PATTERN = /bearer access token is inactive or invalid/i;
const SESSION_EXPIRED_PATTERN = /session (has )?expired|session is invalid/i;

export function classifyWorkspaceBindFailure(input: {
  status?: number | null;
  errorCode?: string | null;
  detail?: string | null;
  reason?:
    | "organization_context"
    | "branch_context"
    | "grant"
    | "access_denied"
    | "context"
    | null;
}): WorkspaceBindFailure {
  const detail = input.detail?.trim() || null;
  const errorCode = input.errorCode?.trim() || null;
  const status = input.status ?? null;

  if (input.reason === "access_denied") {
    const lowered = detail?.toLowerCase() ?? "";
    if (lowered.includes("suspended") || lowered.includes("subscription_ineligible")) {
      return {
        kind: "subscription_suspended",
        detailKey: "commercial.subscriptionSuspended",
        technicalDetail: detail,
      };
    }

    return {
      kind: "product_access_denied",
      detailKey: "commercial.productUnavailable",
      technicalDetail: detail,
    };
  }

  if (
    errorCode === "application.auth.product_access_denied" ||
    errorCode === "application.auth.product_access_inactive" ||
    errorCode === "application.auth.product_access_missing"
  ) {
    const lowered = detail?.toLowerCase() ?? "";
    if (lowered.includes("suspended") || lowered.includes("subscription_ineligible")) {
      return {
        kind: "subscription_suspended",
        detailKey: "commercial.subscriptionSuspended",
        technicalDetail: detail,
      };
    }

    return {
      kind: "product_access_denied",
      detailKey: "commercial.productUnavailable",
      technicalDetail: detail,
    };
  }

  if (
    status === 401 ||
    errorCode === "pos.actor.required" ||
    errorCode === "application.auth.session_invalid" ||
    errorCode === "application.auth.session_expired" ||
    errorCode === "application.auth.access_token_invalid" ||
    (detail !== null &&
      (BEARER_INACTIVE_PATTERN.test(detail) || SESSION_EXPIRED_PATTERN.test(detail)))
  ) {
    return {
      kind: "session_expired",
      detailKey: "accessDenied.sessionExpired",
      technicalDetail: detail,
    };
  }

  if (
    errorCode === "pos.branch.switch.shift_open" ||
    (detail !== null && /open cashier shift before switching POS branch/i.test(detail))
  ) {
    return {
      kind: "open_shift_blocks_branch_switch",
      detailKey: "accessDenied.openShiftBlocksBranchSwitch",
      technicalDetail: detail,
    };
  }

  if (
    status === 404 ||
    errorCode === "application.branch.not_found" ||
    errorCode === "application.branch.not_selectable" ||
    errorCode === "application.branch.access_denied" ||
    errorCode === "pos.customer_order.branch.not_found" ||
    (detail !== null && /selected branch is not an Active branch/i.test(detail)) ||
    (detail !== null && /BranchId cannot be an empty GUID/i.test(detail))
  ) {
    return {
      kind: "branch_not_accessible",
      detailKey: "accessDenied.branchNotAccessible",
      technicalDetail: detail,
    };
  }

  if (
    errorCode === PlatformAntiforgeryDefaults.invalidErrorCode ||
    status === 419 ||
    (status === 400 && detail !== null && /antiforgery/i.test(detail))
  ) {
    return {
      kind: "antiforgery",
      detailKey: "accessDenied.antiforgery",
      technicalDetail: detail,
    };
  }

  if (
    status === 500 ||
    status === 502 ||
    status === 503 ||
    status === 429 ||
    errorCode === "pos.internal_error" ||
    errorCode === "pos.rate_limit.exceeded" ||
    errorCode === "platform.rate_limit.exceeded"
  ) {
    return {
      kind: "service_unavailable",
      detailKey: "accessDenied.serviceUnavailable",
      technicalDetail: detail,
    };
  }

  return {
    kind: "generic",
    detailKey: "accessDenied.generic",
    technicalDetail: detail,
  };
}

export function shouldClearWorkspaceSessionOnOperationalBranchFailure(input: {
  status?: number | null;
  errorCode?: string | null;
  detail?: string | null;
}): boolean {
  const failure = classifyWorkspaceBindFailure(input);
  return failure.kind === "session_expired";
}

/** Title key for ErrorState — product denial vs session vs service. */
export function workspaceBindFailureTitleKey(
  kind: WorkspaceBindFailureKind,
):
  | "accessDenied.title"
  | "accessDenied.sessionTitle"
  | "accessDenied.serviceTitle"
  | "accessDenied.branchTitle"
  | "accessDenied.antiforgeryTitle"
  | "commercial.subscriptionSuspended" {
  switch (kind) {
    case "subscription_suspended":
      return "commercial.subscriptionSuspended";
    case "session_expired":
      return "accessDenied.sessionTitle";
    case "service_unavailable":
      return "accessDenied.serviceTitle";
    case "branch_not_accessible":
      return "accessDenied.branchTitle";
    case "open_shift_blocks_branch_switch":
      return "accessDenied.branchTitle";
    case "antiforgery":
      return "accessDenied.antiforgeryTitle";
    default:
      return "accessDenied.title";
  }
}
