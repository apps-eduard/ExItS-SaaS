import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformAntiforgeryDefaults } from "@/api/platform-antiforgery";
import { activatePlan, updatePlanCommercial } from "@/api/catalog/plan-mutations-client";
import { renameProduct } from "@/api/catalog/product-mutations-client";
import { startTrialSubscription, suspendSubscription } from "@/api/subscriptions/subscription-mutations-client";
import { createManualPayment } from "@/api/payments/payment-mutations-client";
import { generateEntitlementSnapshot } from "@/api/entitlements/entitlement-mutations-client";
import { resetCommercialMutationInFlight } from "@/api/commercial/commercial-http";
import { classifyCommercialMutationFailure } from "@/api/commercial/commercial-errors";
import {
  commercialQueryKeyRoots,
  invalidateCommercialQueries,
} from "@/api/commercial/commercial-query-keys";
import { COMMERCIAL_BACKEND_GAPS } from "@/api/commercial/commercial-backend-gaps";
import { PlatformApiError, clearPlatformAntiforgeryToken, platformRequest } from "@/api/platform-http";
import { mapCatalogPlan } from "@/api/catalog/plan-catalog-client";
import { mapCatalogProduct } from "@/api/catalog/product-catalog-client";
import { mapOrganizationSubscription } from "@/api/organizations/organization-client";
import { mapOrganizationPayment } from "@/api/organizations/organization-client";
import { mapEntitlementSnapshot } from "@/api/organizations/organization-client";
import type { CatalogPlan } from "@/api/catalog/plan-catalog-types";
import type { OrganizationSubscription } from "@/api/organizations/subscription-list-query";

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const plan: CatalogPlan = {
  id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
  productCode: "pinoy-business-pos",
  code: "growth",
  displayName: "Growth",
  status: "Active",
  updatedAtUtc: "2026-08-22T00:00:00Z",
};

const subscription: OrganizationSubscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  productCode: "pinoy-business-pos",
  planId: plan.id,
  status: "Trialing",
  version: 3,
};

function jsonOk(body: unknown): Response {
  return {
    ok: true,
    status: 200,
    json: async () => body,
  } as Response;
}

function jsonProblem(status: number, errorCode: string, detail: string): Response {
  return {
    ok: false,
    status,
    json: async () => ({
      title: "Error",
      status,
      detail,
      errorCode,
    }),
  } as Response;
}

