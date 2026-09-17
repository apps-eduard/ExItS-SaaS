import { describe, expect, it } from "vitest";
import { resolvePoUtangEligibility } from "@/features/purchasing/po-payment-methods";

describe("resolvePoUtangEligibility", () => {
  it("allows Utang when connected, approved, and within available credit", () => {
    expect(
      resolvePoUtangEligibility({
        connected: true,
        creditStatus: "Approved",
        availableCredit: 1000,
        orderTotal: 250,
        canManagePurchasing: true,
      }),
    ).toEqual({ eligible: true });
  });

  it("blocks Utang when credit is not approved", () => {
    expect(
      resolvePoUtangEligibility({
        connected: true,
        creditStatus: "PendingApproval",
        availableCredit: 1000,
        orderTotal: 100,
        canManagePurchasing: true,
      }),
    ).toEqual({ eligible: false, reasonKey: "purchasing.utang.creditNotApproved" });
  });

  it("blocks Utang when order exceeds available credit", () => {
    expect(
      resolvePoUtangEligibility({
        connected: true,
        creditStatus: "Approved",
        availableCredit: 50,
        orderTotal: 100,
        canManagePurchasing: true,
      }),
    ).toEqual({ eligible: false, reasonKey: "purchasing.utang.insufficientCredit" });
  });
});
