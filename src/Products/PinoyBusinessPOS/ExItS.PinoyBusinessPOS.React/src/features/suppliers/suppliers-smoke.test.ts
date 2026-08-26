import { describe, expect, it } from "vitest";
import { resolveSupplierSearchParams } from "@/api/pos/pos-suppliers-client";

describe("suppliers list/form smoke helpers", () => {
  it("routes SUP codes to supplierCode and plain terms to name", () => {
    expect(resolveSupplierSearchParams("SUP0012")).toEqual({ supplierCode: "SUP0012" });
    expect(resolveSupplierSearchParams("Fresh Farms")).toEqual({ name: "Fresh Farms" });
  });
});
