import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeftRight, Plus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listInventoryTransfers,
  type InventoryTransferListItemDto,
} from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { Notice } from "@/components/exits/Notice";
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
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  branchDisplayName,
  formatTransferQty,
  formatTransferTimestamp,
  inventoryTransferExecutor,
  inventoryTransferStatusLabelKey,
  inventoryTransferStatusTone,
} from "@/features/inventory/inventory-transfer-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

const STATUS_FILTERS = [
  { value: "", labelKey: "transfer.filter.all" as const },
  { value: "Draft", labelKey: "transfer.status.draft" as const },
  { value: "InTransit", labelKey: "transfer.status.inTransit" as const },
  { value: "PartiallyReceived", labelKey: "transfer.status.partiallyReceived" as const },
  { value: "Received", labelKey: "transfer.status.received" as const },
  { value: "ClosedWithDiscrepancy", labelKey: "transfer.status.closedWithDiscrepancy" as const },
  { value: "Cancelled", labelKey: "transfer.status.cancelled" as const },
];

type DirectionFilter = "" | "outgoing" | "incoming";

function transferRouteLabel(item: InventoryTransferListItemDto): string {
  const source = branchDisplayName(item.sourceBranchName, item.sourceBranchId);
  const dest = branchDisplayName(item.destinationBranchName, item.destinationBranchId);
  return `${source} → ${dest}`;
}

