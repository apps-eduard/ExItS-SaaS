import { describe, expect, it } from "vitest";
import {
  DEFAULT_DOCUMENT_SETTINGS,
  formatOrganizationAddress,
  organizationDocumentSettingsStorageKey,
  writeOrganizationDocumentSettings,
  readOrganizationDocumentSettings,
} from "@/features/documents/document-settings";

describe("document-settings", () => {
  it("keeps business name mandatory and formats addresses", () => {
    const orgId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    writeOrganizationDocumentSettings(orgId, {
      ...DEFAULT_DOCUMENT_SETTINGS,
      header: {
        ...DEFAULT_DOCUMENT_SETTINGS.header,
        showBusinessName: false,
        showBusinessAddress: false,
      },
    });
    const loaded = readOrganizationDocumentSettings(orgId);
    expect(loaded.header.showBusinessName).toBe(true);
    expect(loaded.header.showBusinessAddress).toBe(false);
    expect(
      formatOrganizationAddress({
        addressLine1: "Main St",
        city: "Iloilo City",
        region: "Iloilo",
      }),
    ).toContain("Iloilo City");
  });

  it("scopes settings by organizationId and does not leak across orgs", () => {
    const orgA = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    const orgB = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    writeOrganizationDocumentSettings(orgA, {
      ...DEFAULT_DOCUMENT_SETTINGS,
      sales: {
        ...DEFAULT_DOCUMENT_SETTINGS.sales,
        salesDisclaimerTitle: "ORG-A-ONLY",
        showSalesDisclaimer: false,
      },
    });
    writeOrganizationDocumentSettings(orgB, {
      ...DEFAULT_DOCUMENT_SETTINGS,
      sales: {
        ...DEFAULT_DOCUMENT_SETTINGS.sales,
        salesDisclaimerTitle: "ORG-B-ONLY",
      },
    });
    expect(readOrganizationDocumentSettings(orgA).sales.salesDisclaimerTitle).toBe("ORG-A-ONLY");
    expect(readOrganizationDocumentSettings(orgA).sales.showSalesDisclaimer).toBe(false);
    expect(readOrganizationDocumentSettings(orgB).sales.salesDisclaimerTitle).toBe("ORG-B-ONLY");
    expect(organizationDocumentSettingsStorageKey(orgA)).toContain(orgA);
  });
});