describe("commercial mutation transport", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    resetCommercialMutationInFlight();
  });

  it("GET requests do not bootstrap antiforgery", async () => {
    const fetchMock = vi.fn(async () => jsonOk({ id: plan.id, productCode: plan.productCode, code: plan.code, displayName: plan.displayName, status: plan.status }));
    vi.stubGlobal("fetch", fetchMock);
    await platformRequest("http://platform.test", {
      path: "/api/v1/platform/catalog/plans/dddddddd-dddd-dddd-dddd-dddddddddddd",
    });
    expect(fetchMock).toHaveBeenCalledOnce();
    const firstCall = fetchMock.mock.calls[0] as unknown as [RequestInfo | URL, RequestInit];
    expect(firstCall[1].method ?? "GET").toBe("GET");
    expect(new Headers(firstCall[1].headers).get(PlatformAntiforgeryDefaults.headerName)).toBeNull();
  });

  it("plan mutation uses existing antiforgery transport and serializes commercial body", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      expect(url).toContain("/catalog/products/pinoy-business-pos/plans/dddddddd-dddd-dddd-dddd-dddddddddddd/commercial");
      expect(init?.method).toBe("PATCH");
      expect(init?.credentials).toBe("include");
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
      expect(JSON.parse(String(init?.body))).toMatchObject({
        displayName: "Growth",
        maxActivePosDevices: 3,
        expectedUpdatedAtUtc: "2026-08-22T00:00:00Z",
      });
      return jsonOk(plan);
    });
    vi.stubGlobal("fetch", fetchMock);

    const result = await updatePlanCommercial("http://platform.test", "pinoy-business-pos", plan.id, {
      displayName: "Growth",
      maxBranches: 3,
      maxActiveStaff: 10,
      maxActivePosDevices: 3,
      maxActiveBusinessTypes: 3,
      maxAreas: 5,
      customerCreditEnabled: true,
      advancedReportsEnabled: true,
      exportEnabled: true,
      trialAllowed: true,
      defaultTrialDays: 14,
      sortOrder: 20,
      monthlyPrice: 699,
      annualPrice: 6990,
      currencyCode: "PHP",
      expectedUpdatedAtUtc: "2026-08-22T00:00:00Z",
    });
    expect(result).toEqual(mapCatalogPlan(plan));
  });

  it("product rename uses existing antiforgery transport and serializes rename body", async () => {
    const product = {
      id: productId,
      code: "pinoy-business-pos",
      displayName: "Pinoy Business POS (Dev)",
      status: "Active",
      updatedAtUtc: "2026-08-22T00:00:00Z",
    };
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      expect(url).toContain(`/catalog/products/${productId}/rename`);
      expect(init?.method).toBe("PATCH");
      expect(init?.credentials).toBe("include");
      expect(new Headers(init?.headers).get("X-XSRF-TOKEN")).toBe("csrf-token");
      expect(JSON.parse(String(init?.body))).toMatchObject({
        displayName: "Pinoy Business POS (Dev)",
        expectedUpdatedAtUtc: "2026-08-22T00:00:00Z",
      });
      return jsonOk(product);
    });
    vi.stubGlobal("fetch", fetchMock);

    const result = await renameProduct("http://platform.test", productId, {
      displayName: "Pinoy Business POS (Dev)",
      expectedUpdatedAtUtc: "2026-08-22T00:00:00Z",
    });
    expect(result).toEqual(mapCatalogProduct(product));
  });

  it("reuses the in-memory CSRF token instead of fetching per mutation", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      return jsonOk(plan);
    });
    vi.stubGlobal("fetch", fetchMock);
    await activatePlan("http://platform.test", "pinoy-business-pos", plan.id);
    await activatePlan("http://platform.test", "pinoy-business-pos", plan.id);
    const tokenCalls = fetchMock.mock.calls.filter(([url]) =>
      String(url).endsWith(PlatformAntiforgeryDefaults.tokenPath),
    );
    expect(tokenCalls).toHaveLength(1);
  });

  it("does not issue a second HTTP mutation for an identical in-flight request", async () => {
    let resolveMutation: ((value: Response) => void) | undefined;
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      return await new Promise<Response>((resolve) => {
        resolveMutation = resolve;
      });
    });
    vi.stubGlobal("fetch", fetchMock);

    const first = suspendSubscription("http://platform.test", subscription.id, { expectedVersion: 3 });
    const second = suspendSubscription("http://platform.test", subscription.id, { expectedVersion: 3 });
    await vi.waitFor(() => {
      expect(fetchMock.mock.calls.filter(([url]) => String(url).includes("/suspend"))).toHaveLength(1);
    });
    resolveMutation?.(jsonOk(subscription));
    await expect(Promise.all([first, second])).resolves.toEqual([
      mapOrganizationSubscription(subscription),
      mapOrganizationSubscription(subscription),
    ]);
  });

  it("preserves 403 permission denied", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonProblem(403, "application.forbidden", "Permission denied.");
      }),
    );
    const error = await updatePlanCommercial("http://platform.test", "pinoy-business-pos", plan.id, {
      displayName: "Growth",
      maxBranches: 3,
      maxActiveStaff: 10,
      maxActivePosDevices: 3,
      maxActiveBusinessTypes: 3,
      maxAreas: 5,
      customerCreditEnabled: true,
      advancedReportsEnabled: true,
      exportEnabled: true,
      trialAllowed: true,
      defaultTrialDays: 14,
      sortOrder: 20,
      monthlyPrice: 699,
      annualPrice: 6990,
      currencyCode: "PHP",
    }).catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(PlatformApiError);
    expect((error as PlatformApiError).status).toBe(403);
    expect(classifyCommercialMutationFailure(error).kind).toBe("permission_denied");
  });

  it("preserves 409 conflict", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonProblem(409, "application.concurrency_conflict", "The plan was updated by another operator.");
      }),
    );
    const error = await updatePlanCommercial("http://platform.test", "pinoy-business-pos", plan.id, {
      displayName: "Growth",
      maxBranches: 3,
      maxActiveStaff: 10,
      maxActivePosDevices: 3,
      maxActiveBusinessTypes: 3,
      maxAreas: 5,
      customerCreditEnabled: true,
      advancedReportsEnabled: true,
      exportEnabled: true,
      trialAllowed: true,
      defaultTrialDays: 14,
      sortOrder: 20,
      monthlyPrice: 699,
      annualPrice: 6990,
      currencyCode: "PHP",
      expectedUpdatedAtUtc: "2026-01-01T00:00:00Z",
    }).catch((caught: unknown) => caught);
    expect((error as PlatformApiError).status).toBe(409);
    expect(classifyCommercialMutationFailure(error).kind).toBe("conflict");
  });

  it("preserves domain/payment-required errors", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonProblem(
          400,
          "application.payment.required_for_paid_activation",
          "Paid subscription creation requires a confirmed paymentId.",
        );
      }),
    );
    const error = await startTrialSubscription("http://platform.test", subscription.organizationId, {
      planId: plan.id,
      planVersionId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      trialDefinitionId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
    }).catch((caught: unknown) => caught);
    const mapped = classifyCommercialMutationFailure(error);
    expect(mapped.kind).toBe("payment_required");
    expect(mapped.message).toContain("confirmed paymentId");
  });

  it("preserves 401 session expired", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonProblem(401, "application.unauthorized", "Session expired.");
      }),
    );
    const error = await suspendSubscription("http://platform.test", subscription.id).catch(
      (caught: unknown) => caught,
    );
    expect((error as PlatformApiError).status).toBe(401);
    expect(classifyCommercialMutationFailure(error).kind).toBe("session_expired");
  });

  it("preserves 422 domain/business rule failures", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
          return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonProblem(
          422,
          "application.subscription.invalid_transition",
          "Suspended subscriptions cannot enter trial.",
        );
      }),
    );
    const error = await startTrialSubscription("http://platform.test", subscription.organizationId, {
      planId: plan.id,
      planVersionId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      trialDefinitionId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
    }).catch((caught: unknown) => caught);
    expect((error as PlatformApiError).status).toBe(422);
    expect(classifyCommercialMutationFailure(error).kind).toBe("domain_rule");
    expect(classifyCommercialMutationFailure(error).message).toContain("cannot enter trial");
  });
});

