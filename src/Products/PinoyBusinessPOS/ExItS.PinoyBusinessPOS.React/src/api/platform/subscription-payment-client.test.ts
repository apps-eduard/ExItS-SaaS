import { afterEach, describe, expect, it, vi } from "vitest";
import { jsonResponse } from "@/test/session-context";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import {
  describeCardSimulatorPan,
  getSubscriptionPayment,
  processSubscriptionPaymentSimulator,
  SUBSCRIPTION_CARD_SIMULATOR,
} from "@/api/platform/subscription-payment-client";

const orgId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const paymentId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const paymentWire = {
  Id: paymentId,
  ReferenceNumber: "PAY-20260329-000001",
  OrganizationId: orgId,
  SubscriptionId: null,
  PlanKey: "growth",
  BillingCycle: "Quarterly",
  BaseAmount: 2097,
  DiscountAmount: 104.85,
  DiscountPercent: 5,
  FinalAmount: 1992.15,
  CurrencyCode: "PHP",
  Channel: null,
  Provider: "Simulator",
  Environment: "Test",
  Status: "Pending",
  ProviderReference: null,
  CardBrand: null,
  CardLast4: null,
  FailureCode: null,
  FailureReason: null,
  CreatedAtUtc: "2026-03-29T10:00:00Z",
  ProcessingAtUtc: null,
  PaidAtUtc: null,
  FailedAtUtc: null,
  CancelledAtUtc: null,
  ExpiredAtUtc: null,
  PeriodStartUtc: null,
  PeriodEndUtc: null,
  SubscriptionActivated: false,
  Activities: [
    {
      EventType: "Created",
      Message: "Pending payment created",
      OccurredAtUtc: "2026-03-29T10:00:00Z",
    },
  ],
};

describe("subscription-payment-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("gets and normalizes a subscription payment DTO", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        expect(url).toContain(
          `/api/v1/platform/organizations/${orgId}/subscription-payments/${paymentId}`,
        );
        return jsonResponse(200, paymentWire);
      }),
    );

    const payment = await getSubscriptionPayment(orgId, paymentId);
    expect(payment.id).toBe(paymentId);
    expect(payment.finalAmount).toBe(1992.15);
    expect(payment.discountPercent).toBe(5);
    expect(payment.activities).toHaveLength(1);
    expect(payment.environment).toBe("Test");
  });

  it("posts process simulator without retaining card fields in the result", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("/api/v1/platform/antiforgery/token")) {
        return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf" });
      }
      expect(url).toContain(
        `/api/v1/platform/organizations/${orgId}/subscription-payments/${paymentId}/process`,
      );
      expect(init?.method).toBe("POST");
      const body = JSON.parse(String(init?.body));
      expect(body.channel).toBe("Card");
      expect(body.cardNumber).toBe(SUBSCRIPTION_CARD_SIMULATOR.successPan);
      expect(body.cardCvv).toBe("123");
      return jsonResponse(200, {
        ...paymentWire,
        Status: "Paid",
        Channel: "Card",
        CardBrand: "Visa",
        CardLast4: "4242",
        PaidAtUtc: "2026-03-29T10:01:00Z",
        SubscriptionActivated: true,
      });
    });
    vi.stubGlobal("fetch", fetchMock);

    const result = await processSubscriptionPaymentSimulator(orgId, paymentId, {
      channel: "Card",
      cardNumber: SUBSCRIPTION_CARD_SIMULATOR.successPan,
      cardExpiry: "12/30",
      cardName: "Test",
      cardCvv: "123",
    });

    expect(result.status).toBe("Paid");
    expect(result.cardLast4).toBe("4242");
    expect(JSON.stringify(result)).not.toContain("123");
    expect(JSON.stringify(result)).not.toContain(SUBSCRIPTION_CARD_SIMULATOR.successPan);
  });

  it("describes card simulator PAN rules for UI copy", () => {
    expect(describeCardSimulatorPan(SUBSCRIPTION_CARD_SIMULATOR.successPan)).toBe("success");
    expect(describeCardSimulatorPan(SUBSCRIPTION_CARD_SIMULATOR.declinePan)).toBe("decline");
    expect(describeCardSimulatorPan(SUBSCRIPTION_CARD_SIMULATOR.pendingPan)).toBe("processing");
    expect(describeCardSimulatorPan("4111111111111111")).toBe("other");
  });
});
