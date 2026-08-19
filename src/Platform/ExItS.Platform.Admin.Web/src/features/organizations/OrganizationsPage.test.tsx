import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
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

describe("organizations list", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders the implemented organizations page and hides mutation controls", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg] });
    window.history.replaceState({}, "", "/admin/organizations");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Organizations" })).toBeInTheDocument();
    expect(screen.getByText("Manage organizations and Platform-level status.")).toBeInTheDocument();
    expect(await screen.findByText("Northwind Market")).toBeInTheDocument();
    expect(screen.getByText("northwind-market")).toBeInTheDocument();
    expect(screen.getAllByText("Active").length).toBeGreaterThan(0);
    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /edit/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /delete/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /northwind/i })).not.toBeInTheDocument();
  });

  it("shows the Organizations nav item when authorized and fail-closes when not", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const { unmount } = render(<App />);
    expect(await screen.findByRole("link", { name: "Organizations" })).toHaveAttribute(
      "href",
      "/admin/organizations",
    );
    unmount();

    mockAuthenticatedFetch({ permissions: ["platform.permission.view_audit_records"] });
    window.history.replaceState({}, "", "/admin/organizations");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Organizations" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Organizations" })).not.toBeInTheDocument();
  });

  it("maps search, status, sort, and pagination to server parameters", async () => {
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
      if (url.includes("/organizations")) {
        return jsonResponse(200, {
          items: [sampleOrg],
          totalCount: 21,
          page: 1,
          pageSize: 20,
        });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organizations");
    const user = userEvent.setup();
    render(<App />);
    await screen.findAllByText("Northwind Market");

    await user.type(screen.getByLabelText("Search"), "north");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await waitFor(() => {
      expect(window.location.search).toContain("search=north");
    });
    await user.selectOptions(screen.getByLabelText("Status"), "Active");
    await user.selectOptions(screen.getByLabelText("Sort"), "CreatedAtUtc");
    await user.selectOptions(screen.getByLabelText("Order"), "desc");
    await user.click(screen.getByRole("button", { name: "Next" }));

    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("search=north"))).toBe(true);
      expect(urls.some((url) => url.includes("status=Active"))).toBe(true);
      expect(urls.some((url) => url.includes("sortBy=CreatedAtUtc"))).toBe(true);
      expect(urls.some((url) => url.includes("sortDesc=true"))).toBe(true);
      expect(urls.some((url) => url.includes("page=2") && url.includes("pageSize=20"))).toBe(true);
    });
    expect(window.location.search).toContain("page=2");
  });

  it("shows empty, zero-result, and retryable error states", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [] });
    window.history.replaceState({}, "", "/admin/organizations");
    const { unmount } = render(<App />);
    expect(await screen.findAllByText("No organizations")).not.toHaveLength(0);
    unmount();

    mockAuthenticatedFetch({ organizationItems: [] });
    window.history.replaceState({}, "", "/admin/organizations?search=zzz");
    const second = render(<App />);
    expect(await screen.findByText("No organizations match your filters.")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "Reset filters" }).length).toBeGreaterThan(0);
    second.unmount();

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
        if (url.includes("/organizations")) {
          if (fail) {
            return jsonResponse(500, { title: "Error", status: 500, detail: "boom" });
          }
          return jsonResponse(200, {
            items: [sampleOrg],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/organizations");
    const user = userEvent.setup();
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Unable to load organizations." }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy diagnostics" })).toBeInTheDocument();
    fail = false;
    await user.click(screen.getByRole("button", { name: "Retry" }));
    expect(await screen.findByText("Northwind Market")).toBeInTheDocument();
  });

  it("localizes to Filipino", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg] });
    window.history.replaceState({}, "", "/admin/organizations");
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Organizations" });
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(await screen.findByRole("heading", { name: "Mga Organisasyon" })).toBeInTheDocument();
    expect(
      screen.getByText("Pamahalaan ang mga organisasyon at status sa antas ng Platform."),
    ).toBeInTheDocument();
  });
});
