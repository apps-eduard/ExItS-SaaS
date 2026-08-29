import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeftRight, ChevronRight } from "lucide-react";
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
import {
  branchDisplayName,
  formatTransferQty,
  formatTransferTimestamp,
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

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = query.data?.items ?? [];
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
      />

      <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="transfer-current-branch">
        {t("transfer.currentBranch")}: <strong>{currentBranchName}</strong>
      </p>

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}

      {allowManage && multiBranch ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("transfer.title")}
          testId="transfer-toolbar"
          items={[
            {
              key: "new",
              label: t("transfer.new"),
              icon: <ArrowLeftRight />,
              href: online ? "/inventory/transfers/new" : undefined,
              disabled: !online,
              testId: "transfer-new",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

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

      {query.isLoading ? <LoadingState label={t("transfer.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("transfer.errorTitle")} detail={t("transfer.loadFailed")} />
      ) : null}

      {!query.isLoading && !query.isError && items.length === 0 ? (
        <>
          <EmptyState
            title={t("transfer.empty")}
            detail={
              multiBranch ? t("transfer.emptyDetail") : t("transfer.singleBranchDetail")
            }
          />
          {allowManage && multiBranch && online ? (
            <Button asChild>
              <Link to="/inventory/transfers/new" data-testid="transfer-empty-cta">
                {t("transfer.new")}
              </Link>
            </Button>
          ) : null}
          {!multiBranch ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="transfer-single-branch">
              {t("transfer.requiresTwoBranches")}
            </p>
          ) : null}
        </>
      ) : null}

      {items.length > 0 ? (
        <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="transfer-list">
          {items.map((item) => {
            const source = branchDisplayName(item.sourceBranchName, item.sourceBranchId);
            const dest = branchDisplayName(item.destinationBranchName, item.destinationBranchId);
            return (
              <li key={item.transferId}>
                <Link
                  to={`/inventory/transfers/${item.transferId}`}
                  className="exits-list__card transfer-row block min-w-0 text-foreground no-underline"
                  data-testid={`transfer-row-${item.transferId}`}
                >
                  <span className="transfer-row__main min-w-0">
                    <span className="flex flex-wrap items-center gap-2">
                      <span className="font-semibold">
                        {item.transferNumber?.trim() || t("transfer.draftNumber")}
                      </span>
                      <StatusChip tone={inventoryTransferStatusTone(item.status)}>
                        {t(inventoryTransferStatusLabelKey(item.status))}
                      </StatusChip>
                    </span>
                    <span className="mt-1 block text-[length:var(--exits-text-sm)]">
                      {source} → {dest}
                    </span>
                    <span className="block text-[length:var(--exits-text-sm)] text-muted">
                      {t("transfer.linesCount").replace("{count}", String(item.lineCount))}
                      {" · "}
                      {t("transfer.sent")}: {formatTransferQty(item.totalSentQty)}
                      {" · "}
                      {t("transfer.received")}: {formatTransferQty(item.totalReceivedQty)}
                      {item.totalDifferenceQty !== 0
                        ? ` · ${t("transfer.difference")}: ${formatTransferQty(item.totalDifferenceQty)}`
                        : ""}
                    </span>
                    <span className="block text-[length:var(--exits-text-xs)] text-muted">
                      {formatTransferTimestamp(item.updatedAtUtc)}
                    </span>
                  </span>
                  <span className="transfer-row__aside flex shrink-0 items-center gap-2">
                    <ChevronRight className="size-5 text-muted" aria-hidden />
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
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
