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

const sampleUser = {
  id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  displayName: "Olivia Mendoza",
  username: "olivia",
  email: "olivia@example.test",
  status: "Active",
  accountClasses: ["Platform"],
  organizationNames: [],
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

describe("platform users directory", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders all accounts and hides mutation controls", async () => {
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
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
      }
      if (url.includes("/api/v1/platform/users")) {
        return jsonResponse(200, {
          items: [sampleUser],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/users");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "All Accounts" })).toBeInTheDocument();
    expect(await screen.findByText("Olivia Mendoza")).toBeInTheDocument();
    expect(await screen.findByText("olivia")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Olivia Mendoza" })).toHaveAttribute(
      "href",
      "/admin/users/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    );
  });

  it("maps directory, search, status, sort, and paging to server parameters", async () => {
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
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
      }
      if (url.includes("/api/v1/platform/users")) {
        return jsonResponse(200, {
          items: [sampleUser],
          totalCount: 21,
          page: 1,
          pageSize: 20,
        });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/users?directory=PlatformStaff");
    const user = userEvent.setup();
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "All Accounts / Platform Staff" }),
    ).toBeInTheDocument();
    await user.type(screen.getByLabelText("Search"), "oli");
    await user.click(screen.getByRole("button", { name: "Search" }));
    await user.selectOptions(screen.getByLabelText("Status"), "Active");
    await user.selectOptions(screen.getByLabelText("Sort"), "Email");
    await user.selectOptions(screen.getByLabelText("Order"), "desc");
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("directory=PlatformStaff"))).toBe(true);
      expect(urls.some((url) => url.includes("search=oli"))).toBe(true);
      expect(urls.some((url) => url.includes("status=Active"))).toBe(true);
      expect(urls.some((url) => url.includes("sortBy=Email"))).toBe(true);
      expect(urls.some((url) => url.includes("sortDesc=true"))).toBe(true);
      expect(urls.some((url) => url.includes("page=2"))).toBe(true);
    });
  });

  it("fail-closes when unauthorized", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: ["platform.permission.view_portfolio"] });
    window.history.replaceState({}, "", "/admin/users");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "All Accounts" })).not.toBeInTheDocument();
  });

  it("rejects unknown directory values without a directory request", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch();
    window.history.replaceState({}, "", "/admin/users?directory=not-a-directory");
    render(<App />);
    expect(await screen.findByText("That directory filter is not supported.")).toBeInTheDocument();
    await waitFor(() => {
      const userCalls = fetchMock.mock.calls
        .map(([input]) => String(input))
        .filter((url) => url.includes("/api/v1/platform/users"));
      expect(userCalls).toHaveLength(0);
    });
  });
});
