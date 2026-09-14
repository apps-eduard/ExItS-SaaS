import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { FileDown, Printer } from "lucide-react";
import {
  getLinkedCustomerSaleReceipt,
  isExtendedHistoryRequiredError,
  type LinkedCustomerSaleReceipt,
} from "@/api/pos/pos-linked-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import type { OrganizationB2bPublicProfile } from "@/api/platform/organization-b2b-public-profile-client";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { customerPurchaseSummaryFromLinkedReceipt } from "@/features/documents/customer-purchase-summary-view";
import { DEFAULT_DOCUMENT_SETTINGS } from "@/features/documents/document-settings";
import {
  exportBusinessDocumentPdf,
  printBusinessDocument,
} from "@/features/documents/print-business-document";
import {
  getOrganizationDocumentPublicIdentity,
  resolveCustomerSellerIdentity,
} from "@/features/documents/resolve-customer-seller-identity";
import { CustomerPurchaseSummaryDocument } from "@/features/documents/SaleBusinessDocument";
import { useI18n } from "@/i18n/I18nProvider";

type ReceiptState =
  | { kind: "loading" }
  | { kind: "offline" }
  | { kind: "forbidden"; detail: string }
  | { kind: "notFound"; detail: string }
  | { kind: "entitlement"; detail: string }
  | { kind: "error"; detail: string }
  | {
      kind: "ready";
      receipt: LinkedCustomerSaleReceipt;
      publicProfile: OrganizationB2bPublicProfile | null;
    };

export function LinkedMerchantReceiptPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const {
    organizationId = "",
    businessCustomerId = "",
    saleId = "",
  } = useParams<{
    organizationId: string;
    businessCustomerId: string;
    saleId: string;
  }>();
  const [state, setState] = useState<ReceiptState>({ kind: "loading" });

  const backHref = `/personal/linked-merchants/${organizationId}/${businessCustomerId}`;
  const pageShell =
    "personal-page personal-commerce-page linked-merchant-receipt-page exits-page flex min-w-0 flex-col gap-3";

  useEffect(() => {
    async function load() {
      if (!organizationId || !businessCustomerId || !saleId) {
        setState({ kind: "notFound", detail: t("personal.merchantReceipt.missing") });
        return;
      }
      if (!online) {
        setState({ kind: "offline" });
        return;
      }

      setState({ kind: "loading" });
      try {
        const receipt = await getLinkedCustomerSaleReceipt(
          organizationId,
          businessCustomerId,
          saleId,
        );
        // Public profile fills logo/email gaps for legacy receipts; never fails the page.
        const publicProfile = await getOrganizationDocumentPublicIdentity(organizationId);
        setState({ kind: "ready", receipt, publicProfile });
      } catch (err) {
        if (isExtendedHistoryRequiredError(err)) {
          setState({
            kind: "entitlement",
            detail: t("personal.merchantStatement.historyLocked"),
          });
          return;
        }
        if (err instanceof PosApiError) {
          if (err.status === 403) {
            setState({ kind: "forbidden", detail: err.message });
            return;
          }
          if (err.status === 404) {
            setState({ kind: "notFound", detail: err.message });
            return;
          }
        }
        setState({
          kind: "error",
          detail: err instanceof Error ? err.message : t("personal.merchantReceipt.loadFailed"),
        });
      }
    }

    void load();
  }, [businessCustomerId, online, organizationId, saleId, t]);

  if (state.kind === "loading") {
    return <LoadingState label={t("loading.label")} />;
  }

  if (state.kind === "offline") {
    return (
      <ErrorState
        title={t("offline.internetRequiredTitle")}
        detail={t("offline.requiredHistory")}
      />
    );
  }

  if (state.kind === "forbidden") {
    return (
      <ErrorState
        title={t("personal.merchantStatement.deniedTitle")}
        detail={state.detail || t("personal.merchantStatement.denied")}
      />
    );
  }

  if (state.kind === "notFound") {
    return (
      <ErrorState
        title={t("personal.merchantReceipt.missingTitle")}
        detail={state.detail || t("personal.merchantReceipt.missing")}
      />
    );
  }

  if (state.kind === "entitlement") {
    return (
      <div className={pageShell} data-testid="linked-merchant-receipt-page">
        <PageHeader
          title={t("personal.merchantReceipt.missingTitle")}
          backTo={backHref}
          backLabel={t("personal.merchantReceipt.backToStatement")}
          backTestId="page-header-back-merchant-receipt"
        />
        <ErrorState
          title={t("personal.merchantStatement.historyLockedTitle")}
          detail={state.detail}
        />
        <Button asChild className="w-fit">
          <Link to="/personal/rewards">{t("personal.merchantStatement.historyUnlock")}</Link>
        </Button>
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className={pageShell} data-testid="linked-merchant-receipt-page">
        <PageHeader
          title={t("personal.merchantReceipt.errorTitle")}
          backTo={backHref}
          backLabel={t("personal.merchantReceipt.backToStatement")}
          backTestId="page-header-back-merchant-receipt"
        />
        <ErrorState title={t("personal.merchantReceipt.errorTitle")} detail={state.detail} />
      </div>
    );
  }

  const { receipt, publicProfile } = state;
  const view = customerPurchaseSummaryFromLinkedReceipt(receipt);
  const { identity, headerVisibility } = resolveCustomerSellerIdentity({
    receipt,
    publicProfile,
  });

  const customerDocumentSettings = {
    ...DEFAULT_DOCUMENT_SETTINGS,
    sales: {
      ...DEFAULT_DOCUMENT_SETTINGS.sales,
      showCashier: false,
      showSku: false,
      showCustomerName: Boolean(view.customerDisplayName),
    },
    footer: {
      ...DEFAULT_DOCUMENT_SETTINGS.footer,
      showDocumentDisclaimer: true,
    },
    header: {
      ...DEFAULT_DOCUMENT_SETTINGS.header,
      showLogo: headerVisibility.showLogo,
      showBusinessAddress: headerVisibility.showBusinessAddress,
      showBusinessPhone: headerVisibility.showBusinessPhone,
      showBusinessEmail: headerVisibility.showBusinessEmail,
      showBranchName: headerVisibility.showBranchName,
      showBranchAddress: headerVisibility.showBranchAddress,
    },
  };

  const headerActions = (
    <div
      className="flex min-w-0 flex-wrap items-center gap-2 print:hidden"
      data-testid="linked-merchant-receipt-actions"
    >
      <Button
        type="button"
        variant="outline"
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="linked-merchant-receipt-print"
        onClick={() => printBusinessDocument()}
      >
        <Printer className="size-4 shrink-0" aria-hidden />
        {t("summary.print")}
      </Button>
      <Button
        type="button"
        variant="outline"
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="linked-merchant-receipt-pdf"
        onClick={() => exportBusinessDocumentPdf()}
      >
        <FileDown className="size-4 shrink-0" aria-hidden />
        {t("summary.exportPdf")}
      </Button>
    </div>
  );

  return (
    <div className={pageShell} data-testid="linked-merchant-receipt-page">
      <PageHeader
        title={t("personal.merchantReceipt.title")}
        description={receipt.receiptNumber}
        backTo={backHref}
        backLabel={t("personal.merchantReceipt.backToStatement")}
        backTestId="page-header-back-merchant-receipt"
        trailing={headerActions}
      />

      <div
        className="linked-merchant-receipt-document mx-auto w-full min-w-0 max-w-[52rem] overflow-x-hidden"
        data-testid="linked-merchant-receipt-document"
      >
        <CustomerPurchaseSummaryDocument
          view={view}
          settings={customerDocumentSettings}
          identity={identity}
          headerVisibility={headerVisibility}
          audience="Customer"
          preview
        />
      </div>
    </div>
  );
}
