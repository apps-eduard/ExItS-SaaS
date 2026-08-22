import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const personalUserId = "11111111-1111-1111-1111-111111111111";
const personalProfileId = "22222222-2222-2222-2222-222222222222";

function createPersonalFetchMock() {
  return vi.fn(async (input: RequestInfo | URL) => {
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
      return {
        ok: true,
        status: 200,
        json: async () => [],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/dashboard")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          userIdentityId: personalUserId,
          accountProfileId: personalProfileId,
          accountClass: "Personal",
          utangAvailable: true,
          contactCount: 2,
          activeRelationshipCount: 1,
          totalLentBalance: 500,
          totalBorrowedBalance: 150,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/todos")) {
      return {
        ok: true,
        status: 200,
        json: async () => [],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        }),
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/customer-link-requests")) {
      return {
        ok: true,
        status: 200,
        json: async () => [],
        text: async () => "",
      } as Response;
    }

    if (url.includes("/api/v1/personal/notifications")) {
      return {
        ok: true,
        status: 200,
        json: async () => [],
        text: async () => "",
      } as Response;
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

describe("Personal shell and home (RMAP-22B)", () => {
  afterEach(async () => {
    // WorkspaceProvider may still be resolving org list when the assertion finishes.
    await new Promise((resolve) => setTimeout(resolve, 25));
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("shows Personal shell navigation without POS top bar", async () => {
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal");

    await waitFor(() => {
      expect(screen.getByTestId("personal-shell")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-bottom-nav")).toBeInTheDocument();
    expect(screen.queryByTestId("app-top-bar")).not.toBeInTheDocument();
    expect(screen.getByTestId("shell-connection-button")).toBeInTheDocument();
    expect(screen.getByTestId("personal-notification-bell")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-notification-bell-badge")).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-nav-home")).toBeInTheDocument();
    expect(screen.getByTestId("personal-nav-utang")).toBeInTheDocument();
    expect(screen.getByTestId("personal-nav-todo")).toBeInTheDocument();
    expect(screen.getByTestId("personal-nav-orders")).toBeInTheDocument();
    expect(screen.getByTestId("personal-nav-more")).toBeInTheDocument();
  });

  it("loads Utang-first home summary and quick actions", async () => {
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal");

    await waitFor(() => {
      expect(screen.getByTestId("personal-utang-summary")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-quick-actions")).toBeInTheDocument();
    expect(screen.getByTestId("personal-qa-lent")).toBeInTheDocument();
    expect(screen.getByTestId("personal-stat-people")).toHaveTextContent("2");
  });

  it("opens Stores and customer link requests under Personal More", async () => {
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("personal-more-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("more-open-stores")).toBeInTheDocument();
    expect(screen.getByTestId("more-open-customer-links")).toBeInTheDocument();
    expect(screen.getByTestId("more-open-orders")).toBeInTheDocument();
    expect(screen.getByTestId("more-open-start-business")).toBeInTheDocument();
    expect(screen.queryByTestId("more-switch-to-business")).not.toBeInTheDocument();
  });

  it("renders Explore POS plans inside Personal shell", async () => {
    const base = createPersonalFetchMock();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/commercial/plans")) {
          return {
            ok: true,
            status: 200,
            json: async () => [
              {
                Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                ProductCode: "pinoy-business-pos",
                Code: "business",
                DisplayName: "Business",
                Status: "Active",
                CreatedAtUtc: "2026-01-01T00:00:00Z",
                UpdatedAtUtc: "2026-01-01T00:00:00Z",
                PlanKey: "business",
                MaxBranches: 3,
                MaxActiveStaff: 10,
                MaxActivePosDevices: 2,
                MaxActiveBusinessTypes: 2,
                CustomerCreditEnabled: true,
                AdvancedReportsEnabled: false,
                ExportEnabled: false,
                TrialAllowed: true,
                DefaultTrialDays: 14,
                SortOrder: 20,
                MonthlyPrice: 999,
                AnnualPrice: 9990,
                CurrencyCode: "PHP",
              },
            ],
            text: async () => "",
          } as Response;
        }
        return base(input);
      }),
    );

    renderAt("/personal/explore-pos");
    await waitFor(() => {
      expect(screen.getByTestId("personal-explore-pos-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-shell")).toBeInTheDocument();
    expect(screen.getByTestId("explore-plan-business")).toBeInTheDocument();
    expect(screen.getByTestId("explore-start-trial-business")).toBeInTheDocument();
  });

  it("renders Start Business form with read-only auto-filled slug", async () => {
    const base = createPersonalFetchMock();
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/commercial/plans")) {
          return {
            ok: true,
            status: 200,
            json: async () => [
              {
                Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                ProductCode: "pinoy-business-pos",
                Code: "business",
                DisplayName: "Business",
                Status: "Active",
                CreatedAtUtc: "2026-01-01T00:00:00Z",
                UpdatedAtUtc: "2026-01-01T00:00:00Z",
                PlanKey: "business",
                MaxBranches: 3,
                MaxActiveStaff: 10,
                MaxActivePosDevices: 2,
                MaxActiveBusinessTypes: 2,
                CustomerCreditEnabled: true,
                AdvancedReportsEnabled: false,
                ExportEnabled: false,
                TrialAllowed: true,
                DefaultTrialDays: 14,
                SortOrder: 20,
                MonthlyPrice: 999,
                AnnualPrice: 9990,
                CurrencyCode: "PHP",
              },
            ],
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/personal/onboarding/business-types")) {
          return {
            ok: true,
            status: 200,
            json: async () => [
              {
                Id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                Code: "retail",
                Name: "General Retail",
                Description: null,
                Status: "Active",
                SortOrder: 10,
              },
            ],
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/personal/profile")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              UserIdentityId: personalUserId,
              AccountProfileId: personalProfileId,
              Username: "ana",
              DisplayName: "Ana Reyes",
              Email: "ana@example.com",
              AccountClass: "Personal",
              Status: "Active",
              Phone: null,
            }),
            text: async () => "",
          } as Response;
        }
        return base(input);
      }),
    );

    renderAt("/personal/start-business?planKey=business&trial=1&payNow=0");
    await waitFor(() => {
      expect(screen.getByTestId("personal-start-business-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("start-business-display-name")).toBeInTheDocument();
    expect(screen.getByTestId("start-business-submit")).toBeInTheDocument();
    const slugInput = screen.getByTestId("start-business-slug");
    expect(slugInput).toBeInTheDocument();
    expect(slugInput).toHaveAttribute("readonly");
    const user = userEvent.setup();
    await user.type(screen.getByTestId("start-business-display-name"), "Ana's Sari-Sari");
    expect(slugInput).toHaveValue("ana-s-sari-sari");
  });

  it("renders Stores and customer links routes inside Personal shell", async () => {
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/linked-merchants");
    await waitFor(() => {
      expect(screen.getByTestId("linked-merchants-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-shell")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Stores" })).toBeInTheDocument();
  });

  it("renders pending customer link requests page", async () => {
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/customer-links");
    await waitFor(() => {
      expect(screen.getByTestId("personal-customer-links-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("personal-shell")).toBeInTheDocument();
  });

  it("denies Personal routes for organization staff principals", async () => {
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
              username: "cashier@ORG000001",
              displayName: "Cashier",
              email: "cashier@example.com",
              selectedOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              accountClass: "Organization",
              homeOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              organizationContextLocked: true,
            }),
            text: async () => "",
          } as Response;
        }
        if (url.includes("/api/v1/platform/auth/organizations")) {
          return {
            ok: true,
            status: 200,
            json: async () => [
              {
                organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                displayName: "Org",
                slug: "org",
              },
            ],
            text: async () => "",
          } as Response;
        }
        return {
          ok: false,
          status: 404,
          json: async () => ({ detail: "unmocked" }),
          text: async () => "",
        } as Response;
      }),
    );

    renderAt("/personal");

    await waitFor(() => {
      expect(screen.queryByTestId("personal-shell")).not.toBeInTheDocument();
    });
  });
});
