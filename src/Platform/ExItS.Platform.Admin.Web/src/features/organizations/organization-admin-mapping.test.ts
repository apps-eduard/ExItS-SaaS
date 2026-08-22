import { describe, expect, it } from "vitest";
import {
  buildCreateInvitationBody,
  buildUpdateOrganizationBody,
  buildUpdateOrganizationBrandingBody,
  organizationBrandingFormValues,
  organizationProfileFormValues,
} from "@/features/organizations/organization-admin-mapping";

const organization = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
  updatedAtUtc: "2026-08-22T10:00:00Z",
  profile: { legalName: "Northwind LLC", contactEmail: "ops@northwind.test" },
  branding: { brandDisplayName: "Northwind", primaryColor: "#112233" },
};

describe("organization admin mapping", () => {
  it("maps profile form values into update organization body", () => {
    const values = organizationProfileFormValues(organization);
    values.legalName = "Northwind Holdings";
    values.contactEmail = "";

    const body = buildUpdateOrganizationBody(values, organization, { includeSlug: true });

    expect(body).toEqual({
      displayName: "Northwind Market",
      slug: "northwind-market",
      legalName: "Northwind Holdings",
      contactEmail: null,
      contactPhone: null,
      addressLine1: null,
      addressLine2: null,
      city: null,
      region: null,
      postalCode: null,
      countryCode: null,
      timeZoneId: null,
      locale: null,
      currencyCode: null,
      expectedUpdatedAtUtc: "2026-08-22T10:00:00Z",
    });
  });

  it("maps branding form values including logoUrl", () => {
    const values = organizationBrandingFormValues(organization);
    values.logoUrl = "https://cdn.example.test/logo.png";

    expect(buildUpdateOrganizationBrandingBody(values, organization)).toEqual({
      brandDisplayName: "Northwind",
      logoUrl: "https://cdn.example.test/logo.png",
      primaryColor: "#112233",
      accentColor: null,
      expectedUpdatedAtUtc: "2026-08-22T10:00:00Z",
    });
  });

  it("omits product role from invitation body", () => {
    expect(
      buildCreateInvitationBody({
        email: "staff@example.test",
        role: "OrganizationMember",
        firstName: "Ana",
        lastName: "Cruz",
        displayName: "",
        phone: "",
        employeeCode: "",
        branch: "Main",
      }),
    ).toEqual({
      email: "staff@example.test",
      role: "OrganizationMember",
      firstName: "Ana",
      lastName: "Cruz",
      displayName: null,
      phone: null,
      employeeCode: null,
      branch: "Main",
      requireEmailVerification: true,
    });
  });
});
