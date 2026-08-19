import { platformRequest } from "@/api/platform-http";
import type {
  AuthSession,
  LocalValidationIdentity,
  LoginRequest,
  LoginResultDto,
} from "@/api/auth/auth-types";

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
  }).then(omitSessionToken);
}

export function getAuthMe(baseUrl: string, signal?: AbortSignal): Promise<AuthSession> {
  return platformRequest<AuthSession>(baseUrl, {
    path: "/api/v1/platform/auth/me",
    signal,
  });
}

export function logout(baseUrl: string, signal?: AbortSignal): Promise<void> {
  return platformRequest<void>(baseUrl, {
    method: "POST",
    path: "/api/v1/platform/auth/logout",
    signal,
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
