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
  createdAtUtc: "2026-01-15T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
  profile: { legalName: "Northwind LLC" },
};

function stubDesktop(table = true) {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || (table && query.includes("min-width: 768px")),
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

describe("organization workspace overview", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("opens the workspace from the organizations list and preserves query state", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      commercialSummary: {
        subscriptions: [{ id: "s1", productCode: "POS", status: "Active" }],
      },
    });
    window.history.replaceState({}, "", "/admin/organizations?search=north&status=Active");
    const user = userEvent.setup();
    render(<App />);
    await user.click(await screen.findByRole("link", { name: "Northwind Market" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(
        "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      );
    });
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
    expect(screen.getAllByText("northwind-market").length).toBeGreaterThan(0);
    expect(screen.getByText("Northwind LLC")).toBeInTheDocument();
    expect(screen.getByText("POS")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /edit/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
    await user.click(
      within(screen.getByRole("navigation", { name: "Breadcrumb" })).getByRole("link", {
        name: "Organizations",
      }),
    );
    await waitFor(() => {
      expect(window.location.pathname).toBe("/admin/organizations");
      expect(window.location.search).toContain("search=north");
      expect(window.location.search).toContain("status=Active");
    });
  });

  it("loads a direct deep link and shows breadcrumbs", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg] });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
    const crumb = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(crumb).toHaveTextContent("Organizations");
    expect(crumb).toHaveTextContent("Northwind Market");
  });

  it("keeps the organization page usable when commercial summary fails and retries", async () => {
    stubDesktop();
    let failCommercial = true;
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
      if (url.includes("commercial-summary")) {
        if (failCommercial) {
          return jsonResponse(500, { title: "Error", status: 500, detail: "summary boom" });
        }
        return jsonResponse(200, {
          subscriptions: [{ id: "s1", productCode: "POS", status: "Active" }],
          payments: [],
          latestEntitlements: [],
        });
      }
      if (url.includes("/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
    expect(
      await screen.findByRole("heading", { name: "Unable to load commercial records." }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy diagnostics" })).toBeInTheDocument();
    failCommercial = false;
    await user.click(screen.getByRole("button", { name: "Retry" }));
    expect(await screen.findByText("POS")).toBeInTheDocument();
    expect(JSON.stringify(fetchMock.mock.calls)).not.toMatch(/"method":"(POST|PUT|PATCH|DELETE)"/i);
  });

  it("shows organization not found for missing and invalid ids without malformed requests", async () => {
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
      if (url.includes("/organizations/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")) {
        return jsonResponse(404, {
          title: "Not Found",
          status: 404,
          errorCode: "application.organization.not_found",
        });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organizations/not-a-guid");
    const first = render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Organization not found" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to Organizations" })).toBeInTheDocument();
    expect(
      fetchMock.mock.calls.every(([input]) => !String(input).includes("/organizations/not-a-guid")),
    ).toBe(true);
    first.unmount();

    window.history.replaceState(
      {},
      "",
      "/admin/organizations/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    );
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Organization not found" }),
    ).toBeInTheDocument();
  });

  it("fail-closes forbidden organization GET without leaking payload text", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenOrganization: true,
    });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("Northwind Market")).not.toBeInTheDocument();
    expect(screen.queryByText("Forbidden.")).not.toBeInTheDocument();
  });

  it("localizes workspace copy and keeps density", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg] });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Northwind Market" });
    expect(document.documentElement.dataset.density).toBe("balanced");
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(await screen.findByText("Pagkakakilanlan")).toBeInTheDocument();
    expect(document.documentElement.dataset.density).toBe("balanced");
  });

  it("offers a mobile Open action", async () => {
    stubDesktop(false);
    mockAuthenticatedFetch({ organizationItems: [sampleOrg] });
    window.history.replaceState({}, "", "/admin/organizations");
    render(<App />);
    expect(await screen.findByRole("link", { name: "Open" })).toHaveAttribute(
      "href",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
  });
});
