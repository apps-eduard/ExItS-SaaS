import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

/**
 * Organization governance password step-up (Platform `GovernanceCriticalActionCodes`).
 * A grant is scoped to user + organization + action code + target type + target id and is
 * single-use with a short server-side lifetime. The POS client never stores the password.
 */
export const POS_DEVICE_REVOKE_ACTION = "platform.pos_device.revoke";
export const TARGET_POS_DEVICE = "PosDevice";

export type GovernanceStepUpRequest = {
  actionCode: string;
  targetType: string;
  targetId?: string | null;
  currentPassword: string;
};

export type GovernanceStepUpTokenDto = {
  stepUpToken: string;
  expiresAtUtc: string;
  actionCode: string;
  targetType: string;
  targetId: string | null;
};

/**
 * Friendly, non-enumerating failure reasons. `wrong_password` deliberately covers every
 * credential rejection so the UI never reveals whether an account exists or is locked.
 */
export type GovernanceStepUpFailureReason =
  | "password_required"
  | "wrong_password"
  | "expired"
  | "consumed"
  | "invalid_scope"
  | "not_allowed"
  | "unavailable";

export type GovernanceStepUpResult =
  | { ok: true; value: GovernanceStepUpTokenDto }
  | {
      ok: false;
      reason: GovernanceStepUpFailureReason;
      status: number;
      errorCode?: string;
      body: PlatformProblemDetails | null;
    };

function governanceStepUpPath(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/governance/step-up`;
}

function normalizeToken(raw: Record<string, unknown>): GovernanceStepUpTokenDto {
  const targetId = raw.targetId ?? raw.TargetId ?? null;
  return {
    stepUpToken: String(raw.stepUpToken ?? raw.StepUpToken ?? ""),
    expiresAtUtc: String(raw.expiresAtUtc ?? raw.ExpiresAtUtc ?? ""),
    actionCode: String(raw.actionCode ?? raw.ActionCode ?? ""),
    targetType: String(raw.targetType ?? raw.TargetType ?? ""),
    targetId: targetId === null || targetId === undefined ? null : String(targetId),
  };
}

/** Map Platform error codes / statuses onto the friendly reasons the revoke UI can explain. */
export function classifyGovernanceStepUpFailure(
  status: number,
  errorCode: string | undefined,
): GovernanceStepUpFailureReason {
  const code = (errorCode ?? "").toLowerCase();

  if (code.includes("governance_step_up_expired")) {
    return "expired";
  }
  if (code.includes("governance_step_up_consumed")) {
    return "consumed";
  }
  if (code.includes("governance_step_up_invalid")) {
    return "invalid_scope";
  }
  if (
    code.includes("current_password_invalid") ||
    code.includes("password_invalid") ||
    code.includes("credential") ||
    code.includes("lockout")
  ) {
    return "wrong_password";
  }
  if (code.includes("step_up_required")) {
    return "password_required";
  }
  if (status === 401 || status === 403) {
    return "not_allowed";
  }
  if (status === 400 || status === 422) {
    return "wrong_password";
  }
  return "unavailable";
}

/**
 * Exchange the signed-in user's current password for a single-use governance step-up token.
 * The caller passes the returned token straight to the guarded mutation and then discards it.
 */
export async function issueGovernanceStepUp(
  organizationId: string,
  request: GovernanceStepUpRequest,
  signal?: AbortSignal,
): Promise<GovernanceStepUpResult> {
  if (!request.currentPassword.trim()) {
    return {
      ok: false,
      reason: "password_required",
      status: 0,
      body: null,
    };
  }

  try {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: governanceStepUpPath(organizationId),
      body: {
        actionCode: request.actionCode,
        targetType: request.targetType,
        targetId: request.targetId ?? null,
        currentPassword: request.currentPassword,
      },
      signal,
    });
    return { ok: true, value: normalizeToken(payload) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return {
        ok: false,
        reason: classifyGovernanceStepUpFailure(error.status, error.errorCode),
        status: error.status,
        errorCode: error.errorCode,
        body: error.problem,
      };
    }
    throw error;
  }
}

/** Convenience wrapper for the only POS governance action this client performs. */
export async function issuePosDeviceRevokeStepUp(
  organizationId: string,
  posDeviceId: string,
  currentPassword: string,
  signal?: AbortSignal,
): Promise<GovernanceStepUpResult> {
  return issueGovernanceStepUp(
    organizationId,
    {
      actionCode: POS_DEVICE_REVOKE_ACTION,
      targetType: TARGET_POS_DEVICE,
      targetId: posDeviceId,
      currentPassword,
    },
    signal,
  );
}
