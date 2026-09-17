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
import type { DocumentAudience } from "@/features/documents/document-audience";
import { isCustomerAudience } from "@/features/documents/document-audience";
import type { CustomerPurchaseSummaryView } from "@/features/documents/customer-purchase-summary-view";
import {
  customerPurchaseSummaryFromPosSale,
} from "@/features/documents/customer-purchase-summary-view";
import {
  SALES_BIR_SAFE_DISCLAIMER,
  SALES_EXTENDED_RECORD_DISCLAIMER,
  type OrganizationDocumentSettings,
} from "@/features/documents/document-settings";
import { formatMoney } from "@/features/documents/use-business-document-identity";
import type { PosSaleDto } from "@/api/pos/pos-sales-client";

function isUtangPayment(paymentMethod: string): boolean {
  return paymentMethod.trim().toLowerCase() === "utang";
}

/**
 * Canonical Customer Purchase Summary — ExItS BusinessDocument adapter for sales.
 * Preview / Print / PDF / connected-customer View must all render this component.
 */
export function CustomerPurchaseSummaryDocument({
  view,
  sale,
  settings,
  identity,
  headerVisibility,
  cashierLabel,
  paymentLabel,
  audience = "Seller",
  preview = true,
  showExtendedDisclaimer = true,
}: {
  /** Preferred normalized view (seller sale or customer receipt adapters). */
  view?: CustomerPurchaseSummaryView;
  /** @deprecated Prefer `view` — still accepted for seller call sites. */
  sale?: PosSaleDto;
  settings: OrganizationDocumentSettings;
  identity: BusinessIdentity;
  headerVisibility: DocumentHeaderVisibility;
  cashierLabel?: string | null;
  paymentLabel?: string;
  audience?: DocumentAudience;
  preview?: boolean;
  /** Longer record-purpose line under the BIR-safe disclaimer. */
  showExtendedDisclaimer?: boolean;
}) {
  const resolved: CustomerPurchaseSummaryView | null =
    view ??
    (sale
      ? customerPurchaseSummaryFromPosSale(
          sale,
          paymentLabel ?? sale.paymentMethod,
        )
      : null);
  if (!resolved) {
    return null;
  }

  const customer = isCustomerAudience(audience);
  const cfg = settings.sales;
  const footer = settings.footer;
  // Customer cannot independently hide seller legal disclaimer.
  const showDisclaimer = customer ? true : cfg.showSalesDisclaimer ?? footer.showDocumentDisclaimer;
  const showCashier = !customer && cfg.showCashier;
  const showSkuColumn = cfg.showSku;

  const anyLineDiscount = resolved.lines.some((line) => (line.lineDiscountAmount ?? 0) > 0);
  const showDiscountColumn = cfg.showDiscount && anyLineDiscount;
  const showDiscountTotal = cfg.showDiscount && resolved.discountTotal > 0;
  const utang =
    isUtangPayment(resolved.paymentMethod) ||
    (resolved.utangBalance != null && resolved.utangBalance > 0);

  const columns = [
    { key: "description", header: "Description" },
    ...(showSkuColumn ? [{ key: "sku", header: "SKU" }] : []),
    { key: "qty", header: "Qty", align: "right" as const },
    { key: "unitPrice", header: "Unit Price", align: "right" as const },
    ...(showDiscountColumn
      ? [{ key: "discount", header: "Discount", align: "right" as const }]
      : []),
    { key: "lineTotal", header: "Line Total", align: "right" as const },
  ];

  const rows = resolved.lines.map((line) => {
    const discount = line.lineDiscountAmount ?? 0;
    return {
      description: line.name,
      sku: line.sku?.trim() || "—",
      qty: `${line.quantity} ${line.unitOfMeasure}`,
      unitPrice: formatMoney(line.unitPrice),
      discount: discount > 0 ? formatMoney(discount) : "—",
      lineTotal: formatMoney(line.lineTotal),
    };
  });

  const contactParts = [
    footer.showBusinessContact ? identity.phone?.trim() : null,
    footer.showBusinessContact ? identity.email?.trim() : null,
  ].filter(Boolean);

  const resolvedPaymentLabel = paymentLabel ?? resolved.paymentLabel;

  return (
    <BusinessDocument preview={preview} testId="customer-purchase-summary-document">
      <DocumentHeader
        identity={identity}
        visibility={headerVisibility}
        title={cfg.title || "Customer Purchase Summary"}
        referenceNumber={resolved.saleNumber}
        dateLabel={new Date(resolved.recordedAtUtc).toLocaleString()}
        statusLabel={resolved.status}
      />
      <div className="exits-bizdoc__parties">
        {cfg.showCustomerName || cfg.showCustomerAddress || cfg.showCustomerContact ? (
          <DocumentPartyBlock
            label="Customer"
            name={cfg.showCustomerName ? resolved.customerDisplayName : null}
            address={cfg.showCustomerAddress ? null : null}
            phone={cfg.showCustomerContact ? null : null}
            email={cfg.showCustomerContact ? null : null}
          />
        ) : null}
        <DocumentMetadata
          items={[
            {
              key: "cashier",
              label: "Cashier",
              value: showCashier ? cashierLabel : null,
            },
            {
              key: "payment",
              label: "Payment method",
              value: cfg.showPaymentMethod ? resolvedPaymentLabel : null,
            },
            {
              key: "branch",
              label: "Branch",
              value: headerVisibility.showBranchName ? identity.branchName : null,
            },
            ...(resolved.gCashReference?.trim()
              ? [
                  {
                    key: "gcash",
                    label: "GCash reference",
                    value: resolved.gCashReference.trim(),
                  },
                ]
              : []),
          ]}
        />
      </div>
      <DocumentLineTable columns={columns} rows={rows} />
      <DocumentTotals
        rows={[
          { key: "subtotal", label: "Subtotal", value: formatMoney(resolved.subtotal) },
          ...(showDiscountTotal
            ? [
                {
                  key: "discount",
                  label: "Discount",
                  value: formatMoney(resolved.discountTotal),
                },
              ]
            : []),
          ...(resolved.taxAmount > 0
            ? [{ key: "tax", label: "Tax", value: formatMoney(resolved.taxAmount) }]
            : []),
          {
            key: "total",
            label: "Total",
            value: formatMoney(resolved.total),
            emphasize: true,
          },
          ...(!utang && resolved.amountTendered != null
            ? [
                {
                  key: "paid",
                  label: "Cash received",
                  value: formatMoney(resolved.amountTendered),
                },
              ]
            : []),
          ...(!utang && resolved.changeAmount != null
            ? [
                {
                  key: "change",
                  label: "Change",
                  value: formatMoney(resolved.changeAmount),
                },
              ]
            : []),
          ...(utang
            ? [
                {
                  key: "utang",
                  label: "Utang / balance",
                  value: formatMoney(resolved.utangBalance ?? resolved.total),
                },
              ]
            : []),
        ]}
      />
      {cfg.showNotes ? <DocumentNotes text={null} /> : null}
      <DocumentFooter
        showCustomFooter={footer.showCustomFooter}
        customText={footer.customFooterText}
        showBusinessContact={footer.showBusinessContact}
        contactLine={contactParts.join(" · ") || null}
        showPageNumber={footer.showPageNumber}
        disclaimer={
          showDisclaimer
            ? settings.sales.salesDisclaimerTitle?.trim() || SALES_BIR_SAFE_DISCLAIMER
            : null
        }
        extendedDisclaimer={
          showDisclaimer && showExtendedDisclaimer
            ? settings.sales.salesDisclaimerBody?.trim() || SALES_EXTENDED_RECORD_DISCLAIMER
            : null
        }
      />
    </BusinessDocument>
  );
}

/** @deprecated Prefer CustomerPurchaseSummaryDocument — same canonical renderer. */
export const SaleBusinessDocument = CustomerPurchaseSummaryDocument;
