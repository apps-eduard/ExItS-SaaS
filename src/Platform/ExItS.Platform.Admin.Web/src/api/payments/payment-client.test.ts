import { describe, expect, it } from "vitest";
import {
  canQueryPaymentPortfolio,
  DEFAULT_PAYMENT_PORTFOLIO_STATUS,
  hasActivePaymentPortfolioFilters,
  parsePaymentPortfolioSearchParams,
  paymentPortfolioSearchParams,
} from "@/api/payments/payment-client";

describe("payment portfolio query params", () => {
  it("defaults to Confirmed when no status or product filter is present", () => {
    const state = parsePaymentPortfolioSearchParams(new URLSearchParams());
    expect(state.status).toBe(DEFAULT_PAYMENT_PORTFOLIO_STATUS);
    expect(state.productCode).toBe("");
    expect(canQueryPaymentPortfolio(state)).toBe(true);
    expect(hasActivePaymentPortfolioFilters(state)).toBe(false);
  });

  it("allows empty status when productCode is set", () => {
    const state = parsePaymentPortfolioSearchParams(
      new URLSearchParams("productCode=PinoyBusinessPOS"),
    );
    expect(state.status).toBe("");
    expect(state.productCode).toBe("PinoyBusinessPOS");
    expect(canQueryPaymentPortfolio(state)).toBe(true);
  });

  it("keeps an explicit status filter", () => {
    const state = parsePaymentPortfolioSearchParams(
      new URLSearchParams("status=PendingConfirmation"),
    );
    expect(state.status).toBe("PendingConfirmation");
    expect(hasActivePaymentPortfolioFilters(state)).toBe(true);
  });

  it("serializes Confirmed default into query string when writing filters", () => {
    const params = paymentPortfolioSearchParams({
      page: 1,
      pageSize: 20,
      status: DEFAULT_PAYMENT_PORTFOLIO_STATUS,
      productCode: "",
      method: "",
    });
    expect(params.get("status")).toBe("Confirmed");
    expect(params.has("productCode")).toBe(false);
  });

  it("rejects querying without status or productCode", () => {
    expect(
      canQueryPaymentPortfolio({
        page: 1,
        pageSize: 20,
        status: "",
        productCode: "",
        method: "Cash",
      }),
    ).toBe(false);
  });
});
