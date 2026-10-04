import { clearPlatformAntiforgeryToken, platformRequest } from "@/api/platform-http";
import type {
  AcceptInvitationResult,
  ActivateAccountRequest,
  AuthSession,
  AuthWorkflowAck,
  ChangePasswordRequest,
  ConfirmRecoveryEmailRequest,
  CredentialStatus,
  LocalValidationIdentity,
  LoginRequest,
  LoginResultDto,
  ProductEntryIssue,
  RecoveryEmailRequest,
  RegisterPersonalAccountRequest,
  RequestPasswordResetRequest,
  ResetPasswordRequest,
  WebHandoffCreated,
  WebWorkspaceList,
} from "@/api/auth/auth-types";

type AuthWorkflowAckDto = AuthWorkflowAck & {
  debugToken?: string | null;
};

function omitDebugToken(dto: AuthWorkflowAckDto): AuthWorkflowAck {
  return {
    message: dto.message,
    expiresAtUtc: dto.expiresAtUtc ?? null,
  };
}

function omitSessionToken(dto: LoginResultDto): AuthSession {
  return {
    sessionId: dto.sessionId,
    userId: dto.userId,
    username: dto.username,
    displayName: dto.displayName,
    email: dto.email,
    expiresAtUtc: dto.expiresAtUtc,
    absoluteExpiresAtUtc: dto.absoluteExpiresAtUtc,
    lastActivityAtUtc: dto.lastActivityAtUtc,
    selectedOrganizationId: dto.selectedOrganizationId,
    selectedOrganizationDisplayName: dto.selectedOrganizationDisplayName,
    organizationSelectionState: dto.organizationSelectionState,
    activeOrganizationCount: dto.activeOrganizationCount,
    accountProfileId: dto.accountProfileId,
    accountClass: dto.accountClass,
    allowedScope: dto.allowedScope,
  };
}

export function login(
  baseUrl: string,
  request: LoginRequest,
  signal?: AbortSignal,
): Promise<AuthSession> {
  return platformRequest<LoginResultDto>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/login",
    body: {
      usernameOrEmail: request.usernameOrEmail,
      password: request.password,
    },
    signal,
    skipAntiforgery: true,
  }).then(omitSessionToken);
}

export function getAuthMe(baseUrl: string, signal?: AbortSignal): Promise<AuthSession> {
  return platformRequest<AuthSession>(baseUrl, {
    path: "/api/v1/platform/auth/me",
    signal,
    // Bootstrap /me failures become unauthenticated (not the session-expired UX path).
    skipSessionExpiry: true,
  });
}

export function logout(baseUrl: string, signal?: AbortSignal): Promise<void> {
  return platformRequest<void>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/logout",
    signal,
  }).finally(() => {
    clearPlatformAntiforgeryToken();
  });
}

export function getLocalValidationEnabled(baseUrl: string, signal?: AbortSignal): Promise<boolean> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/local-validation/enabled",
    signal,
  }).then((value) => value === true);
}

export function listQuickLoginIdentities(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<LocalValidationIdentity[]> {
  return platformRequest<LocalValidationIdentity[]>(baseUrl, {
    path: "/api/v1/platform/local-validation/quick-login-identities",
    signal,
  });
}

export function registerPersonalAccount(
  baseUrl: string,
  request: RegisterPersonalAccountRequest,
  signal?: AbortSignal,
): Promise<AuthWorkflowAck> {
  return platformRequest<AuthWorkflowAckDto>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/register",
    body: {
      displayName: request.displayName,
      email: request.email,
    },
    signal,
    skipAntiforgery: true,
  }).then(omitDebugToken);
}

export function activateAccount(
  baseUrl: string,
  request: ActivateAccountRequest,
  signal?: AbortSignal,
): Promise<void> {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/activate-account",
    body: {
      token: request.token,
      password: request.password,
    },
    signal,
    skipAntiforgery: true,
  }).then(() => undefined);
}

export function requestPasswordReset(
  baseUrl: string,
  request: RequestPasswordResetRequest,
  signal?: AbortSignal,
): Promise<AuthWorkflowAck> {
  return platformRequest<AuthWorkflowAckDto>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/forgot-password",
    body: {
      usernameOrEmail: request.usernameOrEmail,
    },
    signal,
    skipAntiforgery: true,
  }).then(omitDebugToken);
}

export function resetPassword(
  baseUrl: string,
  request: ResetPasswordRequest,
  signal?: AbortSignal,
): Promise<void> {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/reset-password",
    body: {
      token: request.token,
      newPassword: request.newPassword,
    },
    signal,
    skipAntiforgery: true,
  }).then(() => undefined);
}

const SESSION_TOKEN_HEADER = "X-ExItS-Session-Token";

function readString(record: Record<string, unknown>, ...keys: string[]): string | null {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim().length > 0) {
      return value;
    }
  }
  return null;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value !== null && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

export function changePassword(
  baseUrl: string,
  request: ChangePasswordRequest,
  signal?: AbortSignal,
): Promise<void> {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/change-password",
    body: {
      currentPassword: request.currentPassword,
      newPassword: request.newPassword,
    },
    signal,
  }).then(() => undefined);
}

