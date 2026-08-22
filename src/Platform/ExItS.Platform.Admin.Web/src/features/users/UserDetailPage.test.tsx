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

const userId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const sampleUserDetail = {
  id: userId,
  displayName: "Olivia Mendoza",
  username: "olivia",
  email: "olivia@example.test",
  status: "Active",
  accountClasses: ["Platform"],
  organizationNames: [],
  firstName: "Olivia",
  lastName: "Mendoza",
  createdAtUtc: "2026-01-01T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
};

const sampleAssignments = {
  items: [
    {
      id: "11111111-1111-1111-1111-111111111111",
      platformUserId: userId,
      role: "PlatformAdministrator",
      status: "Active",
      grantedByActor: "admin@example.test",
      grantedAtUtc: "2026-08-01T08:00:00Z",
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 10,
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

function createFetchMock(options?: {
  userStatus?: number;
  assignments?: typeof sampleAssignments;
}) {
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
    if (url.includes("/catalog/products")) {
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 100 });
    }
    if (url.includes(`/api/v1/platform/users/${userId}/credentials`)) {
      return jsonResponse(200, {
        userId,
        hasPassword: true,
        emailVerified: true,
        isLockedOut: false,
        failedAccessCount: 0,
      });
    }
    if (url.includes(`/api/v1/platform/users/${userId}/memberships`)) {
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 50 });
    }
    if (url.includes(`/api/v1/platform/users/${userId}/product-access`)) {
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 50 });
    }
    if (
      url.includes(`/api/v1/platform/users/${userId}`) &&
      !url.includes("/memberships") &&
      !url.includes("/product-access") &&
      !url.includes("/credentials")
    ) {
      return jsonResponse(options?.userStatus ?? 200, sampleUserDetail);
    }
    if (url.includes("/api/v1/platform/users")) {
      return jsonResponse(200, { items: [sampleUserDetail], totalCount: 1, page: 1, pageSize: 20 });
    }
    if (url.includes("/api/v1/platform/authorization/assignments")) {
      return jsonResponse(200, options?.assignments ?? sampleAssignments);
    }
    return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
  });
}

describe("platform user detail", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders identity and assignments read-only", async () => {
    stubDesktop();
    vi.stubGlobal("fetch", createFetchMock());
    window.history.replaceState({}, "", `/admin/users/${userId}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Olivia Mendoza" })).toBeInTheDocument();
    expect(screen.getByText("olivia@example.test")).toBeInTheDocument();
    expect(screen.getByText("Platform administrator")).toBeInTheDocument();
    expect(screen.getByTestId("users-lifecycle-suspend")).toBeInTheDocument();
    expect(screen.getByTestId("users-credentials-panel")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /assign/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /revoke/i })).not.toBeInTheDocument();
  });

  it("shows not found for invalid guid", async () => {
    stubDesktop();
    vi.stubGlobal("fetch", createFetchMock());
    window.history.replaceState({}, "", "/admin/users/not-a-guid");
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Account not found" })).toBeInTheDocument();
  });

  it("shows not found for 404 user", async () => {
    stubDesktop();
    vi.stubGlobal("fetch", createFetchMock({ userStatus: 404 }));
    window.history.replaceState({}, "", `/admin/users/${userId}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Account not found" })).toBeInTheDocument();
  });

  it("fail-closes when unauthorized", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ permissions: ["platform.permission.view_portfolio"] });
    window.history.replaceState({}, "", `/admin/users/${userId}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Access denied" })).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Olivia Mendoza" })).not.toBeInTheDocument();
  });

  it("shows empty assignments and supports paging query", async () => {
    stubDesktop();
    const fetchMock = createFetchMock({
      assignments: { items: [], totalCount: 21, page: 1, pageSize: 10 },
    });
    vi.stubGlobal("fetch", fetchMock);
    const user = userEvent.setup();
    window.history.replaceState({}, "", `/admin/users/${userId}`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Olivia Mendoza" })).toBeInTheDocument();
    expect(await screen.findByText("No role assignments were returned.")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("assignments") && url.includes("page=2"))).toBe(true);
    });
  });

  it("preserves unknown role and assignment status values", async () => {
    stubDesktop();
    vi.stubGlobal(
      "fetch",
      createFetchMock({
        assignments: {
          items: [
            {
              id: "22222222-2222-2222-2222-222222222222",
              platformUserId: userId,
              role: "FutureRole",
              status: "FutureStatus",
              grantedByActor: "admin@example.test",
              grantedAtUtc: "2026-08-01T08:00:00Z",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 10,
        },
      }),
    );
    window.history.replaceState({}, "", `/admin/users/${userId}`);
    render(<App />);
    expect(await screen.findByText("FutureRole")).toBeInTheDocument();
    expect(screen.getByText("FutureStatus")).toBeInTheDocument();
  });
});
