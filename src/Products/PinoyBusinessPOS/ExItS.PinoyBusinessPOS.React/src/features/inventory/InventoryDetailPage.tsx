import { useId, useMemo, useRef, useState, type ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarClock, ChevronDown, ChevronRight, PackageMinus, Trash2 } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  adjustInventoryStock,
  addOpeningStock,
  disableInventoryTracking,
  enableInventoryTracking,
  getInventoryProduct,
  getInventoryStockRollup,
  getStockMovement,
  listInventoryMovements,
  listProductLots,
  type PosInventoryAreaRollupDto,
  type PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import { getCatalogProduct } from "@/api/pos/pos-catalog-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { cn } from "@/lib/cn";
import {
  canAddOpeningStock,
  canDisableExpirationTracking,
  computeGoodQuantity,
  sortLotsByExpiry,
} from "@/features/inventory/inventory-detail-helpers";
import { computeOpeningStockValue } from "@/features/catalog/opening-stock-helpers";
import {
  comparePurchaseCostToSellingPrice,
  resolveEffectiveSellingPriceView,
} from "@/features/inventory/inventory-opening-price-feedback";
import {
  buildBranchNameById,
  resolveInventoryBranchDisplayName,
} from "@/features/inventory/inventory-branch-labels";
import { expirationSettingsPath } from "@/features/inventory/expiration-settings-routes";
import { InventoryLotList } from "@/features/inventory/InventoryLotList";
import { InventoryMovementsResponsiveList } from "@/features/inventory/InventoryMovementsResponsiveList";
import {
  requiresOpeningExpirationDate,
  resolveLotExpiryLabel,
  hasMissingExpiry,
  hasValidExpiryDateInput,
} from "@/features/inventory/inventory-lot-status";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { formatPeso } from "@/lib/format-money";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { pageBackNav } from "@/navigation/page-back-nav";
import { resolveAmbiguousMutationOutcome } from "@/runtime/ambiguous-mutation-outcome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const LOT_PAGE_SIZE = 50;

type DeductMode = "auto" | "manual";

function formatLotStatus(lot: PosInventoryLotDto, t: ReturnType<typeof useI18n>["t"]): string {
  const label = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
  switch (label.kind) {
    case "expired":
      return t("inventory.statusExpired");
    case "expiresToday":
      return t("inventory.statusExpiresToday");
    case "expiresInDays":
      return t("inventory.statusExpiresInDays").replace("{days}", String(label.days));
    case "ok":
      return t("inventory.statusGood");
    default:
      return label.status;
  }
}

