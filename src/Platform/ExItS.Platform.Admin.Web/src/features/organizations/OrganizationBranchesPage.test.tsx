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

const primaryBranch = {
  id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  organizationId: sampleOrg.id,
  code: "MAIN",
  name: "Main Store",
  status: "Active",
  isPrimary: true,
  city: "Manila",
  region: "NCR",
};

const secondaryBranch = {
  id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  organizationId: sampleOrg.id,
  code: "QC",
  name: "Quezon Branch",
  status: "Inactive",
  isPrimary: false,
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

describe("organization workspace branches", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("navigates Overview and Branches and maps returned branch fields", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      branchItems: [primaryBranch, secondaryBranch],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
    const workspaceNav = screen.getByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Branches" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/branches`);
    });
    expect(await screen.findByRole("heading", { name: "Branches", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Main Store")).toBeInTheDocument();
    expect(screen.getByText("MAIN")).toBeInTheDocument();
    expect(screen.getByText("Manila, NCR")).toBeInTheDocument();
    expect(screen.getByText("Primary")).toBeInTheDocument();
    expect(screen.getByText("Quezon Branch")).toBeInTheDocument();
    expect(screen.getByText("Inactive")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /edit/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Main Store" })).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/search/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /next/i })).not.toBeInTheDocument();
    const crumb = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(crumb).toHaveTextContent("Organizations");
    expect(crumb).toHaveTextContent("Northwind Market");
    expect(crumb).toHaveTextContent("Branches");
    await user.click(within(workspaceNav).getByRole("link", { name: "Overview" }));
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
  });

  it("loads a branches deep link without query parameters", async () => {
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
      if (url.includes(`/organizations/${sampleOrg.id}/branches`)) {
        expect(url).not.toMatch(/[?&](page|pageSize|search|sort|status)=/);
        return jsonResponse(200, [primaryBranch]);
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/branches`);
    render(<App />);
    expect(await screen.findByText("Main Store")).toBeInTheDocument();
    expect(JSON.stringify(fetchMock.mock.calls)).not.toMatch(/"method":"(POST|PUT|PATCH|DELETE)"/i);
  });

  it("shows empty, retryable error, and copy diagnostics", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg], branchItems: [] });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/branches`);
    const { unmount } = render(<App />);
    expect(await screen.findByText("No branches")).toBeInTheDocument();
    unmount();

    let fail = true;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
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
        if (url.includes("/branches")) {
          if (fail) {
            return jsonResponse(500, { title: "Error", status: 500, detail: "boom" });
          }
          return jsonResponse(200, [primaryBranch]);
        }
        if (url.includes(`/organizations/${sampleOrg.id}`)) {
          return jsonResponse(200, sampleOrg);
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    const user = userEvent.setup();
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Unable to load branches." }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy error details" })).toBeInTheDocument();
    fail = false;
    await user.click(screen.getByRole("button", { name: "Retry" }));
    expect(await screen.findByText("Main Store")).toBeInTheDocument();
  });

  it("fail-closes forbidden branch list without leaking payload", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenBranches: true,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/branches`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("branch-secret")).not.toBeInTheDocument();
    expect(screen.queryByText("Main Store")).not.toBeInTheDocument();
  });

  it("inherits organization not found and invalid id without a branches request", async () => {
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
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organizations/not-a-guid/branches");
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Organization not found" }),
    ).toBeInTheDocument();
    expect(fetchMock.mock.calls.every(([input]) => !String(input).includes("/branches"))).toBe(
      true,
    );
  });

  it("localizes branches and keeps density", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      branchItems: [primaryBranch],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/branches`);
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Branches", level: 1 });
    expect(document.documentElement.dataset.density).toBe("balanced");
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(
      await screen.findByRole("heading", { name: "Mga Sangay", level: 1 }),
    ).toBeInTheDocument();
    expect(document.documentElement.dataset.density).toBe("balanced");
  });
});
