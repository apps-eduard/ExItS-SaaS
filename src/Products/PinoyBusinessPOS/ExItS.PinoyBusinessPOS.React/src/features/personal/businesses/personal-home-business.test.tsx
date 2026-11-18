import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { render, screen } from "@testing-library/react";
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
const membershipId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function createFetch() {
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
      return jsonResponse(200, [
        {
          productCode: "pinoy-business-pos",
          productDisplayName: "PinoyBusinessPOS",
          organizationId: posOrg,
          organizationDisplayName: "ABC Grocery",
          membershipRole: "OrganizationOwner",
          roleDisplay: "Owner",
          planKey: "pro",
          planDisplayName: "Pro",
          subscriptionStatus: "Active",
          trialEndUtc: null,
          canManageBilling: true,
        },
      ]);
    }
    if (url.includes("/api/v1/personal/workplaces")) {
      return jsonResponse(200, [
        {
          organizationId: loanOrg,
          organizationDisplayName: "North Shop",
          publicOrganizationId: "ORG012345",
          staffUserId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
          staffLogin: "ana@ORG012345",
          membershipId,
          membershipRole: "OrganizationMember",
          membershipRoleDisplay: "Staff",
          membershipStatus: "Active",
          productRole: "Cashier",
          productRoleDisplay: "Cashier",
          branches: [],
          planDisplayName: "Basic",
        },
      ]);
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

  it("does not show Your businesses or Your workplace on Personal home", async () => {
    vi.stubGlobal("fetch", createFetch());
    renderHome();

    expect(await screen.findByTestId("personal-utang-summary")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-business-summary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("personal-owned-businesses")).not.toBeInTheDocument();
    expect(screen.queryByTestId("personal-staff-workplaces")).not.toBeInTheDocument();
    expect(screen.queryByText("Your businesses")).not.toBeInTheDocument();
    expect(screen.queryByText("Your workplace")).not.toBeInTheDocument();
    expect(screen.queryByText("ABC Grocery")).not.toBeInTheDocument();
    expect(screen.queryByText("North Shop")).not.toBeInTheDocument();
  });
});
