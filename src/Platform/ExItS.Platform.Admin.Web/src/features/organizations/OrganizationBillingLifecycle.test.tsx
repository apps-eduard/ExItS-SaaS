import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { mockAuthenticatedFetch, sampleAuthorization } from "@/test/auth-fixtures";

const sampleOrg = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const growthPlan = {
  id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
  productCode: "pinoy-business-pos",
  code: "growth",
  displayName: "Growth",
  status: "Active",
  monthlyPrice: 699,
  currencyCode: "PHP",
  sortOrder: 20,
};

const pendingPayment = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: sampleOrg.id,
  productCode: "pinoy-business-pos",
  amount: 699,
  currencyCode: "PHP",
  method: "GCash",
  status: "PendingConfirmation",
  paidAtUtc: "2026-08-01T08:00:00Z",
  externalReference: "GCASH-1001",
};

const confirmedPayment = {
  ...pendingPayment,
  id: "22222222-2222-2222-2222-222222222222",
  status: "Confirmed",
  confirmedAtUtc: "2026-08-02T08:00:00Z",
};

const trialingSubscription = {
  id: "33333333-3333-3333-3333-333333333333",
  organizationId: sampleOrg.id,
  productCode: "pinoy-business-pos",
  planId: growthPlan.id,
  status: "Trialing",
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Growth",
};

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || query.includes("min-width: 768px"),
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

