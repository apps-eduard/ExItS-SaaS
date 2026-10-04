import { describe, expect, it } from "vitest";
import { mapComplianceProfile } from "@/api/platform/organization-compliance-client";

describe("organization compliance profile mapping", () => {
  it("keeps MaskedTin and drops any persisted TIN field", () => {
    const mapped = mapComplianceProfile(
      {
        registeredTaxpayerName: "Mica Store",
        maskedTin: "***-***-789",
        tin: "123-456-789-000",
        Tin: "123-456-789-000",
        setupStatus: "Configured",
      },
      "org-1",
    );

    expect(mapped.maskedTin).toBe("***-***-789");
    expect(mapped.registeredTaxpayerName).toBe("Mica Store");
    expect(JSON.stringify(mapped)).not.toContain("123-456-789-000");
    expect(mapped).not.toHaveProperty("tin");
  });
});
