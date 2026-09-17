import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { FileDown, Printer } from "lucide-react";
import { getOrganizationB2bPublicProfile } from "@/api/platform/organization-b2b-public-profile-client";
import { getDirectPurchaseB2bDetail } from "@/api/pos/pos-direct-purchases-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { customerPurchaseSummaryFromB2bDetail } from "@/features/documents/customer-purchase-summary-view";
import { DEFAULT_DOCUMENT_SETTINGS } from "@/features/documents/document-settings";
import {
  exportBusinessDocumentPdf,
  printBusinessDocument,
} from "@/features/documents/print-business-document";
import { resolveCustomerSellerIdentityParts } from "@/features/documents/resolve-customer-seller-identity";
import { CustomerPurchaseSummaryDocument } from "@/features/documents/SaleBusinessDocument";
import { useI18n } from "@/i18n/I18nProvider";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function DirectPurchaseB2bDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { saleId = "" } = useParams<{ saleId: string }>();
  const { boundWorkspace } = useWorkspace();
  const smartBack = usePageSmartBack({
    fallback: "directPurchases",
    backLabel: t("purchasing.directPurchases"),
    backTestId: "page-header-back-direct-purchases",
  });

  const workspace = useMemo(
    () =>
      boundWorkspace?.organizationId
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId ?? undefined,
          }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["direct-purchase-b2b", workspace?.organizationId, saleId],
    enabled: Boolean(workspace) && Boolean(saleId) && online,
    queryFn: ({ signal }) => getDirectPurchaseB2bDetail(workspace!, saleId, signal),
  });

  const publicProfileQuery = useQuery({
    queryKey: [
      "seller-document-public-identity",
      workspace?.organizationId,
      query.data?.sellerOrganizationId,
    ],
    enabled:
      Boolean(workspace?.organizationId) &&
      Boolean(query.data?.sellerOrganizationId) &&
      online &&
      query.isSuccess,
    queryFn: ({ signal }) =>
      getOrganizationB2bPublicProfile(
        query.data!.sellerOrganizationId,
        workspace!.organizationId,
        signal,
      ).catch(() => null),
    staleTime: 60_000,
  });

  const pageShell =
    "purchasing-direct-b2b-page exits-page flex min-w-0 flex-col gap-3";

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className={pageShell} data-testid="direct-purchase-b2b-detail-page">
        <PageHeader title={t("purchasing.b2bDetailTitle")} {...smartBack} />
        <ErrorState title={t("offline.internetRequiredTitle")} detail={t("purchasing.offline")} />
      </div>
    );
  }

  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className={pageShell} data-testid="direct-purchase-b2b-detail-page">
        <PageHeader title={t("purchasing.b2bDetailTitle")} {...smartBack} />
        <ErrorState
          title={t("purchasing.directNotFound")}
          detail={t("purchasing.b2bDetailMissing")}
        />
      </div>
    );
  }

  const detail = query.data;
  const view = customerPurchaseSummaryFromB2bDetail(detail);
  const { identity, headerVisibility } = resolveCustomerSellerIdentityParts({
    sellerDocumentIdentity: detail.sellerDocumentIdentity,
    merchantDisplayName: detail.sellerDisplayName,
    branchDisplayName: detail.sellerStoreDisplayName,
    publicProfile: publicProfileQuery.data ?? null,
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
      data-testid="b2b-document-actions"
    >
      <Button
        type="button"
        variant="outline"
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="b2b-print"
        onClick={() => printBusinessDocument()}
      >
        <Printer className="size-4 shrink-0" aria-hidden />
        {t("summary.print")}
      </Button>
      <Button
        type="button"
        variant="outline"
        className="h-9 min-h-9 shrink-0 gap-1.5 px-2.5"
        data-testid="b2b-pdf"
        onClick={() => exportBusinessDocumentPdf()}
      >
        <FileDown className="size-4 shrink-0" aria-hidden />
        {t("summary.exportPdf")}
      </Button>
    </div>
  );

  return (
    <div className={pageShell} data-testid="direct-purchase-b2b-detail-page">
      <PageHeader
        title={t("purchasing.b2bDetailTitle")}
        description={detail.saleNumber}
        {...smartBack}
        trailing={headerActions}
      />

      <div
        className="b2b-purchase-document mx-auto w-full min-w-0 max-w-[52rem] overflow-x-hidden"
        data-testid="b2b-purchase-document"
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
