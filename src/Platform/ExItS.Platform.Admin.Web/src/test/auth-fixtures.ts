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

export function pagedJson<T>(items: T[] = [], totalCount = items.length, pageSize = 1) {
  return { items, totalCount, page: 1, pageSize };
}

export function jsonResponse(status: number, body: unknown): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => (typeof body === "string" ? body : JSON.stringify(body)),
  } as Response;
}

export function textResponse(status: number, body: string): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => body,
  } as Response;
}

function pathnameOf(url: string): string {
  try {
    return new URL(url, "http://local.test").pathname;
  } catch {
    return url;
  }
}

export type AuthenticatedFetchOptions = {
  permissions?: string[];
  failOrganizations?: boolean;
  failOrganizationGet?: boolean;
  failCommercialSummary?: boolean;
  forbiddenOrganization?: boolean;
  organizationTotalCount?: number;
  organizationItems?: Array<{
    id: string;
    displayName: string;
    slug: string;
    status: string;
    createdAtUtc?: string;
    updatedAtUtc?: string;
    profile?: Record<string, unknown>;
    branding?: Record<string, unknown>;
  }>;
  commercialSummary?: {
    subscriptions?: Array<Record<string, unknown>>;
    payments?: Array<Record<string, unknown>>;
    latestEntitlements?: Array<Record<string, unknown>>;
  };
};

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

export function mockAuthenticatedFetch(options: AuthenticatedFetchOptions = {}): void {
  vi.stubGlobal(
    "fetch",
    vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      const path = pathnameOf(url);
      if (url.includes("/api/v1/platform/auth/logout")) {
        return {
          ok: true,
          status: 204,
          json: async () => null,
          text: async () => "",
        } as Response;
      }
      if (url.includes("/api/v1/platform/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/api/v1/platform/authorization/me")) {
        return jsonResponse(200, {
          ...sampleAuthorization,
          permissions: options.permissions ?? sampleAuthorization.permissions,
        });
      }
      if (path.endsWith("/health/ready") || path.endsWith("/health")) {
        return textResponse(200, "Healthy");
      }
      if (
        url.includes("/api/v1/platform/admin/organizations") &&
        url.includes("commercial-summary")
      ) {
        if (options.failCommercialSummary) {
          return jsonResponse(500, {
            title: "Error",
            status: 500,
            detail: "Commercial summary failed.",
          });
        }
        const summary = options.commercialSummary ?? {};
        return jsonResponse(200, {
          subscriptions: summary.subscriptions ?? [],
          payments: summary.payments ?? [],
          latestEntitlements: summary.latestEntitlements ?? [],
        });
      }
      const organizationGet = path.match(
        /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$/,
      );
      if (organizationGet) {
        if (options.forbiddenOrganization) {
          return jsonResponse(403, { title: "Forbidden", status: 403, detail: "Forbidden." });
        }
        if (options.failOrganizationGet) {
          return jsonResponse(500, {
            title: "Error",
            status: 500,
            detail: "Organization load failed.",
          });
        }
        const items = options.organizationItems ?? [];
        const match = items.find((item) => item.id === organizationGet[1]);
        if (!match) {
          return jsonResponse(404, {
            title: "Not Found",
            status: 404,
            detail: "Platform Organization was not found.",
            errorCode: "application.organization.not_found",
          });
        }
        return jsonResponse(200, match);
      }
      if (url.includes("/api/v1/platform/organizations")) {
        if (options.failOrganizations) {
          return jsonResponse(500, {
            title: "Error",
            status: 500,
            detail: "Organization list failed.",
          });
        }
        const items = options.organizationItems ?? [];
        return jsonResponse(
          200,
          pagedJson(items, options.organizationTotalCount ?? items.length, items.length || 1),
        );
      }
      if (url.includes("/api/v1/platform/subscriptions")) {
        return jsonResponse(200, pagedJson([], 0, 1));
      }
      if (url.includes("/api/v1/platform/users")) {
        return jsonResponse(200, pagedJson([], 0, 5));
      }
      if (url.includes("/api/v1/platform/audit")) {
        return jsonResponse(200, pagedJson([], 0, 8));
      }
      return jsonResponse(404, { title: "Not Found", status: 404 });
    }),
  );
}
