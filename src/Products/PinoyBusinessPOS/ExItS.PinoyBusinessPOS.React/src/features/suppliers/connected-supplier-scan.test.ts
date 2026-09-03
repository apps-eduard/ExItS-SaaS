import { describe, expect, it } from "vitest";
import { parseConnectedSupplierScanPayload } from "@/features/suppliers/connected-supplier-scan";

describe("parseConnectedSupplierScanPayload", () => {
  it("B2B-03 parses branch storefront QR into org + branch", () => {
    const parsed = parseConnectedSupplierScanPayload(
      "https://pos.example/store/ORG123456/b/cccccccc-cccc-cccc-cccc-cccccccccccc",
    );
    expect(parsed.publicOrganizationId).toBe("ORG123456");
    expect(parsed.supplierBranchId).toBe("cccccccc-cccc-cccc-cccc-cccccccccccc");
    expect(parsed.source).toBe("branch-storefront");
  });

  it("parses org storefront and envelope without branch", () => {
    expect(
      parseConnectedSupplierScanPayload("https://pos.example/store/ORG123456").supplierBranchId,
    ).toBeNull();
    expect(
      parseConnectedSupplierScanPayload("exits://qr/v1/organization/ORG123456").publicOrganizationId,
    ).toBe("ORG123456");
    expect(parseConnectedSupplierScanPayload("ORG123456").source).toBe("public-id");
  });
});
