import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

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
  trialAllowed: true,
  defaultTrialDays: 14,
  maxBranches: 3,
  maxActiveStaff: 10,
  maxActivePosDevices: 3,
  monthlyPrice: 699,
  currencyCode: "PHP",
  sortOrder: 20,
};

const starterPlan = {
  id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  productCode: "pinoy-business-pos",
  code: "starter",
  displayName: "Starter",
  status: "Active",
  trialAllowed: true,
  defaultTrialDays: 14,
  maxBranches: 1,
  maxActiveStaff: 3,
  maxActivePosDevices: 1,
  monthlyPrice: 299,
  currencyCode: "PHP",
  sortOrder: 10,
};

const proPlan = {
  id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
  productCode: "pinoy-business-pos",
  code: "pro",
  displayName: "Pro",
  status: "Active",
  trialAllowed: false,
  maxBranches: 10,
  maxActiveStaff: 30,
  maxActivePosDevices: 10,
  monthlyPrice: 1499,
  currencyCode: "PHP",
  sortOrder: 30,
};

const growthSubscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: sampleOrg.id,
  productCode: "pinoy-business-pos",
  planId: growthPlan.id,
  status: "Trialing",
  version: 1,
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Growth",
  trialEndUtc: "2026-09-01T00:00:00Z",
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

