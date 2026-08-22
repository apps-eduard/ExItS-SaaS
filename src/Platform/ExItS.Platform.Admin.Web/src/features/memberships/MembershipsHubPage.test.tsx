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

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

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

describe("memberships hub", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("lists organizations and links to people workspace", async () => {
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
      if (url.includes("/api/v1/platform/organizations")) {
        return jsonResponse(200, {
          items: [
            {
              id: orgId,
              displayName: "Acme Trading",
              slug: "acme",
              status: "Active",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organization-users");
    render(<App />);
    expect(await screen.findByTestId("memberships-hub-page")).toBeInTheDocument();
    expect(await screen.findByRole("link", { name: "Acme Trading" })).toHaveAttribute(
      "href",
      `/admin/organizations/${orgId}/people`,
    );
  });

  it("shows empty state when no organizations", async () => {
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
      if (url.includes("/api/v1/platform/organizations")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organization-users");
    render(<App />);
    expect(await screen.findByText("No organizations")).toBeInTheDocument();
  });

  it("fails closed without manage_memberships", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: ["platform.permission.view_portfolio"] });
    window.history.replaceState({}, "", "/admin/organization-users");
    render(<App />);
    expect(await screen.findByTestId("forbidden-state")).toBeInTheDocument();
    expect(screen.queryByTestId("memberships-hub-page")).not.toBeInTheDocument();
  });

  it("shows error state with retry on API failure", async () => {
    stubDesktop();
    const user = userEvent.setup();
    let fail = true;
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
      if (url.includes("/api/v1/platform/organizations")) {
        if (fail) {
          return jsonResponse(500, { title: "Server error", detail: "orgs failed" });
        }
        return jsonResponse(200, {
          items: [{ id: orgId, displayName: "Acme Trading", slug: "acme", status: "Active" }],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organization-users");
    render(<App />);
    expect(
      await screen.findByText("Unable to load organizations for memberships."),
    ).toBeInTheDocument();
    fail = false;
    await user.click(screen.getByRole("button", { name: /retry/i }));
    await waitFor(() => {
      expect(screen.getByRole("link", { name: "Acme Trading" })).toBeInTheDocument();
    });
  });
});
