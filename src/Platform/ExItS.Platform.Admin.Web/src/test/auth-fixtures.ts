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
  catalogProductItems?: Array<{
    id: string;
    code: string;
    displayName: string;
    status: string;
    createdAtUtc?: string;
    updatedAtUtc?: string;
  }>;
  catalogPlanItems?: Array<Record<string, unknown>>;
  catalogProductPlans?: Array<Record<string, unknown>>;
  catalogTrials?: Array<Record<string, unknown>>;
  catalogPlanVersions?: Array<Record<string, unknown>>;
  failCatalogProductDetail?: boolean;
  forbiddenCatalogProductDetail?: boolean;
  notFoundCatalogProductDetail?: boolean;
  failCatalogPlans?: boolean;
  forbiddenCatalogPlans?: boolean;
  failCatalogPlanDetail?: boolean;
  forbiddenCatalogPlanDetail?: boolean;
  notFoundCatalogPlanDetail?: boolean;
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
  failBranches?: boolean;
  forbiddenBranches?: boolean;
  branchItems?: Array<Record<string, unknown>>;
  failMembers?: boolean;
  forbiddenMembers?: boolean;
  memberItems?: Array<Record<string, unknown>>;
  memberTotalCount?: number;
  failInvitations?: boolean;
  forbiddenInvitations?: boolean;
  invitationItems?: Array<Record<string, unknown>>;
  invitationTotalCount?: number;
  failOrgSubscriptions?: boolean;
  forbiddenOrgSubscriptions?: boolean;
  orgSubscriptionItems?: Array<Record<string, unknown>>;
  orgSubscriptionTotalCount?: number;
  failEntitlementSnapshots?: boolean;
  forbiddenEntitlementSnapshots?: boolean;
  entitlementSnapshotItems?: Array<Record<string, unknown>>;
  entitlementSnapshotTotalCount?: number;
  entitlementLatestSnapshot?: Record<string, unknown> | null;
  failEntitlementLatest?: boolean;
  forbiddenEntitlementLatest?: boolean;
  featureOverrideItems?: Array<Record<string, unknown>>;
  featureOverrideTotalCount?: number;
  forbiddenFeatureOverrides?: boolean;
  catalogFeatureItems?: Array<Record<string, unknown>>;
  entitlementMutationError?: { status: number; errorCode: string; detail: string };
  onEntitlementMutation?: (method: string, path: string, body: unknown) => void;
  planMutationError?: { status: number; errorCode: string; detail: string };
  onPlanMutation?: (method: string, path: string, body: unknown) => void;
  failOrgPayments?: boolean;
  forbiddenOrgPayments?: boolean;
  orgPaymentItems?: Array<Record<string, unknown>>;
  orgPaymentTotalCount?: number;
  paymentMutationError?: { status: number; errorCode: string; detail: string };
  onPaymentMutation?: (method: string, path: string, body: unknown) => void;
  failOrgAudit?: boolean;
  forbiddenOrgAudit?: boolean;
  orgAuditItems?: Array<Record<string, unknown>>;
  orgAuditTotalCount?: number;
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

