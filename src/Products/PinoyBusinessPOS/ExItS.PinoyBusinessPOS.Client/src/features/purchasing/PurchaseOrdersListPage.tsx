import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { listPurchaseOrders, type PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
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

const PAGE_SIZE = 20;

function statusTone(status: string): "success" | "warning" | "info" | "danger" {
  switch (status) {
    case "Ordered":
      return "success";
    case "PartiallyReceived":
      return "warning";
    case "Received":
      return "success";
    case "Cancelled":
      return "info";
    case "Draft":
      return "info";
    default:
      return "info";
  }
}

export function PurchaseOrdersListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManagePurchasing(sessionGrant);

  const query = useQuery({
    queryKey: ["purchase-orders", workspace?.organizationId, workspace?.branchId, status, page],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listPurchaseOrders(
        workspace!,
        { status: status || undefined, page, pageSize: PAGE_SIZE },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const items: PosPurchaseOrderDto[] = query.data?.items ?? [];

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="purchase-orders-list-page">
      <PageHeader
        title={t("purchasing.orders")}
        description={t("purchasing.ordersLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("purchasing.ordersNoStock")}
      </p>
      {!online ? (
        <Card data-testid="purchasing-offline">
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      <div className="flex flex-wrap gap-2">
        {allowManage ? (
          <Button asChild className="min-h-11" disabled={!online} data-testid="po-new">
            <Link to="/purchasing/new">{t("purchasing.newOrder")}</Link>
          </Button>
        ) : null}
      </div>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("purchasing.statusFilter")}
        <select
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
          data-testid="po-status-filter"
        >
          <option value="">{t("purchasing.statusAll")}</option>
          <option value="Draft">Draft</option>
          <option value="Ordered">Ordered</option>
          <option value="PartiallyReceived">PartiallyReceived</option>
          <option value="Received">Received</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </label>
      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}
      {!query.isLoading && !query.isError && items.length === 0 ? (
        <EmptyState
          title={t("purchasing.ordersEmpty")}
          detail={t("purchasing.ordersEmptyDetail")}
        />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {items.map((po) => (
          <li key={po.purchaseOrderId}>
            <Link
              to={`/purchasing/${po.purchaseOrderId}`}
              className="block rounded-md border border-border p-3 no-underline text-inherit"
              data-testid={`po-row-${po.purchaseOrderId}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-medium">{po.poNumber ?? t("purchasing.unnamedPo")}</span>
                <StatusChip tone={statusTone(po.status)}>
                  {po.displayStatus || po.status}
                </StatusChip>
              </div>
              <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {po.supplierName ?? t("purchasing.unknownSupplier")} · {po.orderDate} ·{" "}
                {t("purchasing.linesCount").replace("{count}", String(po.lines.length))}
              </p>
            </Link>
          </li>
        ))}
      </ul>
      {totalCount > PAGE_SIZE ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("purchasing.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)]">
            {t("purchasing.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("purchasing.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
