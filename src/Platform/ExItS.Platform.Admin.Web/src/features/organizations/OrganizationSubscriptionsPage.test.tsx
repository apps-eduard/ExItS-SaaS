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

const subscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: sampleOrg.id,
  productCode: "POS",
  planId: "22222222-2222-2222-2222-222222222222",
  status: "Active",
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Starter",
  trialEndUtc: "2026-09-01T00:00:00Z",
  agreedPrice: 999,
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

describe("organization workspace subscriptions", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("maps returned subscription fields without mutation or price invention", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgSubscriptionItems: [subscription],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    const workspaceNav = await screen.findByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Subscription" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/subscription`);
    });
    expect(
      await screen.findByRole("heading", { name: "Subscription", level: 1 }),
    ).toBeInTheDocument();
    expect(screen.getByText("Pinoy Business POS")).toBeInTheDocument();
    expect(screen.getByText("Starter")).toBeInTheDocument();
    expect(screen.queryByText("999")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /activate/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /cancel/i })).not.toBeInTheDocument();
  });

  it("applies server search, status, trial, and paging query params", async () => {
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
      if (url.includes("/subscriptions")) {
        return jsonResponse(200, {
          items: [subscription],
          totalCount: 21,
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
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/subscription`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByText("Pinoy Business POS")).toBeInTheDocument();
    await user.type(screen.getByLabelText("Search"), "north");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await waitFor(() => {
      expect(window.location.search).toContain("search=north");
    });
    await user.selectOptions(screen.getByLabelText(/^Status$/), "Active");
    await waitFor(() => {
      expect(window.location.search).toContain("status=Active");
    });
    await user.selectOptions(screen.getByLabelText("Trial"), "true");
    await waitFor(() => {
      expect(window.location.search).toContain("isTrial=true");
    });
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(window.location.search).toContain("page=2");
    });
    const subUrls = fetchMock.mock.calls
      .map(([input]) => String(input))
      .filter((url) => url.includes("/organizations/") && url.includes("/subscriptions"));
    expect(subUrls.some((url) => url.includes("search=north"))).toBe(true);
    expect(subUrls.some((url) => url.includes("status=Active"))).toBe(true);
    expect(subUrls.some((url) => url.includes("isTrial=true"))).toBe(true);
    expect(subUrls.some((url) => url.includes("page=2") && url.includes("pageSize=20"))).toBe(true);
  });

  it("fail-closes forbidden subscription lists", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenOrgSubscriptions: true,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/subscription`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("subscription-secret")).not.toBeInTheDocument();
  });
});
