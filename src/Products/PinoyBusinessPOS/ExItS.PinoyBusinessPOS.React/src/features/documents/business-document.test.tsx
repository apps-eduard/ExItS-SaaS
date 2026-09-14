import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import {
  BusinessDocument,
  DocumentFooter,
  DocumentHeader,
  DocumentLineTable,
  DocumentTotals,
} from "@/components/exits/business-document";
import {
  DEFAULT_DOCUMENT_SETTINGS,
  SALES_BIR_SAFE_DISCLAIMER,
  readOrganizationDocumentSettings,
  writeOrganizationDocumentSettings,
} from "@/features/documents/document-settings";

function withSettings(
  orgId: string,
  mutate: (s: typeof DEFAULT_DOCUMENT_SETTINGS) => typeof DEFAULT_DOCUMENT_SETTINGS,
) {
  const next = mutate(structuredClone(DEFAULT_DOCUMENT_SETTINGS));
  writeOrganizationDocumentSettings(orgId, next);
  return readOrganizationDocumentSettings(orgId);
}

describe("business document shell", () => {
  it("renders reusable header, table, totals, and footer without domain coupling", () => {
    render(
      <BusinessDocument preview testId="demo-doc">
        <DocumentHeader
          identity={{
            businessName: "Paul Coffee",
            address: "Iloilo City",
            phone: "09xx",
            email: "hello@example.com",
          }}
          visibility={{
            showLogo: false,
            showBusinessName: true,
            showBusinessAddress: true,
            showBusinessPhone: true,
            showBusinessEmail: true,
            showWebsite: false,
            showBranchName: false,
            showBranchAddress: false,
          }}
          title="Purchase Order"
          referenceNumber="PO-2026-00124"
          dateLabel="Sep 14, 2026"
          statusLabel="Approved"
        />
        <DocumentLineTable
          columns={[
            { key: "product", header: "Product" },
            { key: "qty", header: "Qty", align: "right" },
            { key: "total", header: "Line Total", align: "right" },
          ]}
          rows={[{ product: "Beans", qty: "2", total: "₱100.00" }]}
        />
        <DocumentTotals
          rows={[{ key: "total", label: "Total", value: "₱100.00", emphasize: true }]}
        />
        <DocumentFooter
          showCustomFooter
          customText="Thank you for your business."
          showBusinessContact={false}
          showPageNumber
          disclaimer={null}
        />
      </BusinessDocument>,
    );

    expect(screen.getByTestId("demo-doc")).toHaveAttribute("data-business-document-root");
    expect(screen.getByTestId("business-document-business-name")).toHaveTextContent("Paul Coffee");
    expect(screen.getByTestId("business-document-title")).toHaveTextContent("Purchase Order");
    expect(screen.getByTestId("business-document-line-table")).toBeInTheDocument();
    expect(screen.getByTestId("business-document-total-total")).toBeInTheDocument();
    expect(screen.queryByTestId("business-document-disclaimer")).not.toBeInTheDocument();
  });

  it("shows BIR-safe disclaimer for sales footer only when enabled", () => {
    render(
      <DocumentFooter
        showCustomFooter
        customText="Thanks"
        showBusinessContact={false}
        showPageNumber={false}
        disclaimer={SALES_BIR_SAFE_DISCLAIMER}
      />,
    );
    expect(screen.getByTestId("business-document-disclaimer")).toHaveTextContent(
      "NOT A BIR INVOICE",
    );
  });

  it("persists header address visibility without mutating defaults clone incorrectly", () => {
    const orgId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    const hidden = withSettings(orgId, (s) => {
      s.header.showBusinessAddress = false;
      return s;
    });
    expect(hidden.header.showBusinessAddress).toBe(false);
    expect(hidden.header.showBusinessName).toBe(true);
    expect(DEFAULT_DOCUMENT_SETTINGS.header.showBusinessAddress).toBe(true);
  });
});
