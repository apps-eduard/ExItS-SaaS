import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

const feature = {
  featureCode: "personal-ad-free",
  displayName: "Ad-free Personal",
  isActive: true,
  rewardPointsPrice: 100,
  defaultEntitlementDurationDays: 30,
  isRewardRedeemable: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
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

function authWithCatalog(manage = true) {
  return {
    ...sampleAuthorization,
    permissions: [
      ...sampleAuthorization.permissions,
      ...(manage ? ["platform.permission.manage_catalog"] : []),
    ],
  };
}

describe("personal features", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("lists features and opens detail", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, authWithCatalog());
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/catalog/products")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
      }
      if (url.includes("/api/v1/platform/personal/features/personal-ad-free")) {
        return jsonResponse(200, feature);
      }
      if (url.includes("/api/v1/platform/personal/features")) {
        return jsonResponse(200, [feature]);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/personal-features");
    render(<App />);
    expect(await screen.findByTestId("personal-features-list-page")).toBeInTheDocument();
    expect(await screen.findByText("Ad-free Personal")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Edit" })).toHaveAttribute(
      "href",
      "/admin/personal-features/personal-ad-free",
    );
  });

  it("shows empty list truthfully", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, authWithCatalog());
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/catalog/products")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
      }
      if (url.includes("/api/v1/platform/personal/features")) {
        return jsonResponse(200, []);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/personal-features");
    render(<App />);
    expect(await screen.findByText("No personal features were returned.")).toBeInTheDocument();
  });

  it("fails closed without view_portfolio", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: ["platform.permission.manage_organizations"] });
    window.history.replaceState({}, "", "/admin/personal-features");
    render(<App />);
    expect(await screen.findByTestId("forbidden-state")).toBeInTheDocument();
  });

  it("saves configuration with manage_catalog", async () => {
    stubDesktop();
    const user = userEvent.setup();
    let current = { ...feature };
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = (init?.method ?? "GET").toUpperCase();
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, authWithCatalog());
      }
      if (url.includes("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/catalog/products")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
      }
      if (url.includes("/api/v1/platform/personal/features/personal-ad-free")) {
        if (method === "PATCH") {
          current = {
            ...current,
            displayName: "Ad-free Plus",
            updatedAtUtc: "2026-08-02T00:00:00Z",
          };
          return jsonResponse(200, current);
        }
        return jsonResponse(200, current);
      }
      if (url.includes("/api/v1/platform/personal/features")) {
        return jsonResponse(200, [current]);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/personal-features/personal-ad-free");
    render(<App />);
    expect(await screen.findByTestId("personal-feature-detail-page")).toBeInTheDocument();
    const name = await screen.findByTestId("personal-features-edit-name");
    await user.clear(name);
    await user.type(name, "Ad-free Plus");
    await user.click(screen.getByTestId("personal-features-save"));
    expect(await screen.findByTestId("personal-features-save-success")).toBeInTheDocument();
  });

  it("shows truthful save failure", async () => {
    stubDesktop();
    const user = userEvent.setup();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const method = (init?.method ?? "GET").toUpperCase();
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, authWithCatalog());
      }
      if (url.includes("/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/catalog/products")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
      }
      if (url.includes("/api/v1/platform/personal/features/personal-ad-free")) {
        if (method === "PATCH") {
          return jsonResponse(500, { title: "Error", detail: "save failed" });
        }
        return jsonResponse(200, feature);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/personal-features/personal-ad-free");
    render(<App />);
    await user.click(await screen.findByTestId("personal-features-save"));
    expect(await screen.findByTestId("personal-features-save-error")).toBeInTheDocument();
    expect(screen.getByText(/save failed/i)).toBeInTheDocument();
    expect(screen.queryByTestId("personal-features-save-success")).not.toBeInTheDocument();
  });
});
