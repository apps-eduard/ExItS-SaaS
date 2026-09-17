/**
 * Organization document & printing settings (Standard Documents).
 * Presentation visibility only — never mutates transaction/canonical data.
 * Persisted client-side per organizationId (must not leak across orgs).
 */

export type DocumentComplianceMode = "Standard" | "BIR";

export type DocumentTypeKey =
  | "sales"
  | "purchaseOrder"
  | "goodsReceipt"
  | "salesSummary"
  | "quotation";

export type DocumentHeaderSettings = {
  showLogo: boolean;
  /** Business name is required on documents; UI should keep this on. */
  showBusinessName: boolean;
  showBusinessAddress: boolean;
  showBusinessPhone: boolean;
  showBusinessEmail: boolean;
  showWebsite: boolean;
  showBranchName: boolean;
  showBranchAddress: boolean;
};

export type DocumentFooterSettings = {
  showCustomFooter: boolean;
  customFooterText: string;
  showBusinessContact: boolean;
  showPageNumber: boolean;
  /**
   * @deprecated Prefer sales.showSalesDisclaimer. Kept for migration of older localStorage.
   */
  showDocumentDisclaimer: boolean;
};

export type SalesDocumentConfig = {
  title: string;
  showCustomerName: boolean;
  showCustomerAddress: boolean;
  showCustomerContact: boolean;
  showCashier: boolean;
  showPaymentMethod: boolean;
  showSku: boolean;
  showDiscount: boolean;
  showNotes: boolean;
  /** Standard sales / Customer Purchase Summary disclaimer ON/OFF. */
  showSalesDisclaimer: boolean;
  salesDisclaimerTitle: string;
  salesDisclaimerBody: string;
};

export type QuotationDocumentConfig = {
  title: string;
  showCustomerName: boolean;
  showCustomerAddress: boolean;
  showCustomerContact: boolean;
  showPreparedBy: boolean;
  showValidUntil: boolean;
  showReference: boolean;
  showNotes: boolean;
  showSku: boolean;
  showDiscount: boolean;
  showQuotationStatement: boolean;
  quotationStatement: string;
};

export type PurchaseOrderDocumentConfig = {
  title: string;
  showSupplierAddress: boolean;
  showSupplierContact: boolean;
  showDeliveryAddress: boolean;
  showExpectedDelivery: boolean;
  showPreparedBy: boolean;
  showNotes: boolean;
  showApprovalInformation: boolean;
};

export type GoodsReceiptDocumentConfig = {
  title: string;
  showSupplier: boolean;
  showPoReference: boolean;
  showReceivedBy: boolean;
  showReceivedDate: boolean;
  showDamagedQuantity: boolean;
  showNotes: boolean;
};

export type SalesSummaryDocumentConfig = {
  title: string;
};

export type OrganizationDocumentSettings = {
  version: 1;
  header: DocumentHeaderSettings;
  footer: DocumentFooterSettings;
  sales: SalesDocumentConfig;
  quotation: QuotationDocumentConfig;
  purchaseOrder: PurchaseOrderDocumentConfig;
  goodsReceipt: GoodsReceiptDocumentConfig;
  salesSummary: SalesSummaryDocumentConfig;
};

/** Non-BIR sales/customer transaction disclaimer title (Standard mode only). */
export const SALES_BIR_SAFE_DISCLAIMER =
  "NOT A BIR INVOICE – FOR TRANSACTION REFERENCE ONLY";

/** Longer Standard sales record disclaimer body. */
export const SALES_EXTENDED_RECORD_DISCLAIMER =
  "This document is for business and customer record purposes only. It does not replace any document the seller may be legally required to issue.";

export const DEFAULT_QUOTATION_STATEMENT =
  "This quotation is not a sales transaction and does not confirm payment or delivery.";

export const DEFAULT_DOCUMENT_SETTINGS: OrganizationDocumentSettings = {
  version: 1,
  header: {
    showLogo: true,
    showBusinessName: true,
    showBusinessAddress: true,
    showBusinessPhone: true,
    showBusinessEmail: true,
    showWebsite: false,
    showBranchName: true,
    showBranchAddress: false,
  },
  footer: {
    showCustomFooter: true,
    customFooterText: "Thank you for your business.",
    showBusinessContact: true,
    showPageNumber: true,
    showDocumentDisclaimer: true,
  },
  sales: {
    title: "Customer Purchase Summary",
    showCustomerName: true,
    showCustomerAddress: true,
    showCustomerContact: true,
    showCashier: true,
    showPaymentMethod: true,
    showSku: false,
    showDiscount: true,
    showNotes: true,
    showSalesDisclaimer: true,
    salesDisclaimerTitle: SALES_BIR_SAFE_DISCLAIMER,
    salesDisclaimerBody: SALES_EXTENDED_RECORD_DISCLAIMER,
  },
  quotation: {
    title: "QUOTATION",
    showCustomerName: true,
    showCustomerAddress: true,
    showCustomerContact: true,
    showPreparedBy: true,
    showValidUntil: true,
    showReference: true,
    showNotes: true,
    showSku: false,
    showDiscount: true,
    showQuotationStatement: true,
    quotationStatement: DEFAULT_QUOTATION_STATEMENT,
  },
  purchaseOrder: {
    title: "Purchase Order",
    showSupplierAddress: true,
    showSupplierContact: true,
    showDeliveryAddress: true,
    showExpectedDelivery: true,
    showPreparedBy: true,
    showNotes: true,
    showApprovalInformation: true,
  },
  goodsReceipt: {
    title: "Goods Receipt",
    showSupplier: true,
    showPoReference: true,
    showReceivedBy: true,
    showReceivedDate: true,
    showDamagedQuantity: true,
    showNotes: true,
  },
  salesSummary: {
    title: "Sales Summary",
  },
};