export function InventoryTransferListPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [direction, setDirection] = useState<DirectionFilter>("");
  const allowManage = canManageInventory(sessionGrant);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const orgBranches = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    return org?.branches.filter((b) => b.isActive) ?? [];
  }, [workspaces, boundWorkspace?.organizationId]);

  const multiBranch = orgBranches.length >= 2;
  const currentBranchName = boundWorkspace?.branchName ?? t("transfer.currentBranch");
  const canCreate = allowManage && multiBranch && online;

  const query = useQuery({
    queryKey: [
      "inventory-transfers",
      workspace?.organizationId,
      workspace?.branchId,
      page,
      status,
      direction,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listInventoryTransfers(
        workspace!,
        {
          page,
          pageSize: PAGE_SIZE,
          status: status || undefined,
          direction: direction || undefined,
        },
        signal,
      ),
  });

  useEffect(() => {
    setPage(1);
  }, [workspace?.organizationId, workspace?.branchId, status, direction]);

  const items = query.data?.items ?? [];
  const actorIds = useMemo(
    () =>
      items.flatMap((item) => [
        item.createdBy,
        item.dispatchedBy,
        item.receivedBy,
        item.cancelledBy,
      ]),
    [items],
  );
  const actors = useActorDirectory(workspace?.organizationId, actorIds);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;

  return (
    <div
      className="inventory-transfer-list-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="inventory-transfer-list-page"
    >
      <PageHeader
        title={t("transfer.title")}
        description={t("transfer.lede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      {!online ? (
        <Notice tone="warning" testId="transfer-offline-notice">
          {t("transfer.offline")}
        </Notice>
      ) : null}

      <Card
        className="flex min-w-0 flex-col gap-3 p-3"
        treatment="bordered"
        data-testid="transfer-list-tools"
      >
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold text-primary">
          {t("transfer.listSection")}
        </h2>

        <div className="flex min-w-0 flex-wrap items-center gap-2">
          {allowManage && multiBranch ? (
            <ExitsChipBar
              variant="actions"
              ariaLabel={t("transfer.title")}
              testId="transfer-toolbar"
              className="exits-animate-toolbar"
              items={[
                {
                  key: "new",
                  label: t("transfer.new"),
                  icon: <Plus />,
                  href: online ? "/inventory/transfers/new" : undefined,
                  disabled: !online,
                  testId: "transfer-new",
                  emphasis: "primary",
                },
              ]}
            />
          ) : null}

          <div
            className="inline-flex min-h-[var(--exits-control-height)] min-w-0 max-w-full items-center rounded-[var(--exits-radius-soft)] border px-[var(--exits-control-padding-x)] text-[length:var(--exits-text-sm)] font-semibold text-primary"
            style={{
              background: "color-mix(in srgb, var(--exits-primary) 14%, var(--exits-surface))",
              borderColor: "color-mix(in srgb, var(--exits-primary) 55%, var(--exits-border))",
            }}
            data-testid="transfer-current-branch"
            title={`${t("transfer.currentBranch")}: ${currentBranchName}`}
          >
            <span className="truncate">
              {t("transfer.currentBranch")}: {currentBranchName}
            </span>
          </div>
        </div>

        <div className="transfer-filters" data-testid="transfer-filters">
          <div className="transfer-filters__row">
            <section
              className="transfer-filters__group flex flex-col gap-1"
              aria-labelledby="transfer-filter-direction-label"
            >
              <p
                id="transfer-filter-direction-label"
                className="catalog-page__filter-section-label m-0"
              >
                {t("transfer.filter.direction")}
              </p>
              <ExitsChipBar
                variant="filter"
                ariaLabel={t("transfer.filter.direction")}
                testId="transfer-direction-filters"
                items={[
                  {
                    key: "all",
                    label: t("transfer.filter.all"),
                    state: direction === "" ? "active" : "idle",
                    onSelect: () => setDirection(""),
                    testId: "transfer-direction-all",
                  },
                  {
                    key: "outgoing",
                    label: t("transfer.filter.outgoing"),
                    state: direction === "outgoing" ? "active" : "idle",
                    onSelect: () => setDirection("outgoing"),
                    testId: "transfer-direction-outgoing",
                  },
                  {
                    key: "incoming",
                    label: t("transfer.filter.incoming"),
                    state: direction === "incoming" ? "active" : "idle",
                    onSelect: () => setDirection("incoming"),
                    testId: "transfer-direction-incoming",
                  },
                ]}
              />
            </section>

            <span className="transfer-filters__sep" aria-hidden />

            <section
              className="transfer-filters__group flex flex-col gap-1"
              aria-labelledby="transfer-filter-status-label"
            >
              <p
                id="transfer-filter-status-label"
                className="catalog-page__filter-section-label m-0"
              >
                {t("transfer.filter.status")}
              </p>
              <ExitsChipBar
                variant="filter"
                ariaLabel={t("transfer.filter.status")}
                testId="transfer-status-filters"
                items={STATUS_FILTERS.map((filter) => ({
                  key: filter.value || "all-status",
                  label: t(filter.labelKey),
                  state: status === filter.value ? "active" : "idle",
                  onSelect: () => setStatus(filter.value),
                  testId: `transfer-status-${filter.value || "all"}`,
                }))}
              />
            </section>
          </div>
        </div>
      </Card>

      {query.isLoading ? <LoadingState label={t("transfer.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("transfer.errorTitle")} detail={t("transfer.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <>
          <EmptyState
            align="center"
            variant={multiBranch ? "default" : "setup"}
            icon={<ArrowLeftRight className="size-5" strokeWidth={1.75} />}
            title={t("transfer.empty")}
            detail={multiBranch ? t("transfer.emptyDetail") : t("transfer.singleBranchDetail")}
            action={
              canCreate ? (
                <Button asChild>
                  <Link to="/inventory/transfers/new" data-testid="transfer-empty-cta">
                    <Plus className="size-4 shrink-0" aria-hidden />
                    {t("transfer.new")}
                  </Link>
                </Button>
              ) : undefined
            }
          />
          {!multiBranch ? (
            <Notice tone="info" testId="transfer-single-branch">
              {t("transfer.requiresTwoBranches")}
            </Notice>
          ) : null}
        </>
      ) : null}

      {!query.isLoading && !query.isError && items.length > 0 ? (
        <ExitsTableContainer data-testid="transfer-list">
          <ExitsTable data-testid="transfer-list-desktop">
            <ExitsTableHeader>
              <ExitsTableRow>
                <ExitsTableHead cellAlign="text">{t("transfer.colNumber")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text" colSize="flex">
                  {t("transfer.colRoute")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="numeric" colSize="numeric">
                  {t("transfer.colLines")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="numeric" colSize="numeric">
                  {t("transfer.sent")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="numeric" colSize="numeric">
                  {t("transfer.received")}
                </ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("purchasing.fieldStatus")}</ExitsTableHead>
                <ExitsTableHead cellAlign="text">{t("transfer.colUpdated")}</ExitsTableHead>
              </ExitsTableRow>
            </ExitsTableHeader>
            <ExitsTableBody>
              {items.map((item) => {
                const route = transferRouteLabel(item);
                const transferNumber = item.transferNumber?.trim() || "—";
                const executor = inventoryTransferExecutor(item);
                const resolved = actors.resolve(executor.actorId);
                const executorName =
                  resolved?.displayName && resolved.actorStatus !== "NotAvailable"
                    ? resolved.displayName
                    : actors.isResolving
                      ? "…"
                      : t("common.notAvailable");
                return (
                  <ExitsTableRow
                    key={item.transferId}
                    interactive
                    data-testid={`transfer-row-${item.transferId}`}
                    onClick={() => navigate(`/inventory/transfers/${item.transferId}`)}
                  >
                    <ExitsTableCell cellAlign="text" className="font-medium tabular-nums">
                      {transferNumber}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text" colSize="flex" className="font-medium">
                      <div>{route}</div>
                      <div
                        className="text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`transfer-executor-${item.transferId}`}
                      >
                        {t(executor.labelKey).replace("{name}", executorName)}
                      </div>
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric" colSize="numeric" className="tabular-nums">
                      {item.lineCount}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(item.totalSentQty)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="numeric" colSize="numeric" className="tabular-nums">
                      {formatTransferQty(item.totalReceivedQty)}
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text">
                      <StatusChip tone={inventoryTransferStatusTone(item.status)}>
                        {t(inventoryTransferStatusLabelKey(item.status))}
                      </StatusChip>
                    </ExitsTableCell>
                    <ExitsTableCell cellAlign="text" className="text-muted">
                      {formatTransferTimestamp(item.updatedAtUtc)}
                    </ExitsTableCell>
                  </ExitsTableRow>
                );
              })}
            </ExitsTableBody>
          </ExitsTable>

          <ExitsTableMobile data-testid="transfer-list-mobile">
            {items.map((item) => {
              const route = transferRouteLabel(item);
              const transferNumber = item.transferNumber?.trim() || "—";
              const executor = inventoryTransferExecutor(item);
              const resolved = actors.resolve(executor.actorId);
              const executorName =
                resolved?.displayName && resolved.actorStatus !== "NotAvailable"
                  ? resolved.displayName
                  : actors.isResolving
                    ? "…"
                    : t("common.notAvailable");
              return (
                <ExitsTableMobileRow
                  key={item.transferId}
                  data-testid={`transfer-row-mobile-${item.transferId}`}
                  onClick={() => navigate(`/inventory/transfers/${item.transferId}`)}
                >
                  <div className="exits-table-mobile__title-row">
                    <span className="exits-table-mobile__title">{route}</span>
                    <StatusChip tone={inventoryTransferStatusTone(item.status)}>
                      {t(inventoryTransferStatusLabelKey(item.status))}
                    </StatusChip>
                  </div>
                  <p className="exits-table-mobile__meta m-0">
                    {transferNumber}
                    {" · "}
                    {t("transfer.linesCount").replace("{count}", String(item.lineCount))}
                  </p>
                  <p className="exits-table-mobile__math mt-1 mb-0">
                    {t("transfer.sent")}: {formatTransferQty(item.totalSentQty)}
                    {" · "}
                    {t("transfer.received")}: {formatTransferQty(item.totalReceivedQty)}
                  </p>
                  <p
                    className="mt-1 mb-0 text-[length:var(--exits-text-xs)] text-muted"
                    data-testid={`transfer-executor-mobile-${item.transferId}`}
                  >
                    {t(executor.labelKey).replace("{name}", executorName)}
                    {" · "}
                    {formatTransferTimestamp(item.updatedAtUtc)}
                  </p>
                </ExitsTableMobileRow>
              );
            })}
          </ExitsTableMobile>

          {totalCount > PAGE_SIZE ? (
            <ExitsTablePagination
              page={page}
              pageSize={PAGE_SIZE}
              total={totalCount}
              pageSizeOptions={[PAGE_SIZE]}
              onPageChange={setPage}
              onPageSizeChange={() => undefined}
              rowsPerPageLabel={t("exitsTable.rowsPerPage")}
              previousLabel={t("transfer.prevPage")}
              nextLabel={t("transfer.nextPage")}
              rangeLabel={t("exitsTable.range")}
              data-testid="transfer-pagination"
            />
          ) : null}
        </ExitsTableContainer>
      ) : null}
    </div>
  );
}