export function InventoryDetailPage() {
  const { t } = useI18n();
  const { productId } = useParams();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const allowManageInventory = canManageInventory(sessionGrant);
  const [openingQty, setOpeningQty] = useState("0");
  const [openingUnitCost, setOpeningUnitCost] = useState("");
  const [openingExpiry, setOpeningExpiry] = useState("");
  const [openingLotNumber, setOpeningLotNumber] = useState("");
  const [adjustQty, setAdjustQty] = useState("");
  const [adjustReason, setAdjustReason] = useState("");
  const [adjustDirection, setAdjustDirection] = useState<"In" | "Out">("In");
  const [adjustExpiry, setAdjustExpiry] = useState("");
  const [adjustLotNumber, setAdjustLotNumber] = useState("");
  const [deductMode, setDeductMode] = useState<DeductMode>("auto");
  const [selectedLotId, setSelectedLotId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [adjusting, setAdjusting] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const [statusDetailsOpen, setStatusDetailsOpen] = useState(false);
  const [areaOverrides, setAreaOverrides] = useState<Record<string, boolean>>({});
  const movementIdRef = useRef<string | null>(null);
  const statusDetailsId = useId();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const accountQuery = useQuery({
    queryKey: ["inventory", "product", workspace?.organizationId, workspace?.branchId, productId],
    enabled: Boolean(workspace) && Boolean(productId),
    queryFn: ({ signal }) => getInventoryProduct(workspace!, productId!, signal),
  });

  const catalogPriceQuery = useQuery({
    queryKey: [
      "catalog",
      "product",
      "effective-price",
      workspace?.organizationId,
      workspace?.branchId,
      productId,
    ],
    enabled:
      Boolean(workspace) &&
      Boolean(productId) &&
      Boolean(accountQuery.data) &&
      (accountQuery.data.isTracked === false || canAddOpeningStock(accountQuery.data)),
    queryFn: ({ signal }) => getCatalogProduct(workspace!, productId!, signal),
  });

  const rollupQuery = useQuery({
    queryKey: ["inventory", "stock-rollup", workspace?.organizationId, productId],
    enabled: Boolean(workspace) && Boolean(productId) && accountQuery.data?.isTracked === true,
    queryFn: ({ signal }) => getInventoryStockRollup(workspace!, productId!, signal),
  });

  const rollup = rollupQuery.data;
  const tracksExpiration = accountQuery.data?.tracksExpiration === true;

  /** Partial-access staff never see authoritative organization figures — only their own branches. */
  const organizationTotals = {
    onHand: rollup?.organizationTotalsVisible
      ? (rollup.organizationOnHandQuantity ?? 0)
      : (rollup?.accessibleOnHandQuantity ?? 0),
    reserved: rollup?.organizationTotalsVisible
      ? (rollup.organizationReservedQuantity ?? 0)
      : (rollup?.accessibleReservedQuantity ?? 0),
    available: rollup?.organizationTotalsVisible
      ? (rollup.organizationAvailableQuantity ?? 0)
      : (rollup?.accessibleAvailableQuantity ?? 0),
  };

  /** Areas start compact; the area holding the working branch opens first. */
  function isAreaExpanded(area: PosInventoryAreaRollupDto): boolean {
    const key = area.areaId ?? "unassigned";
    const override = areaOverrides[key];
    if (override !== undefined) {
      return override;
    }
    const currentBranchId = boundWorkspace?.branchId?.toLowerCase() ?? null;
    return (
      currentBranchId !== null &&
      area.branches.some((row) => row.branchId.toLowerCase() === currentBranchId)
    );
  }

  function toggleArea(key: string, expanded: boolean) {
    setAreaOverrides((prev) => ({ ...prev, [key]: !expanded }));
  }

  const branchLabel = boundWorkspace?.branchName ?? t("transfer.currentBranch");

  const branchNameById = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === workspace?.organizationId);
    return buildBranchNameById(org?.branches ?? []);
  }, [workspaces, workspace?.organizationId]);

  const movementsQuery = useQuery({
    queryKey: ["inventory", "movements", workspace?.organizationId, workspace?.branchId, productId],
    enabled: Boolean(workspace) && Boolean(productId),
    queryFn: ({ signal }) => listInventoryMovements(workspace!, productId!, {}, signal),
  });

  const lotsQuery = useInfiniteQuery({
    queryKey: ["inventory", "lots", workspace?.organizationId, workspace?.branchId, productId],
    enabled: Boolean(workspace) && Boolean(productId) && tracksExpiration,
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      listProductLots(
        workspace!,
        productId!,
        { includeDepleted: false, page: pageParam, pageSize: LOT_PAGE_SIZE },
        signal,
      ),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.totalCount ? lastPage.page + 1 : undefined;
    },
  });

  const lots = useMemo(
    () => sortLotsByExpiry(lotsQuery.data?.pages.flatMap((page) => page.items) ?? []),
    [lotsQuery.data],
  );

  const movementActorIds = useMemo(
    () => (movementsQuery.data?.items ?? []).map((movement) => movement.recordedBy),
    [movementsQuery.data],
  );
  const actors = useActorDirectory(workspace?.organizationId, movementActorIds);

  const selectedLot = lots.find((lot) => lot.lotId === selectedLotId) ?? null;

  async function invalidateInventory() {
    await queryClient.invalidateQueries({ queryKey: ["inventory"] });
    await queryClient.invalidateQueries({ queryKey: ["catalog"] });
  }

  const enableMutation = useMutation({
    mutationFn: () => {
      const qty = Number(openingQty);
      const openingQuantity = Number.isNaN(qty) || qty <= 0 ? null : qty;
      if (
        requiresOpeningExpirationDate(tracksExpiration, openingQuantity) &&
        !openingExpiry.trim()
      ) {
        throw new Error(t("inventory.expirationDateRequired"));
      }
      if (openingQuantity) {
        const unitCost = Number(openingUnitCost);
        if (!openingUnitCost.trim() || Number.isNaN(unitCost) || unitCost <= 0) {
          throw new Error(t("openingStock.unitCostRequired"));
        }
        return enableInventoryTracking(workspace!, productId!, {
          openingQuantity,
          unitCost,
          expirationDate:
            tracksExpiration && openingExpiry.trim() ? openingExpiry.trim() : null,
          lotNumber: tracksExpiration && openingLotNumber.trim() ? openingLotNumber.trim() : null,
        });
      }
      return enableInventoryTracking(workspace!, productId!, {
        openingQuantity: 0,
      });
    },
    onSuccess: async () => {
      setError(null);
      setOpeningUnitCost("");
      setOpeningExpiry("");
      setOpeningLotNumber("");
      await invalidateInventory();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const addOpeningStockMutation = useMutation({
    mutationFn: () => {
      const quantity = Number(openingQty);
      const unitCost = Number(openingUnitCost);
      if (!openingQty.trim() || Number.isNaN(quantity) || quantity <= 0) {
        throw new Error(t("openingStock.quantityRequired"));
      }
      if (!openingUnitCost.trim() || Number.isNaN(unitCost) || unitCost <= 0) {
        throw new Error(t("openingStock.unitCostRequired"));
      }
      if (requiresOpeningExpirationDate(tracksExpiration, quantity) && !openingExpiry.trim()) {
        throw new Error(t("inventory.expirationDateRequired"));
      }
      return addOpeningStock(workspace!, productId!, {
        openingQuantity: quantity,
        unitCost,
        expirationDate:
          tracksExpiration && openingExpiry.trim() ? openingExpiry.trim() : null,
        lotNumber: tracksExpiration && openingLotNumber.trim() ? openingLotNumber.trim() : null,
      });
    },
    onSuccess: async () => {
      setError(null);
      setOpeningQty("");
      setOpeningUnitCost("");
      setOpeningExpiry("");
      setOpeningLotNumber("");
      await invalidateInventory();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const disableMutation = useMutation({
    mutationFn: () => disableInventoryTracking(workspace!, productId!),
    onSuccess: async () => {
      setError(null);
      await invalidateInventory();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  async function onAdjust() {
    if (!workspace || !productId || statusLocked || adjusting) {
      return;
    }

    const qty = Number(adjustQty);
    if (!adjustQty.trim() || Number.isNaN(qty) || qty <= 0) {
      setError(t("inventory.quantityRequired"));
      return;
    }

    const reason = adjustReason.trim();
    if (!reason) {
      setError(t("inventory.reasonRequired"));
      return;
    }

    if (tracksExpiration && adjustDirection === "In" && !adjustExpiry.trim()) {
      setError(t("inventory.expirationDateRequired"));
      return;
    }

    if (tracksExpiration && adjustDirection === "Out" && deductMode === "manual") {
      if (!selectedLotId) {
        setError(t("inventory.lotRequired"));
        return;
      }
      if (selectedLot && qty > selectedLot.quantityOnHand) {
        setError(t("inventory.lotQuantityExceeded"));
        return;
      }
    }

    setAdjusting(true);
    setError(null);
    try {
      if (!movementIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) {
          setError(t("inventory.reasonRequired"));
          return;
        }
        movementIdRef.current = generated.id;
      }
      const movementId = movementIdRef.current;
      await adjustInventoryStock(workspace, productId, {
        direction: adjustDirection,
        quantity: qty,
        reason,
        movementId,
        expirationDate:
          tracksExpiration && adjustDirection === "In" ? adjustExpiry.trim() || null : null,
        lotNumber:
          tracksExpiration && adjustDirection === "In" && adjustLotNumber.trim()
            ? adjustLotNumber.trim()
            : null,
        lotId:
          tracksExpiration && adjustDirection === "Out" && deductMode === "manual" && selectedLotId
            ? selectedLotId
            : null,
      });
      movementIdRef.current = null;
      setStatusLocked(false);
      setAdjustQty("");
      setAdjustReason("");
      setAdjustExpiry("");
      setAdjustLotNumber("");
      setSelectedLotId("");
      setError(null);
      await invalidateInventory();
    } catch (err) {
      const movementId = movementIdRef.current;
      if (movementId && workspace) {
        setError(t("checkout.confirmingTransaction"));
        const outcome = await resolveAmbiguousMutationOutcome({
          error: err,
          lookup: () => getStockMovement(workspace, movementId),
        });
        if (outcome.kind === "confirmed") {
          movementIdRef.current = null;
          setStatusLocked(false);
          setAdjustQty("");
          setAdjustReason("");
          setAdjustExpiry("");
          setAdjustLotNumber("");
          setSelectedLotId("");
          setError(null);
          await invalidateInventory();
          return;
        }
        if (outcome.kind === "still_unknown") {
          setStatusLocked(true);
          setError(t("checkout.transactionStatusUnknown"));
          return;
        }
        if (outcome.kind === "not_found") {
          setError(describePosApiError(outcome.lookupError, t, "error.detail"));
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : describePosApiError(err, t, "error.detail"),
      );
    } finally {
      setAdjusting(false);
    }
  }

  if (!workspace || accountQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (accountQuery.isError) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={describePosApiError(accountQuery.error, t, "inventory.notFound")}
        error={accountQuery.error}
        operation="load inventory product"
      />
    );
  }

  const account = accountQuery.data;
  if (!account) {
    return <ErrorState title={t("error.title")} detail={t("inventory.notFound")} />;
  }

  const goodQuantity = computeGoodQuantity(account);
  const showAddOpeningStock = canAddOpeningStock(account);
  const canDisableInventory = canDisableExpirationTracking(account);
  const openingStockValue = computeOpeningStockValue(
    Number(openingQty),
    Number(openingUnitCost),
  );
  const effectiveSelling = catalogPriceQuery.data
    ? resolveEffectiveSellingPriceView(catalogPriceQuery.data)
    : null;
  const purchaseCostFeedback = comparePurchaseCostToSellingPrice(
    openingUnitCost,
    effectiveSelling?.amount,
  );
  const sellingPriceAwareness = effectiveSelling ? (
    <div
      className="inventory-detail-selling-price flex flex-col gap-0.5"
      data-testid="inventory-current-selling-price"
    >
      <span className="text-[length:var(--exits-text-sm)] font-semibold">
        {t("inventory.currentSellingPrice")}
      </span>
      <p className="m-0 text-[length:var(--exits-text-md)] font-medium">
        {formatPeso(effectiveSelling.amount)} / {account.unitOfMeasure}
      </p>
      <span
        className="text-[length:var(--exits-text-xs)] text-muted"
        data-testid="inventory-selling-price-source"
      >
        {effectiveSelling.source === "branch"
          ? t("inventory.sellingPriceBranch")
          : t("inventory.sellingPriceOrganization")}
      </span>
      <Button asChild type="button" variant="ghost" className="mt-1 w-fit px-0">
        <Link
          to={`/catalog/products/${account.productId}/edit`}
          data-testid="inventory-review-selling-price"
        >
          {t("inventory.reviewSellingPrice")}
        </Link>
      </Button>
    </div>
  ) : null;
  const purchaseCostAwareness =
    purchaseCostFeedback.kind === "zeroMargin" ? (
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="inventory-purchase-cost-zero-margin"
      >
        {t("inventory.purchaseCostZeroMargin")}
      </p>
    ) : purchaseCostFeedback.kind === "higherCost" ? (
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-warning,#b45309)]"
        role="status"
        data-testid="inventory-purchase-cost-high-warning"
      >
        {t("inventory.purchaseCostHigherThanSelling")}{" "}
        {t("inventory.purchaseCostHigherBy").replace(
          "{amount}",
          formatPeso(purchaseCostFeedback.difference),
        )}
      </p>
    ) : null;
  const formatStatus = (lot: PosInventoryLotDto) => formatLotStatus(lot, t);
  const lotTotal = lots.reduce((sum, lot) => sum + (lot.quantityOnHand ?? 0), 0);
  const needsExpirationSetup = hasMissingExpiry(
    tracksExpiration,
    account.onHandQuantity,
    lotsQuery.isLoading ? null : lotTotal,
  );
  const openingQuantityValue = Number(openingQty);
  const openingRequiresExpiry = requiresOpeningExpirationDate(
    tracksExpiration,
    openingQuantityValue,
  );
  const openingExpiryReady =
    !openingRequiresExpiry || hasValidExpiryDateInput(openingExpiry);
  const adjustQuantityValue = Number(adjustQty);
  const adjustInRequiresExpiry =
    tracksExpiration &&
    adjustDirection === "In" &&
    Number.isFinite(adjustQuantityValue) &&
    adjustQuantityValue > 0;
  const adjustExpiryReady =
    !adjustInRequiresExpiry || hasValidExpiryDateInput(adjustExpiry);

  const expirationPendingCard =
    tracksExpiration && !showAddOpeningStock && needsExpirationSetup ? (
      <Card
        className="flex flex-col gap-3 p-3"
        data-testid="inventory-expiration-pending"
      >
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {t("inventory.missingExpirationShort")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("inventory.expirationPendingSummary")
            .replace("{qty}", String(account.onHandQuantity))
            .replace("{uom}", account.unitOfMeasure)}
        </p>
      </Card>
    ) : null;

  const expirationSummaryStrip =
    tracksExpiration && !showAddOpeningStock && !needsExpirationSetup ? (
      <div
        className="inventory-expiration-summary-strip"
        data-testid="inventory-expiration-summary"
      >
        <span className="inventory-expiration-summary-strip__title">
          {t("inventory.expirationInventory")}
        </span>
        <span className="inventory-expiration-summary-strip__onhand">
          {account.onHandQuantity} {account.unitOfMeasure} {t("inventory.onHandSummary")}
        </span>
        <div
          className="inventory-expiry-counts inventory-expiration-summary-strip__counts"
          data-testid="inventory-expiry-totals"
        >
          <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--good">
            {t("inventory.statusGood")} {goodQuantity}
          </span>
          <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--near">
            {t("inventory.nearExpiryQty")} {account.nearExpiryQuantity ?? 0}
          </span>
          <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--expired">
            {t("inventory.expiredQty")} {account.expiredQuantity ?? 0}
          </span>
        </div>
      </div>
    ) : null;

  const actionLinkContent = (icon: ReactNode, label: string) => (
    <>
      <span className="inventory-detail-action-btn__icon" aria-hidden>
        {icon}
      </span>
      <span className="inventory-detail-action-btn__label">{label}</span>
      <ChevronRight className="inventory-detail-action-btn__caret size-4 shrink-0" aria-hidden />
    </>
  );

  const lotsPanel =
    tracksExpiration && !showAddOpeningStock && !needsExpirationSetup ? (
      <Card className="inventory-lots-panel flex flex-col gap-2 p-3" data-testid="inventory-lots">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("inventory.stockLots")}
        </h2>
        {lotsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {lots.length === 0 && !lotsQuery.isLoading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.lotsEmptyHint")}
          </p>
        ) : (
          <InventoryLotList
            lots={lots}
            unitOfMeasure={account.unitOfMeasure}
            formatStatus={formatStatus}
          />
        )}
        {lotsQuery.hasNextPage ? (
          <Button
            type="button"
            variant="ghost"
            className="w-fit"
            disabled={lotsQuery.isFetchingNextPage}
            onClick={() => void lotsQuery.fetchNextPage()}
            data-testid="inventory-lots-load-more"
          >
            {lotsQuery.isFetchingNextPage
              ? t("inventory.loadingMore")
              : t("inventory.loadMore")}
          </Button>
        ) : null}
      </Card>
    ) : null;

  const adjustApplyLabel = adjusting
    ? t("checkout.confirmingTransaction")
    : adjustDirection === "In"
      ? t("inventory.applyIncrease")
      : t("inventory.applyDecrease");

  return (
    <div
      className="inventory-detail-page flex min-w-0 flex-col gap-3"
      data-testid="inventory-detail-page"
    >
      <PageHeader
        title={account.name}
        description={t("inventory.detailLede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <Card className="overflow-hidden p-0" data-testid="inventory-status">
        {account.isTracked ? (
          <>
            <button
              type="button"
              className="flex w-full items-center justify-between gap-3 px-3 py-3 text-left transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
              aria-expanded={statusDetailsOpen}
              aria-controls={statusDetailsId}
              onClick={() => setStatusDetailsOpen((open) => !open)}
              data-testid="inventory-status-toggle"
            >
              <span className="min-w-0 font-semibold leading-snug" data-testid="inventory-on-hand">
                {t("inventory.onHandAtBranch")
                  .replace("{branch}", branchLabel)
                  .replace("{qty}", String(account.onHandQuantity))
                  .replace("{uom}", account.unitOfMeasure)}
              </span>
              <ChevronDown
                aria-hidden
                className={cn(
                  "size-5 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)]",
                  statusDetailsOpen && "rotate-180",
                )}
              />
            </button>

            {statusDetailsOpen ? (
              <div
                id={statusDetailsId}
                className="flex flex-col gap-3 border-t border-border px-3 pt-3 pb-3"
                data-testid="inventory-status-details"
              >
                {rollup?.isTracked ? (
                  <div
                    className="flex flex-col gap-2"
                    data-testid="inventory-organization-summary"
                  >
                    <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                      {rollup.organizationTotalsVisible
                        ? t("inventory.organizationInventory")
                        : t("inventory.accessibleInventory")}
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="inventory-org-on-hand">
                      {t(
                        rollup.organizationTotalsVisible
                          ? "inventory.organizationOnHand"
                          : "inventory.accessibleOnHand",
                      )
                        .replace("{qty}", String(organizationTotals.onHand))
                        .replace("{uom}", account.unitOfMeasure)}
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="inventory-org-reserved">
                      {t(
                        rollup.organizationTotalsVisible
                          ? "inventory.organizationReserved"
                          : "inventory.accessibleReserved",
                      ).replace("{qty}", String(organizationTotals.reserved))}
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="inventory-org-available">
                      {t(
                        rollup.organizationTotalsVisible
                          ? "inventory.organizationAvailable"
                          : "inventory.accessibleAvailable",
                      ).replace("{qty}", String(organizationTotals.available))}
                    </p>
                    {!rollup.organizationTotalsVisible ? (
                      <p
                        className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid="inventory-accessible-scope-note"
                      >
                        {t("inventory.accessibleScopeNote")}
                      </p>
                    ) : null}
                    {rollup.areas.length > 0 ? (
                      <div className="flex flex-col gap-2" data-testid="inventory-branch-breakdown">
                        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                          {rollup.hasAreas
                            ? t("inventory.areaBreakdown")
                            : t("inventory.branchBreakdown")}
                        </p>
                        <ul className="m-0 flex list-none flex-col gap-2 p-0">
                          {rollup.areas.map((area) => {
                            const key = area.areaId ?? "unassigned";
                            const expanded = isAreaExpanded(area);
                            const branchRows = (
                              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                                {area.branches.map((row) => {
                                  const name = resolveInventoryBranchDisplayName({
                                    branchId: row.branchId,
                                    branchNameById,
                                    currentBranchId: workspace?.branchId,
                                    currentBranchName: boundWorkspace?.branchName,
                                    unknownLabel:
                                      row.branchName || t("inventory.branchNameUnknown"),
                                  });
                                  const isCurrent =
                                    Boolean(workspace?.branchId) &&
                                    row.branchId.toLowerCase() ===
                                      workspace!.branchId.toLowerCase();
                                  return (
                                    <li
                                      key={row.branchId}
                                      className="rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-2.5"
                                      data-testid={`inventory-branch-row-${row.branchId}`}
                                    >
                                      <p className="m-0 font-medium leading-snug text-foreground">
                                        {name}
                                        {isCurrent ? (
                                          <span className="ml-1.5 text-[length:var(--exits-text-xs)] font-normal text-muted">
                                            ({t("inventory.thisBranch")})
                                          </span>
                                        ) : null}
                                      </p>
                                      <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                                        {t("inventory.branchBreakdownMetrics")
                                          .replace("{onHand}", String(row.onHandQuantity))
                                          .replace("{reserved}", String(row.reservedQuantity))
                                          .replace("{available}", String(row.availableQuantity))}
                                      </p>
                                    </li>
                                  );
                                })}
                              </ul>
                            );

                            if (!rollup.hasAreas) {
                              return (
                                <li key={key} className="m-0">
                                  {branchRows}
                                </li>
                              );
                            }

                            return (
                              <li
                                key={key}
                                className="overflow-hidden rounded-[var(--exits-radius-md)] border border-border"
                                data-testid={`inventory-area-row-${key}`}
                              >
                                <button
                                  type="button"
                                  className="flex w-full items-center justify-between gap-2 px-3 py-2.5 text-left transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
                                  aria-expanded={expanded}
                                  onClick={() => toggleArea(key, expanded)}
                                  data-testid={`inventory-area-toggle-${key}`}
                                >
                                  <span className="min-w-0">
                                    <span className="block truncate font-medium text-foreground">
                                      {area.isUnassigned
                                        ? t("areas.unassigned")
                                        : (area.areaName ?? t("areas.singular"))}
                                    </span>
                                    <span
                                      className="block text-[length:var(--exits-text-sm)] text-muted"
                                      data-testid={`inventory-area-metrics-${key}`}
                                    >
                                      {t("inventory.branchBreakdownMetrics")
                                        .replace("{onHand}", String(area.onHandQuantity))
                                        .replace("{reserved}", String(area.reservedQuantity))
                                        .replace("{available}", String(area.availableQuantity))}
                                    </span>
                                  </span>
                                  <ChevronDown
                                    aria-hidden
                                    className={cn(
                                      "size-5 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)]",
                                      expanded && "rotate-180",
                                    )}
                                  />
                                </button>
                                {expanded ? (
                                  <div className="border-t border-border px-3 py-2.5">
                                    {branchRows}
                                  </div>
                                ) : null}
                              </li>
                            );
                          })}
                        </ul>
                      </div>
                    ) : null}
                  </div>
                ) : null}

                <p
                  className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
                  data-testid="inventory-expiration-status"
                >
                  {tracksExpiration
                    ? t("inventory.expirationTrackingOnWithWarning").replace(
                        "{days}",
                        String(account.expirationWarningDays ?? 7),
                      )
                    : t("inventory.expirationTrackingOff")}
                </p>

                {needsExpirationSetup ? (
                  <div
                    className="flex flex-col gap-2"
                    data-testid="inventory-expiration-setup-required"
                  >
                    <p
                      className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
                      data-testid="inventory-missing-expiry-badge"
                    >
                      {t("inventory.missingExpirationShort")}
                    </p>
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("inventory.expirationSetupRequiredDetail")}
                    </p>
                  </div>
                ) : null}
              </div>
            ) : null}
          </>
        ) : (
          <div className="flex flex-col gap-2 p-3">
            <p className="m-0 font-semibold">{t("inventory.notTracked")}</p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("inventory.untrackedHint")}
            </p>
          </div>
        )}
      </Card>

      {account.isTracked && allowManageInventory ? (
        <Card
          className="inventory-detail-actions-card flex flex-col gap-2.5 p-3"
          data-testid="inventory-quick-actions"
        >
          {expirationSummaryStrip}
          <div className="inventory-detail-quick-actions">
            {needsExpirationSetup ? (
              <>
                <Button asChild type="button" className="inventory-detail-action-btn inventory-detail-action-btn--solid w-full">
                  <Link
                    to={expirationSettingsPath(productId!, "assign")}
                    data-testid="inventory-expiration-setup-assign"
                  >
                    {actionLinkContent(
                      <CalendarClock className="size-4 shrink-0" />,
                      t("inventory.assignExpirationDates"),
                    )}
                  </Link>
                </Button>
                <Button
                  asChild
                  type="button"
                  variant="outline"
                  className="inventory-detail-action-btn w-full"
                >
                  <Link
                    to={expirationSettingsPath(productId!, "warning")}
                    data-testid="inventory-manage-expiration"
                  >
                    {actionLinkContent(
                      <CalendarClock className="size-4 shrink-0" />,
                      t("inventory.manageExpirationSettings"),
                    )}
                  </Link>
                </Button>
              </>
            ) : tracksExpiration ? (
              <Button
                asChild
                type="button"
                variant="outline"
                className="inventory-detail-action-btn w-full"
              >
                <Link
                  to={expirationSettingsPath(productId!, "warning")}
                  data-testid="inventory-manage-expiration"
                >
                  {actionLinkContent(
                    <CalendarClock className="size-4 shrink-0" />,
                    t("inventory.manageExpirationSettings"),
                  )}
                </Link>
              </Button>
            ) : (
              <Button asChild type="button" className="inventory-detail-action-btn inventory-detail-action-btn--solid w-full">
                <Link
                  to={expirationSettingsPath(productId!)}
                  data-testid="inventory-enable-expiration"
                >
                  {actionLinkContent(
                    <CalendarClock className="size-4 shrink-0" />,
                    t("inventory.enableExpirationTracking"),
                  )}
                </Link>
              </Button>
            )}
            <Button
              asChild
              type="button"
              variant="outline"
              className="inventory-detail-action-btn w-full"
            >
              <Link
                to={`/inventory/stock-use/new?productId=${encodeURIComponent(account.productId)}`}
                data-testid="inventory-record-stock-use"
              >
                {actionLinkContent(
                  <PackageMinus className="size-4 shrink-0" />,
                  t("inventory.recordStockUse"),
                )}
              </Link>
            </Button>
            <Button
              asChild
              type="button"
              variant="outline"
              className="inventory-detail-action-btn w-full"
            >
              <Link
                to={`/inventory/waste-loss/new?productId=${encodeURIComponent(account.productId)}`}
                data-testid="inventory-record-waste-loss"
              >
                {actionLinkContent(
                  <Trash2 className="size-4 shrink-0" />,
                  t("inventory.recordWasteLoss"),
                )}
              </Link>
            </Button>
          </div>
        </Card>
      ) : account.isTracked ? (
        expirationSummaryStrip
      ) : null}

      {!account.isTracked ? (
        allowManageInventory ? (
        <Card className="flex flex-col gap-3 p-3" data-testid="inventory-enable-tracking">
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("inventory.enableTracking")}
          </h2>
          <div className="inventory-detail-opening-fields">
            <Input
              label={t("inventory.openingQuantityOptional")}
              name="openingQuantity"
              inputMode="decimal"
              value={openingQty}
              onChange={(e) => setOpeningQty(e.target.value)}
            />
            {Number(openingQty) > 0 ? (
              <Input
                label={`${t("inventory.unitPurchaseCost")} (₱ / ${account.unitOfMeasure})`}
                name="openingUnitCost"
                inputMode="decimal"
                value={openingUnitCost}
                onChange={(e) => setOpeningUnitCost(e.target.value)}
                data-testid="inventory-enable-unit-cost"
              />
            ) : null}
          </div>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.openingHint")}
          </p>
          {sellingPriceAwareness}
          {Number(openingQty) > 0 ? (
            <>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("openingStock.unitCostHelper")}
              </p>
              {purchaseCostAwareness}
              {openingStockValue !== null ? (
                <p
                  className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
                  data-testid="inventory-enable-stock-value"
                >
                  {t("inventory.stockValue")}: ₱{openingStockValue.toFixed(2)}
                </p>
              ) : null}
            </>
          ) : null}
          {tracksExpiration && Number(openingQty) > 0 ? (
            <>
              <div className="inventory-detail-opening-fields">
                <Input
                  label={t("inventory.expirationDate")}
                  name="openingExpirationDate"
                  type="date"
                  value={openingExpiry}
                  onChange={(e) => setOpeningExpiry(e.target.value)}
                  data-testid="inventory-opening-expiry"
                />
                <Input
                  label={t("inventory.batchLotNumber")}
                  name="openingLotNumber"
                  value={openingLotNumber}
                  onChange={(e) => setOpeningLotNumber(e.target.value)}
                />
              </div>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("inventory.openingExpiryHint")}
              </p>
            </>
          ) : null}
          <Button
            type="button"
            disabled={
              enableMutation.isPending ||
              (Number(openingQty) > 0 &&
                (!openingUnitCost.trim() ||
                  !Number.isFinite(Number(openingUnitCost)) ||
                  Number(openingUnitCost) <= 0)) ||
              !openingExpiryReady
            }
            onClick={() => enableMutation.mutate()}
            data-testid="inventory-enable"
          >
            {t("inventory.enable")}
          </Button>
        </Card>
        ) : null
      ) : showAddOpeningStock && allowManageInventory ? (
        <>
          <Card className="flex flex-col gap-3 p-3" data-testid="inventory-add-opening-stock">
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("inventory.addOpeningStockTitle").replace("{location}", branchLabel)}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("inventory.addOpeningStockHint")}
            </p>
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="inventory-opening-vs-purchase-hint"
            >
              {t("inventory.openingVsPurchaseHint")}
            </p>
            {sellingPriceAwareness}
            <div className="inventory-detail-opening-fields">
              <Input
                label={`${t("openingStock.quantity")} (${account.unitOfMeasure})`}
                name="openingQuantity"
                inputMode="decimal"
                value={openingQty}
                onChange={(e) => setOpeningQty(e.target.value)}
                data-testid="inventory-opening-quantity"
              />
              <Input
                label={`${t("inventory.unitPurchaseCost")} (₱ / ${account.unitOfMeasure})`}
                name="openingUnitCost"
                inputMode="decimal"
                value={openingUnitCost}
                onChange={(e) => setOpeningUnitCost(e.target.value)}
                data-testid="inventory-opening-unit-cost"
              />
            </div>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("openingStock.unitCostHelper")}
            </p>
            {purchaseCostAwareness}
            {openingStockValue !== null ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
                data-testid="inventory-opening-stock-value"
              >
                {t("inventory.stockValue")}: ₱{openingStockValue.toFixed(2)}
              </p>
            ) : null}
            {tracksExpiration && Number(openingQty) > 0 ? (
              <>
                <div className="inventory-detail-opening-fields">
                  <Input
                    label={t("inventory.expirationDate")}
                    name="openingExpirationDate"
                    type="date"
                    value={openingExpiry}
                    onChange={(e) => setOpeningExpiry(e.target.value)}
                    data-testid="inventory-opening-expiry"
                  />
                  <Input
                    label={t("inventory.batchLotNumber")}
                    name="openingLotNumber"
                    value={openingLotNumber}
                    onChange={(e) => setOpeningLotNumber(e.target.value)}
                  />
                </div>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.openingExpiryHint")}
                </p>
              </>
            ) : null}
            <Button
              type="button"
              className="w-fit"
              disabled={
                addOpeningStockMutation.isPending ||
                !(openingQuantityValue > 0) ||
                !openingUnitCost.trim() ||
                !Number.isFinite(Number(openingUnitCost)) ||
                Number(openingUnitCost) <= 0 ||
                !openingExpiryReady
              }
              onClick={() => addOpeningStockMutation.mutate()}
              data-testid="inventory-add-opening-stock-submit"
            >
              {t("inventory.addOpeningStock")}
            </Button>
          </Card>
          <Button
            type="button"
            variant="ghost"
            className="w-fit"
            disabled={disableMutation.isPending || !canDisableInventory}
            onClick={() => disableMutation.mutate()}
            data-testid="inventory-disable"
          >
            {t("inventory.disable")}
          </Button>
        </>
      ) : (
        <>
          {expirationPendingCard}

          {allowManageInventory || lotsPanel ? (
            <div
              className={cn(
                "inventory-detail-workspace",
                Boolean(lotsPanel && allowManageInventory) && "inventory-detail-workspace--split",
              )}
              data-testid="inventory-detail-workspace"
            >
              {allowManageInventory ? (
                <div className="inventory-detail-workspace__adjust flex min-w-0 flex-col gap-2">
                  <Card
                    className="inventory-adjust-form flex flex-col gap-2.5 p-3"
                    data-testid="inventory-adjust-form"
                  >
                    <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                      {t("inventory.stockAdjustment")}
                    </h2>

                    <fieldset className="m-0 border-0 p-0">
                      <legend className="mb-1 text-[length:var(--exits-text-sm)] font-semibold">
                        {t("inventory.direction")}
                      </legend>
                      <div
                        className="inventory-adjust-direction flex flex-wrap gap-1.5"
                        data-testid="inventory-adjust-direction"
                      >
                        <label className="inventory-direction-option">
                          <input
                            type="radio"
                            name="adjustDirection"
                            value="In"
                            checked={adjustDirection === "In"}
                            onChange={() => {
                              setAdjustDirection("In");
                              setSelectedLotId("");
                            }}
                          />
                          <span>{t("inventory.adjustIn")}</span>
                        </label>
                        <label className="inventory-direction-option">
                          <input
                            type="radio"
                            name="adjustDirection"
                            value="Out"
                            checked={adjustDirection === "Out"}
                            onChange={() => setAdjustDirection("Out")}
                          />
                          <span>{t("inventory.adjustOut")}</span>
                        </label>
                      </div>
                    </fieldset>

                    <div className="inventory-adjust-qty">
                      <Input
                        label={t("inventory.adjustQuantityRequired")}
                        name="adjustQuantity"
                        inputMode="decimal"
                        value={adjustQty}
                        onChange={(e) => setAdjustQty(e.target.value)}
                      />
                      <span
                        className="inventory-adjust-qty__uom-chip"
                        aria-hidden="true"
                        data-testid="inventory-adjust-uom"
                      >
                        {account.unitOfMeasure}
                      </span>
                    </div>

                    {tracksExpiration && adjustDirection === "In" ? (
                      <div className="flex flex-col gap-2" data-testid="inventory-stock-details">
                        <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                          {t("inventory.stockDetails")}
                        </h3>
                        <div className="inventory-detail-opening-fields">
                          <Input
                            label={t("inventory.expirationDateRequiredLabel")}
                            name="adjustExpirationDate"
                            type="date"
                            value={adjustExpiry}
                            onChange={(e) => setAdjustExpiry(e.target.value)}
                            data-testid="inventory-adjust-expiry"
                          />
                          <Input
                            label={t("inventory.batchLotNumber")}
                            name="adjustLotNumber"
                            value={adjustLotNumber}
                            onChange={(e) => setAdjustLotNumber(e.target.value)}
                          />
                        </div>
                        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                          {t("inventory.stockInExpiryHint")}
                        </p>
                      </div>
                    ) : null}

                    {tracksExpiration && adjustDirection === "Out" ? (
                      <fieldset className="m-0 border-0 p-0" data-testid="inventory-deduct-mode">
                        <legend className="mb-1 text-[length:var(--exits-text-sm)] font-semibold">
                          {t("inventory.deductFrom")}
                        </legend>
                        <div className="flex flex-col gap-1.5">
                          <label className="inventory-direction-option">
                            <input
                              type="radio"
                              name="deductMode"
                              value="auto"
                              checked={deductMode === "auto"}
                              onChange={() => {
                                setDeductMode("auto");
                                setSelectedLotId("");
                              }}
                              data-testid="inventory-deduct-auto"
                            />
                            <span>{t("inventory.deductAutoFefo")}</span>
                          </label>
                          <label className="inventory-direction-option">
                            <input
                              type="radio"
                              name="deductMode"
                              value="manual"
                              checked={deductMode === "manual"}
                              onChange={() => setDeductMode("manual")}
                              data-testid="inventory-deduct-manual"
                            />
                            <span>{t("inventory.deductChooseLot")}</span>
                          </label>
                        </div>
                        <p className="mt-1.5 mb-0 text-[length:var(--exits-text-xs)] text-muted">
                          {t("inventory.deductAutoHint")}
                        </p>
                        {deductMode === "manual" && lots.length > 0 ? (
                          <div className="mt-2">
                            <InventoryLotList
                              lots={lots}
                              unitOfMeasure={account.unitOfMeasure}
                              formatStatus={formatStatus}
                              selectable
                              selectedLotId={selectedLotId}
                              onSelectLot={setSelectedLotId}
                              namePrefix="inventory-adjust-lot"
                            />
                          </div>
                        ) : null}
                        {deductMode === "manual" && lots.length === 0 ? (
                          <p className="mt-1.5 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                            {t("inventory.lotsEmpty")}
                          </p>
                        ) : null}
                      </fieldset>
                    ) : null}

                    <Input
                      label={t("inventory.reason")}
                      name="adjustReason"
                      value={adjustReason}
                      onChange={(e) => setAdjustReason(e.target.value)}
                      placeholder={t("inventory.reasonStockCountPlaceholder")}
                      list="inventory-adjust-reason-suggestions"
                    />
                    <datalist id="inventory-adjust-reason-suggestions">
                      <option value={t("inventory.reasonStockCountPlaceholder")} />
                    </datalist>

                    <div className="inventory-adjust-form__actions">
                      <Button
                        type="button"
                        className="inventory-adjust-form__submit shrink-0"
                        disabled={
                          adjusting ||
                          statusLocked ||
                          !adjustQty.trim() ||
                          !adjustExpiryReady ||
                          (tracksExpiration &&
                            adjustDirection === "Out" &&
                            deductMode === "manual" &&
                            !selectedLotId)
                        }
                        onClick={() => void onAdjust()}
                        data-testid="inventory-adjust"
                      >
                        {adjustApplyLabel}
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        className="inventory-adjust-form__disable h-auto min-h-0 max-w-[min(100%,14rem)] flex-1 justify-end whitespace-normal px-1 py-1 text-right text-[length:var(--exits-text-xs)] font-normal leading-snug text-muted"
                        disabled={disableMutation.isPending || !canDisableInventory}
                        onClick={() => disableMutation.mutate()}
                        data-testid="inventory-disable"
                      >
                        {t("inventory.disable")}
                      </Button>
                    </div>
                  </Card>
                </div>
              ) : null}

              {lotsPanel ? (
                <div className="inventory-detail-workspace__lots">{lotsPanel}</div>
              ) : null}
            </div>
          ) : null}
        </>
      )}

      <div data-testid="inventory-movements">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {t("inventory.movements")}
        </h2>
        {movementsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        <InventoryMovementsResponsiveList
          movements={movementsQuery.data?.items ?? []}
          unitOfMeasure={account.unitOfMeasure}
          resolveActor={(actorId) => actors.resolve(actorId)}
          actorsLoading={actors.isResolving}
        />
      </div>
    </div>
  );
}
