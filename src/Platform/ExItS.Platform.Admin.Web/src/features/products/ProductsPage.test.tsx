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

const sampleProduct = {
  id: "11111111-1111-1111-1111-111111111111",
  code: "future-product-x",
  displayName: "Future Product X",
  status: "Active",
  createdAtUtc: "2026-01-01T08:00:00Z",
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

function createFetchMock() {
  return vi.fn(async (input: RequestInfo | URL) => {
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
    if (url.includes("/api/v1/platform/catalog/products")) {
      return jsonResponse(200, {
        items: [sampleProduct],
        totalCount: 21,
        page: 1,
        pageSize: 20,
      });
    }
    return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
  });
}

describe("platform product catalog", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders catalog read-only with future product row link", async () => {
    stubDesktop();
    vi.stubGlobal("fetch", createFetchMock());
    window.history.replaceState({}, "", "/admin/products");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Products" })).toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "future-product-x" })).toHaveAttribute(
      "href",
      "/admin/products/11111111-1111-1111-1111-111111111111",
    );
    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("maps search, status, sort, and paging to server parameters", async () => {
    stubDesktop();
    const fetchMock = createFetchMock();
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/products");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Products" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Search"), "future");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await user.selectOptions(screen.getByLabelText("Status"), "Active");
    await user.selectOptions(screen.getByLabelText("Sort"), "Code");
    await user.selectOptions(screen.getByLabelText("Order"), "desc");
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("search=future"))).toBe(true);
      expect(urls.some((url) => url.includes("status=Active"))).toBe(true);
      expect(urls.some((url) => url.includes("sortBy=Code"))).toBe(true);
      expect(urls.some((url) => url.includes("sortDesc=true"))).toBe(true);
      expect(urls.some((url) => url.includes("page=2"))).toBe(true);
    });
  });

  it("fail-closes when unauthorized", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: [] });
    window.history.replaceState({}, "", "/admin/products");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Products" })).not.toBeInTheDocument();
  });
});
