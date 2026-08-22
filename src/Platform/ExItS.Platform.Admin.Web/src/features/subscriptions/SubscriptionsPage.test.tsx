import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { App } from "@/app/App";
import { jsonResponse, sampleAuthorization, textResponse } from "@/test/auth-fixtures";

const subscription = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationDisplayName: "Northwind Market",
  productCode: "pinoy-business-pos",
  planId: "22222222-2222-2222-2222-222222222222",
  status: "Active",
  productDisplayName: "Pinoy Business POS",
  planDisplayName: "Starter",
};

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 768px"),
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

describe("SubscriptionsPage portfolio list", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("shows ErrorState on API 500 instead of empty list", async () => {
    stubDesktop();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, {
            sessionId: "11111111-1111-1111-1111-111111111111",
            userId: "22222222-2222-2222-2222-222222222222",
            username: "olivia",
            displayName: "Olivia Mendoza",
            email: "olivia@example.test",
            expiresAtUtc: "2026-08-19T12:00:00Z",
            absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
            selectedOrganizationId: null,
            selectedOrganizationDisplayName: null,
            organizationSelectionState: "None",
            activeOrganizationCount: 0,
            accountClass: "Platform",
          });
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        if (url.includes("/api/v1/platform/subscriptions")) {
          return jsonResponse(500, {
            title: "Error",
            status: 500,
            detail: "subscription-portfolio-boom",
          });
        }
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/subscriptions");
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Unable to load subscriptions.", level: 2 }),
    ).toBeInTheDocument();
    expect(screen.queryByText("No subscriptions")).not.toBeInTheDocument();
  });

  it("shows empty state on successful empty response", async () => {
    stubDesktop();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, {
            sessionId: "11111111-1111-1111-1111-111111111111",
            userId: "22222222-2222-2222-2222-222222222222",
            username: "olivia",
            displayName: "Olivia Mendoza",
            email: "olivia@example.test",
            expiresAtUtc: "2026-08-19T12:00:00Z",
            absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
            selectedOrganizationId: null,
            selectedOrganizationDisplayName: null,
            organizationSelectionState: "None",
            activeOrganizationCount: 0,
            accountClass: "Platform",
          });
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        if (url.includes("/api/v1/platform/subscriptions")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/subscriptions");
    render(<App />);
    expect(await screen.findByText("No subscriptions")).toBeInTheDocument();
  });

  it("renders subscription rows when data is returned", async () => {
    stubDesktop();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/auth/me")) {
          return jsonResponse(200, {
            sessionId: "11111111-1111-1111-1111-111111111111",
            userId: "22222222-2222-2222-2222-222222222222",
            username: "olivia",
            displayName: "Olivia Mendoza",
            email: "olivia@example.test",
            expiresAtUtc: "2026-08-19T12:00:00Z",
            absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
            selectedOrganizationId: null,
            selectedOrganizationDisplayName: null,
            organizationSelectionState: "None",
            activeOrganizationCount: 0,
            accountClass: "Platform",
          });
        }
        if (url.includes("/authorization/me")) {
          return jsonResponse(200, sampleAuthorization);
        }
        if (url.includes("/health")) {
          return textResponse(200, "Healthy");
        }
        if (url.includes("/api/v1/platform/subscriptions")) {
          return jsonResponse(200, {
            items: [subscription],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          });
        }
        if (url.includes("/catalog/products")) {
          return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
        }
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
      }),
    );
    window.history.replaceState({}, "", "/admin/subscriptions");
    render(<App />);
    await waitFor(() => {
      expect(screen.getAllByText("Northwind Market").length).toBeGreaterThan(0);
    });
    expect(screen.getAllByText("Pinoy Business POS").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Starter").length).toBeGreaterThan(0);
  });
});
