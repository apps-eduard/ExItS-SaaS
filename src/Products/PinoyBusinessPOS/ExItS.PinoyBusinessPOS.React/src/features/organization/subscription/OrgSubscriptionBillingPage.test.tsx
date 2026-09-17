import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

const ORG_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const SUBSCRIPTION_ID = "33333333-3333-4333-8333-333333333333";
const GROWTH_PLAN_ID = "11111111-1111-4111-8111-111111111111";
const STARTER_PLAN_ID = "44444444-4444-4444-8444-444444444444";
const PRO_PLAN_ID = "22222222-2222-4222-8222-222222222222";
const PRO_PLUS_PLAN_ID = "55555555-5555-4555-8555-555555555555";

const getOrganizationCurrentPlan = vi.fn();
const getBranchCapacity = vi.fn();
const getPosDeviceCapacity = vi.fn();
const listOrganizationAreas = vi.fn();
const listOrganizationMembers = vi.fn();
const getOrganizationPlanChangePreview = vi.fn();
const upgradeOrganizationSubscription = vi.fn();
const downgradeOrganizationSubscription = vi.fn();
const listOrganizationSaasPayments = vi.fn();
const refreshSessionGrant = vi.fn(async () => null);

let membershipRole: string | null = "OrganizationOwner";

vi.mock("@/api/platform/organization-current-plan-client", () => ({
  getOrganizationCurrentPlan: (...args: unknown[]) => getOrganizationCurrentPlan(...args),
}));

vi.mock("@/api/platform/organization-branches-client", () => ({
  getBranchCapacity: (...args: unknown[]) => getBranchCapacity(...args),
}));

vi.mock("@/api/platform/pos-devices-client", () => ({
  getPosDeviceCapacity: (...args: unknown[]) => getPosDeviceCapacity(...args),
}));

vi.mock("@/api/platform/organization-areas-client", () => ({
  listOrganizationAreas: (...args: unknown[]) => listOrganizationAreas(...args),
}));

vi.mock("@/api/platform/organization-members-client", () => ({
  listOrganizationMembers: (...args: unknown[]) => listOrganizationMembers(...args),
}));

vi.mock("@/api/platform/organization-plan-change-client", () => ({
  getOrganizationPlanChangePreview: (...args: unknown[]) =>
    getOrganizationPlanChangePreview(...args),
  upgradeOrganizationSubscription: (...args: unknown[]) =>
    upgradeOrganizationSubscription(...args),
  downgradeOrganizationSubscription: (...args: unknown[]) =>
    downgradeOrganizationSubscription(...args),
  createPlanChangeIdempotencyKey: (prefix = "pos-plan") => `${prefix}-test-key`,
}));

vi.mock("@/api/platform/organization-saas-payments-client", () => ({
  listOrganizationSaasPayments: (...args: unknown[]) => listOrganizationSaasPayments(...args),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: ORG_ID,
      organizationDisplayName: "Mica Store",
      branchId: "88888888-8888-4888-8888-888888888888",
      branchName: "Main branch",
      experience: "manage_business",
    },
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole,
      organizationManagementAuthority: true,
      featureCodes: ["store-area-management"],
      grantedFeatureCodes: ["store-area-management"],
    },
    refreshSessionGrant,
  }),
}));

const { OrgSubscriptionBillingPage } = await import(
  "@/features/organization/subscription/OrgSubscriptionBillingPage"
);

function planPayload(overrides: Record<string, unknown> = {}) {
  return {
    id: GROWTH_PLAN_ID,
    planKey: "growth",
    code: "GROWTH",
    displayName: "Growth",
    description: null,
    status: "Active",
    maxBranches: 3,
    maxActiveStaff: 10,
    maxActivePosDevices: 4,
    maxActiveBusinessTypes: 2,
    maxAreas: 5,
    customerCreditEnabled: true,
    advancedReportsEnabled: false,
    exportEnabled: false,
    trialAllowed: true,
    defaultTrialDays: 14,
    sortOrder: 20,
    monthlyPrice: 1099,
    annualPrice: 11880,
    currencyCode: "PHP",
    ...overrides,
  };
}

