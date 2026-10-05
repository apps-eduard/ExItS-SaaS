import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PersonalProductAffiliationDto } from "@/api/platform/product-affiliations-client";
import { PersonalProductSubscriptionsPage } from "@/features/personal/subscriptions/PersonalProductSubscriptionsPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const listPersonalProductAffiliations = vi.fn();
const setOrganizationContext = vi.fn();

vi.mock("@/api/platform/product-affiliations-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/product-affiliations-client")>();
  return {
    ...actual,
    listPersonalProductAffiliations: (...args: unknown[]) => listPersonalProductAffiliations(...args),
  };
});

vi.mock("@/api/platform/platform-auth-client", () => ({
  setOrganizationContext: (...args: unknown[]) => setOrganizationContext(...args),
}));

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

function row(overrides: Partial<PersonalProductAffiliationDto>): PersonalProductAffiliationDto {
  return {
    productCode: "pinoy-business-pos",
    productDisplayName: "PinoyBusinessPOS",
    organizationId: orgId,
    organizationDisplayName: "ABC Grocery",
    membershipRole: "OrganizationOwner",
    roleDisplay: "Owner",
    planKey: "pro",
    planDisplayName: "Pro",
    subscriptionStatus: "Active",
    trialEndUtc: null,
    canManageBilling: true,
    ...overrides,
  };
}

function renderPage() {
  const router = createMemoryRouter(
    [
      { path: "/personal/subscriptions", element: <PersonalProductSubscriptionsPage /> },
      { path: "/", element: <div data-testid="opened-product">open</div> },
      { path: "/org/subscription", element: <div data-testid="manage-subscription">manage</div> },
      { path: "/personal/explore-pos", element: <div data-testid="choose-plan">plans</div> },
    ],
    { initialEntries: ["/personal/subscriptions"] },
  );
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <RouterProvider router={router} />
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalProductSubscriptionsPage", () => {
  beforeEach(() => {
    listPersonalProductAffiliations.mockReset();
    setOrganizationContext.mockReset();
    setOrganizationContext.mockResolvedValue({ ok: true });
  });

  it("shows the existing organization and billing actions for an owner", async () => {
    listPersonalProductAffiliations.mockResolvedValue([row({})]);
    renderPage();

    expect(await screen.findByText("ABC Grocery")).toBeInTheDocument();
    expect(screen.getByText("Pro")).toBeInTheDocument();
    expect(screen.getByText("Active")).toBeInTheDocument();
    expect(screen.getByText("You already have a PinoyBusinessPOS organization.")).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "Manage Subscription" }));
    expect(setOrganizationContext).toHaveBeenCalledWith(orgId);
    expect(await screen.findByTestId("manage-subscription")).toBeInTheDocument();
  });

  it("hides billing actions from staff", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        membershipRole: "OrganizationMember",
        roleDisplay: "Cashier",
        canManageBilling: false,
      }),
    ]);
    renderPage();

    expect(await screen.findByText(/Cashier/)).toBeInTheDocument();
    expect(screen.getByText("Subscription managed by organization")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Manage Subscription" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Open" })).toBeInTheDocument();
  });

  it("offers Get Started when the product has no organization", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        organizationId: null,
        organizationDisplayName: null,
        membershipRole: null,
        roleDisplay: null,
        planKey: null,
        planDisplayName: null,
        subscriptionStatus: null,
        canManageBilling: false,
      }),
    ]);
    renderPage();

    await userEvent.click(await screen.findByRole("button", { name: "Get Started" }));
    expect(await screen.findByTestId("choose-plan")).toBeInTheDocument();
  });

  it("keeps the organization and offers Choose Plan when the owner has no subscription", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        planKey: null,
        planDisplayName: null,
        subscriptionStatus: null,
      }),
    ]);
    renderPage();

    expect(await screen.findByText("ABC Grocery")).toBeInTheDocument();
    expect(screen.getByText("No active subscription")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Choose Plan" }));
    expect(await screen.findByTestId("choose-plan")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Get Started" })).not.toBeInTheDocument();
  });

  it("sends a cancelled subscription to choose a plan for the same product", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({ subscriptionStatus: "Cancelled" }),
    ]);
    renderPage();

    await userEvent.click(await screen.findByRole("button", { name: "Reactivate" }));
    expect(await screen.findByTestId("choose-plan")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Get Started" })).not.toBeInTheDocument();
  });
});