async function openBillingPage() {
  window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/billing`);
  render(<App />);
  expect(await screen.findByRole("heading", { name: "Billing", level: 1 })).toBeInTheDocument();
}

describe("organization billing lifecycle", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("fail-closes billing without manage_manual_payments", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenOrgPayments: true,
      permissions: ["platform.permission.view_portfolio", "platform.permission.manage_organizations"],
    });
    await openBillingPage();
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
  });

  it("shows payment actions only with manage_manual_payments and hides activation without manage_subscriptions", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [confirmedPayment],
      orgSubscriptionItems: [trialingSubscription],
      permissions: sampleAuthorization.permissions.filter(
        (item) => item !== "platform.permission.manage_subscriptions",
      ),
    });
    await openBillingPage();
    expect(await screen.findByRole("button", { name: "Void" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Activate from payment" })).not.toBeInTheDocument();
  });

  it("shows no-payment empty state", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg], orgPaymentItems: [] });
    await openBillingPage();
    expect(await screen.findByText("No SaaS payments")).toBeInTheDocument();
  });

  it("shows record payment with manage_manual_payments", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [],
      catalogProductPlans: [growthPlan],
    });
    await openBillingPage();
    expect(await screen.findByRole("button", { name: "Record payment" })).toBeInTheDocument();
  });

  it("shows confirm and reject for pending payments", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [pendingPayment],
    });
    await openBillingPage();
    expect(await screen.findByRole("button", { name: "Confirm" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reject" })).toBeInTheDocument();
  });

  it("records a manual payment using catalog price", async () => {
    stubDesktop();
    const mutations: unknown[] = [];
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [],
      catalogProductPlans: [growthPlan],
      onPaymentMutation: (_method, _path, body) => mutations.push(body),
    });
    await openBillingPage();
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Record payment" }));
    await user.type(screen.getByLabelText("Reference"), "REF-001");
    await user.click(screen.getByRole("button", { name: "Record payment", hidden: false }));
    await waitFor(() => {
      expect(mutations.some((body) => (body as { amount: number }).amount === 699)).toBe(true);
    });
    expect(await screen.findByText("Payment recorded.")).toBeInTheDocument();
  });

  it("confirms a pending payment", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [pendingPayment],
    });
    await openBillingPage();
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Confirm" }));
    await user.click(await screen.findByRole("button", { name: "Confirm", hidden: false }));
    expect(await screen.findByText("Payment confirmed.")).toBeInTheDocument();
  });

  it("rejects a pending payment with reason", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [pendingPayment],
    });
    await openBillingPage();
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Reject" }));
    await user.type(screen.getByLabelText("Reason"), "Duplicate reference");
    await user.click(screen.getByRole("button", { name: "Reject", hidden: false }));
    expect(await screen.findByText("Payment rejected.")).toBeInTheDocument();
  });

  it("voids a confirmed unused payment", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [confirmedPayment],
    });
    await openBillingPage();
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Void" }));
    await user.type(screen.getByLabelText("Void reason"), "Entered in error");
    await user.click(screen.getByRole("button", { name: "Void", hidden: false }));
    expect(await screen.findByText("Payment voided.")).toBeInTheDocument();
  });

  it("offers activate from payment for trialing subscription", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [confirmedPayment],
      orgSubscriptionItems: [trialingSubscription],
    });
    await openBillingPage();
    expect(await screen.findByRole("button", { name: "Activate from payment" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Activate$/i })).not.toBeInTheDocument();
  });

  it("does not offer activate from unconfirmed payment", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [pendingPayment],
      orgSubscriptionItems: [trialingSubscription],
    });
    await openBillingPage();
    await screen.findByRole("button", { name: "Confirm" });
    expect(screen.queryByRole("button", { name: "Activate from payment" })).not.toBeInTheDocument();
  });

  it("activates subscription from confirmed payment", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [confirmedPayment],
      orgSubscriptionItems: [trialingSubscription],
    });
    await openBillingPage();
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Activate from payment" }));
    await user.click(await screen.findByRole("button", { name: "Activate from payment", hidden: false }));
    expect(await screen.findByText("Subscription activated.")).toBeInTheDocument();
  });

  it("states SaaS payments are not POS payments", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg], orgPaymentItems: [confirmedPayment] });
    await openBillingPage();
    expect(
      await screen.findByText(/not customer POS transactions/i),
    ).toBeInTheDocument();
  });

  it("shows local validation simulate only when runtime flag enabled", async () => {
    stubDesktop();
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [],
      orgSubscriptionItems: [trialingSubscription],
      catalogProductPlans: [growthPlan],
    });
    await openBillingPage();
    expect(
      await screen.findByRole("button", { name: /LOCAL VALIDATION — Simulate payment/i }),
    ).toBeInTheDocument();
  });

  it("hides local validation simulate without runtime flag", async () => {
    stubDesktop();
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: false };
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [],
      orgSubscriptionItems: [trialingSubscription],
      catalogProductPlans: [growthPlan],
    });
    await openBillingPage();
    await screen.findByRole("button", { name: "Record payment" });
    expect(screen.queryByRole("button", { name: /LOCAL VALIDATION/i })).not.toBeInTheDocument();
  });

  it("surfaces backend 403 on payment mutation", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [pendingPayment],
      paymentMutationError: {
        status: 403,
        errorCode: "application.authorization.forbidden",
        detail: "Insufficient permission.",
      },
    });
    await openBillingPage();
    const user = userEvent.setup();
    await user.click(await screen.findByRole("button", { name: "Confirm" }));
    await user.click(await screen.findByRole("button", { name: "Confirm", hidden: false }));
    expect(await screen.findByText(/Insufficient permission/i)).toBeInTheDocument();
  });

  it("shows subscribe with payment when no subscription exists", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgPaymentItems: [],
      orgSubscriptionItems: [],
      catalogProductPlans: [growthPlan],
      catalogPlanVersions: [
        {
          id: "version-1",
          planId: growthPlan.id,
          productCode: growthPlan.productCode,
          versionNumber: 1,
          status: "Published",
        },
      ],
    });
    await openBillingPage();
    expect(await screen.findByRole("button", { name: "Subscribe with payment" })).toBeInTheDocument();
  });
});

describe("organization workspace billing page", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("fail-closes forbidden payments without leaking amount", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenOrgPayments: true,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/billing`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("9999.99")).not.toBeInTheDocument();
  });
});
