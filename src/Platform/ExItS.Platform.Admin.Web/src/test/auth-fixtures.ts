import { vi } from "vitest";
import { AUTH_ERROR_CODES, type AuthSession } from "@/api/auth/auth-types";

export const sampleSession: AuthSession = {
  sessionId: "11111111-1111-1111-1111-111111111111",
  userId: "22222222-2222-2222-2222-222222222222",
  username: "olivia",
  displayName: "Olivia Mendoza",
  email: "olivia@example.test",
  expiresAtUtc: "2026-08-19T12:00:00Z",
  absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
  lastActivityAtUtc: "2026-08-19T11:00:00Z",
  selectedOrganizationId: null,
  selectedOrganizationDisplayName: null,
  organizationSelectionState: "None",
  activeOrganizationCount: 0,
  accountClass: "Platform",
  allowedScope: "Platform",
};

export const sampleAuthorization = {
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  platformUserId: "22222222-2222-2222-2222-222222222222",
  organizationId: null,
  permissions: [
    "platform.permission.view_portfolio",
    "platform.permission.manage_organizations",
    "platform.permission.manage_platform_users",
    "platform.permission.manage_memberships",
    "platform.permission.manage_subscriptions",
    "platform.permission.manage_manual_payments",
    "platform.permission.manage_entitlement_overrides",
    "platform.permission.view_audit_records",
    "platform.permission.view_global_catalog",
    "platform.permission.view_privacy_compliance",
  ],
};

export function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
  } as Response;
}

export function mockUnauthenticatedFetch(): void {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/v1/platform/auth/me")) {
        return jsonResponse(401, {
          title: "Unauthorized",
          status: 401,
          detail: "Session is not valid.",
          errorCode: AUTH_ERROR_CODES.sessionInvalid,
        });
      }
      if (url.includes("/api/v1/platform/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      return jsonResponse(404, { title: "Not Found", status: 404 });
    }),
  );
}

export function mockAuthenticatedFetch(): void {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/v1/platform/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/api/v1/platform/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      return jsonResponse(404, { title: "Not Found", status: 404 });
    }),
  );
}
