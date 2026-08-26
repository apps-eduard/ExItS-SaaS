import { clearPlatformAntiforgeryToken, platformRequest } from "@/api/platform-http";
import type {
  ActivateAccountRequest,
  AuthSession,
  AuthWorkflowAck,
  LocalValidationIdentity,
  LoginRequest,
  LoginResultDto,
  RegisterPersonalAccountRequest,
  RequestPasswordResetRequest,
  ResetPasswordRequest,
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
