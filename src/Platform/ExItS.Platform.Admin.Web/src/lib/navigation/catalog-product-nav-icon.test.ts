import { describe, expect, it } from "vitest";
import { catalogProductNavIcon } from "@/lib/navigation/catalog-product-nav-icon";

describe("catalogProductNavIcon", () => {
  it("maps known products to distinct icons", () => {
    expect(catalogProductNavIcon("pinoy-business-pos")).toBe("store");
    expect(catalogProductNavIcon("pinoy-loan-manager")).toBe("landmark");
  });

  it("falls back for unknown products", () => {
    expect(catalogProductNavIcon("future-product-x")).toBe("box");
  });
});
