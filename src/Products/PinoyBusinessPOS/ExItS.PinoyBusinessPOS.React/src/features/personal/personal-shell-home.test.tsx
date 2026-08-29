import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RouterProvider, createMemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { appRoutes } from "@/app/router";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import * as storesToPay from "@/features/personal/stores-to-pay";

vi.mock("@/features/personal/stores-to-pay", async (importOriginal) => {
  const actual = await importOriginal<typeof storesToPay>();
  return {
    ...actual,
    loadStoresToPayPreview: vi.fn(),
  };
});

const personalUserId = "11111111-1111-1111-1111-111111111111";
const personalProfileId = "22222222-2222-2222-2222-222222222222";

function createPersonalFetchMock() {
  return vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input);

    if (url.includes("/api/v1/platform/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
    }

    if (url.includes("/api/v1/platform/auth/me")) {
      return jsonResponse(200, {
          sessionId: personalUserId,
          userId: personalUserId,
          username: "ana",
          displayName: "Ana Reyes",
          email: "ana@example.com",
          selectedOrganizationId: null,
          accountClass: "Personal",
          homeOrganizationId: null,
          organizationContextLocked: false,
        });
    }

    if (url.includes("/api/v1/platform/auth/organizations")) {
      return jsonResponse(200, []);
    }

    if (url.includes("/api/v1/personal/dashboard")) {
      return jsonResponse(200, {
          userIdentityId: personalUserId,
          accountProfileId: personalProfileId,
          accountClass: "Personal",
          utangAvailable: true,
          contactCount: 2,
          activeRelationshipCount: 1,
          totalLentBalance: 500,
          totalBorrowedBalance: 150,
          pendingConfirmationCount: 1,
        });
    }

    if (url.includes("/api/v1/personal/utang/contacts")) {
      return jsonResponse(200, []);
    }

    if (url.includes("/api/v1/personal/utang/relationships/lent")) {
      return jsonResponse(200, []);
    }

    if (url.includes("/api/v1/personal/utang/relationships/borrowed")) {
      return jsonResponse(200, []);
    }

    if (url.includes("/api/v1/personal/todos")) {
      return jsonResponse(200, []);
    }

    if (url.includes("/api/v1/personal/linked-merchants")) {
      return jsonResponse(200, {
          items: [],
          totalCount: 0,
          page: 1,
          pageSize: 50,
        });
    }

    if (url.includes("/api/v1/personal/customer-link-requests")) {
      return jsonResponse(200, []);
    }

    if (url.includes("/api/v1/personal/notifications")) {
      return jsonResponse(200, []);
    }

    return jsonResponse(404, { detail: `unmocked ${url}` });
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
  beforeEach(() => {
    vi.mocked(storesToPay.loadStoresToPayPreview).mockResolvedValue({
      storeCount: 0,
      activeCount: 0,
      preview: [],
    });
  });

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
    expect(screen.getByText("Personal Tracker")).toBeInTheDocument();
    expect(screen.getByTestId("personal-stores-to-pay")).toBeInTheDocument();
    expect(screen.getByText("Stores to Pay")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("personal-stat-stores")).toHaveTextContent("0");
    });
    expect(screen.getByTestId("personal-quick-actions")).toBeInTheDocument();
    expect(screen.getByTestId("personal-qa-start-business")).toBeInTheDocument();
    expect(screen.getByTestId("personal-qa-lent")).toBeInTheDocument();
    expect(screen.getByTestId("personal-qa-owe")).toBeInTheDocument();
    expect(screen.getByTestId("personal-qa-stores")).toBeInTheDocument();
    expect(screen.getByTestId("personal-qa-people")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-qa-todo")).not.toBeInTheDocument();
    expect(screen.getByTestId("personal-stat-people")).toHaveTextContent("2");
    expect(await screen.findByTestId("personal-needs-attention")).toBeInTheDocument();
    expect(screen.getByTestId("personal-attention-pendingConfirmation")).toBeInTheDocument();
  });

  it("shows Stores to Pay preview rows from linked merchant balances", async () => {
    vi.mocked(storesToPay.loadStoresToPayPreview).mockResolvedValue({
      storeCount: 3,
      activeCount: 2,
      preview: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          businessCustomerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          displayName: "Store A",
          outstandingBalance: 2000,
          currency: "PHP",
          href: "/personal/linked-merchants/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        },
        {
          organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
          businessCustomerId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          displayName: "Store B",
          outstandingBalance: 1500,
          currency: "PHP",
          href: "/personal/linked-merchants/cccccccc-cccc-cccc-cccc-cccccccccccc/dddddddd-dddd-dddd-dddd-dddddddddddd",
        },
      ],
    });
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal");

    await waitFor(() => {
      expect(screen.getByTestId("personal-stores-to-pay-list")).toBeInTheDocument();
    });
    expect(screen.getByText("Store A")).toBeInTheDocument();
    expect(screen.getByText("Store B")).toBeInTheDocument();
    expect(screen.getByTestId("personal-stat-stores")).toHaveTextContent("3");
    expect(screen.getByTestId("personal-stat-stores-active")).toHaveTextContent("2");
  });

  it("opens Stores and customer link requests under Personal More", async () => {
    vi.stubGlobal("fetch", createPersonalFetchMock());
    renderAt("/personal/more");

    await waitFor(() => {
      expect(screen.getByTestId("personal-more-page")).toBeInTheDocument();
    });
    expect(screen.getByTestId("more-open-stores")).toBeInTheDocument();
    expect(screen.getByTestId("more-open-guide")).toBeInTheDocument();
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
          return jsonResponse(200, [
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
            ]);
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
          return jsonResponse(200, [
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
            ]);
        }
        if (url.includes("/api/v1/personal/onboarding/business-types")) {
          return jsonResponse(200, [
              {
                Id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
                Code: "retail",
                Name: "General Retail",
                Description: null,
                Status: "Active",
                SortOrder: 10,
              },
            ]);
        }
        if (url.includes("/api/v1/personal/profile")) {
          return jsonResponse(200, {
              UserIdentityId: personalUserId,
              AccountProfileId: personalProfileId,
              Username: "ana",
              DisplayName: "Ana Reyes",
              Email: "ana@example.com",
              AccountClass: "Personal",
              Status: "Active",
              Phone: null,
            });
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
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        if (url.includes("/api/v1/platform/auth/me")) {
          return jsonResponse(200, {
              sessionId: personalUserId,
              username: "cashier@ORG000001",
              displayName: "Cashier",
              email: "cashier@example.com",
              selectedOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              accountClass: "Organization",
              homeOrganizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
              organizationContextLocked: true,
            });
        }
        if (url.includes("/api/v1/platform/auth/organizations")) {
          return jsonResponse(200, [
              {
                organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                displayName: "Org",
                slug: "org",
              },
            ]);
        }
        return jsonResponse(404, { detail: "unmocked" });
      }),
    );

    renderAt("/personal");

    await waitFor(() => {
      expect(screen.queryByTestId("personal-shell")).not.toBeInTheDocument();
    });
  });
});
