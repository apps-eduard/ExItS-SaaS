import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ClipboardCheck } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  inspectInventoryTransferDamageCustody,
  inspectInventoryTransferExceptionCustody,
  listInventoryTransfersAwaitingInspection,
  type InventoryTransferAwaitingInspectionItemDto,
} from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableMobile,
  ExitsTableMobileRow,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { RESPONSIVE_DATA_TABLE_MIN_MD } from "@/components/exits/responsive-data-view";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  InventoryInspectDispositionPanel,
  isInspectDispositionReady,
} from "@/features/inventory/InventoryInspectDispositionPanel";
import {
  emptyInspectDraft,
  parseInspectDisposition,
  type InspectDispositionDraft,
} from "@/features/inventory/inventory-inspect-disposition";
import { inventoryAwaitingInspectionQueryKey } from "@/features/inventory/inventory-awaiting-inspection-nav";
import {
  formatTransferQty,
  formatTransferTimestamp,
} from "@/features/inventory/inventory-transfer-labels";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function custodyKindLabelKey(kind: string): MessageKey {
  return kind === "Exception"
    ? "inspectionQueue.kind.exception"
    : "inspectionQueue.kind.damage";
}

function statusLabelKey(status: string): MessageKey | null {
  switch (status) {
    case "ReceivedAtSource":
      return "transfer.custody.receivedAtSource";
    case "AwaitingInspection":
      return "transfer.custody.awaitingInspection";
    default:
      return null;
  }
}

function formatInspectionStatus(
  item: InventoryTransferAwaitingInspectionItemDto,
  t: (key: MessageKey) => string,
): string {
  const fromBranch = item.returnedFromBranchName?.trim();
  if (
    (item.status === "ReceivedAtSource" || item.status === "AwaitingInspection") &&
    fromBranch
  ) {
    return t("transfer.custody.returnedFromBranch").replace("{branch}", fromBranch);
  }
  const statusKey = statusLabelKey(item.status);
  return statusKey ? t(statusKey) : item.status;
}

function rowTitle(item: InventoryTransferAwaitingInspectionItemDto): string {
  return item.productName?.trim() || item.productId;
}

function confirmedLabelKey(kind: string): MessageKey {
  return kind === "Exception"
    ? "inspectionQueue.col.nonSellable"
    : "inspectionQueue.col.damaged";
}

export function InventoryAwaitingInspectionPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const { layout } = useResponsiveDataLayout({ tableMinWidthPx: RESPONSIVE_DATA_TABLE_MIN_MD });
  const [drafts, setDrafts] = useState<Record<string, InspectDispositionDraft>>({});
  const [inspectingId, setInspectingId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const workspace =
    boundWorkspace?.branchId != null
      ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
      : null;

  const query = useQuery({
    queryKey: inventoryAwaitingInspectionQueryKey(
      workspace?.organizationId,
      workspace?.branchId,
    ),
    enabled: Boolean(workspace) && online,
    staleTime: 30_000,
    queryFn: ({ signal }) => listInventoryTransfersAwaitingInspection(workspace!, signal),
  });

  const items = query.data?.items ?? [];

  useEffect(() => {
    setDrafts((prev) => {
      const next: Record<string, InspectDispositionDraft> = {};
      for (const item of items) {
        next[item.custodyId] = prev[item.custodyId] ?? emptyInspectDraft();
      }
      return next;
    });
    setInspectingId((current) =>
      current && items.some((item) => item.custodyId === current) ? current : null,
    );
  }, [items]);

  const canInspect = Boolean(workspace) && online && allowManage;

  function startInspect(item: InventoryTransferAwaitingInspectionItemDto) {
    setInspectingId(item.custodyId);
    setDrafts((prev) => ({
      ...prev,
      [item.custodyId]: prev[item.custodyId] ?? emptyInspectDraft(),
    }));
  }

  function cancelInspect(custodyId: string) {
    setInspectingId((current) => (current === custodyId ? null : current));
    setDrafts((prev) => ({ ...prev, [custodyId]: emptyInspectDraft() }));
  }

  async function onConfirm(item: InventoryTransferAwaitingInspectionItemDto) {
    if (!workspace || busyId || !canInspect) {
      return;
    }
    const draft = drafts[item.custodyId] ?? emptyInspectDraft();
    const parsed = parseInspectDisposition(draft, item.quantity);
    if (!parsed.ok) {
      showToast(t("inspectionQueue.inspectInvalidQty"), "error");
      return;
    }
    setBusyId(item.custodyId);
    try {
      if (item.custodyKind === "Exception") {
        await inspectInventoryTransferExceptionCustody(workspace, item.custodyId, {
          recoveredSellableQty: parsed.recovered,
          confirmedNonSellableQty: parsed.confirmed,
        });
      } else {
        await inspectInventoryTransferDamageCustody(workspace, item.custodyId, {
          recoveredSellableQty: parsed.recovered,
          confirmedDamagedQty: parsed.confirmed,
        });
      }
      await queryClient.invalidateQueries({
        queryKey: ["inventory-transfers", "awaiting-inspection"],
      });
      await queryClient.invalidateQueries({ queryKey: ["inventory-transfers"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      showToast(t("inspectionQueue.inspectSuccess"), "success");
      setInspectingId(null);
    } catch {
      showToast(t("inspectionQueue.inspectFailed"), "error");
    } finally {
      setBusyId(null);
    }
  }

  const header = useMemo(
    () => (
      <PageHeader
        title={t("inspectionQueue.title")}
        description={t("inspectionQueue.description")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
      />
    ),
    [t],
  );

  function renderDisposition(
    item: InventoryTransferAwaitingInspectionItemDto,
    rowBusy: boolean,
  ) {
    const draft = drafts[item.custodyId] ?? emptyInspectDraft();
    const ready = isInspectDispositionReady(draft, item.quantity);
    return (
      <div className="flex w-full min-w-0 flex-col gap-2">
        <InventoryInspectDispositionPanel
          totalQty={item.quantity}
          draft={draft}
          onChange={(next) =>
            setDrafts((prev) => ({
              ...prev,
              [item.custodyId]: next,
            }))
          }
          recoveredLabel={t("inspectionQueue.col.sellable")}
          confirmedLabel={t(confirmedLabelKey(item.custodyKind))}
          helpText={t("inspectionQueue.helpAllocate")}
          allSellableLabel={t("inspectionQueue.allSellable")}
          allDamagedLabel={
            item.custodyKind === "Exception"
              ? t("inspectionQueue.allNonSellable")
              : t("inspectionQueue.allDamaged")
          }
          mustEqualLabel={t("inspectionQueue.mustEqualQty")}
          disabled={!canInspect || rowBusy}
          testIdPrefix={`inspection-queue-${item.custodyId}`}
        />
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            intent="primary"
            appearance="solid"
            size="sm"
            disabled={!canInspect || rowBusy || busyId != null || !ready}
            onClick={() => void onConfirm(item)}
            data-testid={`inspection-queue-confirm-${item.custodyId}`}
          >
            <ClipboardCheck className="size-4 shrink-0" aria-hidden />
            {t("inspectionQueue.confirm")}
          </Button>
          <Button
            type="button"
            intent="danger"
            appearance="solid"
            emphasis="soft"
            size="sm"
            disabled={rowBusy}
            onClick={() => cancelInspect(item.custodyId)}
            data-testid={`inspection-queue-cancel-${item.custodyId}`}
          >
            {t("inspectionQueue.cancel")}
          </Button>
        </div>
      </div>
    );
  }

  if (!boundWorkspace?.branchId) {
    return <BranchRequiredPanel />;
  }

  if (!online) {
    return (
      <div className="space-y-4" data-testid="inspection-queue-page">
        {header}
        <EmptyState
          icon={<ClipboardCheck className="size-5" />}
          title={t("inspectionQueue.offlineTitle")}
          detail={t("inspectionQueue.offlineDescription")}
        />
      </div>
    );
  }

  if (query.isLoading) {
    return <LoadingState label={t("inspectionQueue.loading")} />;
  }

  if (query.isError) {
    return (
      <div className="space-y-4" data-testid="inspection-queue-page">
        {header}
        <ErrorState
          title={t("inspectionQueue.loadFailed")}
          detail={t("inspectionQueue.loadFailedDetail")}
          error={query.error}
          operation="listInventoryTransfersAwaitingInspection"
        />
      </div>
    );
  }

  return (
    <div className="space-y-4" data-testid="inspection-queue-page">
      {header}

      {items.length === 0 ? (
        <EmptyState
          icon={<ClipboardCheck className="size-5" />}
          title={t("inspectionQueue.emptyTitle")}
          detail={t("inspectionQueue.emptyDescription")}
          testId="inspection-queue-empty"
        />
      ) : (
        <ExitsResponsiveDataView
          layout={layout}
          table={
            <ExitsTableContainer>
              <ExitsTable data-testid="inspection-queue-table">
                <ExitsTableHeader>
                  <ExitsTableRow>
                    <ExitsTableHead>{t("inspectionQueue.col.product")}</ExitsTableHead>
                    <ExitsTableHead>{t("inspectionQueue.col.kind")}</ExitsTableHead>
                    <ExitsTableHead className="text-end">{t("inspectionQueue.col.qty")}</ExitsTableHead>
                    <ExitsTableHead>{t("inspectionQueue.col.transfer")}</ExitsTableHead>
                    <ExitsTableHead>{t("inspectionQueue.col.status")}</ExitsTableHead>
                    <ExitsTableHead>{t("inspectionQueue.col.action")}</ExitsTableHead>
                  </ExitsTableRow>
                </ExitsTableHeader>
                <ExitsTableBody>
                  {items.map((item) => {
                    const rowBusy = busyId === item.custodyId;
                    const inspecting = inspectingId === item.custodyId;
                    return (
                      <ExitsTableRow key={`${item.custodyKind}-${item.custodyId}`}>
                        <ExitsTableCell>
                          <Link
                            to={`/inventory/transfers/${item.transferId}`}
                            className="font-medium text-primary underline-offset-2 hover:underline"
                            data-testid={`inspection-queue-row-${item.custodyId}`}
                          >
                            {rowTitle(item)}
                          </Link>
                          {item.expectedProductName ? (
                            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                              {t("inspectionQueue.expected").replace(
                                "{name}",
                                item.expectedProductName,
                              )}
                            </p>
                          ) : null}
                        </ExitsTableCell>
                        <ExitsTableCell>{t(custodyKindLabelKey(item.custodyKind))}</ExitsTableCell>
                        <ExitsTableCell className="text-end tabular-nums">
                          {formatTransferQty(item.quantity)}
                        </ExitsTableCell>
                        <ExitsTableCell>
                          {item.transferNumber?.trim() || item.transferId.slice(0, 8)}
                        </ExitsTableCell>
                        <ExitsTableCell>
                          {formatInspectionStatus(item, t)}
                          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                            {formatTransferTimestamp(item.updatedAtUtc)}
                          </p>
                        </ExitsTableCell>
                        <ExitsTableCell className="min-w-[16rem]">
                          {inspecting ? (
                            renderDisposition(item, rowBusy)
                          ) : (
                            <Button
                              type="button"
                              intent="primary"
                              appearance="solid"
                              size="sm"
                              disabled={!canInspect || busyId != null}
                              onClick={() => startInspect(item)}
                              data-testid={`inspection-queue-inspect-${item.custodyId}`}
                            >
                              <ClipboardCheck className="size-4 shrink-0" aria-hidden />
                              {t("inspectionQueue.inspect")}
                            </Button>
                          )}
                        </ExitsTableCell>
                      </ExitsTableRow>
                    );
                  })}
                </ExitsTableBody>
              </ExitsTable>
            </ExitsTableContainer>
          }
          list={
            <ExitsTableMobile data-testid="inspection-queue-mobile">
              {items.map((item) => {
                const rowBusy = busyId === item.custodyId;
                const inspecting = inspectingId === item.custodyId;
                return (
                  <ExitsTableMobileRow key={`${item.custodyKind}-${item.custodyId}`}>
                    <div className="flex flex-col gap-2">
                      <Link
                        to={`/inventory/transfers/${item.transferId}`}
                        className="font-medium text-primary underline-offset-2 hover:underline"
                        data-testid={`inspection-queue-row-${item.custodyId}`}
                      >
                        {rowTitle(item)}
                      </Link>
                      <p className="exits-table-mobile__meta m-0">
                        {t(custodyKindLabelKey(item.custodyKind))} ·{" "}
                        {formatTransferQty(item.quantity)} ·{" "}
                        {item.transferNumber?.trim() || item.transferId.slice(0, 8)}
                      </p>
                      <p className="exits-table-mobile__meta m-0">
                        {formatInspectionStatus(item, t)}
                      </p>
                      {inspecting ? (
                        renderDisposition(item, rowBusy)
                      ) : (
                        <Button
                          type="button"
                          intent="primary"
                          appearance="solid"
                          disabled={!canInspect || busyId != null}
                          onClick={() => startInspect(item)}
                          data-testid={`inspection-queue-inspect-${item.custodyId}`}
                        >
                          <ClipboardCheck className="size-4 shrink-0" aria-hidden />
                          {t("inspectionQueue.inspect")}
                        </Button>
                      )}
                    </div>
                  </ExitsTableMobileRow>
                );
              })}
            </ExitsTableMobile>
          }
        />
      )}
    </div>
  );
}
