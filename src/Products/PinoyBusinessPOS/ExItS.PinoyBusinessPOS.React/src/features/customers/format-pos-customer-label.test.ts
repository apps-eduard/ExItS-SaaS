import { describe, expect, it } from "vitest";
import {
  checkoutCustomerTitle,
  isSeededWalkInCustomerName,
  posCustomerDisplayName,
  shouldShowCheckoutCustomerWhenIdle,
  visibleCheckoutCustomers,
} from "@/features/customers/format-pos-customer-label";

describe("isSeededWalkInCustomerName", () => {
  it("detects Local Validation walk-in seeds", () => {
    expect(isSeededWalkInCustomerName("Local Walkin 20260826230002")).toBe(true);
    expect(isSeededWalkInCustomerName("Local Walkin")).toBe(true);
  });

  it("leaves ordinary, merchant-named, and linked-seed names", () => {
    expect(isSeededWalkInCustomerName("Juan Dela Cruz")).toBe(false);
    expect(isSeededWalkInCustomerName("Walk-in Ana")).toBe(false);
    expect(isSeededWalkInCustomerName("Mica Linked 20260826230121")).toBe(false);
  });
});

describe("posCustomerDisplayName", () => {
  it("strips run stamps and walk-in prefixes", () => {
    expect(posCustomerDisplayName("Local Walkin 20260826230002", "Walk-in")).toBe("Walk-in");
    expect(posCustomerDisplayName("Mica Linked 20260826230121", "Walk-in")).toBe("Mica");
    expect(posCustomerDisplayName("Juan Dela Cruz", "Walk-in")).toBe("Juan Dela Cruz");
  });

  it("does not treat a year in an ordinary name as a run stamp", () => {
    expect(posCustomerDisplayName("Store 2024", "Walk-in")).toBe("Store 2024");
  });
});

describe("checkoutCustomerTitle", () => {
  it("prefers the resolved Personal name so cashiers can check who they scanned", () => {
    expect(
      checkoutCustomerTitle(
        {
          displayName: "Local Walkin 20260826230002",
          resolvedPersonalDisplayName: "Rosa Santos",
        },
        "Walk-in",
      ),
    ).toBe("Rosa Santos");
  });
});

describe("visibleCheckoutCustomers", () => {
  const walkIn = {
    customerId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    displayName: "Local Walkin 20260826230002",
    status: "Active",
  };
  const named = {
    customerId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
    displayName: "Juan Dela Cruz",
    status: "Active",
  };
  const linked = {
    customerId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    displayName: "Mica Linked 20260826230121",
    status: "Active",
    linkedPersonalPublicUserId: "EX-4827-1936",
  };

  it("hides seeded walk-ins from the idle list", () => {
    expect(shouldShowCheckoutCustomerWhenIdle(walkIn)).toBe(false);
    expect(visibleCheckoutCustomers([walkIn, named, linked], "")).toEqual([linked, named]);
  });

  it("shows walk-ins when the cashier searches", () => {
    expect(visibleCheckoutCustomers([walkIn, named], "0917")).toEqual([named, walkIn]);
  });
});
