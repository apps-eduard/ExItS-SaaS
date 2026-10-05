import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createMemoryRouter, RouterProvider } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { BillingPaymentReturnPage } from "@/features/subscription-checkout/BillingPaymentReturnPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const paymentId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const syncPersonalSubscriptionHostedCheckout = vi.fn();
const cancelPersonalSubscriptionHostedCheckout = vi.fn();

vi.mock("@/api/platform/subscription-payment-client", () => ({
  syncPersonalSubscriptionHostedCheckout: (...args: unknown[]) =>
    syncPersonalSubscriptionHostedCheckout(...args),
  cancelPersonalSubscriptionHostedCheckout: (...args: unknown[]) =>
    cancelPersonalSubscriptionHostedCheckout(...args),
  syncOrganizationSubscriptionHostedCheckout: vi.fn(),
  cancelOrganizationSubscriptionHostedCheckout: vi.fn(),
}));

function payment(status: string) {
  return {
    id: paymentId,
    referenceNumber: "PAY-20261004-000001",
    organizationId: null,
    subscriptionId: status === "Paid" ? "dddddddd-dddd-dddd-dddd-dddddddddddd" : null,
    planKey: "pro",
    billingCycle: "Monthly",
    baseAmount: 1499,
    discountAmount: 0,
    discountPercent: 0,
    finalAmount: 1499,
    currencyCode: "PHP",
    channel: null,
    provider: "PayMongo",
    environment: "Test",
    status,
    providerReference: "cs_test_1",
    cardBrand: null,
    cardLast4: null,
    failureCode: status === "Failed" ? "payment_failed" : null,
    failureReason: null,
    createdAtUtc: "2026-10-04T08:00:00.000Z",
    processingAtUtc: "2026-10-04T08:00:10.000Z",
    paidAtUtc: status === "Paid" ? "2026-10-04T08:01:00.000Z" : null,
    failedAtUtc: status === "Failed" ? "2026-10-04T08:01:00.000Z" : null,
    cancelledAtUtc: status === "Cancelled" ? "2026-10-04T08:01:00.000Z" : null,
    expiredAtUtc: null,
    periodStartUtc: null,
    periodEndUtc: null,
    subscriptionActivated: status === "Paid",
    activities: [],
  };
}

function renderReturn(outcome: "success" | "cancelled", maxAttempts = 8) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const router = createMemoryRouter(
    [
      {
        path: "/billing/payment/:outcome",
        element: <BillingPaymentReturnPage outcome={outcome} maxAttempts={maxAttempts} />,
      },
    ],
    { initialEntries: [`/billing/payment/${outcome}?paymentId=${paymentId}`] },
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

describe("BillingPaymentReturnPage", () => {
  beforeEach(() => {
    syncPersonalSubscriptionHostedCheckout.mockReset();
    cancelPersonalSubscriptionHostedCheckout.mockReset();
  });

  it("shows verifying before a paid confirmation", async () => {
    syncPersonalSubscriptionHostedCheckout.mockResolvedValue(payment("Processing"));
    renderReturn("success", 2);
    expect(await screen.findByText("Verifying your payment...")).toBeInTheDocument();
    expect(screen.getByTestId("billing-payment-verifying")).toBeInTheDocument();
    expect(screen.queryByTestId("billing-payment-paid")).not.toBeInTheDocument();
  });

  it("shows paid only after the server reports Paid", async () => {
    syncPersonalSubscriptionHostedCheckout.mockResolvedValue(payment("Paid"));
    renderReturn("success");
    expect(await screen.findByTestId("billing-payment-paid")).toHaveTextContent(
      "Your subscription is active.",
    );
    const continueLink = screen.getByTestId("billing-payment-continue");
    expect(continueLink).toHaveTextContent("Continue");
    expect(continueLink).toHaveAttribute("href", expect.stringContaining("/personal/start-business"));
    expect(continueLink.getAttribute("href")).toContain(`paymentId=${paymentId}`);
    expect(screen.queryByTestId("billing-payment-try-again")).not.toBeInTheDocument();
  });

  it("shows a failed payment separately from cancellation", async () => {
    syncPersonalSubscriptionHostedCheckout.mockResolvedValue(payment("Failed"));
    renderReturn("success");
    expect(await screen.findByTestId("billing-payment-failed")).toBeInTheDocument();
    expect(screen.queryByTestId("billing-payment-cancelled")).not.toBeInTheDocument();
  });

  it("explains a cancelled checkout without treating it as a system error", async () => {
    cancelPersonalSubscriptionHostedCheckout.mockResolvedValue(payment("Cancelled"));
    renderReturn("cancelled");
    expect(await screen.findByTestId("billing-payment-cancelled")).toHaveTextContent(
      "No subscription change was made",
    );
    expect(screen.getByTestId("billing-payment-try-again")).toBeInTheDocument();
    expect(screen.getByTestId("billing-payment-back")).toBeInTheDocument();
    expect(screen.queryByTestId("billing-payment-missing")).not.toBeInTheDocument();
  });

  it("stops on a provider error without showing the provider message", async () => {
    syncPersonalSubscriptionHostedCheckout.mockRejectedValue(
      new Error("PayMongo request failed with provider payload"),
    );
    renderReturn("success", 8);
    expect(await screen.findByTestId("billing-payment-delayed")).toHaveTextContent(
      "still being confirmed",
    );
    expect(screen.queryByText(/PayMongo request failed/i)).not.toBeInTheDocument();
    expect(screen.queryByTestId("billing-payment-paid")).not.toBeInTheDocument();
  });

  it("stops polling with a calm delay message", async () => {
    syncPersonalSubscriptionHostedCheckout.mockResolvedValue(payment("Processing"));
    renderReturn("success", 1);
    expect(await screen.findByTestId("billing-payment-delayed")).toHaveTextContent(
      "still being confirmed",
    );
  });
});
