import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { clearPlatformAntiforgeryToken } from "@/api/platform-http";
import {
  mockAuthenticatedFetch,
  sampleAuthorization,
} from "@/test/auth-fixtures";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

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

describe("global catalog admin", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    clearPlatformAntiforgeryToken();
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
  });

  it("renders categories list with server-side filters", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/global-catalog/categories?search=bev&status=Active");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Categories" })).toBeInTheDocument();
    expect(await screen.findByText("Beverages")).toBeInTheDocument();
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("/global-catalog/categories"))).toBe(true);
      expect(urls.some((url) => url.includes("search=bev"))).toBe(true);
      expect(urls.some((url) => url.includes("status=Active"))).toBe(true);
    });
  });

  it("maps product list search to server parameters", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/global-catalog/products");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Global Products" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Search"), "water");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("/global-catalog/products") && url.includes("search=water"))).toBe(
        true,
      );
    });
  });

  it("creates a category with CSRF header on mutation", async () => {
    stubDesktop();
    const mutationHeaders: Headers[] = [];
    const innerMock = mockAuthenticatedFetch();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const method = init?.method ?? "GET";
      if (method !== "GET") {
        mutationHeaders.push(new Headers(init?.headers));
      }
      return innerMock(input, init);
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/global-catalog/categories/new");
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Create category" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Name"), "Snacks");
    await user.click(screen.getByRole("button", { name: "Save" }));
    await waitFor(() => {
      expect(
        mutationHeaders.some((headers) => headers.get("X-XSRF-TOKEN") === "test-antiforgery-token"),
      ).toBe(true);
    });
  });

  it("shows conflict detail on stale category edit", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      globalCatalogMutationError: {
        status: 409,
        errorCode: "application.concurrency_conflict",
        detail: "Category was updated by another operator.",
      },
    });
    window.history.replaceState(
      {},
      "",
      "/admin/global-catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/edit",
    );
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Edit category" })).toBeInTheDocument();
    const nameInput = await screen.findByLabelText("Name");
    await user.clear(nameInput);
    await user.type(nameInput, "Beverages Updated");
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(
      await screen.findByText("Category was updated by another operator."),
    ).toBeInTheDocument();
  });

  it("runs lifecycle actions with confirmation", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.history.replaceState(
      {},
      "",
      "/admin/global-catalog/categories/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Beverages" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Archive" }));
    await user.click(screen.getByRole("button", { name: "Archive", hidden: false }));
    expect(await screen.findByText("Archived")).toBeInTheDocument();
  });

  it("fail-closes pages without viewGlobalCatalog", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      permissions: sampleAuthorization.permissions.filter(
        (item) => item !== "platform.permission.view_global_catalog",
      ),
    });
    window.history.replaceState({}, "", "/admin/global-catalog/categories");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
  });

  it("hides mutation controls without manage permissions", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      permissions: ["platform.permission.view_global_catalog"],
    });
    window.history.replaceState({}, "", "/admin/global-catalog/categories");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Categories" })).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Create category" })).not.toBeInTheDocument();
  });

  it("renders Filipino labels when language preference is fil-PH", async () => {
    stubDesktop();
    mockAuthenticatedFetch();
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({
        theme: "system",
        density: "balanced",
        language: "fil-PH",
        sidebarCollapsed: false,
      }),
    );
    window.history.replaceState({}, "", "/admin/global-catalog/categories");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Mga Kategorya" })).toBeInTheDocument();
  });
});
