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
const posOrg = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const loanOrg = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

function affiliation(overrides: Record<string, unknown>) {
  return {
    productCode: "pinoy-business-pos",
    productDisplayName: "PinoyBusinessPOS",
    organizationId: null,
    organizationDisplayName: null,
    membershipRole: null,
    roleDisplay: null,
    planKey: null,
    planDisplayName: null,
    subscriptionStatus: null,
    trialEndUtc: null,
    canManageBilling: false,
    ...overrides,
  };
}

function createFetch(affiliations: unknown[] | "error") {
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
    if (url.includes("/api/v1/personal/product-affiliations")) {
      if (affiliations === "error") {
        return jsonResponse(500, { title: "unavailable" });
      }
      return jsonResponse(200, affiliations);
    }
    if (url.includes("/api/v1/personal/dashboard")) {
      return jsonResponse(200, {
        userIdentityId: personalUserId,
        accountProfileId: "22222222-2222-2222-2222-222222222222",
        accountClass: "Personal",
        utangAvailable: true,
        contactCount: 0,
        activeRelationshipCount: 0,
        totalLentBalance: 0,
        totalBorrowedBalance: 0,
        sharedWithMeLentBalance: 0,
        sharedWithMeBorrowedBalance: 0,
        sharedWithMeActiveCount: 0,
        pendingConfirmationCount: 0,
      });
    }
    if (url.includes("/api/v1/personal/")) {
      return jsonResponse(200, []);
    }
    if (url.includes("/api/v1/personal/notifications")) {
      return jsonResponse(200, []);
    }
    return jsonResponse(404, { detail: `unmocked ${url}` });
  });
}

function renderHome() {
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: ["/personal"] });
  render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
  );
  return memoryRouter;
}

describe("Personal home businesses", () => {
  beforeEach(() => {
    vi.mocked(storesToPay.loadStoresToPayPreview).mockResolvedValue({
      storeCount: 0,
      activeCount: 0,
      preview: [],
    });
  });

  afterEach(async () => {
    await new Promise((resolve) => setTimeout(resolve, 25));
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("offers Start a Business on the portfolio when there are no affiliations", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("fetch", createFetch([]));
    const router = renderHome();

    expect(await screen.findByTestId("personal-utang-summary")).toBeInTheDocument();
    const start = await screen.findByTestId("personal-home-start-business");
    expect(start).toHaveAttribute("href", "/personal/businesses");
    await user.click(start);
    await waitFor(() => {
      expect(router.state.location.pathname).toBe("/personal/businesses");
    });
  });

  it("shows the POS business and an Open action", async () => {
    vi.stubGlobal(
      "fetch",
      createFetch([
        affiliation({
          organizationId: posOrg,
          organizationDisplayName: "ABC Grocery",
          roleDisplay: "Owner",
          subscriptionStatus: "Active",
          canManageBilling: true,
        }),
      ]),
    );
    renderHome();

    expect(await screen.findByTestId("personal-utang-summary")).toBeInTheDocument();
    expect(await screen.findByText("ABC Grocery")).toBeInTheDocument();
    expect(screen.getByText("Owner · Active")).toBeInTheDocument();
    const open = screen.getByTestId("personal-business-open-pinoy-business-pos");
    const startAnother = screen.getByTestId("personal-home-start-another");
    expect(startAnother).toHaveAttribute("href", "/personal/businesses");
    expect(startAnother.parentElement).toBe(open.parentElement);
  });

  it("shows each different product once", async () => {
    vi.stubGlobal(
      "fetch",
      createFetch([
        affiliation({
          organizationId: posOrg,
          organizationDisplayName: "ABC Grocery",
          roleDisplay: "Owner",
          subscriptionStatus: "Active",
          canManageBilling: true,
        }),
        affiliation({
          productCode: "pinoy-loan-manager",
          productDisplayName: "PinoyLoanManager",
          organizationId: loanOrg,
          organizationDisplayName: "ABC Lending",
          roleDisplay: "Owner",
          subscriptionStatus: "Trialing",
          canManageBilling: true,
        }),
        affiliation({
          productCode: "pinoy-business-pos",
          organizationId: posOrg,
          organizationDisplayName: "ABC Grocery",
        }),
      ]),
    );
    renderHome();

    expect(await screen.findByText("ABC Grocery")).toBeInTheDocument();
    expect(screen.getByText("ABC Lending")).toBeInTheDocument();
    expect(screen.getAllByText("ABC Grocery")).toHaveLength(1);
    expect(screen.getByTestId("personal-business-pinoy-business-pos")).toBeInTheDocument();
    expect(screen.getByTestId("personal-business-pinoy-loan-manager")).toBeInTheDocument();
  });

  it("keeps Personal home usable when business lookup fails", async () => {
    vi.stubGlobal("fetch", createFetch("error"));
    renderHome();

    expect(await screen.findByTestId("personal-utang-summary")).toBeInTheDocument();
    expect(await screen.findByTestId("personal-business-summary")).toHaveTextContent(
      "Business information is temporarily unavailable.",
    );
    expect(screen.getByTestId("personal-quick-actions")).toBeInTheDocument();
  });
});
