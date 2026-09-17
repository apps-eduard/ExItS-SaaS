import {
  BusinessDocument,
  DocumentFooter,
  DocumentHeader,
  DocumentLineTable,
  DocumentMetadata,
  DocumentNotes,
  DocumentPartyBlock,
  DocumentTotals,
  type BusinessIdentity,
  type DocumentHeaderVisibility,
} from "@/components/exits/business-document";
import type { PosQuotation } from "@/api/pos/pos-quotations-client";
import type { OrganizationDocumentSettings } from "@/features/documents/document-settings";
import { formatMoney } from "@/features/documents/use-business-document-identity";

/** Standard QUOTATION document — never uses sales BIR disclaimer. */
export function QuotationBusinessDocument({
  quotation,
  settings,
  identity,
  headerVisibility,
  preparedByLabel,
  branchName,
  preview = true,
}: {
  quotation: PosQuotation;
  settings: OrganizationDocumentSettings;
  identity: BusinessIdentity;
  headerVisibility: DocumentHeaderVisibility;
  preparedByLabel?: string | null;
  branchName?: string | null;
  preview?: boolean;
}) {
  const cfg = settings.quotation;
  const footer = settings.footer;
  const showDiscountColumn =
    cfg.showDiscount && quotation.lines.some((l) => (l.discountAmount ?? 0) > 0);

  const columns = [
    { key: "description", header: "Description" },
    ...(cfg.showSku ? [{ key: "sku", header: "SKU" }] : []),
    { key: "qty", header: "Qty", align: "right" as const },
    { key: "unitPrice", header: "Unit Price", align: "right" as const },
    ...(showDiscountColumn
      ? [{ key: "discount", header: "Discount", align: "right" as const }]
      : []),
    { key: "lineTotal", header: "Line Total", align: "right" as const },
  ];

  const rows = quotation.lines.map((line) => {
    const discount = line.discountAmount ?? 0;
    return {
      description: line.nameSnapshot?.trim() || "Product",
      sku: line.skuSnapshot?.trim() || "—",
      qty: `${line.quantity}${line.uomSnapshot ? ` ${line.uomSnapshot}` : ""}`,
      unitPrice: formatMoney(line.unitPrice),
      discount: discount > 0 ? formatMoney(discount) : "—",
      lineTotal: formatMoney(line.lineTotal),
    };
  });

  const contactParts = [
    footer.showBusinessContact ? identity.phone?.trim() : null,
    footer.showBusinessContact ? identity.email?.trim() : null,
  ].filter(Boolean);

  const issuedLabel = quotation.issuedAtUtc
    ? new Date(quotation.issuedAtUtc).toLocaleString()
    : new Date(quotation.createdAtUtc).toLocaleString();

  return (
    <BusinessDocument preview={preview} testId="quotation-business-document">
      <DocumentHeader
        identity={identity}
        visibility={headerVisibility}
        title={cfg.title || "QUOTATION"}
        referenceNumber={quotation.quotationNumber ?? "Draft"}
        dateLabel={issuedLabel}
        statusLabel={quotation.status}
      />
      <div className="exits-bizdoc__parties">
        {cfg.showCustomerName || cfg.showCustomerAddress || cfg.showCustomerContact ? (
          <DocumentPartyBlock
            label="Customer"
            name={cfg.showCustomerName ? quotation.customerDisplayName : null}
            address={cfg.showCustomerAddress ? quotation.customerAddress : null}
            phone={cfg.showCustomerContact ? quotation.customerMobileNumber : null}
            email={null}
          />
        ) : null}
        <DocumentMetadata
          items={[
            {
              key: "validUntil",
              label: "Valid until",
              value: cfg.showValidUntil ? quotation.validUntil : null,
            },
            {
              key: "preparedBy",
              label: "Prepared by",
              value: cfg.showPreparedBy ? preparedByLabel : null,
            },
            {
              key: "branch",
              label: "Branch",
              value: headerVisibility.showBranchName
                ? branchName ?? identity.branchName
                : null,
            },
            {
              key: "reference",
              label: "Reference",
              value: cfg.showReference ? quotation.reference : null,
            },
          ]}
        />
      </div>
      <DocumentLineTable columns={columns} rows={rows} />
      <DocumentTotals
        rows={[
          { key: "subtotal", label: "Subtotal", value: formatMoney(quotation.subtotal) },
          {
            key: "total",
            label: "Total",
            value: formatMoney(quotation.subtotal),
            emphasize: true,
          },
        ]}
      />
      {cfg.showNotes ? (
        <DocumentNotes text={[quotation.notes, quotation.terms].filter(Boolean).join("\n\n") || null} />
      ) : null}
      <DocumentFooter
        showCustomFooter={footer.showCustomFooter}
        customText={footer.customFooterText}
        showBusinessContact={footer.showBusinessContact}
        contactLine={contactParts.join(" · ") || null}
        showPageNumber={footer.showPageNumber}
        disclaimer={
          cfg.showQuotationStatement ? cfg.quotationStatement?.trim() || null : null
        }
        extendedDisclaimer={null}
      />
    </BusinessDocument>
  );
}
