import { describe, expect, it } from "vitest";
import {
  checkoutOptionKey,
  isCheckoutBusiness,
  mapCheckoutSearchItemToOption,
} from "@/features/checkout/checkout-customer-option";

describe("checkout-customer-option", () => {
  it("maps Customer and Business search rows", () => {
    const person = mapCheckoutSearchItemToOption({
      kind: "Customer",
      customerId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      displayName: "Juan",
      status: "Active",
      mobileNumber: "0917",
      creditStatus: "Approved",
      availableCredit: 12500,
      creditLimit: 50000,
      outstandingAmount: 37500,
      defaultTermDays: 90,
    });
    expect(person?.kind).toBe("Customer");
    expect(person && checkoutOptionKey(person)).toBe("c:aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    expect(person && person.kind === "Customer" && person.creditStatus).toBe("Approved");
    expect(person && person.kind === "Customer" && person.availableCredit).toBe(12500);

    const business = mapCheckoutSearchItemToOption({
      kind: "Business",
      connectionId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      buyerOrganizationId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      buyerPublicOrganizationId: "ORG123456",
      displayName: "ABC Trading",
      status: "Active",
      creditStatus: "Approved",
      availableCredit: 50000,
      creditLimit: 50000,
      defaultTermDays: 30,
    });
    expect(isCheckoutBusiness(business)).toBe(true);
    expect(business && checkoutOptionKey(business)).toBe("b:bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    expect(business && business.kind === "Business" && business.creditStatus).toBe("Approved");
    expect(business && business.kind === "Business" && business.availableCredit).toBe(50000);
  });

  it("drops incomplete Business rows", () => {
    expect(
      mapCheckoutSearchItemToOption({
        kind: "Business",
        displayName: "ABC",
        status: "Active",
        connectionId: null,
        buyerOrganizationId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
      }),
    ).toBeNull();
  });
});
