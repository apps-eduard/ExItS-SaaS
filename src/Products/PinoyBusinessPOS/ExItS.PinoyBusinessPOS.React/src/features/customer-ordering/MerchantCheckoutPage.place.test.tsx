import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PosApiError } from "@/api/pos/pos-http";
import { MerchantCheckoutPage } from "@/features/customer-ordering/MerchantCheckoutPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const orgId = "05462d10-3e2e-4994-ab91-d8193df25af1";
const branchId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const productId = "11111111-1111-4111-8111-111111111111";
const customerId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const userId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

const ensurePersonalBuyerPosToken = vi.fn();
const getCustomerStorefront = vi.fn();
const placeCustomerOrder = vi.fn();
const getLinkedCustomerStatement = vi.fn();
const clearAll = vi.fn();
const navigate = vi.fn();

vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual<typeof import("react-router-dom")>("react-router-dom");
  return {
    ...actual,
    useNavigate: () => navigate,
  };
});

vi.mock("@/api/platform/personal-buyer-token", () => ({
  ensurePersonalBuyerPosToken: () => ensurePersonalBuyerPosToken(),
}));

vi.mock("@/api/pos/pos-customer-orders-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/pos/pos-customer-orders-client")>(
    "@/api/pos/pos-customer-orders-client",
  );
  return {
    ...actual,
    getCustomerStorefront: (...args: unknown[]) => getCustomerStorefront(...args),
    placeCustomerOrder: (...args: unknown[]) => placeCustomerOrder(...args),
    quoteCustomerDelivery: vi.fn(),
  };
});

vi.mock("@/api/pos/pos-linked-customers-client", () => ({
  getLinkedCustomerStatement: (...args: unknown[]) => getLinkedCustomerStatement(...args),
}));

vi.mock("@/features/customer-ordering/useLinkedMerchantShopContext", () => ({
  useLinkedMerchantShopContext: () => ({
    isLoading: false,
    isError: false,
    data: {
      organizationDisplayName: "Mica store",
      customerDisplayName: "Ana",
      businessCustomerId: customerId,
      statementTo: null,
    },
  }),
}));

vi.mock("@/features/customer-ordering/PersonalMerchantCartProvider", () => ({
  usePersonalMerchantCart: () => ({
    cart: {
      sellerOrganizationId: orgId,
      organizationDisplayName: "Mica store",
      lines: [
        {
          productId,
          name: "Rice",
          sku: "R1",
          unitOfMeasure: "pc",
          unitPrice: 50,
          quantity: 1,
        },
      ],
    },
    merchandiseSubtotal: 50,
    clearAll,
    clearLines: vi.fn(),
    setQuantity: vi.fn(),
  }),
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({ session: { userId, displayName: "Ana", email: "ana@example.com" } }),
}));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/features/customer-ordering/use-storefront-product-image", () => ({
  useStorefrontProductImageUrl: () => null,
}));

function storefront(partial: Record<string, unknown> = {}) {
  return {
    organizationId: orgId,
    organizationDisplayName: "Mica store",
    canCustomerOrder: true,
    canCustomerDelivery: false,
    categories: [],
    products: [],
    productTotalCount: 0,
    page: 1,
    pageSize: 1,
    fulfillmentAvailability: "ready",
    branches: [
      {
        branchId,
        name: "Main",
        pickupEnabled: true,
        deliveryEnabled: false,
        customerOrderingOperational: true,
        pickupOperational: true,
        deliveryOperational: false,
        onlineOrdersPaused: false,
        storeStatusMessage: null,
        deliveryServiceAreas: [],
      },
    ],
    ...partial,
  };
}