function currentPlanValue(overrides: Record<string, unknown> = {}) {
  return {
    organizationId: ORG_ID,
    productCode: "pinoy-business-pos",
    currentSubscription: {
      id: SUBSCRIPTION_ID,
      status: "Active",
      billingCycle: "Annual",
      agreedPrice: 11880,
      currencyCode: "PHP",
      planId: GROWTH_PLAN_ID,
      planKey: "growth",
      planDisplayName: "Growth",
      trialStartUtc: null,
      trialEndUtc: null,
      paidPeriodStartUtc: null,
      paidPeriodEndUtc: null,
      currentPeriodStartUtc: "2026-02-01T00:00:00Z",
      currentPeriodEndUtc: "2027-02-01T00:00:00Z",
      gracePeriodEndUtc: null,
      renewalDateUtc: "2027-02-01T00:00:00Z",
      suspendedAtUtc: null,
      pastDueAtUtc: null,
      cancelledAtUtc: null,
      expiredAtUtc: null,
    },
    currentPlan: planPayload(),
    pendingPlanChange: null,
    availablePlans: [
      planPayload({
        id: STARTER_PLAN_ID,
        planKey: "starter",
        code: "STARTER",
        displayName: "Starter",
        sortOrder: 10,
        maxBranches: 1,
        maxActiveStaff: 3,
        maxActivePosDevices: 1,
        maxAreas: 1,
        customerCreditEnabled: false,
        monthlyPrice: 499,
        annualPrice: 5388,
      }),
      planPayload(),
      planPayload({
        id: PRO_PLAN_ID,
        planKey: "pro",
        code: "PRO",
        displayName: "Pro",
        sortOrder: 30,
        maxBranches: 10,
        monthlyPrice: 2499,
        annualPrice: 26988,
        advancedReportsEnabled: true,
      }),
      planPayload({
        id: PRO_PLUS_PLAN_ID,
        planKey: "pro-plus",
        code: "PRO_PLUS",
        displayName: "Pro Plus",
        sortOrder: 40,
        maxBranches: 25,
        monthlyPrice: 4999,
        annualPrice: 53988,
        advancedReportsEnabled: true,
        exportEnabled: true,
      }),
    ],
    entitlement: null,
    productInstancePresent: true,
    planDisplayName: "Growth",
    planKey: "growth",
    subscriptionStatus: "Active",
    ...overrides,
  };
}

function renderPage(initialPath = "/org/subscription") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <QueryClientProvider client={queryClient}>
        <PreferencesProvider>
          <I18nProvider>
            <OrgSubscriptionBillingPage />
          </I18nProvider>
        </PreferencesProvider>
      </QueryClientProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  membershipRole = "OrganizationOwner";
  getOrganizationCurrentPlan.mockResolvedValue({ ok: true, value: currentPlanValue() });
  getBranchCapacity.mockResolvedValue({ ok: true, value: { used: 1, allowed: 3 } });
  getPosDeviceCapacity.mockResolvedValue({ ok: true, value: { used: 1, allowed: 4 } });
  listOrganizationAreas.mockResolvedValue({
    ok: true,
    value: { areas: [], unassignedBranchCount: 0, activeAreaCount: 1, maxAreas: 5 },
  });
  listOrganizationMembers.mockResolvedValue({ ok: true, members: [{ status: "Active" }] });
  getOrganizationPlanChangePreview.mockResolvedValue({
    ok: true,
    value: {
      currentPlanId: GROWTH_PLAN_ID,
      currentPlanDisplayName: "Growth",
      targetPlanId: PRO_PLAN_ID,
      targetPlanDisplayName: "Pro",
      activeStaffCount: 1,
      activeBranchCount: 1,
      branchCountAvailable: true,
      branchCountUnavailableReason: null,
      usageConflicts: [],
      lostFeatures: [],
      hasBlockingUsageConflicts: false,
    },
  });
  upgradeOrganizationSubscription.mockResolvedValue({
    ok: true,
    value: {
      id: SUBSCRIPTION_ID,
      status: "Active",
      billingCycle: "Annual",
      agreedPrice: 26988,
      currencyCode: "PHP",
      planId: PRO_PLAN_ID,
      pendingPlanId: null,
      pendingPlanEffectiveAtUtc: null,
      currentPeriodEndUtc: "2027-02-01T00:00:00Z",
      renewalDateUtc: "2027-02-01T00:00:00Z",
      version: 2,
    },
  });
  downgradeOrganizationSubscription.mockResolvedValue({
    ok: true,
    value: {
      id: SUBSCRIPTION_ID,
      status: "Active",
      billingCycle: "Annual",
      agreedPrice: 11880,
      currencyCode: "PHP",
      planId: GROWTH_PLAN_ID,
      pendingPlanId: STARTER_PLAN_ID,
      pendingPlanEffectiveAtUtc: "2027-02-01T00:00:00Z",
      currentPeriodEndUtc: "2027-02-01T00:00:00Z",
      renewalDateUtc: "2027-02-01T00:00:00Z",
      version: 2,
    },
  });
  listOrganizationSaasPayments.mockResolvedValue({
    ok: true,
    value: { items: [], totalCount: 0 },
  });
  refreshSessionGrant.mockResolvedValue(null);
});

