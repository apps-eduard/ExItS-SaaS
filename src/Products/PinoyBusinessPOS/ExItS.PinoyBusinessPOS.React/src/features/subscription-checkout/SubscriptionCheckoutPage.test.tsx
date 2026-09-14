import { render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { SubscriptionCheckoutPage } from "@/features/subscription-checkout/SubscriptionCheckoutPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const paymentId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const getPersonalSubscriptionPayment = vi.fn();
const retryPersonalSubscriptionPayment = vi.fn();
const selectPersonalSubscriptionPaymentChannel = vi.fn();

vi.mock("@/api/platform/subscription-payment-client", () => ({
  getPersonalSubscriptionPayment: (...args: unknown[]) => getPersonalSubscriptionPayment(...args),
  retryPersonalSubscriptionPayment: (...args: unknown[]) =>
    retryPersonalSubscriptionPayment(...args),
  selectPersonalSubscriptionPaymentChannel: (...args: unknown[]) =>
    selectPersonalSubscriptionPaymentChannel(...args),
}));

function pendingPayment(
  status: "Pending" | "Processing" | "Failed" | "Cancelled" | "Expired" | "Paid",
  channel: string | null = null,
) {
  return {
    id: paymentId,
    referenceNumber: "PAY-20260913-000001",
    organizationId: null,
    subscriptionId: status === "Paid" ? "dddddddd-dddd-dddd-dddd-dddddddddddd" : null,
    planKey: "pro",
    billingCycle: "Monthly",
    baseAmount: 1499,
    discountAmount: 0,
    discountPercent: 0,
    finalAmount: 1499,
    currencyCode: "PHP",
    channel,
    provider: "Simulator",
    environment: "Test",
    status,
    providerReference: status === "Paid" ? "SIM-GC-260913-ABC123" : null,
    cardBrand: null,
    cardLast4: null,
    failureCode: status === "Failed" ? "card_declined" : null,
    failureReason: status === "Failed" ? "Simulated card decline" : null,
    createdAtUtc: "2026-09-13T21:14:00.000Z",
    processingAtUtc:
      status === "Processing" || status === "Paid" || status === "Failed"
        ? "2026-09-13T21:14:10.000Z"
        : null,
    paidAtUtc: status === "Paid" ? "2026-09-13T21:15:00.000Z" : null,
    failedAtUtc: status === "Failed" ? "2026-09-13T21:15:00.000Z" : null,
    cancelledAtUtc: status === "Cancelled" ? "2026-09-13T21:15:00.000Z" : null,
    expiredAtUtc: status === "Expired" ? "2026-09-13T21:15:00.000Z" : null,
    periodStartUtc: status === "Paid" ? "2026-09-13T21:15:00.000Z" : null,
    periodEndUtc: status === "Paid" ? "2026-12-13T21:15:00.000Z" : null,
    subscriptionActivated: false,
    activities: [{ eventType: "PaymentCreated", message: "Payment created", occurredAtUtc: "2026-09-13T21:14:00.000Z" }],
  };
}

function renderCheckout() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const router = createMemoryRouter(
    [
      {
        path: "/subscription-checkout/:paymentId",
        element: <SubscriptionCheckoutPage />,
      },
      { path: "/onboarding", element: <div data-testid="onboarding-hijack">onboarding</div> },
      {
        path: "/personal/start-business",
        element: <div data-testid="start-business-next">start-business</div>,
      },
      { path: "/personal/explore-pos", element: <div data-testid="explore-pos">explore</div> },
    ],
    { initialEntries: [`/subscription-checkout/${paymentId}`] },
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

  return router;
}

describe("SubscriptionCheckoutPage pre-org state UX", () => {
  beforeEach(() => {
    getPersonalSubscriptionPayment.mockReset();
    retryPersonalSubscriptionPayment.mockReset();
    selectPersonalSubscriptionPaymentChannel.mockReset();
  });

  it("keeps loading without navigating before payment resolves", async () => {
    getPersonalSubscriptionPayment.mockImplementation(
      () =>
        new Promise((resolve) => {
          window.setTimeout(() => resolve(pendingPayment("Pending")), 40);
        }),
    );

    const router = renderCheckout();
    expect(screen.getByTestId("subscription-checkout-loading")).toBeInTheDocument();
    expect(screen.queryByTestId("onboarding-hijack")).not.toBeInTheDocument();
    expect(router.state.location.pathname).toBe(`/subscription-checkout/${paymentId}`);

    await waitFor(() => expect(screen.getByTestId("subscription-checkout-page")).toBeInTheDocument());
    expect(screen.getByTestId("checkout-method-gcash")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-method-maya")).toBeInTheDocument();
    expect(screen.getByTestId("checkout-method-card")).toBeInTheDocument();
    expect(screen.getByTestId("subscription-payment-details")).toBeInTheDocument();
  });

  it("shows method picker for Pending with no channel", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Pending"));
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-method-picker")).toBeInTheDocument());
    expect(screen.getByTestId("checkout-method-gcash")).toBeInTheDocument();
  });

  it("stays on checkout for Processing", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Processing", "GCash"));
    const router = renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-refresh-status")).toBeInTheDocument());
    expect(router.state.location.pathname).toBe(`/subscription-checkout/${paymentId}`);
  });

  it("shows retry for Failed without mutating history", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Failed", "Card"));
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-try-again")).toBeInTheDocument());
    expect(screen.getByTestId("subscription-payment-details")).toBeInTheDocument();
  });

  it("shows receipt and Continue for Paid", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Paid", "GCash"));
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-receipt")).toBeInTheDocument());
    expect(screen.getByTestId("subscription-continue-onboarding")).toBeInTheDocument();
  });

  it("Cancelled and Expired offer new attempt / back to plans", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Cancelled"));
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-try-again")).toBeInTheDocument());
    expect(screen.getByTestId("subscription-back-to-plans")).toBeInTheDocument();
  });
});
