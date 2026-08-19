import { describe, expect, it } from "vitest";
import {
  mapOrganizationCommercialSummary,
  mapOrganizationDetail,
} from "@/api/organizations/organization-client";

describe("mapOrganizationDetail", () => {
  it("maps identity, profile, and branding without inventing fields", () => {
    const mapped = mapOrganizationDetail({
      id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      displayName: "Northwind Market",
      slug: "northwind-market",
      status: "Active",
      createdAtUtc: "2026-01-15T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
      profile: { legalName: "Northwind LLC", contactEmail: "ops@example.test" },
      branding: { brandDisplayName: "Northwind", primaryColor: "#1847d4" },
    });
    expect(mapped).toEqual({
      id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      displayName: "Northwind Market",
      slug: "northwind-market",
      status: "Active",
      createdAtUtc: "2026-01-15T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
      profile: {
        legalName: "Northwind LLC",
        contactEmail: "ops@example.test",
        contactPhone: undefined,
        addressLine1: undefined,
        addressLine2: undefined,
        city: undefined,
        region: undefined,
        postalCode: undefined,
        countryCode: undefined,
        timeZoneId: undefined,
        locale: undefined,
        currencyCode: undefined,
      },
      branding: {
        brandDisplayName: "Northwind",
        primaryColor: "#1847d4",
        accentColor: undefined,
      },
    });
  });
});

describe("mapOrganizationCommercialSummary", () => {
  it("maps confirmed record fields and ignores amounts as totals", () => {
    const mapped = mapOrganizationCommercialSummary({
      subscriptions: [
        {
          id: "11111111-1111-1111-1111-111111111111",
          productCode: "POS",
          status: "Active",
          agreedPrice: 999,
        },
      ],
      payments: [
        {
          id: "22222222-2222-2222-2222-222222222222",
          productCode: "POS",
          status: "Confirmed",
          amount: 1200,
          paidAtUtc: "2026-08-01T08:00:00Z",
        },
      ],
      latestEntitlements: [
        {
          id: "33333333-3333-3333-3333-333333333333",
          productCode: "POS",
          subscriptionStatus: "Active",
          generatedAtUtc: "2026-08-01T08:00:00Z",
        },
      ],
    });
    expect(mapped.subscriptions).toEqual([
      {
        id: "11111111-1111-1111-1111-111111111111",
        productCode: "POS",
        status: "Active",
      },
    ]);
    expect(mapped.payments[0]).toEqual({
      id: "22222222-2222-2222-2222-222222222222",
      productCode: "POS",
      status: "Confirmed",
      paidAtUtc: "2026-08-01T08:00:00Z",
    });
    expect(mapped.latestEntitlements[0]?.productCode).toBe("POS");
    expect(JSON.stringify(mapped)).not.toMatch(/agreedPrice|amount|999|1200/i);
  });
});
