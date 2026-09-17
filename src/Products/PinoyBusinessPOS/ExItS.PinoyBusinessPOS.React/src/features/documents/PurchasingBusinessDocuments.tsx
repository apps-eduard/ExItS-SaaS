import type {
  PosGoodsReceiptDto,
  PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import {
  BusinessDocument,
  DocumentFooter,
  DocumentHeader,
  DocumentLineTable,
  DocumentMetadata,
  DocumentNotes,
  DocumentPartyBlock,
  DocumentSignatures,
  DocumentTotals,
  type BusinessIdentity,
  type DocumentHeaderVisibility,
} from "@/components/exits/business-document";
import type { OrganizationDocumentSettings } from "@/features/documents/document-settings";
import { formatMoney } from "@/features/documents/use-business-document-identity";

export function PurchaseOrderBusinessDocument({
  po,
  supplierName,
  settings,
  identity,
  headerVisibility,
  preparedByLabel,
  deliveryAddress,
  preview = true,
}: {
  po: PosPurchaseOrderDto;
  supplierName: string;
  settings: OrganizationDocumentSettings;
  identity: BusinessIdentity;
  headerVisibility: DocumentHeaderVisibility;
  preparedByLabel?: string | null;
  deliveryAddress?: string | null;
  preview?: boolean;
}) {
  const cfg = settings.purchaseOrder;
  const columns = [
    { key: "product", header: "Product" },
    { key: "qty", header: "Qty", align: "right" as const },
    { key: "unit", header: "Unit" },
    { key: "unitCost", header: "Unit Cost", align: "right" as const },
    { key: "lineTotal", header: "Line Total", align: "right" as const },
  ];
  const rows = po.lines.map((line) => ({
    product: line.nameSnapshot?.trim() || "—",
    qty: String(line.orderedQty),
    unit: line.uomSnapshot?.trim() || "—",
    unitCost: formatMoney(line.unitPurchaseCost),
    lineTotal: formatMoney(line.lineTotal),
  }));
  const subtotal = po.lines.reduce((sum, line) => sum + line.lineTotal, 0);
  const contactParts = [
    settings.footer.showBusinessContact ? identity.phone?.trim() : null,
    settings.footer.showBusinessContact ? identity.email?.trim() : null,
  ].filter(Boolean);

  return (
    <BusinessDocument preview={preview} testId="po-business-document">
      <DocumentHeader
        identity={identity}
        visibility={headerVisibility}
        title={cfg.title}
        referenceNumber={po.poNumber}
        dateLabel={po.orderDate}
        statusLabel={po.displayStatus || po.status}
      />
      <div className="exits-bizdoc__parties">
        <DocumentPartyBlock
          label="Supplier"
          name={supplierName}
          address={cfg.showSupplierAddress ? null : null}
          phone={cfg.showSupplierContact ? null : null}
          email={cfg.showSupplierContact ? null : null}
        />
        {cfg.showDeliveryAddress ? (
          <DocumentPartyBlock
            label="Ship To"
            name={identity.branchName}
            address={deliveryAddress ?? identity.branchAddress}
          />
        ) : null}
        <DocumentMetadata
          items={[
            {
              key: "expectedDelivery",
              label: "Expected delivery",
              value: cfg.showExpectedDelivery ? po.expectedDeliveryDate : null,
            },
            {
              key: "preparedBy",
              label: "Prepared by",
              value: cfg.showPreparedBy ? preparedByLabel : null,
            },
            {
              key: "approval",
              label: "Approval",
              value:
                cfg.showApprovalInformation && po.supplierAcceptedAtUtc
                  ? `Accepted ${new Date(po.supplierAcceptedAtUtc).toLocaleString()}`
                  : null,
            },
            {
              key: "payment",
              label: "Payment term",
              value: po.paymentTermLabel || po.paymentTerm || null,
            },
          ]}
        />
      </div>
      <DocumentLineTable columns={columns} rows={rows} />
      <DocumentTotals
        rows={[
          { key: "subtotal", label: "Subtotal", value: formatMoney(subtotal) },
          {
            key: "total",
            label: "Total",
            value: formatMoney(subtotal),
            emphasize: true,
          },
        ]}
      />
      {cfg.showNotes ? <DocumentNotes title="Notes" text={po.notes} /> : null}
      <DocumentSignatures
        slots={[
          { key: "prepared", label: "Prepared by", name: preparedByLabel },
          { key: "approved", label: "Approved by" },
        ]}
      />
      <DocumentFooter
        showCustomFooter={settings.footer.showCustomFooter}
        customText={settings.footer.customFooterText}
        showBusinessContact={settings.footer.showBusinessContact}
        contactLine={contactParts.join(" · ") || null}
        showPageNumber={settings.footer.showPageNumber}
        disclaimer={null}
      />
    </BusinessDocument>
  );
}

export function GoodsReceiptBusinessDocument({
  receipt,
  po,
  poNumber,
  supplierName,
  settings,
  identity,
  headerVisibility,
  receivedByLabel,
  preview = true,
}: {
  receipt: PosGoodsReceiptDto;
  po?: PosPurchaseOrderDto | null;
  poNumber?: string | null;
  supplierName?: string | null;
  settings: OrganizationDocumentSettings;
  identity: BusinessIdentity;
  headerVisibility: DocumentHeaderVisibility;
  receivedByLabel?: string | null;
  preview?: boolean;
}) {
  const cfg = settings.goodsReceipt;
  const columns = [
    { key: "product", header: "Product" },
    { key: "ordered", header: "Ordered", align: "right" as const },
    { key: "previous", header: "Previously Received", align: "right" as const },
    { key: "received", header: "Received Now", align: "right" as const },
    ...(cfg.showDamagedQuantity
      ? [{ key: "damaged", header: "Damaged", align: "right" as const }]
      : []),
  ];
  const rows = receipt.lines.map((line) => {
    const poLine = po?.lines.find((l) => l.lineId === line.purchaseOrderLineId);
    const receivedNow = line.quantityReceived ?? line.receivedQty ?? 0;
    const ordered = poLine?.orderedQty;
    const previously =
      ordered != null ? Math.max(0, (poLine?.receivedQty ?? 0) - receivedNow) : null;
    return {
      product: line.nameSnapshot?.trim() || "—",
      ordered: ordered != null ? String(ordered) : "—",
      previous: previously != null ? String(previously) : "—",
      received: String(receivedNow),
      damaged: String(line.damagedQty ?? 0),
    };
  });
  const contactParts = [
    settings.footer.showBusinessContact ? identity.phone?.trim() : null,
    settings.footer.showBusinessContact ? identity.email?.trim() : null,
  ].filter(Boolean);

  return (
    <BusinessDocument preview={preview} testId="grn-business-document">
      <DocumentHeader
        identity={identity}
        visibility={headerVisibility}
        title={cfg.title}
        referenceNumber={receipt.grnNumber}
        dateLabel={
          cfg.showReceivedDate && receipt.receivedAtUtc
            ? new Date(receipt.receivedAtUtc).toLocaleString()
            : receipt.receivedDate
        }
        statusLabel={receipt.status}
      />
      <div className="exits-bizdoc__parties">
        {cfg.showSupplier ? (
          <DocumentPartyBlock label="Supplier" name={supplierName} />
        ) : null}
        <DocumentMetadata
          items={[
            {
              key: "po",
              label: "Related PO",
              value: cfg.showPoReference ? (poNumber ?? po?.poNumber) : null,
            },
            {
              key: "receivedBy",
              label: "Received by",
              value: cfg.showReceivedBy ? receivedByLabel : null,
            },
          ]}
        />
      </div>
      <DocumentLineTable columns={columns} rows={rows} />
      {cfg.showNotes ? <DocumentNotes title="Notes" text={receipt.notes} /> : null}
      <DocumentSignatures
        slots={[{ key: "received", label: "Received by", name: receivedByLabel }]}
      />
      <DocumentFooter
        showCustomFooter={settings.footer.showCustomFooter}
        customText={settings.footer.customFooterText}
        showBusinessContact={settings.footer.showBusinessContact}
        contactLine={contactParts.join(" · ") || null}
        showPageNumber={settings.footer.showPageNumber}
        disclaimer={null}
      />
    </BusinessDocument>
  );
}
