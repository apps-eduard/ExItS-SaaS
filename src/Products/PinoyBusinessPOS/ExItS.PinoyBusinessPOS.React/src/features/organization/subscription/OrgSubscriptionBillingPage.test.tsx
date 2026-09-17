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

const getOrganizationCurrentPlan = vi.fn();
const getBranchCapacity = vi.fn();
const getPosDeviceCapacity = vi.fn();
const listOrganizationAreas = vi.fn();
const listOrganizationMembers = vi.fn();
const getOrganizationPlanChangePreview = vi.fn();

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
      }),
      planPayload(),
      planPayload({
        id: PRO_PLAN_ID,
        planKey: "pro",
        code: "PRO",
        displayName: "Pro",
        sortOrder: 30,
        maxBranches: 10,
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
      targetPlanId: STARTER_PLAN_ID,
      targetPlanDisplayName: "Starter",
      activeStaffCount: 1,
      activeBranchCount: 1,
      branchCountAvailable: true,
      branchCountUnavailableReason: null,
      usageConflicts: [],
      lostFeatures: [],
      hasBlockingUsageConflicts: false,
    },
  });
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

  it("states that billing is managed by ExItS instead of showing a card form", async () => {
    renderPage("/org/subscription?tab=billing");

    const notice = await screen.findByTestId("org-subscription-billing-managed");
    expect(notice).toHaveTextContent("Billing is managed by ExItS.");
    expect(screen.queryByLabelText(/card number/i)).not.toBeInTheDocument();
  });

  it("shows a truthful empty Invoices state", async () => {
    renderPage("/org/subscription?tab=invoices");

    const empty = await screen.findByTestId("org-subscription-invoices-empty");
    expect(empty).toHaveTextContent("No invoices to show");
    expect(empty).toHaveTextContent(/not subscription invoices/i);
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

    const check = await screen.findByTestId("org-subscription-check-starter");
    await userEvent.click(check);

    const blocked = await screen.findByTestId("org-subscription-preview-blocked");
    expect(blocked).toHaveTextContent("Active branches (3) exceed the Starter limit of 1.");
    expect(screen.queryByTestId("org-subscription-preview-ok")).not.toBeInTheDocument();
    expect(screen.getByTestId("org-subscription-preview-lost")).toHaveTextContent(
      "CustomerCredit",
    );

    // Preview is read-only; no plan mutation is offered or attempted from this app.
    expect(getOrganizationPlanChangePreview).toHaveBeenCalledTimes(1);
    expect(screen.getByTestId("org-subscription-change-managed")).toHaveTextContent(
      "Plan changes are applied by ExItS",
    );
  });

  it("confirms a non-conflicting plan without offering a fake checkout", async () => {
    renderPage("/org/subscription?tab=plan");

    await userEvent.click(await screen.findByTestId("org-subscription-check-pro"));

    expect(await screen.findByTestId("org-subscription-preview-ok")).toBeInTheDocument();
    expect(screen.queryByTestId("org-subscription-preview-blocked")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /pay now/i })).not.toBeInTheDocument();
  });

  it("denies non-owner principals", async () => {
    membershipRole = null;
    renderPage();

    expect(await screen.findByTestId("org-subscription-denied")).toBeInTheDocument();
    expect(getOrganizationCurrentPlan).not.toHaveBeenCalled();
  });
});
