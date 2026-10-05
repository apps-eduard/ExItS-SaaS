import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { PersonalProductAffiliationDto } from "@/api/platform/product-affiliations-client";
import { PersonalBusinessesPage } from "@/features/personal/businesses/PersonalBusinessesPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const listPersonalProductAffiliations = vi.fn();
const setOrganizationContext = vi.fn();
const enterBusiness = vi.fn();

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

vi.mock("@/workspace/use-switch-to-business", () => ({
  useEnterBusiness: () => ({ enterBusiness, entering: false }),
  PERSONAL_BUSINESSES_PATH: "/personal/businesses",
}));

const posOrg = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const loanOrg = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

function row(overrides: Partial<PersonalProductAffiliationDto>): PersonalProductAffiliationDto {
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

const catalog = [
  row({ productCode: "pinoy-business-pos", productDisplayName: "PinoyBusinessPOS" }),
  row({ productCode: "pinoy-loan-manager", productDisplayName: "PinoyLoanManager" }),
  row({ productCode: "pinoy-service-pro", productDisplayName: "PinoyServicePro" }),
  row({ productCode: "pinoy-pawn-manager", productDisplayName: "PinoyPawnManager" }),
  row({ productCode: "pinoy-buy-now-pay-later", productDisplayName: "PinoyBuyNowPayLater" }),
];

function renderPage() {
  const router = createMemoryRouter(
    [
      { path: "/personal/businesses", element: <PersonalBusinessesPage /> },
      { path: "/personal/explore-pos", element: <div data-testid="explore-pos">plans</div> },
      { path: "/org/subscription", element: <div data-testid="manage-subscription">manage</div> },
    ],
    { initialEntries: ["/personal/businesses"] },
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

describe("PersonalBusinessesPage", () => {
  beforeEach(() => {
    listPersonalProductAffiliations.mockReset();
    setOrganizationContext.mockReset();
    enterBusiness.mockReset();
    setOrganizationContext.mockResolvedValue({ ok: true });
  });

  it("shows every catalog product and starts only the ready product", async () => {
    listPersonalProductAffiliations.mockResolvedValue(catalog);
    renderPage();

    expect(await screen.findByTestId("product-portfolio-pinoy-business-pos")).toBeInTheDocument();
    expect(screen.getByTestId("product-portfolio-pinoy-loan-manager")).toBeInTheDocument();
    expect(screen.getByTestId("product-portfolio-pinoy-service-pro")).toBeInTheDocument();
    expect(screen.getByTestId("product-portfolio-pinoy-pawn-manager")).toBeInTheDocument();
    expect(screen.getByTestId("product-portfolio-pinoy-buy-now-pay-later")).toBeInTheDocument();
    expect(screen.getAllByText("Coming soon").length).toBe(4);
    expect(screen.getByTestId("portfolio-start-pinoy-business-pos")).toBeInTheDocument();
    expect(screen.queryByTestId("portfolio-start-pinoy-loan-manager")).not.toBeInTheDocument();

    await userEvent.click(screen.getByTestId("portfolio-start-pinoy-business-pos"));
    expect(await screen.findByTestId("explore-pos")).toBeInTheDocument();
  });

  it("opens an existing POS business instead of starting another", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        organizationId: posOrg,
        organizationDisplayName: "ABC Grocery",
        roleDisplay: "Owner",
        planDisplayName: "Pro",
        subscriptionStatus: "Active",
        canManageBilling: true,
      }),
      ...catalog.slice(1),
    ]);
    renderPage();

    expect(await screen.findByText("ABC Grocery")).toBeInTheDocument();
    expect(screen.getByText("Already using")).toBeInTheDocument();
    expect(screen.queryByTestId("portfolio-start-pinoy-business-pos")).not.toBeInTheDocument();
    await userEvent.click(screen.getByTestId("portfolio-open-pinoy-business-pos"));
    expect(enterBusiness).toHaveBeenCalledWith(posOrg);
    await userEvent.click(screen.getByTestId("portfolio-manage-pinoy-business-pos"));
    expect(setOrganizationContext).toHaveBeenCalledWith(posOrg);
    expect(await screen.findByTestId("manage-subscription")).toBeInTheDocument();
  });

  it("offers Choose Plan for an existing organization without a subscription", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        organizationId: posOrg,
        organizationDisplayName: "ABC Grocery",
        roleDisplay: "Owner",
        canManageBilling: true,
      }),
    ]);
    renderPage();

    expect(await screen.findByText("No active subscription")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Start a Business" })).not.toBeInTheDocument();
    await userEvent.click(screen.getByTestId("portfolio-choose-plan-pinoy-business-pos"));
    expect(await screen.findByTestId("explore-pos")).toBeInTheDocument();
  });

  it("hides subscription management from non-billing staff", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        organizationId: posOrg,
        organizationDisplayName: "ABC Grocery",
        roleDisplay: "Cashier",
        subscriptionStatus: "Active",
        canManageBilling: false,
      }),
    ]);
    renderPage();

    expect(await screen.findByText("Subscription managed by organization")).toBeInTheDocument();
    expect(screen.getByTestId("portfolio-open-pinoy-business-pos")).toBeInTheDocument();
    expect(screen.queryByTestId("portfolio-manage-pinoy-business-pos")).not.toBeInTheDocument();
  });

  it("lists each different product business once", async () => {
    listPersonalProductAffiliations.mockResolvedValue([
      row({
        organizationId: posOrg,
        organizationDisplayName: "ABC Grocery",
        roleDisplay: "Owner",
        subscriptionStatus: "Active",
        canManageBilling: true,
      }),
      row({
        productCode: "pinoy-loan-manager",
        productDisplayName: "PinoyLoanManager",
        organizationId: loanOrg,
        organizationDisplayName: "ABC Lending",
        roleDisplay: "Owner",
        subscriptionStatus: "Trialing",
        canManageBilling: true,
      }),
    ]);
    renderPage();

    expect(await screen.findByText("ABC Grocery")).toBeInTheDocument();
    expect(screen.getByText("ABC Lending")).toBeInTheDocument();
    expect(screen.getAllByTestId(/product-portfolio-/)).toHaveLength(2);
    expect(screen.queryByTestId("portfolio-open-pinoy-loan-manager")).not.toBeInTheDocument();
    expect(screen.getByText("Coming soon")).toBeInTheDocument();
  });
});
