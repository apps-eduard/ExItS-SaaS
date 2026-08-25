import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const personalUserId = "11111111-1111-1111-1111-111111111111";
const orgAId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const orgBId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const branchAId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

function createPersonalWithOrgsFetchMock(orgCount: 1 | 2) {
  let accountClass: "Personal" | "Organization" = "Personal";

  return vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = init?.method ?? "GET";

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/me")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          sessionId: personalUserId,
          username: "ana",
          displayName: "Ana Reyes",
          email: "ana@example.com",
          selectedOrganizationId: accountClass === "Organization" ? orgAId : null,
          accountClass,
          homeOrganizationId: null,
          organizationContextLocked: false,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/account-profiles") && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () => [
          {
            id: "22222222-2222-2222-2222-222222222222",
            userIdentityId: personalUserId,
            accountClass: "Personal",
            allowedScope: "Personal",
            status: "Active",
          },
          {
            id: "33333333-3333-3333-3333-333333333333",
            userIdentityId: personalUserId,
            accountClass: "Organization",
            allowedScope: "Organization",
            status: "Active",
          },
        ],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/account-profiles/select") && method === "POST") {
      accountClass = "Organization";
      return {
        ok: true,
        status: 200,
        json: async () => ({
          sessionId: personalUserId,
          username: "ana",
          displayName: "Ana Reyes",
          email: "ana@example.com",
          selectedOrganizationId: null,
          accountClass: "Organization",
          homeOrganizationId: null,
          organizationContextLocked: false,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/organizations") && method === "GET") {
      const organizations =
        orgCount === 1
          ? [
              {
                organizationId: orgAId,
                displayName: "Kizy Store",
                slug: "kizy-store",
                membershipRole: "OrganizationOwner",
              },
            ]
          : [
              {
                organizationId: orgAId,
                displayName: "Kizy Store",
                slug: "kizy-store",
                membershipRole: "OrganizationOwner",
              },
              {
                organizationId: orgBId,
                displayName: "Second Store",
                slug: "second-store",
                membershipRole: "OrganizationOwner",
              },
            ];
      return {
        ok: true,
        status: 200,
        json: async () => organizations,
        text: async () => "",
      } as Response;
    }

    if (url.includes("/organizations/") && url.includes("/branches") && method === "GET") {
      return {
        ok: true,
        status: 200,
        json: async () => [
          {
            id: branchAId,
            organizationId: orgAId,
            code: "MAIN",
            name: "Main Branch",
            isPrimary: true,
            status: "Active",
          },
        ],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/platform/auth/organization-context") && method === "PUT") {
      return { ok: true, status: 204, json: async () => null, text: async () => "" } as Response;
    }

    if (url.includes("/api/v1/platform/auth/token") && method === "POST") {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          accessToken: "grant-token",
          productAccessAllowed: true,
          mappedPosRoleCode: "Owner",
          productLocalRoleCode: "Owner",
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/notifications")) {
      return { ok: true, status: 200, json: async () => [], text: async () => "" } as Response;
    }

    return {
      ok: false,
      status: 404,
      json: async () => ({ detail: `unmocked ${url}` }),
      text: async () => "",
    } as Response;
  });
}

function renderAt(path: string) {
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });
  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
  );
}

describe("Personal More switch to business", () => {
  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 25));
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("hides Switch to business when the user has no accessible organizations", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/platform/antiforgery/token")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/platform/auth/me")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              sessionId: personalUserId,
              username: "ana",
              displayName: "Ana Reyes",
              email: "ana@example.com",
              selectedOrganizationId: null,
              accountClass: "Personal",
              homeOrganizationId: null,
              organizationContextLocked: false,
            }),
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/platform/auth/organizations")) {
          return { ok: true, status: 200, json: async () => [], text: async () => "" } as Response;
        }
        if (url.includes("/api/v1/personal/notifications")) {
          return { ok: true, status: 200, json: async () => [], text: async () => "" } as Response;
        }
        return {
          ok: false,
          status: 404,
          json: async () => ({ detail: `unmocked ${url}` }),
          text: async () => "",
        } as Response;
      }),
    );

    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("personal-more-page")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("more-switch-to-business")).not.toBeInTheDocument();
  });

  it("shows Switch to business when the user has accessible organizations", async () => {
    vi.stubGlobal("fetch", createPersonalWithOrgsFetchMock(1));
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeInTheDocument();
    });
    expect(screen.getByTestId("more-switch-to-business")).toHaveTextContent("Switch to business");
  });

  it("routes multiple organizations through the workspace chooser", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalWithOrgsFetchMock(2));
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("more-switch-to-business"));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Choose workspace" })).toBeInTheDocument();
    });
  });

  it("routes a single organization with multiple destinations through the workspace chooser", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalWithOrgsFetchMock(1));
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("more-switch-to-business"));

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Choose workspace" })).toBeInTheDocument();
    });
  });
});

describe("Personal avatar account menu switch to business", () => {
  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 25));
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
    Object.defineProperty(window.navigator, "onLine", {
      configurable: true,
      value: true,
    });
  });

  it("hides Switch to business in the avatar menu when the user has no accessible organizations", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/platform/antiforgery/token")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({ headerName: "X-XSRF-TOKEN", token: "csrf-token" }),
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/platform/auth/me")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              sessionId: personalUserId,
              username: "ana",
              displayName: "Ana Reyes",
              email: "ana@example.com",
              selectedOrganizationId: null,
              accountClass: "Personal",
              homeOrganizationId: null,
              organizationContextLocked: false,
            }),
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/platform/auth/organizations")) {
          return { ok: true, status: 200, json: async () => [], text: async () => "" } as Response;
        }
        if (url.includes("/api/v1/personal/notifications")) {
          return { ok: true, status: 200, json: async () => [], text: async () => "" } as Response;
        }
        return {
          ok: false,
          status: 404,
          json: async () => ({ detail: `unmocked ${url}` }),
          text: async () => "",
        } as Response;
      }),
    );

    renderAt("/personal");

    await waitFor(() => {
      expect(screen.getByTestId("account-menu-trigger")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("account-switch-to-business")).not.toBeInTheDocument();
  });

  it("shows Switch to business in the personal avatar menu when organizations exist", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalWithOrgsFetchMock(1));
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeInTheDocument();
    });

    await user.click(screen.getByTestId("account-menu-trigger"));
    await waitFor(() => {
      expect(screen.getByTestId("account-switch-to-business")).toHaveTextContent(
        "Switch to business",
      );
    });
    expect(screen.getByTestId("account-edit-profile")).toHaveTextContent("Edit profile");
  });

  it("keeps Personal More switch to business as a convenient entry", async () => {
    vi.stubGlobal("fetch", createPersonalWithOrgsFetchMock(1));
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeInTheDocument();
    });
  });

  it("blocks avatar Switch to business while offline", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createPersonalWithOrgsFetchMock(1));
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeInTheDocument();
    });

    Object.defineProperty(window.navigator, "onLine", {
      configurable: true,
      value: false,
    });
    window.dispatchEvent(new Event("offline"));

    await waitFor(() => {
      expect(screen.getByTestId("more-switch-to-business")).toBeDisabled();
    });

    await user.click(screen.getByTestId("account-menu-trigger"));
    await waitFor(() => {
      const menu = screen.getByRole("menu");
      expect(within(menu).getByTestId("account-switch-to-business")).toBeDisabled();
      expect(within(menu).getByText("Switching to business needs internet.")).toBeInTheDocument();
    });
  });
});
