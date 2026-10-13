import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PlatformApiError } from "@/api/platform/platform-http";
import { SubscriptionCheckoutPage } from "@/features/subscription-checkout/SubscriptionCheckoutPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const paymentId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const getPersonalSubscriptionPayment = vi.fn();
const syncPersonalSubscriptionHostedCheckout = vi.fn();
const retryPersonalSubscriptionPayment = vi.fn();
const startPersonalSubscriptionHostedCheckout = vi.fn();
const redirectToHostedCheckout = vi.fn();

vi.mock("@/api/platform/subscription-payment-client", () => ({
  getPersonalSubscriptionPayment: (...args: unknown[]) => getPersonalSubscriptionPayment(...args),
  syncPersonalSubscriptionHostedCheckout: (...args: unknown[]) =>
    syncPersonalSubscriptionHostedCheckout(...args),
  retryPersonalSubscriptionPayment: (...args: unknown[]) =>
    retryPersonalSubscriptionPayment(...args),
  startPersonalSubscriptionHostedCheckout: (...args: unknown[]) =>
    startPersonalSubscriptionHostedCheckout(...args),
}));

vi.mock("@/features/subscription-checkout/hosted-checkout-redirect", () => ({
  redirectToHostedCheckout: (...args: unknown[]) => redirectToHostedCheckout(...args),
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
    providerReference:
      status === "Paid" || status === "Processing" ? "cs_test_checkout" : null,
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
    syncPersonalSubscriptionHostedCheckout.mockReset();
    syncPersonalSubscriptionHostedCheckout.mockImplementation(async () =>
      getPersonalSubscriptionPayment(),
    );
    retryPersonalSubscriptionPayment.mockReset();
    startPersonalSubscriptionHostedCheckout.mockReset();
    redirectToHostedCheckout.mockReset();
    sessionStorage.clear();
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
    expect(screen.getByTestId("subscription-continue-secure")).toBeInTheDocument();
    expect(screen.getByTestId("subscription-payment-details")).toBeInTheDocument();
  });

  it("starts hosted checkout from the review and redirects once", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Pending"));
    startPersonalSubscriptionHostedCheckout.mockResolvedValue({
      paymentId,
      checkoutUrl: "https://checkout.paymongo.test/cs_test",
      status: "Processing",
      amount: 1499,
      currencyCode: "PHP",
      planKey: "pro",
      billingCycle: "Monthly",
      organizationId: null,
    });
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-checkout-review")).toBeInTheDocument());
    await userEvent.click(screen.getByTestId("subscription-continue-secure"));
    await waitFor(() =>
      expect(redirectToHostedCheckout).toHaveBeenCalledWith("https://checkout.paymongo.test/cs_test"),
    );
    expect(startPersonalSubscriptionHostedCheckout).toHaveBeenCalledTimes(1);
  });

  it("does not offer the local test payment page when the provider is not configured", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Pending"));
    startPersonalSubscriptionHostedCheckout.mockRejectedValue(
      new PlatformApiError(503, {
        detail: "Subscription payments are not configured.",
        errorCode: "application.payment.not_configured",
      }),
    );
    renderCheckout();
    await userEvent.click(await screen.findByTestId("subscription-continue-secure"));
    expect(await screen.findByText("Subscription payments are not configured.")).toBeInTheDocument();
    expect(screen.queryByTestId("subscription-simulator-methods")).not.toBeInTheDocument();
  });

  it("shows retry for Failed without mutating history", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Failed", "Card"));
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-try-again")).toBeInTheDocument());
    expect(screen.getByTestId("subscription-payment-details")).toBeInTheDocument();
  });

  it("reopens PayMongo when a checkout session is still processing", async () => {
    const processing = pendingPayment("Processing");
    getPersonalSubscriptionPayment.mockResolvedValue(processing);
    syncPersonalSubscriptionHostedCheckout.mockResolvedValue(processing);
    startPersonalSubscriptionHostedCheckout.mockResolvedValue({
      paymentId,
      checkoutUrl: "https://checkout.paymongo.test/cs_test",
      status: "Processing",
      amount: 1499,
      currencyCode: "PHP",
      planKey: "pro",
      billingCycle: "Monthly",
      organizationId: null,
    });
    renderCheckout();
    await waitFor(() =>
      expect(redirectToHostedCheckout).toHaveBeenCalledWith("https://checkout.paymongo.test/cs_test"),
    );
    expect(screen.getByTestId("subscription-continue-secure")).toBeInTheDocument();
    expect(startPersonalSubscriptionHostedCheckout).toHaveBeenCalledTimes(1);
  });

  it("asks PayMongo to confirm a processing checkout", async () => {
    getPersonalSubscriptionPayment.mockResolvedValue(pendingPayment("Processing"));
    syncPersonalSubscriptionHostedCheckout.mockResolvedValue(pendingPayment("Paid", "GCash"));
    renderCheckout();
    await waitFor(() => expect(screen.getByTestId("subscription-receipt")).toBeInTheDocument());
    expect(syncPersonalSubscriptionHostedCheckout).toHaveBeenCalledWith(paymentId, expect.any(AbortSignal));
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
