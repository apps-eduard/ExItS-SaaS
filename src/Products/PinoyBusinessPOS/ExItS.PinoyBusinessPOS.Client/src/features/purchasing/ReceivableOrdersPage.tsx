import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  isReceivablePurchaseOrderStatus,
  listPurchaseOrders,
} from "@/api/pos/pos-purchase-orders-client";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ReceivableOrdersPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["purchase-orders", "receivable", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace) && online,
    queryFn: async ({ signal }) => {
      const ordered = await listPurchaseOrders(
        workspace!,
        { status: "Ordered", pageSize: 50 },
        signal,
      );
      const partial = await listPurchaseOrders(
        workspace!,
        { status: "PartiallyReceived", pageSize: 50 },
        signal,
      );
      return [...ordered.items, ...partial.items].filter(
        (po) => isReceivablePurchaseOrderStatus(po.status) && (po.canReceiveConnected ?? true),
      );
    },
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="receivable-orders-page">
      <PageHeader
        title={t("purchasing.receipts")}
        description={t("purchasing.receiptsLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("purchasing.receiptsStockNote")}
      </p>
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}
      {!query.isLoading && !query.isError && (query.data?.length ?? 0) === 0 ? (
        <EmptyState
          title={t("purchasing.receiptsEmpty")}
          detail={t("purchasing.receiptsEmptyDetail")}
        />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {(query.data ?? []).map((po) => (
          <li key={po.purchaseOrderId}>
            <Link
              to={`/purchasing/${po.purchaseOrderId}/receive`}
              className="block rounded-md border border-border p-3 no-underline text-inherit"
              data-testid={`receivable-row-${po.purchaseOrderId}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-medium">{po.poNumber ?? t("purchasing.unnamedPo")}</span>
                <StatusChip tone="warning">{po.status}</StatusChip>
              </div>
              <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.receiveAgainstPo")}
              </p>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