describe("commercial mutation serialization", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    resetCommercialMutationInFlight();
  });

  it("serializes subscription start-trial and suspend bodies", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/trials")) {
        expect(JSON.parse(String(init?.body))).toEqual({
          planId: plan.id,
          planVersionId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
          trialDefinitionId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
        });
        return jsonOk(subscription);
      }
      expect(url).toContain(`/subscriptions/${subscription.id}/suspend`);
      expect(JSON.parse(String(init?.body))).toEqual({ expectedVersion: 3 });
      return jsonOk({ ...subscription, status: "Suspended" });
    });
    vi.stubGlobal("fetch", fetchMock);
    await startTrialSubscription("http://platform.test", subscription.organizationId, {
      planId: plan.id,
      planVersionId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      trialDefinitionId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
    });
    await suspendSubscription("http://platform.test", subscription.id, { expectedVersion: 3 });
  });

  it("serializes manual payment create body", async () => {
    const payment = {
      id: "99999999-9999-9999-9999-999999999999",
      organizationId: subscription.organizationId,
      productCode: "pinoy-business-pos",
      amount: 699,
      currencyCode: "PHP",
      method: "GCash",
      status: "PendingConfirmation",
    };
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      expect(JSON.parse(String(init?.body))).toMatchObject({
        organizationId: subscription.organizationId,
        method: "GCash",
        amount: 699,
      });
      return jsonOk(payment);
    });
    vi.stubGlobal("fetch", fetchMock);
    const result = await createManualPayment("http://platform.test", {
      organizationId: subscription.organizationId,
      productCode: "pinoy-business-pos",
      amount: 699,
      currencyCode: "PHP",
      method: "GCash",
      externalReference: "GCASH-1",
      paidAtUtc: "2026-08-22T00:00:00Z",
    });
    expect(result).toEqual(mapOrganizationPayment(payment));
  });

  it("serializes entitlement snapshot generate body", async () => {
    const snapshot = {
      id: "88888888-8888-8888-8888-888888888888",
      organizationId: subscription.organizationId,
      productCode: "pinoy-business-pos",
      subscriptionId: subscription.id,
      planCode: "growth",
      snapshotVersion: 2,
      subscriptionStatus: "Trialing",
      inGracePeriod: false,
      grants: [{ featureCode: "plan-max-active-pos-devices", enabled: true, numericLimit: 3 }],
    };
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      if (String(input).endsWith(PlatformAntiforgeryDefaults.tokenPath)) {
        return jsonOk({ headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      expect(String(input)).toContain("/entitlements/snapshots");
      expect(JSON.parse(String(init?.body))).toEqual({ expectedNextVersion: 2 });
      return jsonOk(snapshot);
    });
    vi.stubGlobal("fetch", fetchMock);
    const result = await generateEntitlementSnapshot(
      "http://platform.test",
      subscription.organizationId,
      "pinoy-business-pos",
      { expectedNextVersion: 2 },
    );
    expect(result).toEqual(mapEntitlementSnapshot(snapshot));
  });
});

