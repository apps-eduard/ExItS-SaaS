import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus } from "lucide-react";
import { canManagePurchasing } from "@/access/pos-capabilities";
import { listPurchaseOrders, type PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

type StatusFilter = "" | "Draft" | "Ordered" | "PartiallyReceived" | "Received" | "Cancelled";

const STATUS_FILTERS: Array<{
  value: StatusFilter;
  key: string;
  labelKey:
    | "purchasing.statusAll"
    | "purchasing.statusDraft"
    | "purchasing.statusOrdered"
    | "purchasing.statusPartial"
    | "purchasing.statusReceived"
    | "purchasing.statusCancelled";
}> = [
  { value: "", key: "all", labelKey: "purchasing.statusAll" },
  { value: "Draft", key: "Draft", labelKey: "purchasing.statusDraft" },
  { value: "Ordered", key: "Ordered", labelKey: "purchasing.statusOrdered" },
  { value: "PartiallyReceived", key: "PartiallyReceived", labelKey: "purchasing.statusPartial" },
  { value: "Received", key: "Received", labelKey: "purchasing.statusReceived" },
  { value: "Cancelled", key: "Cancelled", labelKey: "purchasing.statusCancelled" },
];

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
  const [status, setStatus] = useState<StatusFilter>("");

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
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;

  return (
    <div
      className="purchasing-orders-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="purchase-orders-list-page"
    >
      <PageHeader
        title={t("purchasing.orders")}
        description={t("purchasing.ordersLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("purchasing.ordersNoStock")}</p>
      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="purchasing-offline">
          {t("purchasing.offline")}
        </p>
      ) : null}

      {allowManage ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("purchasing.orders")}
          testId="po-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "new",
              label: t("purchasing.newOrder"),
              icon: <Plus />,
              href: online ? "/purchasing/new" : undefined,
              disabled: !online,
              testId: "po-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("purchasing.statusFilter")}
        testId="po-status-filter"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: status === filter.value ? "active" : "idle",
          testId: `po-status-${filter.key}`,
          onSelect: () => {
            setStatus(filter.value);
            setPage(1);
          },
        }))}
      />

      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}
      {!query.isLoading && !query.isError && items.length === 0 ? (
        <EmptyState title={t("purchasing.ordersEmpty")} detail={t("purchasing.ordersEmptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0">
        {items.map((po) => (
          <li key={po.purchaseOrderId}>
            <Link
              to={`/purchasing/${po.purchaseOrderId}`}
              className="exits-list__card purchasing-row block min-w-0 text-foreground no-underline"
              data-testid={`po-row-${po.purchaseOrderId}`}
            >
              <span className="purchasing-row__main min-w-0">
                <span className="exits-list__name block truncate font-semibold">
                  {po.poNumber ?? t("purchasing.unnamedPo")}
                </span>
                <span className="purchasing-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {po.supplierName ?? t("purchasing.unknownSupplier")} · {po.orderDate} ·{" "}
                  {t("purchasing.linesCount").replace("{count}", String(po.lines.length))}
                </span>
              </span>
              <span className="purchasing-row__aside">
                <StatusChip tone={statusTone(po.status)}>{po.displayStatus || po.status}</StatusChip>
                <ChevronRight className="purchasing-row__chevron size-4 shrink-0 text-muted" aria-hidden />
              </span>
            </Link>
          </li>
        ))}
      </ul>

      {query.isSuccess && totalCount > 0 ? (
        <div className="exits-pagination">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </p>
          <div className="exits-pagination__actions flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              disabled={!canPrev}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("purchasing.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              disabled={!canNext}
              onClick={() => setPage((current) => current + 1)}
            >
              {t("purchasing.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