export function getMyCredentials(baseUrl: string, signal?: AbortSignal): Promise<CredentialStatus> {
  return platformRequest<Record<string, unknown>>(baseUrl, {
    path: "/api/v1/platform/auth/credentials",
    signal,
  }).then((payload) => {
    const record = asRecord(payload) ?? {};
    return {
      userId: readString(record, "userId", "UserId") ?? "",
      hasPassword: record.hasPassword === true || record.HasPassword === true,
      emailVerified: record.emailVerified === true || record.EmailVerified === true,
      recoveryEmail: readString(record, "recoveryEmail", "RecoveryEmail"),
      recoveryEmailVerified:
        record.recoveryEmailVerified === true || record.RecoveryEmailVerified === true,
      pendingRecoveryEmail: readString(record, "pendingRecoveryEmail", "PendingRecoveryEmail"),
      needsRecoveryEmailPrompt:
        record.needsRecoveryEmailPrompt === true || record.NeedsRecoveryEmailPrompt === true,
    };
  });
}

export function requestRecoveryEmail(
  baseUrl: string,
  request: RecoveryEmailRequest,
  signal?: AbortSignal,
): Promise<AuthWorkflowAck> {
  return platformRequest<AuthWorkflowAckDto>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/recovery-email/request",
    body: { recoveryEmail: request.recoveryEmail },
    signal,
  }).then(omitDebugToken);
}

export function confirmRecoveryEmail(
  baseUrl: string,
  request: ConfirmRecoveryEmailRequest,
  signal?: AbortSignal,
): Promise<void> {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/recovery-email/confirm",
    body: { token: request.token },
    signal,
    skipAntiforgery: true,
  }).then(() => undefined);
}

export function skipRecoveryEmail(baseUrl: string, signal?: AbortSignal): Promise<void> {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/recovery-email/skip",
    signal,
  }).then(() => undefined);
}

export function clearRecoveryEmail(baseUrl: string, signal?: AbortSignal): Promise<CredentialStatus> {
  return platformRequest<unknown>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/recovery-email/clear",
    signal,
  }).then(() => getMyCredentials(baseUrl, signal));
}

export function acceptOrganizationInvitation(
  baseUrl: string,
  token: string,
  password: string,
  signal?: AbortSignal,
): Promise<AcceptInvitationResult> {
  return platformRequest<Record<string, unknown>>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/invitations/accept",
    body: { token, password },
    signal,
    skipAntiforgery: true,
  }).then((payload) => {
    const record = asRecord(payload) ?? {};
    return {
      staffLogin: readString(record, "staffLogin", "StaffLogin") ?? "",
      contactEmail: readString(record, "contactEmail", "ContactEmail") ?? "",
      organizationDisplayName:
        readString(record, "organizationDisplayName", "OrganizationDisplayName") ?? "",
    };
  });
}

export function listWebWorkspaces(baseUrl: string, signal?: AbortSignal): Promise<WebWorkspaceList> {
  return platformRequest<Record<string, unknown>>(baseUrl, {
    path: "/api/v1/platform/auth/workspaces",
    signal,
  }).then((payload) => {
    const record = asRecord(payload);
    const raw = record?.workspaces ?? record?.Workspaces;
    const items = Array.isArray(raw) ? raw : [];
    return {
      workspaces: items.flatMap((item) => {
        const row = asRecord(item);
        if (!row) {
          return [];
        }
        const app = readString(row, "app", "App");
        const label = readString(row, "label", "Label");
        if (!app || !label) {
          return [];
        }
        return [
          {
            app,
            label,
            organizationId: readString(row, "organizationId", "OrganizationId"),
            organizationName: readString(row, "organizationName", "OrganizationName"),
            roleLabel: readString(row, "roleLabel", "RoleLabel"),
          },
        ];
      }),
    };
  });
}

export function createWebHandoff(
  baseUrl: string,
  targetApp: string,
  organizationId: string | null,
  returnPath: string,
  signal?: AbortSignal,
): Promise<WebHandoffCreated> {
  return platformRequest<Record<string, unknown>>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/web-handoff",
    body: {
      targetApp,
      organizationId,
      returnPath,
    },
    signal,
  }).then((payload) => {
    const record = asRecord(payload) ?? {};
    const ticket = readString(record, "ticket", "Ticket");
    if (!ticket) {
      throw new Error("Handoff ticket was not issued.");
    }
    return {
      ticket,
      targetApp: readString(record, "targetApp", "TargetApp") ?? targetApp,
      returnPath: readString(record, "returnPath", "ReturnPath") ?? returnPath,
    };
  });
}

/** Validates a one-time session token and lets the API set the HttpOnly cookie. The token is not returned. */
export function adoptSessionToken(
  baseUrl: string,
  sessionToken: string,
  signal?: AbortSignal,
): Promise<AuthSession> {
  return platformRequest<AuthSession>(baseUrl, {
    path: "/api/v1/platform/auth/me",
    headers: { [SESSION_TOKEN_HEADER]: sessionToken },
    signal,
    skipSessionExpiry: true,
  });
}

export function issueProductEntryToken(
  baseUrl: string,
  organizationId: string,
  productCode: string,
  signal?: AbortSignal,
): Promise<ProductEntryIssue> {
  return platformRequest<Record<string, unknown>>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/token",
    body: {
      grantType: "session",
      organizationId,
      productCode,
    },
    signal,
  }).then((payload) => {
    const record = asRecord(payload) ?? {};
    const accessToken = readString(record, "accessToken", "AccessToken");
    if (!accessToken) {
      throw new Error("Product entry token was not issued.");
    }
    const allowed = record.productAccessAllowed ?? record.ProductAccessAllowed;
    return {
      accessToken,
      expiresAtUtc: readString(record, "expiresAtUtc", "ExpiresAtUtc") ?? "",
      productCode: readString(record, "productCode", "ProductCode"),
      productAccessAllowed: typeof allowed === "boolean" ? allowed : null,
      productAccessReasonCode: readString(
        record,
        "productAccessReasonCode",
        "ProductAccessReasonCode",
      ),
    };
  });
}
