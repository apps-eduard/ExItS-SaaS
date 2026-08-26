import { describe, expect, it } from "vitest";
import { PosApiError } from "@/api/pos/pos-http";
import { mapSupplierErrorKey } from "@/features/suppliers/supplier-errors";

describe("mapSupplierErrorKey", () => {
  it("maps supplier conflict and concurrency codes", () => {
    expect(
      mapSupplierErrorKey(
        new PosApiError(409, { errorCode: "pos.supplier.name.conflict", detail: "Name" }),
      ),
    ).toBe("suppliers.errorNameConflict");
    expect(
      mapSupplierErrorKey(
        new PosApiError(409, { errorCode: "pos.supplier.email.conflict", detail: "Email" }),
      ),
    ).toBe("suppliers.errorEmailConflict");
    expect(
      mapSupplierErrorKey(
        new PosApiError(409, { errorCode: "pos.supplier.mobile.conflict", detail: "Mobile" }),
      ),
    ).toBe("suppliers.errorMobileConflict");
    expect(
      mapSupplierErrorKey(
        new PosApiError(409, {
          errorCode: "pos.supplier.tax_number.conflict",
          detail: "Tax",
        }),
      ),
    ).toBe("suppliers.errorTaxConflict");
    expect(
      mapSupplierErrorKey(
        new PosApiError(409, {
          errorCode: "pos.supplier.concurrency_conflict",
          detail: "Stale",
        }),
      ),
    ).toBe("suppliers.errorConcurrency");
  });

  it("maps denied and not found", () => {
    expect(
      mapSupplierErrorKey(
        new PosApiError(403, {
          errorCode: "application.auth.capability.denied",
          detail: "Denied",
        }),
      ),
    ).toBe("suppliers.errorDenied");
    expect(
      mapSupplierErrorKey(
        new PosApiError(404, { errorCode: "pos.supplier.not_found", detail: "Missing" }),
      ),
    ).toBe("suppliers.errorNotFound");
  });
});