describe("commercial query invalidation", () => {
  it("invalidates organization commercial keys without dashboard users", async () => {
    const seen: unknown[][] = [];
    await invalidateCommercialQueries(
      {
        invalidateQueries: ({ queryKey }) => {
          seen.push([...queryKey]);
        },
      },
      { organizationId: subscription.organizationId },
    );
    expect(seen).toContainEqual([
      ...commercialQueryKeyRoots.organizationSubscriptions,
      subscription.organizationId,
    ]);
    expect(seen).toContainEqual([
      ...commercialQueryKeyRoots.organizationEntitlements,
      subscription.organizationId,
    ]);
    expect(seen).toContainEqual([
      ...commercialQueryKeyRoots.organizationBilling,
      subscription.organizationId,
    ]);
    expect(seen).toContainEqual([...commercialQueryKeyRoots.dashboardSubscriptions]);
    expect(seen.some((key) => key[0] === "dashboard" && key[1] === "users")).toBe(false);
    expect(seen.some((key) => key[0] === "catalog-plans")).toBe(false);
  });

  it("does not mutate cached reads when a mutation fails", async () => {
    const cache = new Map<string, unknown>([["organizations:subscriptions", ["cached"]]]);
    await expect(
      invalidateCommercialQueries(
        {
          invalidateQueries: () => {
            throw new Error("should not invalidate on caller failure");
          },
        },
        { organizationId: subscription.organizationId },
      ),
    ).rejects.toThrow();
    expect(cache.get("organizations:subscriptions")).toEqual(["cached"]);
  });
});

describe("commercial DTO reuse", () => {
  it("reuses catalog plan and organization subscription mappers", () => {
    expect(mapCatalogPlan(plan)?.id).toBe(plan.id);
    expect(mapOrganizationSubscription(subscription).version).toBe(3);
  });
});

describe("backend gap register", () => {
  it("records missing plan-version retire and renew HTTP", () => {
    expect(COMMERCIAL_BACKEND_GAPS.planVersionRetireHttp.available).toBe(false);
    expect(COMMERCIAL_BACKEND_GAPS.subscriptionRenewHttp.available).toBe(false);
    expect(COMMERCIAL_BACKEND_GAPS.draftBusinessTypeGrants.available).toBe(false);
    expect(COMMERCIAL_BACKEND_GAPS.subscriptionActivateWithoutPayment.available).toBe(false);
    expect(COMMERCIAL_BACKEND_GAPS.maxActiveStaffInviteEnforcement.available).toBe(false);
    expect(COMMERCIAL_BACKEND_GAPS.entitlementGenerateReconcileOverride.available).toBe(true);
  });
});
