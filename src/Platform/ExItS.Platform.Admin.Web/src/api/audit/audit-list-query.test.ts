import { describe, expect, it } from "vitest";
import {
  hasActivePlatformAuditFilters,
  parseAuditRecordId,
  parsePlatformAuditSearchParams,
  platformAuditDetailPath,
  platformAuditListPath,
  platformAuditSearchParams,
} from "@/api/audit/audit-list-query";

describe("platform audit list query", () => {
  it("parses filters and ignores invalid organization ids", () => {
    const state = parsePlatformAuditSearchParams(
      new URLSearchParams({
        fromUtc: "2026-01-01T00:00:00Z",
        toUtc: "2026-01-31T23:59:59Z",
        actor: "dev-admin",
        action: "platform.subscription.activated",
        organizationId: "not-a-guid",
        productCode: "POS",
        outcome: "Succeeded",
        page: "2",
      }),
    );

    expect(state).toEqual({
      fromUtc: "2026-01-01T00:00:00Z",
      toUtc: "2026-01-31T23:59:59Z",
      actor: "dev-admin",
      action: "platform.subscription.activated",
      organizationId: "",
      productCode: "POS",
      outcome: "Succeeded",
      page: 2,
    });
    expect(hasActivePlatformAuditFilters(state)).toBe(true);
  });

  it("round-trips search params without empty values", () => {
    const params = platformAuditSearchParams({
      fromUtc: "2026-01-01T00:00:00Z",
      toUtc: "",
      actor: "olivia",
      action: "",
      organizationId: "11111111-1111-1111-1111-111111111111",
      productCode: "POS",
      outcome: "Denied",
      page: 1,
    });

    expect(params.get("fromUtc")).toBe("2026-01-01T00:00:00Z");
    expect(params.get("toUtc")).toBeNull();
    expect(params.get("actor")).toBe("olivia");
    expect(params.get("organizationId")).toBe("11111111-1111-1111-1111-111111111111");
    expect(params.get("productCode")).toBe("POS");
    expect(params.get("outcome")).toBe("Denied");
    expect(params.get("page")).toBeNull();
  });

  it("builds list and detail API paths", () => {
    expect(
      platformAuditListPath({
        fromUtc: "",
        toUtc: "",
        actor: "dev-admin",
        action: "",
        organizationId: "",
        productCode: "",
        outcome: "Failed",
        page: 3,
      }),
    ).toBe("/api/v1/platform/audit?actor=dev-admin&outcome=Failed&page=3&pageSize=20");

    expect(platformAuditDetailPath("22222222-2222-2222-2222-222222222222")).toBe(
      "/api/v1/platform/audit/22222222-2222-2222-2222-222222222222",
    );
    expect(parseAuditRecordId("not-guid")).toBeNull();
    expect(parseAuditRecordId("22222222-2222-2222-2222-222222222222")).toBe(
      "22222222-2222-2222-2222-222222222222",
    );
  });
});
