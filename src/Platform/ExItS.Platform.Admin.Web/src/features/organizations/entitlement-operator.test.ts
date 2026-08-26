import { describe, expect, it } from "vitest";
import type { FeatureOverride } from "@/api/organizations/entitlement-list-query";
import { overrideEffectiveStatus } from "@/features/organizations/entitlement-operator-utils";

const baseOverride: FeatureOverride = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  productCode: "POS",
  featureCode: "store-export",
  enabled: false,
  status: "Active",
};

describe("overrideEffectiveStatus", () => {
  it("returns Revoked when backend status is Revoked", () => {
    expect(
      overrideEffectiveStatus({ ...baseOverride, status: "Revoked" }, Date.parse("2026-08-01T00:00:00Z")),
    ).toBe("Revoked");
  });

  it("returns Expired when active override is past expiry", () => {
    expect(
      overrideEffectiveStatus(
        { ...baseOverride, expiresAtUtc: "2026-07-01T00:00:00Z" },
        Date.parse("2026-08-01T00:00:00Z"),
      ),
    ).toBe("Expired");
  });

  it("returns Active for a current override", () => {
    expect(
      overrideEffectiveStatus(
        { ...baseOverride, expiresAtUtc: "2026-09-01T00:00:00Z" },
        Date.parse("2026-08-01T00:00:00Z"),
      ),
    ).toBe("Active");
  });
});