function stubNarrow() {
  vi.spyOn(window, "matchMedia").mockImplementation(() => {
    return {
      matches: false,
      media: "",
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

async function openSubscriptionPage() {
  window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/subscription`);
  render(<App />);
  expect(await screen.findByRole("heading", { name: "Subscription", level: 1 })).toBeInTheDocument();
  await waitFor(() => {
    expect(screen.queryByLabelText("Loading subscriptions")).not.toBeInTheDocument();
  });
}

function jsonProblem(status: number, errorCode: string, detail: string) {
  return jsonResponse(status, { title: "Error", status, detail, errorCode });
}

describe("organization subscription lifecycle", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders no-subscription empty state and Start trial with permission", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [],
      catalogProductPlans: [growthPlan, proPlan],
      catalogPlanVersions: [{ id: "version-1", planId: growthPlan.id, productCode: growthPlan.productCode, versionNumber: 1, status: "Published" }],
      catalogTrials: [
        {
          id: "trial-1",
          productCode: "pinoy-business-pos",
          planId: growthPlan.id,
          displayName: "Growth trial",
          status: "Active",
        },
      ],
    });
    await openSubscriptionPage();
    expect(await screen.findByText("No Pinoy Business POS subscription")).toBeInTheDocument();
    expect(await screen.findByRole("button", { name: "Start trial" })).toBeInTheDocument();
  });

  it("hides Start trial without manage_subscriptions", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [],
      catalogProductPlans: [growthPlan],
      permissions: ["platform.permission.view_portfolio", "platform.permission.manage_organizations"],
    });
    await openSubscriptionPage();
    expect(await screen.findByText("No Pinoy Business POS subscription")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Start trial" })).not.toBeInTheDocument();
  });

  it("starts a trial and shows success", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/trials") && method === "POST") {
        expect(JSON.parse(String(init?.body))).toEqual({
          planId: growthPlan.id,
          planVersionId: "version-1",
          trialDefinitionId: "trial-1",
        });
        return jsonResponse(200, growthSubscription);
      }
      if (url.includes("/trials")) {
        return jsonResponse(200, [
          {
            id: "trial-1",
            productCode: "pinoy-business-pos",
            planId: growthPlan.id,
            displayName: "Growth trial",
            status: "Active",
          },
        ]);
      }
      if (url.includes("/versions")) {
        return jsonResponse(200, [
          {
            id: "version-1",
            planId: growthPlan.id,
            productCode: growthPlan.productCode,
            versionNumber: 1,
            status: "Published",
          },
        ]);
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan, proPlan]);
      }
      if (url.includes("/subscriptions") && method === "GET") {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`) && method === "GET") {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(await screen.findByRole("button", { name: "Start trial" }));
    const dialog = await screen.findByRole("dialog");
    const confirm = within(dialog).getByRole("button", { name: "Start trial" });
    await waitFor(() => expect(confirm).toBeEnabled());
    await user.click(confirm);
    expect(await screen.findByText("Trial subscription created.")).toBeInTheDocument();
  });

  it("binds Starter and Growth trials exactly and does not fall back across plans", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/trials") && method === "POST") {
        const body = JSON.parse(String(init?.body)) as { planId: string; trialDefinitionId: string };
        expect(body.planId).toBe(growthPlan.id);
        expect(body.trialDefinitionId).toBe("trial-growth");
        return jsonResponse(200, growthSubscription);
      }
      if (url.includes("/trials")) {
        return jsonResponse(200, [
          {
            id: "trial-growth",
            productCode: "pinoy-business-pos",
            planId: growthPlan.id,
            displayName: "Growth trial",
            status: "Active",
          },
        ]);
      }
      if (url.includes("/versions")) {
        const planId = url.includes(starterPlan.id) ? starterPlan.id : growthPlan.id;
        return jsonResponse(200, [
          {
            id: `version-${planId}`,
            planId,
            productCode: "pinoy-business-pos",
            versionNumber: 1,
            status: "Published",
          },
        ]);
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [starterPlan, growthPlan, proPlan]);
      }
      if (url.includes("/subscriptions") && method === "GET") {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`) && method === "GET") {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(await screen.findByRole("button", { name: "Start trial" }));
    const dialog = await screen.findByRole("dialog");
    await user.selectOptions(within(dialog).getByLabelText("Plan"), starterPlan.id);
    expect(
      await within(dialog).findByText("No active trial definition is available for this plan."),
    ).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Start trial" })).toBeDisabled();
    await user.selectOptions(within(dialog).getByLabelText("Plan"), growthPlan.id);
    const confirm = within(dialog).getByRole("button", { name: "Start trial" });
    await waitFor(() => expect(confirm).toBeEnabled());
    await user.click(confirm);
    expect(await screen.findByText("Trial subscription created.")).toBeInTheDocument();
  });

  it("shows trial failure from the Platform error", async () => {
    stubDesktop();
    vi.stubGlobal("fetch", async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/trials") && (init?.method ?? "GET") === "POST") {
        return jsonProblem(400, "application.subscription.ineligible", "Trial is not eligible.");
      }
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/trials")) {
        return jsonResponse(200, [
          {
            id: "trial-1",
            productCode: "pinoy-business-pos",
            planId: growthPlan.id,
            displayName: "Growth trial",
            status: "Active",
          },
        ]);
      }
      if (url.includes("/versions")) {
        return jsonResponse(200, [
          {
            id: "version-1",
            planId: growthPlan.id,
            productCode: growthPlan.productCode,
            versionNumber: 1,
            status: "Published",
          },
        ]);
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(await screen.findByRole("button", { name: "Start trial" }));
    const dialog = await screen.findByRole("dialog");
    const confirm = within(dialog).getByRole("button", { name: "Start trial" });
    await waitFor(() => expect(confirm).toBeEnabled());
    await user.click(confirm);
    expect(
      await screen.findByText("This change is not allowed for the current subscription state."),
    ).toBeInTheDocument();
    expect(screen.getByText("Trial is not eligible.")).toBeInTheDocument();
  });

  it("renders Trialing state with upgrade and suspend, not reactivate or activate", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [growthSubscription],
      catalogProductPlans: [growthPlan, proPlan],
    });
    await openSubscriptionPage();
    expect(screen.getAllByText("Growth").length).toBeGreaterThan(0);
    expect(await screen.findByText("3 POS devices")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Change plan" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Suspend subscription" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Reactivate subscription" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /^Activate$/i })).not.toBeInTheDocument();
  });

  it("shows current and new device limits from catalog on plan preview", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("plan-change-preview")) {
        return jsonResponse(200, {
          currentPlanId: growthPlan.id,
          currentPlanDisplayName: "Growth",
          targetPlanId: proPlan.id,
          targetPlanDisplayName: "Pro",
          usageConflicts: [],
          lostFeatures: [],
          hasBlockingUsageConflicts: false,
        });
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan, proPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [growthSubscription],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(await screen.findByText("3 POS devices")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Change plan" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText((_content, element) => {
      const text = element?.textContent ?? "";
      return element?.tagName === "LI" && text.includes("POS devices") && text.includes("3") && text.includes("10");
    })).toBeInTheDocument();
    expect(within(dialog).getByRole("button", { name: "Upgrade plan" })).toBeInTheDocument();
  });

  it("confirms upgrade and scheduled downgrade copy", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [growthSubscription],
      catalogProductPlans: [growthPlan, proPlan],
    });
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(await screen.findByText("3 POS devices")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Change plan" }));
    expect(screen.queryByText(/scheduled downgrade/i)).not.toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("New plan"), proPlan.id);
    expect(screen.getByRole("button", { name: "Upgrade plan" })).toBeInTheDocument();
  });

  it("submits upgrade and shows catalog device limits", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/plan-change-preview")) {
        return jsonResponse(200, { hasBlockingUsageConflicts: false, usageConflicts: [], lostFeatures: [] });
      }
      if (url.includes("/upgrade") && method === "POST") {
        return jsonResponse(200, { ...growthSubscription, planId: proPlan.id, planDisplayName: "Pro" });
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan, proPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [growthSubscription],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(await screen.findByText("3 POS devices")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Change plan" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Upgrade plan" }));
    expect(await screen.findByText("Plan upgraded.")).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).includes("/upgrade"))).toBe(true);
  });

  it("explains scheduled downgrade and cancel does not delete records", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Active", planId: proPlan.id, planDisplayName: "Pro" }],
      catalogProductPlans: [growthPlan, proPlan],
    });
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(await screen.findByText("10 POS devices")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Change plan" }));
    const planDialog = await screen.findByRole("dialog");
    await user.selectOptions(within(planDialog).getByLabelText("New plan"), growthPlan.id);
    expect(within(planDialog).getByText(/scheduled downgrade/i)).toBeInTheDocument();
    await user.click(within(planDialog).getByRole("button", { name: "Back" }));
    await user.click(screen.getByRole("button", { name: "Cancel subscription" }));
    expect(
      await screen.findByText(/Historical organization and POS data is not deleted/i),
    ).toBeInTheDocument();
  });

  it("suspends after confirmation and can reactivate from Suspended", async () => {
    stubDesktop();
    let current = { ...growthSubscription, status: "Active" };
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/suspend") && method === "POST") {
        current = { ...current, status: "Suspended" };
        return jsonResponse(200, current);
      }
      if (url.includes("/reactivate") && method === "POST") {
        current = { ...current, status: "Active" };
        return jsonResponse(200, current);
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan, proPlan]);
      }
      if (url.includes("/subscriptions") && !url.includes("/suspend")) {
        return jsonResponse(200, { items: [current], totalCount: 1, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(screen.getByRole("button", { name: "Suspend subscription" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText(/protected POS operations/i)).toBeInTheDocument();
    await user.click(within(dialog).getByRole("button", { name: "Suspend subscription" }));
    expect(await screen.findByText("Subscription suspended.")).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).includes("/suspend"))).toBe(true);
    expect(await screen.findByRole("button", { name: "Reactivate subscription" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Reactivate subscription" }));
    await user.click(
      within(await screen.findByRole("dialog")).getByRole("button", { name: "Reactivate subscription" }),
    );
    expect(await screen.findByText("Subscription reactivated.")).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => String(url).includes("/reactivate"))).toBe(true);
    const subscriptionReads = fetchMock.mock.calls.filter(([url, init]) => {
      const path = String(url);
      const method = init?.method ?? "GET";
      return (
        method === "GET" &&
        path.includes("/organizations/") &&
        path.includes("/subscriptions") &&
        !path.includes("/suspend") &&
        !path.includes("/reactivate")
      );
    });
    expect(subscriptionReads.length).toBeGreaterThan(1);
  });

  it("shows Reactivate for Suspended and not Suspend", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Suspended" }],
      catalogProductPlans: [growthPlan],
    });
    await openSubscriptionPage();
    expect(await screen.findByRole("button", { name: "Reactivate subscription" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Suspend subscription" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Change plan" })).not.toBeInTheDocument();
  });

  it("does not show Reactivate for Cancelled", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Cancelled" }],
      catalogProductPlans: [growthPlan],
    });
    await openSubscriptionPage();
    expect(screen.queryByRole("button", { name: "Reactivate subscription" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Suspend subscription" })).not.toBeInTheDocument();
  });

  it("keeps grace/past due/expire under Support actions", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Active" }],
      catalogProductPlans: [growthPlan],
    });
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(screen.queryByRole("button", { name: "Mark grace period" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Support actions" }));
    expect(await screen.findByRole("menuitem", { name: "Mark grace period" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Mark past due" })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: "Expire subscription" })).toBeInTheDocument();
  });

  it("handles 403, 409, and invalid transition without exposing Activate", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = init?.method ?? "GET";
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/suspend") && method === "POST") {
        return jsonProblem(403, "application.forbidden", "Permission denied.");
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [{ ...growthSubscription, status: "Active" }],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(screen.queryByRole("button", { name: /^Activate$/i })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Suspend subscription" }));
    await user.click(
      within(await screen.findByRole("dialog")).getByRole("button", { name: "Suspend subscription" }),
    );
    expect(
      await screen.findByText("You do not have permission to change this subscription."),
    ).toBeInTheDocument();
  });

  it("preserves 409 conflict text", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/cancel") && (init?.method ?? "GET") === "POST") {
        return jsonProblem(409, "application.concurrency_conflict", "Version mismatch.");
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [{ ...growthSubscription, status: "Active" }],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(screen.getByRole("button", { name: "Cancel subscription" }));
    await user.click(
      within(await screen.findByRole("dialog")).getByRole("button", { name: "Cancel subscription" }),
    );
    expect(
      await screen.findByText("Another operator updated this subscription. Refresh and try again."),
    ).toBeInTheDocument();
    expect(screen.getByText("Version mismatch.")).toBeInTheDocument();
  });

  it("shows invalid transition copy without an Activate control", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/cancel") && (init?.method ?? "GET") === "POST") {
        return jsonProblem(400, "application.subscription.invalid_transition", "Cannot cancel from this state.");
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [{ ...growthSubscription, status: "Active" }],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    expect(screen.queryByRole("button", { name: /^Activate$/i })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancel subscription" }));
    await user.click(
      within(await screen.findByRole("dialog")).getByRole("button", { name: "Cancel subscription" }),
    );
    expect(
      await screen.findByText("This change is not allowed for the current subscription state."),
    ).toBeInTheDocument();
    expect(screen.getByText("Cannot cancel from this state.")).toBeInTheDocument();
  });

  it("disables duplicate confirm while a mutation is in flight", async () => {
    stubDesktop();
    let resolveSuspend: ((value: Response) => void) | undefined;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.endsWith("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/suspend") && (init?.method ?? "GET") === "POST") {
        return await new Promise<Response>((resolve) => {
          resolveSuspend = resolve;
        });
      }
      if (/\/catalog\/products\/[^/]+\/plans$/.test(url)) {
        return jsonResponse(200, [growthPlan]);
      }
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [{ ...growthSubscription, status: "Active" }],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(screen.getByRole("button", { name: "Suspend subscription" }));
    const confirm = within(await screen.findByRole("dialog")).getByRole("button", {
      name: "Suspend subscription",
    });
    await user.click(confirm);
    expect(confirm).toBeDisabled();
    resolveSuspend?.(jsonResponse(200, { ...growthSubscription, status: "Suspended" }));
    await waitFor(() => expect(screen.getByText("Subscription suspended.")).toBeInTheDocument());
  });

  it("hides mutation controls for read-only operators", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Active" }],
      catalogProductPlans: [growthPlan],
      permissions: ["platform.permission.view_portfolio", "platform.permission.manage_organizations"],
    });
    await openSubscriptionPage();
    expect(screen.getAllByText("Pinoy Business POS").length).toBeGreaterThan(0);
    expect(screen.queryByRole("button", { name: "Change plan" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Suspend subscription" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Start trial" })).not.toBeInTheDocument();
  });

  it("keeps the page usable on a narrow viewport", async () => {
    stubNarrow();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Active" }],
      catalogProductPlans: [growthPlan],
    });
    await openSubscriptionPage();
    expect(screen.getByRole("button", { name: "Suspend subscription" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Subscription", level: 1 })).toBeInTheDocument();
  });

  it("exposes dialog title, description, and dismiss for accessibility", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [{ ...growthSubscription, status: "Active" }],
      catalogProductPlans: [growthPlan],
    });
    const user = userEvent.setup();
    await openSubscriptionPage();
    await user.click(screen.getByRole("button", { name: "Suspend subscription" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByRole("heading", { name: "Suspend subscription" })).toBeInTheDocument();
    expect(within(dialog).getByText(/protected POS operations/i)).toBeInTheDocument();
    await user.keyboard("{Escape}");
    await waitFor(() => {
      expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    });
  });
});