describe("OrgSubscriptionBillingPage", () => {
  it("renders plan, status, cycle, and next payment on Overview", async () => {
    renderPage();

    expect(await screen.findByTestId("org-subscription-summary")).toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-plan")).toHaveTextContent("Growth");
    expect(screen.getByTestId("org-subscription-status")).toHaveTextContent("Active");
    expect(screen.getByTestId("org-subscription-cycle")).toHaveTextContent("Annual");
    expect(screen.getByTestId("org-subscription-next-payment")).not.toHaveTextContent(
      "Not available",
    );

    await waitFor(() =>
      expect(screen.getByTestId("org-subscription-usage-branches")).toHaveTextContent("1 / 3"),
    );
  });

  it("warns on Plan & Usage when a plan allowance is fully used", async () => {
    getBranchCapacity.mockResolvedValue({ ok: true, value: { used: 3, allowed: 3 } });
    renderPage("/org/subscription?tab=plan");

    expect(await screen.findByTestId("org-subscription-at-limit")).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByTestId("org-subscription-usage-branches")).toHaveTextContent("3 / 3"),
    );
    expect(screen.getByTestId("org-subscription-feature-customerCredit")).toHaveAttribute(
      "data-included",
      "true",
    );
    expect(screen.getByTestId("org-subscription-feature-advancedReports")).toHaveAttribute(
      "data-included",
      "false",
    );
  });

  it("marks the current plan and lists Starter through Pro Plus", async () => {
    renderPage("/org/subscription?tab=plan");

    expect(await screen.findByTestId("org-plan-change-panel")).toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-plan-growth")).toHaveAttribute(
      "data-current",
      "true",
    );
    expect(screen.getByTestId("org-subscription-current-badge")).toHaveTextContent("Current plan");
    expect(screen.getByTestId("org-subscription-plan-starter")).toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-plan-pro")).toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-plan-pro-plus")).toBeInTheDocument();
  });

  it("states that billing mode is Simulated instead of showing a card form", async () => {
    renderPage("/org/subscription?tab=billing");

    const notice = await screen.findByTestId("org-subscription-billing-managed");
    expect(notice).toHaveTextContent(/Simulated/i);
    expect(screen.queryByLabelText(/card number/i)).not.toBeInTheDocument();
  });

  it("shows a truthful empty Invoices state", async () => {
    renderPage("/org/subscription?tab=invoices");

    const empty = await screen.findByTestId("org-subscription-invoices-empty");
    expect(empty).toHaveTextContent("No invoices to show");
  });

  it("lists simulated invoices after a successful plan-change payment exists", async () => {
    listOrganizationSaasPayments.mockResolvedValue({
      ok: true,
      value: {
        items: [
          {
            id: "pay-1",
            subscriptionId: SUBSCRIPTION_ID,
            amount: 26988,
            currencyCode: "PHP",
            method: "Card",
            externalReference: "lvp_pay_000001",
            status: "Confirmed",
            paidAtUtc: "2026-03-01T00:00:00Z",
            confirmedAtUtc: "2026-03-01T00:00:00Z",
            createdAtUtc: "2026-03-01T00:00:00Z",
          },
        ],
        totalCount: 1,
      },
    });

    renderPage("/org/subscription?tab=invoices");

    expect(await screen.findByTestId("org-subscription-invoices")).toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-invoice-row")).toHaveTextContent("lvp_pay_000001");
    expect(screen.getByTestId("org-subscription-invoice-row")).toHaveTextContent("Simulated");
  });

  it("blocks a lower plan when the preview reports blocking usage conflicts", async () => {
    getOrganizationPlanChangePreview.mockResolvedValue({
      ok: true,
      value: {
        currentPlanId: GROWTH_PLAN_ID,
        currentPlanDisplayName: "Growth",
        targetPlanId: STARTER_PLAN_ID,
        targetPlanDisplayName: "Starter",
        activeStaffCount: 8,
        activeBranchCount: 3,
        branchCountAvailable: true,
        branchCountUnavailableReason: null,
        usageConflicts: [
          {
            resource: "Branches",
            currentUsage: 3,
            targetLimit: 1,
            message: "Active branches (3) exceed the Starter limit of 1.",
          },
        ],
        lostFeatures: ["CustomerCredit"],
        hasBlockingUsageConflicts: true,
      },
    });

    renderPage("/org/subscription?tab=plan");

    await userEvent.click(await screen.findByTestId("org-subscription-check-starter"));

    const blocked = await screen.findByTestId("org-subscription-preview-blocked");
    expect(blocked).toHaveTextContent("Cannot downgrade yet");
    expect(blocked).toHaveTextContent("Active branches (3) exceed the Starter limit of 1.");
    expect(screen.queryByTestId("org-subscription-preview-ok")).not.toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-preview-lost")).toHaveTextContent(
      "CustomerCredit",
    );
    expect(screen.queryByTestId("org-subscription-schedule-downgrade")).not.toBeEnabled();
    expect(downgradeOrganizationSubscription).not.toHaveBeenCalled();
  });

  it("upgrades immediately after a successful simulated payment", async () => {
    renderPage("/org/subscription?tab=plan");

    await userEvent.click(await screen.findByTestId("org-subscription-check-pro"));
    expect(await screen.findByTestId("org-subscription-preview-ok")).toBeInTheDocument();

    await userEvent.click(screen.getByTestId("org-subscription-continue-payment"));
    expect(await screen.findByTestId("org-subscription-simulated-payment")).toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-simulated-banner")).toHaveTextContent(
      "not a real payment gateway",
    );

    getOrganizationCurrentPlan.mockResolvedValue({
      ok: true,
      value: currentPlanValue({
        currentPlan: planPayload({
          id: PRO_PLAN_ID,
          planKey: "pro",
          code: "PRO",
          displayName: "Pro",
          sortOrder: 30,
          maxBranches: 10,
          monthlyPrice: 2499,
          annualPrice: 26988,
          advancedReportsEnabled: true,
        }),
        currentSubscription: {
          ...currentPlanValue().currentSubscription,
          planId: PRO_PLAN_ID,
          planKey: "pro",
          planDisplayName: "Pro",
          agreedPrice: 26988,
        },
        planDisplayName: "Pro",
        planKey: "pro",
      }),
    });

    await userEvent.click(screen.getByTestId("org-subscription-simulate-success"));

    expect(await screen.findByTestId("org-subscription-upgrade-success")).toHaveTextContent(
      "Plan upgraded successfully.",
    );
    expect(upgradeOrganizationSubscription).toHaveBeenCalledWith(
      expect.objectContaining({
        organizationId: ORG_ID,
        subscriptionId: SUBSCRIPTION_ID,
        planId: PRO_PLAN_ID,
        paymentSimulation: "succeed",
      }),
    );
    expect(refreshSessionGrant).toHaveBeenCalled();
  });

  it("leaves the plan unchanged when simulated payment fails", async () => {
    upgradeOrganizationSubscription.mockResolvedValue({
      ok: false,
      status: 409,
      body: { detail: "Upgrade payment was not successful (Failed)." },
    });

    renderPage("/org/subscription?tab=plan");

    await userEvent.click(await screen.findByTestId("org-subscription-check-pro"));
    await userEvent.click(await screen.findByTestId("org-subscription-continue-payment"));
    await userEvent.click(await screen.findByTestId("org-subscription-simulate-failure"));

    expect(await screen.findByTestId("org-subscription-payment-failed")).toBeInTheDocument();
    expect(screen.queryByTestId("org-subscription-upgrade-success")).not.toBeInTheDocument();
    expect(upgradeOrganizationSubscription).toHaveBeenCalledWith(
      expect.objectContaining({ paymentSimulation: "fail" }),
    );
    expect(refreshSessionGrant).not.toHaveBeenCalled();
  });

  it("schedules a valid downgrade at next renewal", async () => {
    getOrganizationPlanChangePreview.mockResolvedValue({
      ok: true,
      value: {
        currentPlanId: GROWTH_PLAN_ID,
        currentPlanDisplayName: "Growth",
        targetPlanId: STARTER_PLAN_ID,
        targetPlanDisplayName: "Starter",
        activeStaffCount: 1,
        activeBranchCount: 1,
        branchCountAvailable: true,
        branchCountUnavailableReason: null,
        usageConflicts: [],
        lostFeatures: ["CustomerCredit"],
        hasBlockingUsageConflicts: false,
      },
    });

    renderPage("/org/subscription?tab=plan");

    await userEvent.click(await screen.findByTestId("org-subscription-check-starter"));
    expect(await screen.findByTestId("org-subscription-preview-ok")).toBeInTheDocument();

    await userEvent.click(screen.getByTestId("org-subscription-schedule-downgrade"));

    expect(await screen.findByTestId("org-subscription-downgrade-success")).toBeInTheDocument();
    expect(downgradeOrganizationSubscription).toHaveBeenCalledWith(
      expect.objectContaining({
        organizationId: ORG_ID,
        subscriptionId: SUBSCRIPTION_ID,
        planId: STARTER_PLAN_ID,
        effectiveAtUtc: "2027-02-01T00:00:00Z",
      }),
    );
    expect(refreshSessionGrant).toHaveBeenCalled();
  });

  it("denies non-owner principals", async () => {
    membershipRole = null;
    renderPage();

    expect(await screen.findByTestId("org-subscription-denied")).toBeInTheDocument();
    expect(getOrganizationCurrentPlan).not.toHaveBeenCalled();
  });
});