function renderCheckout() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const router = createMemoryRouter(
    [
      { path: "/personal/linked-merchants/:organizationId/shop/checkout", element: <MerchantCheckoutPage /> },
      { path: "/personal/orders/:orderId", element: <div>order</div> },
    ],
    { initialEntries: [`/personal/linked-merchants/${orgId}/shop/checkout`] },
  );
  render(
    <QueryClientProvider client={queryClient}>
      <PreferencesProvider>
        <I18nProvider>
          <RouterProvider router={router} />
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("MerchantCheckoutPage place order", () => {
  beforeEach(() => {
    ensurePersonalBuyerPosToken.mockReset();
    getCustomerStorefront.mockReset();
    placeCustomerOrder.mockReset();
    getLinkedCustomerStatement.mockReset();
    clearAll.mockReset();
    navigate.mockReset();
    ensurePersonalBuyerPosToken.mockResolvedValue({ ok: true });
    getCustomerStorefront.mockResolvedValue(storefront());
    getLinkedCustomerStatement.mockResolvedValue({
      creditStatus: "Approved",
      availableCredit: 1000,
      creditLimit: 1000,
      outstandingBalance: 0,
      pendingOnlineUtangCommitment: 0,
    });
    placeCustomerOrder.mockResolvedValue({ orderId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd" });
  });

  it("shows an error and retry when the personal POS token fails", async () => {
    ensurePersonalBuyerPosToken.mockResolvedValue({ ok: false, detail: "Personal access token was empty." });
    renderCheckout();
    expect(await screen.findByTestId("merchant-checkout-token-error")).toBeInTheDocument();
    expect(screen.getByText("Personal access token was empty.")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-token-retry")).toBeInTheDocument();
    expect(placeCustomerOrder).not.toHaveBeenCalled();
  });

  it("does not post when pickup cannot be placed", async () => {
    getCustomerStorefront.mockResolvedValue(
      storefront({
        fulfillmentAvailability: "pickup-unavailable",
        branches: [
          {
            branchId,
            name: "Main",
            pickupEnabled: true,
            deliveryEnabled: false,
            customerOrderingOperational: true,
            pickupOperational: false,
            deliveryOperational: false,
            onlineOrdersPaused: false,
            storeStatusMessage: null,
            deliveryServiceAreas: [],
          },
        ],
      }),
    );
    renderCheckout();
    const button = await screen.findByTestId("place-order");
    expect(button).toBeDisabled();
    expect(screen.getByTestId("checkout-place-unavailable")).toHaveTextContent(
      "Pickup is currently unavailable at this store.",
    );
    await userEvent.click(button);
    expect(placeCustomerOrder).not.toHaveBeenCalled();
  });

  it("posts cash pickup once and opens the buyer order", async () => {
    renderCheckout();
    const button = await screen.findByTestId("place-order");
    expect(button).toBeEnabled();
    await userEvent.click(button);
    await waitFor(() => expect(placeCustomerOrder).toHaveBeenCalledTimes(1));
    expect(clearAll).toHaveBeenCalledTimes(1);
    expect(navigate).toHaveBeenCalledWith("/personal/orders/dddddddd-dddd-4ddd-8ddd-dddddddddddd");
    const request = placeCustomerOrder.mock.calls[0]?.[2] as { paymentMethod: string; requestedPickupTime: string | null };
    expect(request.paymentMethod).toBe("Cash");
    expect(request.requestedPickupTime).toBeNull();
  });

  it("keeps the cart when the post fails and shows a stock conflict", async () => {
    placeCustomerOrder.mockRejectedValue(
      new PosApiError(409, { errorCode: "pos.inventory.insufficient_stock", detail: "low" }),
    );
    renderCheckout();
    await userEvent.click(await screen.findByTestId("place-order"));
    expect(await screen.findByText("Some items are no longer available. Refresh the storefront and update your cart.")).toBeInTheDocument();
    expect(clearAll).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });

  it("ignores a second click while the first post is in flight", async () => {
    let release: (value: { orderId: string }) => void = () => undefined;
    placeCustomerOrder.mockImplementation(
      () =>
        new Promise((resolve) => {
          release = resolve;
        }),
    );
    renderCheckout();
    const button = await screen.findByTestId("place-order");
    await userEvent.click(button);
    await userEvent.click(button);
    await waitFor(() => expect(placeCustomerOrder).toHaveBeenCalledTimes(1));
    release({ orderId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd" });
    await waitFor(() => expect(navigate).toHaveBeenCalledTimes(1));
  });
});
