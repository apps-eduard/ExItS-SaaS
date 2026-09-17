import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ClipboardList } from "lucide-react";
import {
  isPurchaseOrderReceivable,
  listPurchaseOrders,
  type PosPurchaseOrderDto,
} from "@/api/pos/pos-purchase-orders-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTablePagination,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip, type StatusChipTone } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { navigateWithReturn } from "@/navigation/smart-back";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const;

function statusTone(status: string): StatusChipTone {
  switch (status) {
    case "Ordered":
      return "success";
    case "PartiallyReceived":
      return "warning";
    default:
      return "info";
  }
}

function poOutstandingSummary(po: PosPurchaseOrderDto) {
  const outstandingLines = po.lines.filter((line) => line.outstandingQty > 0);
  const totalOutstanding = outstandingLines.reduce((sum, line) => sum + line.outstandingQty, 0);
  return { totalOutstanding, lineCount: outstandingLines.length };
}

export function ReceivableOrdersPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

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
      return [...ordered.items, ...partial.items].filter((po) => isPurchaseOrderReceivable(po));
    },
  });

  const items = query.data ?? [];
  const pagedItems = useMemo(() => {
    const start = (page - 1) * pageSize;
    return items.slice(start, start + pageSize);
  }, [items, page, pageSize]);

  function openReceive(purchaseOrderId: string) {
    navigateWithReturn(navigate, `/purchasing/${purchaseOrderId}/receive`, location);
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div
      className="purchasing-receipts-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="receivable-orders-page"
    >
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
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("purchasing.offline")}</p>
      ) : null}

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("purchasing.receipts")}
        testId="receivable-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "orders",
            label: t("purchasing.orders"),
            icon: <ClipboardList />,
            href: "/purchasing/orders",
            testId: "receivable-open-orders",
          },
        ]}
      />

      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <EmptyState
          align="center"
          icon={<ClipboardList className="size-5" strokeWidth={1.75} />}
          title={t("purchasing.receiptsEmpty")}
          detail={t("purchasing.receiptsEmptyDetail")}
        />
      ) : null}

      {!query.isLoading && !query.isError && items.length > 0 ? (
        <ExitsTableContainer data-testid="receivable-orders-table">
          <ExitsTable>
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text">{t("purchasing.poNumber")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.supplier")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.orderDate")}</ExitsTableHead>
                <ExitsTableHead cellAlign="numeric">{t("purchasing.outstanding")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.fieldStatus")}</ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {pagedItems.map((po) => {
                const { totalOutstanding } = poOutstandingSummary(po);
                return (
                  <ExitsTableRow
                    key={po.purchaseOrderId}
                    interactive
                    data-testid={`receivable-row-${po.purchaseOrderId}`}
                    onClick={() => openReceive(po.purchaseOrderId)}
                  >
                    <ExitsTableCell cellAlign="text" className="font-medium">
                      {po.poNumber ?? t("purchasing.unnamedPo")}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      {po.supplierName ?? t("purchasing.unknownSupplier")}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">{po.orderDate}</ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric" className="tabular-nums">
                      {totalOutstanding}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <StatusChip tone={statusTone(po.status)}>
                        {po.displayStatus || po.status}
                      </StatusChip>
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>

          <ExitsTableMobile data-testid="receivable-orders-mobile">
            {pagedItems.map((po) => {
              const { totalOutstanding, lineCount } = poOutstandingSummary(po);
              return (
                <ExitsTableMobileRow
                  key={po.purchaseOrderId}
                  data-testid={`receivable-row-mobile-${po.purchaseOrderId}`}
                  onClick={() => openReceive(po.purchaseOrderId)}
                >
                  <div className="exits-table-mobile__title-row">
                    <p className="exits-table-mobile__title">
                      {po.poNumber ?? t("purchasing.unnamedPo")}
                    </p>
                    <StatusChip tone={statusTone(po.status)}>
                      {po.displayStatus || po.status}
                    </StatusChip>
                  </div>
                  <p className="exits-table-mobile__meta">
                    {po.supplierName ?? t("purchasing.unknownSupplier")} · {po.orderDate}
                  </p>
                  <p className="exits-table-mobile__math">
                    {t("purchasing.outstandingSummary")
                      .replace("{qty}", String(totalOutstanding))
                      .replace("{count}", String(lineCount))}
                  </p>
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>

          {items.length > 10 ? (
            <ExitsTablePagination
              page={page}
              pageSize={pageSize}
              total={items.length}
              pageSizeOptions={PAGE_SIZE_OPTIONS}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
              rowsPerPageLabel={t("exitsTable.rowsPerPage")}
              previousLabel={t("exitsTable.previous")}
              nextLabel={t("exitsTable.next")}
              rangeLabel={t("exitsTable.range")}
            />
          ) : null}
        </ExitsTableContainer>
      ) : null}
    </div>
  );
}
