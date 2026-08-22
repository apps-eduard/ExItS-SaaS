import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { mockAuthenticatedFetch, sampleAuthorization } from "@/test/auth-fixtures";

const growthPlanId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

const growthPlan = {
  id: growthPlanId,
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
};

const catalogFeatures = [
  {
    productCode: "pinoy-business-pos",
    featureCode: "store-customer-ordering",
    displayName: "Customer ordering",
    valueType: "Boolean",
    status: "Active",
  },
  {
    productCode: "pinoy-business-pos",
    featureCode: "store-delivery-orders",
    displayName: "Delivery orders",
    valueType: "Boolean",
    status: "Active",
  },
  {
    productCode: "pinoy-business-pos",
    featureCode: "plan-max-active-pos-devices",
    displayName: "Max active POS devices",
    valueType: "QuantityLimit",
    status: "Active",
  },
];

function manageCatalogPermissions() {
  return [
    ...sampleAuthorization.permissions,
    PLATFORM_PERMISSIONS.manageCatalog,
  ];
}

function renderGrowthPlan(options?: Parameters<typeof mockAuthenticatedFetch>[0]) {
  mockAuthenticatedFetch({
    permissions: manageCatalogPermissions(),
    catalogPlanItems: [growthPlan],
    catalogFeatureItems: catalogFeatures,
    ...options,
  });
  window.history.pushState({}, "", `/admin/plans/${growthPlanId}`);
  render(<App />);
}

describe("PlanDetailPage commercial editor", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("gates mutation controls without manage_catalog permission", async () => {
    mockAuthenticatedFetch({ catalogPlanItems: [growthPlan] });
    window.history.pushState({}, "", `/admin/plans/${growthPlanId}`);
    render(<App />);

    expect(await screen.findByRole("heading", { name: "Growth" })).toBeInTheDocument();
    expect(screen.getByText(/read-only plan view/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /save commercial package/i })).not.toBeInTheDocument();
  });

  it("persists Growth maxActivePosDevices through commercial PATCH and refetch", async () => {
    const mutations: Array<{ method: string; path: string; body: unknown }> = [];
    renderGrowthPlan({
      onPlanMutation: (method, path, body) => {
        mutations.push({ method, path, body });
      },
    });

    expect(await screen.findByRole("heading", { name: "Growth" })).toBeInTheDocument();

    const posDevicesInput = screen.getByLabelText(/max active pos devices/i);
    await userEvent.clear(posDevicesInput);
    await userEvent.type(posDevicesInput, "5");

    await userEvent.click(screen.getByRole("button", { name: /save commercial package/i }));

    await waitFor(() => {
      expect(mutations.some((item) => item.method === "PATCH" && item.path.includes("/commercial"))).toBe(
        true,
      );
    });

    const patch = mutations.find((item) => item.path.includes("/commercial"));
    expect((patch?.body as { maxActivePosDevices?: number }).maxActivePosDevices).toBe(5);

    await waitFor(() => {
      expect(posDevicesInput).toHaveValue(5);
    });
  });

  it("shows ordering and delivery grant truth from server feature definitions", async () => {
    renderGrowthPlan({
      catalogPlanVersions: [
        {
          id: "11111111-1111-1111-1111-111111111111",
          planId: growthPlanId,
          productCode: "pinoy-business-pos",
          versionNumber: 1,
          status: "Published",
          grants: [
            { featureCode: "store-customer-ordering", enabled: true },
            { featureCode: "store-delivery-orders", enabled: true },
          ],
        },
      ],
    });

    expect(await screen.findByRole("heading", { name: "Growth" })).toBeVisible();
    await waitFor(() => {
      expect(screen.getByText("Customer ordering")).toBeInTheDocument();
    });
    const section = screen.getByRole("heading", { name: /ordering and delivery grants/i }).closest("section");
    expect(section).not.toBeNull();
    const withinSection = within(section!);
    await waitFor(() => {
      expect(withinSection.getAllByText(/^yes$/i).length).toBeGreaterThanOrEqual(2);
    });
  });

  it("does not expose a retire version button", async () => {
    renderGrowthPlan();
    expect(await screen.findByRole("heading", { name: "Growth" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /retire version/i })).not.toBeInTheDocument();
  });

  it("surfaces 409 conflict feedback without overwriting stale values silently", async () => {
    renderGrowthPlan({
      planMutationError: {
        status: 409,
        errorCode: "application.plan.concurrency_conflict",
        detail: "Plan was updated by another operator.",
      },
    });

    expect(await screen.findByRole("heading", { name: "Growth" })).toBeInTheDocument();
    const posDevicesInput = screen.getByLabelText(/max active pos devices/i);
    await userEvent.clear(posDevicesInput);
    await userEvent.type(posDevicesInput, "8");
    await userEvent.click(screen.getByRole("button", { name: /save commercial package/i }));

    expect(await screen.findByText(/conflict/i)).toBeInTheDocument();
    await waitFor(() => {
      expect(posDevicesInput).toHaveValue(3);
    });
  });
});
