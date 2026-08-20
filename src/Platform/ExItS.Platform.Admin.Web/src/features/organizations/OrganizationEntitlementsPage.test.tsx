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
    expect(screen.getByText("4")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /override/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /reconcile/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /snapshot/i })).not.toBeInTheDocument();
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
});