/** Org-scoped key: switching organizations must not leak settings. */
const STORAGE_PREFIX = "exits.pos.document-settings.v1:";

export function organizationDocumentSettingsStorageKey(organizationId: string): string {
  return `${STORAGE_PREFIX}${organizationId.trim()}`;
}

function storageKey(organizationId: string): string {
  return organizationDocumentSettingsStorageKey(organizationId);
}

function isObject(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value);
}

function mergeSales(raw: unknown, base: SalesDocumentConfig): SalesDocumentConfig {
  if (!isObject(raw)) {
    return base;
  }
  const merged = { ...base, ...raw } as SalesDocumentConfig;
  if (typeof merged.salesDisclaimerTitle !== "string" || !merged.salesDisclaimerTitle.trim()) {
    merged.salesDisclaimerTitle = base.salesDisclaimerTitle;
  }
  if (typeof merged.salesDisclaimerBody !== "string" || !merged.salesDisclaimerBody.trim()) {
    merged.salesDisclaimerBody = base.salesDisclaimerBody;
  }
  if (typeof merged.showSalesDisclaimer !== "boolean") {
    merged.showSalesDisclaimer = base.showSalesDisclaimer;
  }
  return merged;
}

function mergeQuotation(raw: unknown, base: QuotationDocumentConfig): QuotationDocumentConfig {
  if (!isObject(raw)) {
    return base;
  }
  const merged = { ...base, ...raw } as QuotationDocumentConfig;
  if (typeof merged.quotationStatement !== "string") {
    merged.quotationStatement = base.quotationStatement;
  }
  if (typeof merged.showQuotationStatement !== "boolean") {
    merged.showQuotationStatement = base.showQuotationStatement;
  }
  return merged;
}

function mergeSettings(raw: unknown): OrganizationDocumentSettings {
  if (!isObject(raw)) {
    return structuredClone(DEFAULT_DOCUMENT_SETTINGS);
  }
  const base = structuredClone(DEFAULT_DOCUMENT_SETTINGS);
  const header = isObject(raw.header) ? { ...base.header, ...raw.header } : base.header;
  const footer = isObject(raw.footer) ? { ...base.footer, ...raw.footer } : base.footer;
  header.showBusinessName = true;

  const sales = mergeSales(raw.sales, base.sales);
  // Migrate older localStorage that only had footer.showDocumentDisclaimer.
  if (!isObject(raw.sales) || typeof (raw.sales as Record<string, unknown>).showSalesDisclaimer !== "boolean") {
    if (typeof footer.showDocumentDisclaimer === "boolean") {
      sales.showSalesDisclaimer = footer.showDocumentDisclaimer;
    }
  }
  footer.showDocumentDisclaimer = sales.showSalesDisclaimer;

  return {
    version: 1,
    header,
    footer: {
      ...footer,
      customFooterText:
        typeof footer.customFooterText === "string"
          ? footer.customFooterText
          : base.footer.customFooterText,
    },
    sales,
    quotation: mergeQuotation(raw.quotation, base.quotation),
    purchaseOrder: isObject(raw.purchaseOrder)
      ? { ...base.purchaseOrder, ...raw.purchaseOrder }
      : base.purchaseOrder,
    goodsReceipt: isObject(raw.goodsReceipt)
      ? { ...base.goodsReceipt, ...raw.goodsReceipt }
      : base.goodsReceipt,
    salesSummary: isObject(raw.salesSummary)
      ? { ...base.salesSummary, ...raw.salesSummary }
      : base.salesSummary,
  };
}

export function readOrganizationDocumentSettings(
  organizationId: string,
): OrganizationDocumentSettings {
  if (typeof localStorage === "undefined" || !organizationId.trim()) {
    return structuredClone(DEFAULT_DOCUMENT_SETTINGS);
  }
  try {
    const raw = localStorage.getItem(storageKey(organizationId));
    if (!raw) {
      return structuredClone(DEFAULT_DOCUMENT_SETTINGS);
    }
    return mergeSettings(JSON.parse(raw) as unknown);
  } catch {
    return structuredClone(DEFAULT_DOCUMENT_SETTINGS);
  }
}

export function writeOrganizationDocumentSettings(
  organizationId: string,
  settings: OrganizationDocumentSettings,
): OrganizationDocumentSettings {
  const next = mergeSettings(settings);
  next.footer.showDocumentDisclaimer = next.sales.showSalesDisclaimer;
  if (typeof localStorage !== "undefined" && organizationId.trim()) {
    localStorage.setItem(storageKey(organizationId), JSON.stringify(next));
  }
  return next;
}

export function resetSalesDisclaimerDefaults(
  settings: OrganizationDocumentSettings,
): OrganizationDocumentSettings {
  return {
    ...settings,
    sales: {
      ...settings.sales,
      showSalesDisclaimer: true,
      salesDisclaimerTitle: SALES_BIR_SAFE_DISCLAIMER,
      salesDisclaimerBody: SALES_EXTENDED_RECORD_DISCLAIMER,
    },
    footer: {
      ...settings.footer,
      showDocumentDisclaimer: true,
    },
  };
}

export function formatOrganizationAddress(parts: {
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  region?: string | null;
  postalCode?: string | null;
  countryCode?: string | null;
}): string | null {
  const lines = [
    parts.addressLine1?.trim(),
    parts.addressLine2?.trim(),
    [parts.city?.trim(), parts.region?.trim()].filter(Boolean).join(", ") || null,
    parts.postalCode?.trim(),
    parts.countryCode?.trim(),
  ].filter(Boolean) as string[];
  return lines.length > 0 ? lines.join("\n") : null;
}