export function mockAuthenticatedFetch(options: AuthenticatedFetchOptions = {}) {
  let paymentItems = [...(options.orgPaymentItems ?? [])];
  let entitlementSnapshotItems = [...(options.entitlementSnapshotItems ?? [])];
  let latestEntitlementSnapshot =
    options.entitlementLatestSnapshot === undefined
      ? entitlementSnapshotItems[0] ?? null
      : options.entitlementLatestSnapshot;
  let featureOverrideItems = [...(options.featureOverrideItems ?? [])];
  const defaultCatalogPlanItems = [
    {
      id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      productCode: "pinoy-business-pos",
      code: "growth",
      displayName: "Growth",
      status: "Active",
      maxBranches: 3,
      maxActiveStaff: 10,
      maxActivePosDevices: 3,
      maxActiveBusinessTypes: 3,
      customerCreditEnabled: true,
      advancedReportsEnabled: true,
      exportEnabled: true,
      trialAllowed: true,
      defaultTrialDays: 14,
      monthlyPrice: 699,
      annualPrice: 6990,
      currencyCode: "PHP",
      updatedAtUtc: "2026-08-01T08:00:00Z",
    },
  ];
  let catalogPlanItems = [...(options.catalogPlanItems ?? defaultCatalogPlanItems)];
  let catalogPlanVersions = [...(options.catalogPlanVersions ?? [])];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const path = pathnameOf(url);
    const method = init?.method ?? "GET";
    const parseBody = () => {
      try {
        return init?.body ? JSON.parse(String(init.body)) : null;
      } catch {
        return null;
      }
    };
    if (url.includes("/api/v1/platform/auth/logout")) {
      return {
        ok: true,
        status: 204,
        json: async () => null,
        text: async () => "",
      } as Response;
    }
    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" });
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
    if (url.includes("/api/v1/platform/catalog/products")) {
      const productPlansMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans$/,
      );
      if (productPlansMatch) {
        return jsonResponse(200, options.catalogProductPlans ?? catalogPlanItems);
      }
      const planCommercialMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans\/([0-9a-fA-F-]{36})\/commercial$/,
      );
      if (planCommercialMatch && method === "PATCH") {
        const body = parseBody() as Record<string, unknown>;
        options.onPlanMutation?.(method, path, body);
        if (options.planMutationError) {
          return jsonResponse(options.planMutationError.status, {
            title: "Error",
            status: options.planMutationError.status,
            detail: options.planMutationError.detail,
            errorCode: options.planMutationError.errorCode,
          });
        }
        const plan = catalogPlanItems.find((item) => item.id === planCommercialMatch[2]);
        if (!plan) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        const updated = {
          ...plan,
          ...body,
          updatedAtUtc: new Date().toISOString(),
        };
        catalogPlanItems = catalogPlanItems.map((item) =>
          item.id === planCommercialMatch[2] ? updated : item,
        );
        return jsonResponse(200, updated);
      }
      const planRenameMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans\/([0-9a-fA-F-]{36})\/rename$/,
      );
      if (planRenameMatch && method === "PATCH") {
        const body = parseBody() as Record<string, unknown>;
        options.onPlanMutation?.(method, path, body);
        const plan = catalogPlanItems.find((item) => item.id === planRenameMatch[2]);
        if (!plan) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        const updated = {
          ...plan,
          displayName: body.displayName,
          updatedAtUtc: new Date().toISOString(),
        };
        catalogPlanItems = catalogPlanItems.map((item) =>
          item.id === planRenameMatch[2] ? updated : item,
        );
        return jsonResponse(200, updated);
      }
      const planLifecycleMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans\/([0-9a-fA-F-]{36})\/(activate|deactivate|retire)$/,
      );
      if (planLifecycleMatch && method === "POST") {
        options.onPlanMutation?.(method, path, null);
        const plan = catalogPlanItems.find((item) => item.id === planLifecycleMatch[2]);
        if (!plan) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        const nextStatus =
          planLifecycleMatch[3] === "activate"
            ? "Active"
            : planLifecycleMatch[3] === "deactivate"
              ? "Inactive"
              : "Retired";
        const updated = { ...plan, status: nextStatus, updatedAtUtc: new Date().toISOString() };
        catalogPlanItems = catalogPlanItems.map((item) =>
          item.id === planLifecycleMatch[2] ? updated : item,
        );
        return jsonResponse(200, updated);
      }
      const draftVersionMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans\/([0-9a-fA-F-]{36})\/versions\/draft$/,
      );
      if (draftVersionMatch && method === "POST") {
        const body = parseBody() as Record<string, unknown>;
        options.onPlanMutation?.(method, path, body);
        const version = {
          id: crypto.randomUUID(),
          planId: draftVersionMatch[2],
          productCode: draftVersionMatch[1],
          versionNumber: body.versionNumber,
          status: "Draft",
          billingPeriod: body.billingPeriod ?? "Monthly",
          trialEligible: body.trialEligible ?? false,
          grants: body.grants ?? [],
        };
        catalogPlanVersions = [...catalogPlanVersions, version];
        return jsonResponse(201, version);
      }
      const publishVersionMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans\/([0-9a-fA-F-]{36})\/versions\/(\d+)\/publish$/,
      );
      if (publishVersionMatch && method === "POST") {
        options.onPlanMutation?.(method, path, null);
        const versionNumber = Number.parseInt(publishVersionMatch[3]!, 10);
        const version = catalogPlanVersions.find(
          (item) =>
            item.planId === publishVersionMatch[2] && item.versionNumber === versionNumber,
        );
        if (!version) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        const updated = { ...version, status: "Published" };
        catalogPlanVersions = catalogPlanVersions.map((item) =>
          item.id === version.id ? updated : item,
        );
        return jsonResponse(200, updated);
      }
      const featureGrantMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/plans\/([0-9a-fA-F-]{36})\/versions\/(\d+)\/feature-grants\/([^/]+)$/,
      );
      if (featureGrantMatch && method === "PUT") {
        const body = parseBody() as Record<string, unknown>;
        options.onPlanMutation?.(method, path, body);
        const versionNumber = Number.parseInt(featureGrantMatch[3]!, 10);
        const featureCode = decodeURIComponent(featureGrantMatch[4]!);
        const version = catalogPlanVersions.find(
          (item) =>
            item.planId === featureGrantMatch[2] && item.versionNumber === versionNumber,
        );
        if (!version) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        const grants = Array.isArray(version.grants) ? [...version.grants] : [];
        const nextGrant = {
          featureCode,
          enabled: body.enabled === true,
          numericLimit: body.numericLimit ?? null,
        };
        const index = grants.findIndex(
          (grant) => (grant as { featureCode?: string }).featureCode === featureCode,
        );
        if (index >= 0) {
          grants[index] = nextGrant;
        } else {
          grants.push(nextGrant);
        }
        const updated = { ...version, grants };
        catalogPlanVersions = catalogPlanVersions.map((item) =>
          item.id === version.id ? updated : item,
        );
        return jsonResponse(200, updated);
      }
      if (/\/plans\/[^/]+\/versions$/.test(path)) {
        return jsonResponse(200, catalogPlanVersions);
      }
      if (/\/trials$/.test(path)) {
        return jsonResponse(200, options.catalogTrials ?? []);
      }
      const productFeaturesMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([^/]+)\/features$/,
      );
      if (productFeaturesMatch) {
        return jsonResponse(
          200,
          options.catalogFeatureItems ?? [
            {
              productCode: productFeaturesMatch[1],
              featureCode: "store-customer-credit",
              displayName: "Customer credit",
              valueType: "Boolean",
              status: "Active",
            },
            {
              productCode: productFeaturesMatch[1],
              featureCode: "store-export",
              displayName: "Export",
              valueType: "Boolean",
              status: "Active",
            },
          ],
        );
      }
      const productDetailMatch = path.match(
        /\/api\/v1\/platform\/catalog\/products\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$/,
      );
      if (productDetailMatch) {
        if (options.forbiddenCatalogProductDetail) {
          return jsonResponse(403, { title: "Forbidden", status: 403 });
        }
        if (options.notFoundCatalogProductDetail) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        if (options.failCatalogProductDetail) {
          return jsonResponse(500, { title: "Error", status: 500 });
        }
        const items = options.catalogProductItems ?? [
          {
            id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
            code: "future-product-x",
            displayName: "Future Product X",
            status: "Active",
            createdAtUtc: "2026-01-01T08:00:00Z",
            updatedAtUtc: "2026-08-01T08:00:00Z",
          },
        ];
        const match = items.find((item) => item.id === productDetailMatch[1]);
        if (!match) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        return jsonResponse(200, match);
      }
      return jsonResponse(
        200,
        pagedJson(
          options.catalogProductItems ?? [
            {
              id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              code: "future-product-x",
              displayName: "Future Product X",
              status: "Active",
            },
          ],
          options.catalogProductItems?.length ?? 1,
          100,
        ),
      );
    }
    if (url.includes("/api/v1/platform/catalog/plans")) {
      const planDetailMatch = path.match(
        /\/api\/v1\/platform\/catalog\/plans\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$/,
      );
      if (planDetailMatch) {
        if (options.forbiddenCatalogPlanDetail) {
          return jsonResponse(403, { title: "Forbidden", status: 403 });
        }
        if (options.notFoundCatalogPlanDetail) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        if (options.failCatalogPlanDetail) {
          return jsonResponse(500, { title: "Error", status: 500 });
        }
        const items = catalogPlanItems;
        const match = items.find((item) => item.id === planDetailMatch[1]);
        if (!match) {
          return jsonResponse(404, { title: "Not Found", status: 404 });
        }
        return jsonResponse(200, match);
      }
      if (options.forbiddenCatalogPlans) {
        return jsonResponse(403, { title: "Forbidden", status: 403 });
      }
      if (options.failCatalogPlans) {
        return jsonResponse(500, { title: "Error", status: 500 });
      }
      const items = catalogPlanItems;
      return jsonResponse(200, pagedJson(items, items.length, 20));
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
    const branchesGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/branches$/,
    );
    if (branchesGet) {
      if (options.forbiddenBranches) {
        return jsonResponse(403, { title: "Forbidden", status: 403, detail: "branch-secret" });
      }
      if (options.failBranches) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Branch list failed.",
        });
      }
      return jsonResponse(200, options.branchItems ?? []);
    }
    const membersGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/members$/,
    );
    if (membersGet) {
      if (options.forbiddenMembers) {
        return jsonResponse(403, { title: "Forbidden", status: 403, detail: "member-secret" });
      }
      if (options.failMembers) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Member list failed.",
        });
      }
      const items = options.memberItems ?? [];
      return jsonResponse(
        200,
        pagedJson(items, options.memberTotalCount ?? items.length, items.length || 20),
      );
    }
    const invitationsGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/invitations$/,
    );
    if (invitationsGet) {
      if (options.forbiddenInvitations) {
        return jsonResponse(403, {
          title: "Forbidden",
          status: 403,
          detail: "invitation-secret",
        });
      }
      if (options.failInvitations) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Invitation list failed.",
        });
      }
      const items = options.invitationItems ?? [];
      return jsonResponse(
        200,
        pagedJson(items, options.invitationTotalCount ?? items.length, items.length || 20),
      );
    }
    const orgSubscriptionsGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/subscriptions$/,
    );
    if (orgSubscriptionsGet) {
      if (options.forbiddenOrgSubscriptions) {
        return jsonResponse(403, {
          title: "Forbidden",
          status: 403,
          detail: "subscription-secret",
        });
      }
      if (options.failOrgSubscriptions) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Subscription list failed.",
        });
      }
      const items = options.orgSubscriptionItems ?? [];
      return jsonResponse(
        200,
        pagedJson(items, options.orgSubscriptionTotalCount ?? items.length, items.length || 20),
      );
    }
    if (path.includes("/plan-change-preview")) {
      const current = options.orgSubscriptionItems?.[0];
      const target = options.catalogProductPlans?.find((plan) => plan.id !== current?.planId);
      return jsonResponse(200, {
        currentPlanId: current?.planId ?? "current-plan",
        currentPlanDisplayName: current?.planDisplayName ?? "Current",
        targetPlanId: target?.id ?? "target-plan",
        targetPlanDisplayName: target?.displayName ?? "Target",
        usageConflicts: [],
        lostFeatures: [],
        hasBlockingUsageConflicts: false,
      });
    }
    const entitlementSnapshotsGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/products\/[^/]+\/entitlements\/snapshots$/,
    );
    if (entitlementSnapshotsGet && method === "GET") {
      if (options.forbiddenEntitlementSnapshots) {
        return jsonResponse(403, {
          title: "Forbidden",
          status: 403,
          detail: "entitlement-secret",
        });
      }
      if (options.failEntitlementSnapshots) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Entitlement snapshot list failed.",
        });
      }
      const items = entitlementSnapshotItems;
      return jsonResponse(
        200,
        pagedJson(items, options.entitlementSnapshotTotalCount ?? items.length, items.length || 20),
      );
    }
    const entitlementLatestGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/products\/[^/]+\/entitlements\/snapshots\/latest$/,
    );
    if (entitlementLatestGet) {
      if (options.forbiddenEntitlementLatest || options.forbiddenEntitlementSnapshots) {
        return jsonResponse(403, { title: "Forbidden", status: 403 });
      }
      if (options.failEntitlementLatest) {
        return jsonResponse(500, { title: "Error", status: 500, detail: "Latest snapshot failed." });
      }
      if (!latestEntitlementSnapshot) {
        return jsonResponse(404, {
          title: "Not Found",
          status: 404,
          detail: "No entitlement snapshot was found for this organization and product.",
          errorCode: "application.entitlement_snapshot.not_found",
        });
      }
      return jsonResponse(200, latestEntitlementSnapshot);
    }
    const featureOverridesGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/products\/[^/]+\/feature-overrides$/,
    );
    if (featureOverridesGet && method === "GET") {
      if (options.forbiddenFeatureOverrides) {
        return jsonResponse(403, { title: "Forbidden", status: 403 });
      }
      const items = featureOverrideItems;
      return jsonResponse(
        200,
        pagedJson(items, options.featureOverrideTotalCount ?? items.length, items.length || 20),
      );
    }
    const entitlementGenerateMatch = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F-]{36})\/products\/([^/]+)\/entitlements\/snapshots$/,
    );
    if (entitlementGenerateMatch && method === "POST") {
      if (options.entitlementMutationError) {
        return jsonResponse(options.entitlementMutationError.status, {
          title: "Error",
          status: options.entitlementMutationError.status,
          detail: options.entitlementMutationError.detail,
          errorCode: options.entitlementMutationError.errorCode,
        });
      }
      const body = parseBody();
      options.onEntitlementMutation?.(method, path, body);
      const currentVersion = Number(latestEntitlementSnapshot?.snapshotVersion ?? 0);
      const expected = (body as Record<string, unknown> | null)?.expectedNextVersion;
      if (expected != null && Number(expected) !== currentVersion + 1) {
        return jsonResponse(409, {
          title: "Conflict",
          status: 409,
          detail: "Snapshot version conflict.",
          errorCode: "application.entitlement_snapshot.version_conflict",
        });
      }
      const nextVersion = currentVersion + 1;
      const created = {
        ...(latestEntitlementSnapshot ?? entitlementSnapshotItems[0] ?? {}),
        id: crypto.randomUUID(),
        snapshotVersion: nextVersion,
        generatedAtUtc: new Date().toISOString(),
      };
      latestEntitlementSnapshot = created;
      entitlementSnapshotItems = [created, ...entitlementSnapshotItems];
      return jsonResponse(201, created);
    }
    const entitlementReconcileMatch = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F-]{36})\/products\/([^/]+)\/entitlements\/reconcile$/,
    );
    if (entitlementReconcileMatch && method === "POST") {
      if (options.entitlementMutationError) {
        return jsonResponse(options.entitlementMutationError.status, {
          title: "Error",
          status: options.entitlementMutationError.status,
          detail: options.entitlementMutationError.detail,
          errorCode: options.entitlementMutationError.errorCode,
        });
      }
      const body = parseBody();
      options.onEntitlementMutation?.(method, path, body);
      const currentVersion = Number(latestEntitlementSnapshot?.snapshotVersion ?? 0);
      const created = {
        ...(latestEntitlementSnapshot ?? entitlementSnapshotItems[0] ?? {}),
        id: crypto.randomUUID(),
        snapshotVersion: currentVersion + 1,
        generatedAtUtc: new Date().toISOString(),
      };
      latestEntitlementSnapshot = created;
      entitlementSnapshotItems = [created, ...entitlementSnapshotItems];
      return jsonResponse(201, created);
    }
    const featureOverrideCreateMatch = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F-]{36})\/products\/([^/]+)\/feature-overrides$/,
    );
    if (featureOverrideCreateMatch && method === "POST") {
      if (options.entitlementMutationError) {
        return jsonResponse(options.entitlementMutationError.status, {
          title: "Error",
          status: options.entitlementMutationError.status,
          detail: options.entitlementMutationError.detail,
          errorCode: options.entitlementMutationError.errorCode,
        });
      }
      const body = parseBody() as Record<string, unknown>;
      options.onEntitlementMutation?.(method, path, body);
      const created = {
        id: crypto.randomUUID(),
        organizationId: featureOverrideCreateMatch[1],
        productCode: decodeURIComponent(featureOverrideCreateMatch[2]!),
        featureCode: body.featureCode,
        enabled: body.enabled,
        numericLimit: body.numericLimit ?? null,
        reason: body.reason,
        effectiveFromUtc: new Date().toISOString(),
        expiresAtUtc: body.expiresAtUtc ?? null,
        status: "Active",
        createdAtUtc: new Date().toISOString(),
        createdByUserId: sampleSession.userId,
      };
      featureOverrideItems = [created, ...featureOverrideItems];
      return jsonResponse(201, created);
    }
    const featureOverrideRevokeMatch = path.match(
      /\/api\/v1\/platform\/feature-overrides\/([0-9a-fA-F-]{36})\/revoke$/,
    );
    if (featureOverrideRevokeMatch && method === "POST") {
      const body = parseBody();
      options.onEntitlementMutation?.(method, path, body);
      const overrideId = featureOverrideRevokeMatch[1]!;
      const existing = featureOverrideItems.find((item) => item.id === overrideId);
      if (!existing) {
        return jsonResponse(404, { title: "Not Found", status: 404 });
      }
      const revoked = {
        ...existing,
        status: "Revoked",
        revokedAtUtc: new Date().toISOString(),
        revokedByUserId: sampleSession.userId,
        revocationReason: (body as Record<string, unknown> | null)?.reason,
      };
      featureOverrideItems = featureOverrideItems.map((item) =>
        item.id === overrideId ? revoked : item,
      );
      return jsonResponse(200, revoked);
    }
    const orgPaymentsGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/payments$/,
    );
    if (orgPaymentsGet) {
      if (options.forbiddenOrgPayments) {
        return jsonResponse(403, {
          title: "Forbidden",
          status: 403,
          detail: "payment-secret",
          amount: 9999.99,
        });
      }
      if (options.failOrgPayments) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Payment list failed.",
        });
      }
      const items = paymentItems;
      return jsonResponse(
        200,
        pagedJson(items, options.orgPaymentTotalCount ?? items.length, items.length || 20),
      );
    }
    if (path.endsWith("/payments/manual") && method === "POST") {
      if (options.paymentMutationError) {
        return jsonResponse(options.paymentMutationError.status, {
          title: "Error",
          status: options.paymentMutationError.status,
          detail: options.paymentMutationError.detail,
          errorCode: options.paymentMutationError.errorCode,
        });
      }
      const body = parseBody() as Record<string, unknown>;
      options.onPaymentMutation?.(method, path, body);
      const created = {
        id: crypto.randomUUID(),
        organizationId: body.organizationId,
        productCode: body.productCode,
        amount: body.amount,
        currencyCode: body.currencyCode,
        method: body.method,
        externalReference: body.externalReference,
        status: "PendingConfirmation",
        paidAtUtc: body.paidAtUtc,
      };
      paymentItems = [created, ...paymentItems];
      return jsonResponse(201, created);
    }
    const paymentActionMatch = path.match(
      /\/api\/v1\/platform\/payments\/([0-9a-fA-F-]{36})\/(confirm|reject|void|activate-subscription|upgrade-subscription)$/,
    );
    if (paymentActionMatch && method === "POST") {
      if (options.paymentMutationError) {
        return jsonResponse(options.paymentMutationError.status, {
          title: "Error",
          status: options.paymentMutationError.status,
          detail: options.paymentMutationError.detail,
          errorCode: options.paymentMutationError.errorCode,
        });
      }
      const paymentId = paymentActionMatch[1]!;
      const action = paymentActionMatch[2]!;
      const body = parseBody();
      options.onPaymentMutation?.(method, path, body);
      const existing = paymentItems.find((item) => item.id === paymentId);
      if (!existing) {
        return jsonResponse(404, {
          title: "Not Found",
          status: 404,
          detail: "Payment was not found.",
          errorCode: "application.payment.not_found",
        });
      }
      if (action === "confirm") {
        const updated = { ...existing, status: "Confirmed", confirmedAtUtc: new Date().toISOString() };
        paymentItems = paymentItems.map((item) => (item.id === paymentId ? updated : item));
        return jsonResponse(200, updated);
      }
      if (action === "reject") {
        const updated = {
          ...existing,
          status: "Rejected",
          rejectedAtUtc: new Date().toISOString(),
          rejectionReason: (body as Record<string, unknown>)?.reason ?? "rejected",
        };
        paymentItems = paymentItems.map((item) => (item.id === paymentId ? updated : item));
        return jsonResponse(200, updated);
      }
      if (action === "void") {
        const updated = {
          ...existing,
          status: "Voided",
          voidedAtUtc: new Date().toISOString(),
          voidReason: (body as Record<string, unknown>)?.reason ?? "voided",
        };
        paymentItems = paymentItems.map((item) => (item.id === paymentId ? updated : item));
        return jsonResponse(200, updated);
      }
      const activateBody = body as Record<string, unknown>;
      const subscriptionId = activateBody?.subscriptionId;
      const subscription = (options.orgSubscriptionItems ?? []).find((item) => item.id === subscriptionId);
      const updatedPayment = {
        ...existing,
        status: "Confirmed",
        subscriptionId,
        confirmedAtUtc: existing.confirmedAtUtc ?? new Date().toISOString(),
      };
      paymentItems = paymentItems.map((item) => (item.id === paymentId ? updatedPayment : item));
      if (action === "upgrade-subscription") {
        const targetPlanId = activateBody?.targetPlanId;
        return jsonResponse(200, {
          payment: updatedPayment,
          subscription: subscription
            ? {
                ...subscription,
                planId: targetPlanId ?? subscription.planId,
                status: "Active",
              }
            : {
                id: subscriptionId,
                organizationId: existing.organizationId,
                productCode: existing.productCode,
                planId: targetPlanId,
                status: "Active",
              },
        });
      }
      return jsonResponse(200, {
        payment: updatedPayment,
        subscription: subscription ?? {
          id: subscriptionId,
          organizationId: existing.organizationId,
          productCode: existing.productCode,
          status: "Active",
        },
      });
    }
    const createPaidMatch = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F-]{36})\/subscriptions$/,
    );
    if (createPaidMatch && method === "POST" && !path.includes("/trials") && !path.includes("/from-catalog")) {
      const body = parseBody() as Record<string, unknown>;
      options.onPaymentMutation?.(method, path, body);
      const subscription = {
        id: crypto.randomUUID(),
        organizationId: createPaidMatch[1],
        productCode: "pinoy-business-pos",
        planId: body.planId,
        status: "Active",
      };
      return jsonResponse(201, subscription);
    }
    if (path.endsWith("/local-validation/payments/simulate") && method === "POST") {
      const body = parseBody() as Record<string, unknown>;
      options.onPaymentMutation?.(method, path, body);
      return jsonResponse(200, {
        status: "Succeeded",
        provider: "local-validation",
        providerReference: "LV-TEST",
        amount: body.amount,
        currencyCode: body.currencyCode,
        isTest: true,
        idempotencyKey: body.idempotencyKey,
      });
    }
    const orgAuditGet = path.match(
      /\/api\/v1\/platform\/organizations\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\/audit$/,
    );
    if (orgAuditGet) {
      if (options.forbiddenOrgAudit) {
        return jsonResponse(403, {
          title: "Forbidden",
          status: 403,
          detail: "audit-secret",
        });
      }
      if (options.failOrgAudit) {
        return jsonResponse(500, {
          title: "Error",
          status: 500,
          detail: "Organization audit list failed.",
        });
      }
      const items = options.orgAuditItems ?? [];
      return jsonResponse(
        200,
        pagedJson(items, options.orgAuditTotalCount ?? items.length, items.length || 20),
      );
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
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}
