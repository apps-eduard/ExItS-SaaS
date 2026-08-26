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
    expect(screen.getByRole("link", { name: "Northwind Market" })).toHaveAttribute(
      "href",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
  });

  it("shows the Organizations nav item when authorized and fail-closes when not", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin");
    const { unmount } = render(<App />);
    expect(await screen.findByRole("link", { name: "All Organizations" })).toHaveAttribute(
      "href",
      "/admin/organizations",
    );
    unmount();

    mockAuthenticatedFetch({ permissions: ["platform.permission.view_audit_records"] });
    window.history.replaceState({}, "", "/admin/organizations");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Organizations" })).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "All Organizations" })).not.toBeInTheDocument();
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
      if (url.includes("/catalog/products")) {
        return jsonResponse(200, {
          items: [
            {
              id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
              code: "future-product-x",
              displayName: "Future Product X",
              status: "Active",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 100,
        });
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
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, {
            items: [
              {
                id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                code: "future-product-x",
                displayName: "Future Product X",
                status: "Active",
              },
            ],
            totalCount: 1,
            page: 1,
            pageSize: 100,
          });
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
    expect(screen.getByRole("button", { name: "Copy error details" })).toBeInTheDocument();
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

  it("shows product context and server-filtered organizations for a sanitized catalog product", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      catalogProductItems: [
        {
          id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          code: "future-product-x",
          displayName: "Future Product X",
          status: "Active",
        },
        {
          id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          code: "pinoy-business-pos",
          displayName: "Pinoy Business POS",
          status: "Active",
        },
      ],
    });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations?product=future-product-x&search=north&status=Active&page=2",
    );
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Organizations / Future Product X" }),
    ).toBeInTheDocument();
    expect(await screen.findByText("Northwind Market")).toBeInTheDocument();
    expect(screen.getByTestId("product-org-filter-results")).toHaveAttribute(
      "data-product-code",
      "future-product-x",
    );
    expect(screen.getByTestId("product-org-filter-results")).toHaveAttribute(
      "data-total-count",
      "1",
    );
    expect(screen.queryByTestId("product-org-filter-blocked")).not.toBeInTheDocument();
    expect(screen.getByLabelText("Product")).toHaveValue("future-product-x");
    expect(screen.getByLabelText("Search")).toHaveValue("north");
    expect(screen.getByLabelText("Status")).toHaveValue("Active");

    await waitFor(() => {
      const orgListCalls = fetchMock.mock.calls
        .map(([input]) => String(input))
        .filter(
          (url) =>
            url.includes("/api/v1/platform/organizations") &&
            !url.includes("/branches") &&
            !url.includes("commercial-summary") &&
            !/\/organizations\/[0-9a-fA-F-]{36}/.test(url),
        );
      expect(orgListCalls.some((url) => url.includes("productCode=future-product-x"))).toBe(true);
      expect(orgListCalls.some((url) => url.includes("search=north"))).toBe(true);
      expect(orgListCalls.some((url) => url.includes("status=Active"))).toBe(true);
      expect(orgListCalls.every((url) => !url.includes("product=future-product-x"))).toBe(true);
    });
  });

  it("rejects invalid product codes without product-specific API calls", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch({ organizationItems: [sampleOrg] });
    window.history.replaceState({}, "", "/admin/organizations?product=not-a-real-product");
    render(<App />);
    expect(
      await screen.findByText("That product is not available in the authorized catalog."),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("product-org-filter-blocked")).not.toBeInTheDocument();
    expect(screen.queryByText("Northwind Market")).not.toBeInTheDocument();
    await waitFor(() => {
      const orgListCalls = fetchMock.mock.calls
        .map(([input]) => String(input))
        .filter(
          (url) =>
            url.includes("/api/v1/platform/organizations?") ||
            url.endsWith("/api/v1/platform/organizations") ||
            url.includes("/api/v1/platform/organizations?page="),
        );
      expect(orgListCalls).toHaveLength(0);
    });
  });

  it("preserves list filters when selecting a dynamic product", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      catalogProductItems: [
        {
          id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          code: "future-product-x",
          displayName: "Future Product X",
          status: "Active",
        },
      ],
    });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations?search=acme&status=Suspended&sortBy=Slug&sortDesc=true&page=2",
    );
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: /^Organizations$/ });
    await waitFor(() => {
      expect(screen.getByLabelText("Product")).not.toBeDisabled();
      expect(screen.getByRole("option", { name: "Future Product X" })).toBeInTheDocument();
    });
    await user.selectOptions(screen.getByLabelText("Product"), "future-product-x");
    await waitFor(() => {
      expect(window.location.search).toContain("product=future-product-x");
      expect(window.location.search).toContain("search=acme");
      expect(window.location.search).toContain("status=Suspended");
      expect(window.location.search).toContain("sortBy=Slug");
      expect(window.location.search).toContain("sortDesc=true");
    });
    expect(window.location.search).not.toContain("page=2");
    expect(await screen.findByTestId("product-org-filter-results")).toHaveAttribute(
      "data-product-code",
      "future-product-x",
    );
    expect(
      screen.queryByText("Product-specific organization filtering is not available yet."),
    ).not.toBeInTheDocument();
  });
});
