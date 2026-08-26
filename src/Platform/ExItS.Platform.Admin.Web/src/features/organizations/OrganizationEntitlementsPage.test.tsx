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

const snapshot = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: sampleOrg.id,
  productCode: "POS",
  subscriptionId: "33333333-3333-3333-3333-333333333333",
  planCode: "starter",
  planVersionNumber: 2,
  snapshotVersion: 4,
  schemaVersion: 1,
  subscriptionStatus: "Active",
  inGracePeriod: false,
  generatedAtUtc: "2026-08-01T08:00:00Z",
  grants: [{ featureCode: "pos.checkout", enabled: true }],
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

function stubMobile() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: false,
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

describe("organization workspace entitlements", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("loads product-scoped snapshot history from authorized product access", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [
          {
            id: "e1",
            productCode: "POS",
            productDisplayName: "Pinoy Business POS",
            subscriptionStatus: "Active",
          },
        ],
      },
      entitlementSnapshotItems: [snapshot],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    const workspaceNav = await screen.findByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Entitlements" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/entitlements`);
    });
    expect(
      await screen.findByRole("heading", { name: "Entitlements", level: 1 }),
    ).toBeInTheDocument();
    expect(await screen.findByText("starter")).toBeInTheDocument();
    expect(screen.getAllByText("1 enabled · 0 disabled").length).toBeGreaterThan(0);
    expect(screen.getAllByRole("button", { name: "Show grants" }).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Generate snapshot" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reconcile" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Create override" })).toBeInTheDocument();
    expect(screen.queryByText("pos.checkout")).not.toBeInTheDocument();
  });

  it("does not call snapshot history for an unsanitized product code", async () => {
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
        return jsonResponse(200, {
          subscriptions: [],
          payments: [],
          latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
        });
      }
      if (url.includes("/entitlements/snapshots")) {
        return jsonResponse(200, { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=UNKNOWN`,
    );
    render(<App />);
    expect(
      await screen.findByText("This product is not in the authorized product access list."),
    ).toBeInTheDocument();
    const snapshotUrls = fetchMock.mock.calls
      .map(([input]) => String(input))
      .filter((url) => url.includes("/entitlements/snapshots"));
    expect(snapshotUrls).toEqual([]);
  });

  it("stays truthful when product access is empty and never calls snapshot history", async () => {
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
      if (url.includes("/entitlements/snapshots")) {
        return jsonResponse(200, { items: [snapshot], totalCount: 1, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/entitlements`);
    render(<App />);
    expect(await screen.findByText("No product access records")).toBeInTheDocument();
    const snapshotUrls = fetchMock.mock.calls
      .map(([input]) => String(input))
      .filter((url) => url.includes("/entitlements/snapshots"));
    expect(snapshotUrls).toEqual([]);
  });

  it("fail-closes forbidden snapshot history without leaking payload", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      forbiddenEntitlementSnapshots: true,
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    render(<App />);
    expect(await screen.findByText("This list is not available.")).toBeInTheDocument();
    expect(screen.queryByText("entitlement-secret")).not.toBeInTheDocument();
  });

  it("localizes Cancelled, Expired, and GracePeriod in fil-PH", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [
        {
          ...snapshot,
          id: "11111111-1111-1111-1111-111111111111",
          subscriptionStatus: "Cancelled",
        },
        {
          ...snapshot,
          id: "22222222-2222-2222-2222-222222222222",
          subscriptionStatus: "Expired",
        },
        {
          ...snapshot,
          id: "33333333-3333-3333-3333-333333333333",
          subscriptionStatus: "GracePeriod",
        },
      ],
      entitlementSnapshotTotalCount: 3,
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Entitlements", level: 1 });
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(await screen.findAllByText("Nakansela")).not.toHaveLength(0);
    expect(screen.getAllByText("Nag-expire").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Grace period").length).toBeGreaterThan(0);
    expect(screen.queryByText("Cancelled")).not.toBeInTheDocument();
    expect(screen.queryByText("Expired")).not.toBeInTheDocument();
  });

  it("shows grant feature codes with enabled and disabled states", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [
        {
          ...snapshot,
          grants: [
            { featureCode: "pos.checkout", enabled: true, numericLimit: 5 },
            { featureCode: "pos.reports", enabled: false },
          ],
        },
        {
          ...snapshot,
          id: "22222222-2222-2222-2222-222222222222",
          grants: [],
        },
      ],
      entitlementSnapshotTotalCount: 2,
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByText("1 enabled · 1 disabled")).toBeInTheDocument();
    expect(screen.getByText("No grants")).toBeInTheDocument();
    expect(screen.queryByText("pos.checkout")).not.toBeInTheDocument();
    const showButton = screen.getAllByRole("button", { name: "Show grants" })[0]!;
    expect(showButton).toHaveAttribute("aria-expanded", "false");
    await user.click(showButton);
    expect(screen.getByRole("button", { name: "Hide grants" })).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByText("pos.checkout")).toBeInTheDocument();
    expect(screen.getByText("pos.reports")).toBeInTheDocument();
    expect(screen.getByText("Enabled")).toBeInTheDocument();
    expect(screen.getByText("Disabled")).toBeInTheDocument();
    expect(screen.getByText("Limit 5")).toBeInTheDocument();
  });

  it("shows grants on mobile cards", async () => {
    stubMobile();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [snapshot],
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    const user = userEvent.setup();
    render(<App />);
    expect((await screen.findAllByText("1 enabled · 0 disabled")).length).toBeGreaterThan(0);
    expect(screen.getAllByText("Grants").length).toBeGreaterThan(0);
    expect(screen.queryByText("pos.checkout")).not.toBeInTheDocument();
    await user.click(screen.getAllByRole("button", { name: "Show grants" })[0]!);
    expect(screen.getByText("pos.checkout")).toBeInTheDocument();
    expect(screen.getByText("Enabled")).toBeInTheDocument();
  });

  it("hides operator mutations without subscription and override permissions", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      permissions: sampleAuthorization.permissions.filter(
        (item) =>
          item !== "platform.permission.manage_subscriptions" &&
          item !== "platform.permission.manage_entitlement_overrides",
      ),
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [snapshot],
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    render(<App />);
    await screen.findByRole("heading", { name: "Entitlements", level: 1 });
    expect(screen.queryByRole("button", { name: "Generate snapshot" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Reconcile" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Create override" })).not.toBeInTheDocument();
  });

  it("generates a snapshot with expected next version and refreshes state", async () => {
    stubDesktop();
    const mutations: Array<{ method: string; path: string; body: unknown }> = [];
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [snapshot],
      onEntitlementMutation: (method, path, body) => {
        mutations.push({ method, path, body });
      },
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Current entitlement", level: 2 });
    await user.click(screen.getByRole("button", { name: "Generate snapshot" }));
    const dialog = await screen.findByRole("dialog");
    await user.click(within(dialog).getByRole("button", { name: "Generate snapshot" }));
    await waitFor(() => {
      expect(screen.getByText("Entitlement snapshot generated.")).toBeInTheDocument();
    });
    expect(mutations.some((call) => call.path.includes("/entitlements/snapshots"))).toBe(true);
    expect(
      (mutations.find((call) => call.path.includes("/entitlements/snapshots"))?.body as Record<
        string,
        unknown
      >)?.expectedNextVersion,
    ).toBe(5);
  });

  it("creates an override from catalog features and shows reconcile hint", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [snapshot],
      featureOverrideItems: [],
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Feature overrides", level: 2 });
    await user.click(screen.getByRole("button", { name: "Create override" }));
    await user.selectOptions(
      await screen.findByLabelText("Feature"),
      "store-customer-credit",
    );
    await user.click(screen.getByLabelText("Disabled"));
    await user.type(screen.getByLabelText("Reason"), "Support hold");
    await user.click(screen.getAllByRole("button", { name: "Create override" }).at(-1)!);
    expect(await screen.findByText("Feature override created.")).toBeInTheDocument();
    expect(
      screen.getAllByText("Override saved. Reconcile entitlement to generate an updated snapshot.")
        .length,
    ).toBeGreaterThan(0);
  });

  it("lists empty feature overrides truthfully", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        latestEntitlements: [{ id: "e1", productCode: "POS", subscriptionStatus: "Active" }],
      },
      entitlementSnapshotItems: [snapshot],
      featureOverrideItems: [],
    });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/entitlements?product=POS`,
    );
    render(<App />);
    expect(await screen.findByText("No feature overrides")).toBeInTheDocument();
  });
});
