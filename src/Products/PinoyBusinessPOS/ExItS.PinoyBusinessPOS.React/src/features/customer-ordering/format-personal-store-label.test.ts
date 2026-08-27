import { describe, expect, it } from "vitest";
import {
  personalCustomerRelationshipLabel,
  personalStoreDisplayName,
  personalStoreSearchText,
  stripPersonalRunStamp,
} from "@/features/customer-ordering/format-personal-store-label";

describe("stripPersonalRunStamp", () => {
  it("removes a trailing compact datetime from Local Validation names", () => {
    expect(stripPersonalRunStamp("Kizy Store 20260826225642")).toBe("Kizy Store");
    expect(stripPersonalRunStamp("Mica Linked 20260826230121")).toBe("Mica Linked");
    expect(stripPersonalRunStamp("Mica Org 20260826225701571")).toBe("Mica Org");
  });

  it("leaves ordinary names unchanged", () => {
    expect(stripPersonalRunStamp("Kizy Store")).toBe("Kizy Store");
    expect(stripPersonalRunStamp("Store 2024")).toBe("Store 2024");
    expect(stripPersonalRunStamp("Room 101")).toBe("Room 101");
  });
});

describe("personalStoreDisplayName", () => {
  it("returns the human store name", () => {
    expect(personalStoreDisplayName("Kizy Store 20260826225642")).toBe("Kizy Store");
  });

  it("returns empty for blank input", () => {
    expect(personalStoreDisplayName("   ")).toBe("");
    expect(personalStoreDisplayName(null)).toBe("");
  });
});

describe("personalCustomerRelationshipLabel", () => {
  it("hides generated Local Validation customer labels", () => {
    expect(personalCustomerRelationshipLabel("Mica Linked 20260826230121")).toBeNull();
    expect(personalCustomerRelationshipLabel("Mica Linked 30121")).toBeNull();
    expect(personalCustomerRelationshipLabel("Mica Linked")).toBeNull();
  });

  it("keeps a real customer name the merchant uses", () => {
    expect(personalCustomerRelationshipLabel("Ana Reyes")).toBe("Ana Reyes");
    expect(personalCustomerRelationshipLabel("Ben Buyer")).toBe("Ben Buyer");
  });

  it("hides the label when it is the Personal viewer's own name", () => {
    expect(personalCustomerRelationshipLabel("Kizy", "Kizy")).toBeNull();
    expect(personalCustomerRelationshipLabel("kizy", "Kizy")).toBeNull();
    expect(personalCustomerRelationshipLabel("Ana Reyes", "Kizy")).toBe("Ana Reyes");
  });
});

describe("personalStoreSearchText", () => {
  it("matches both the stamped source and the cleaned store name", () => {
    const hay = personalStoreSearchText("Kizy Store 20260826225642", "Mica Linked 20260826230121");
    expect(hay).toContain("kizy store");
    expect(hay).toContain("20260826225642");
    expect(hay).toContain("mica linked");
  });
});
