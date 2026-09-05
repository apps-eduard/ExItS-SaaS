import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Plus } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { listInventoryTransfers } from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
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
  { value: "Cancelled", labelKey: "transfer.status.cancelled" as const },
];

type DirectionFilter = "" | "outgoing" | "incoming";

export function InventoryTransferListPage() {
  const { t } = useI18n();
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
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

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
        trailing={
          allowManage && multiBranch ? (
            <Button asChild disabled={!online}>
              <Link
                to={online ? "/inventory/transfers/new" : "#"}
                data-testid="transfer-new"
                aria-disabled={!online}
                onClick={(event) => {
                  if (!online) {
                    event.preventDefault();
                  }
                }}
              >
                <Plus className="size-4 shrink-0" aria-hidden />
                {t("transfer.new")}
              </Link>
            </Button>
          ) : null
        }
      />

      <p
        className="m-0 text-[length:var(--exits-text-xs)] text-muted"
        data-testid="transfer-current-branch"
      >
        {t("transfer.currentBranch")}: {currentBranchName}
      </p>
      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}

      <div className="flex min-w-0 flex-col gap-3 sm:flex-row sm:items-start sm:gap-4">
        <div className="flex min-w-0 shrink-0 flex-col gap-1">
          <span className="exits-type-label">{t("transfer.filter.direction")}</span>
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
        </div>

        <div className="flex min-w-0 flex-1 flex-col gap-1">
          <span className="exits-type-label">{t("transfer.filter.status")}</span>
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
        </div>
      </div>

      {query.isLoading ? <LoadingState label={t("transfer.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("transfer.errorTitle")} detail={t("transfer.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <>
          <EmptyState
            title={t("transfer.empty")}
            detail={multiBranch ? t("transfer.emptyDetail") : t("transfer.singleBranchDetail")}
          />
          {canCreate ? (
            <Button asChild>
              <Link to="/inventory/transfers/new" data-testid="transfer-empty-cta">
                <Plus className="size-4 shrink-0" aria-hidden />
                {t("transfer.new")}
              </Link>
            </Button>
          ) : null}
          {!multiBranch ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="transfer-single-branch"
            >
              {t("transfer.requiresTwoBranches")}
            </p>
          ) : null}
        </>
      ) : null}

      {items.length > 0 ? (
        <section className="flex min-w-0 flex-col gap-1.5">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("transfer.listSection")}
          </h2>
          <ul
            className="m-0 grid w-full list-none grid-cols-1 gap-2 p-0 md:grid-cols-2"
            data-testid="transfer-list"
          >
            {items.map((item) => {
              const source = branchDisplayName(item.sourceBranchName, item.sourceBranchId);
              const dest = branchDisplayName(item.destinationBranchName, item.destinationBranchId);
              const transferNumber = item.transferNumber?.trim() || "";
              const executor = inventoryTransferExecutor(item);
              const resolved = actors.resolve(executor.actorId);
              const executorName =
                resolved?.displayName && resolved.actorStatus !== "NotAvailable"
                  ? resolved.displayName
                  : actors.isResolving
                    ? "…"
                    : t("common.notAvailable");
              return (
                <li key={item.transferId} className="min-w-0">
                  <Link
                    to={`/inventory/transfers/${item.transferId}`}
                    className="exits-list__card transfer-row flex h-full w-full min-w-0 items-center gap-3 text-foreground no-underline"
                    data-testid={`transfer-row-${item.transferId}`}
                  >
                    <span className="transfer-row__main flex min-w-0 flex-1 flex-col gap-1">
                      <span className="flex min-w-0 items-start justify-between gap-2">
                        <span className="min-w-0">
                          {transferNumber ? (
                            <span className="mb-0.5 block truncate text-[length:var(--exits-text-xs)] text-muted">
                              {transferNumber}
                            </span>
                          ) : null}
                          <span className="block truncate text-[length:var(--exits-text-md)] font-semibold text-foreground">
                            {source} → {dest}
                          </span>
                        </span>
                        <StatusChip tone={inventoryTransferStatusTone(item.status)}>
                          {t(inventoryTransferStatusLabelKey(item.status))}
                        </StatusChip>
                      </span>
                      <span className="flex flex-wrap gap-x-2 gap-y-0.5 text-[length:var(--exits-text-sm)] text-muted">
                        <span>{t("transfer.linesCount").replace("{count}", String(item.lineCount))}</span>
                        <span aria-hidden>·</span>
                        <span>
                          {t("transfer.sent")} {formatTransferQty(item.totalSentQty)}
                        </span>
                        <span aria-hidden>·</span>
                        <span>
                          {t("transfer.received")} {formatTransferQty(item.totalReceivedQty)}
                        </span>
                        {item.totalDifferenceQty !== 0 ? (
                          <>
                            <span aria-hidden>·</span>
                            <span>
                              {t("transfer.difference")} {formatTransferQty(item.totalDifferenceQty)}
                            </span>
                          </>
                        ) : null}
                      </span>
                      <span
                        className="flex flex-wrap gap-x-2 gap-y-0.5 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`transfer-executor-${item.transferId}`}
                      >
                        <span>{t(executor.labelKey).replace("{name}", executorName)}</span>
                        <span aria-hidden>·</span>
                        <span>{formatTransferTimestamp(item.updatedAtUtc)}</span>
                      </span>
                    </span>
                    <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden />
                  </Link>
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}

      {totalCount > PAGE_SIZE ? (
        <div className="flex flex-wrap items-center justify-between gap-2" data-testid="transfer-pagination">
          <Button
            type="button"
            variant="outline"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            data-testid="transfer-prev"
          >
            {t("transfer.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {t("transfer.pageOf")
              .replace("{page}", String(page))
              .replace("{pages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="outline"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            data-testid="transfer-next"
          >
            {t("transfer.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
