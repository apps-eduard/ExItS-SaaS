import { useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  adjustInventoryStock,
  addOpeningStock,
  disableInventoryTracking,
  enableInventoryTracking,
  getInventoryProduct,
  getStockMovement,
  listInventoryMovements,
  listProductLots,
  type PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  canAddOpeningStock,
  canDisableExpirationTracking,
  computeGoodQuantity,
  sortLotsByExpiry,
} from "@/features/inventory/inventory-detail-helpers";
import { computeOpeningStockValue } from "@/features/catalog/opening-stock-helpers";
import { expirationSettingsPath } from "@/features/inventory/expiration-settings-routes";
import { InventoryLotList } from "@/features/inventory/InventoryLotList";
import {
  requiresOpeningExpirationDate,
  resolveLotExpiryLabel,
} from "@/features/inventory/inventory-lot-status";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  inventoryMovementTypeLabelKey,
  resolveMovementStockValue,
} from "@/features/purchasing/purchase-cost-display";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
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
  const { boundWorkspace, sessionGrant } = useWorkspace();
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
  const movementIdRef = useRef<string | null>(null);

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

  const tracksExpiration = accountQuery.data?.tracksExpiration === true;

  const branchLabel = boundWorkspace?.branchName ?? t("transfer.currentBranch");

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
  const formatStatus = (lot: PosInventoryLotDto) => formatLotStatus(lot, t);
  const lotTotal = lots.reduce((sum, lot) => sum + (lot.quantityOnHand ?? 0), 0);
  const needsExpirationSetup =
    tracksExpiration &&
    account.onHandQuantity > 0 &&
    !lotsQuery.isLoading &&
    lotTotal === 0;

  const lotsSection =
    tracksExpiration && !showAddOpeningStock ? (
      needsExpirationSetup ? (
        <Card
          className="flex flex-col gap-3 p-3"
          data-testid="inventory-expiration-pending"
        >
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("inventory.expirationInventory")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.expirationPendingSummary")
              .replace("{qty}", String(account.onHandQuantity))
              .replace("{uom}", account.unitOfMeasure)}
          </p>
        </Card>
      ) : (
        <>
          <Card className="flex flex-col gap-3 p-3" data-testid="inventory-expiration-summary">
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("inventory.expirationInventory")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {account.onHandQuantity} {account.unitOfMeasure} {t("inventory.onHandSummary")}
            </p>
            <div
              className="inventory-expiry-counts flex min-w-0 flex-wrap gap-2"
              data-testid="inventory-expiry-totals"
            >
              <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--good">
                {t("inventory.statusGood")}: {goodQuantity}
              </span>
              <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--near">
                {t("inventory.nearExpiryQty")}: {account.nearExpiryQuantity ?? 0}
              </span>
              <span className="inventory-expiry-counts__stat inventory-expiry-counts__stat--expired">
                {t("inventory.expiredQty")}: {account.expiredQuantity ?? 0}
              </span>
            </div>
          </Card>

          <Card className="flex flex-col gap-3 p-3" data-testid="inventory-lots">
            <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
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
                className="min-h-11 w-fit"
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
        </>
      )
    ) : null;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="inventory-detail-page">
      <PageHeader
        title={account.name}
        description={t("inventory.detailLede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
        backTestId="page-header-back-inventory"
      />

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <Card className="p-3" data-testid="inventory-status">
        {account.isTracked ? (
          <>
            <p className="m-0 font-semibold" data-testid="inventory-on-hand">
              {t("inventory.onHandAtBranch")
                .replace("{branch}", branchLabel)
                .replace("{qty}", String(account.onHandQuantity))
                .replace("{uom}", account.unitOfMeasure)}
            </p>
            <p
              className="mt-2 mb-0 text-[length:var(--exits-text-sm)] font-semibold"
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
                className="mt-3 flex flex-col gap-2"
                data-testid="inventory-expiration-setup-required"
              >
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("inventory.expirationSetupRequired")}
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.expirationSetupRequiredDetail")}
                </p>
                {allowManageInventory ? (
                  <div className="flex flex-wrap gap-2">
                    <Button asChild type="button" className="min-h-11">
                      <Link
                        to={expirationSettingsPath(productId!, "assign")}
                        data-testid="inventory-expiration-setup-assign"
                      >
                        {t("inventory.assignExpirationDates")}
                      </Link>
                    </Button>
                    <Button asChild type="button" variant="outline" className="min-h-11">
                      <Link
                        to={expirationSettingsPath(productId!, "warning")}
                        data-testid="inventory-manage-expiration"
                      >
                        {t("inventory.manageExpirationSettings")}
                      </Link>
                    </Button>
                  </div>
                ) : null}
              </div>
            ) : tracksExpiration ? (
              allowManageInventory ? (
                <div className="mt-3">
                  <Button asChild type="button" variant="outline" className="min-h-11">
                    <Link
                      to={expirationSettingsPath(productId!, "warning")}
                      data-testid="inventory-manage-expiration"
                    >
                      {t("inventory.manageExpirationSettings")}
                    </Link>
                  </Button>
                </div>
              ) : null
            ) : allowManageInventory ? (
              <div className="mt-3">
                <Button asChild type="button" className="min-h-11">
                  <Link
                    to={expirationSettingsPath(productId!)}
                    data-testid="inventory-enable-expiration"
                  >
                    {t("inventory.enableExpirationTracking")}
                  </Link>
                </Button>
              </div>
            ) : null}
          </>
        ) : (
          <>
            <p className="m-0 font-semibold">{t("inventory.notTracked")}</p>
            <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("inventory.untrackedHint")}
            </p>
          </>
        )}
      </Card>

      {account.isTracked && allowManageInventory ? (
        <div className="flex flex-wrap gap-2">
          <Button asChild type="button" variant="outline" className="min-h-11">
            <Link
              to={`/inventory/stock-use/new?productId=${encodeURIComponent(account.productId)}`}
              data-testid="inventory-record-stock-use"
            >
              {t("inventory.recordStockUse")}
            </Link>
          </Button>
          <Button asChild type="button" variant="outline" className="min-h-11">
            <Link
              to={`/inventory/waste-loss/new?productId=${encodeURIComponent(account.productId)}`}
              data-testid="inventory-record-waste-loss"
            >
              {t("inventory.recordWasteLoss")}
            </Link>
          </Button>
        </div>
      ) : null}

      {!account.isTracked ? (
        allowManageInventory ? (
        <Card className="flex flex-col gap-3 p-3">
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("inventory.enableTracking")}
          </h2>
          <Input
            label={t("inventory.openingQuantityOptional")}
            name="openingQuantity"
            inputMode="decimal"
            value={openingQty}
            onChange={(e) => setOpeningQty(e.target.value)}
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.openingHint")}
          </p>
          {Number(openingQty) > 0 ? (
            <>
              <Input
                label={`${t("inventory.unitPurchaseCost")} (₱ / ${account.unitOfMeasure})`}
                name="openingUnitCost"
                inputMode="decimal"
                value={openingUnitCost}
                onChange={(e) => setOpeningUnitCost(e.target.value)}
                data-testid="inventory-enable-unit-cost"
              />
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("openingStock.unitCostHelper")}
              </p>
              {openingStockValue !== null ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("inventory.stockValue")}: ₱{openingStockValue.toFixed(2)}
                </p>
              ) : null}
            </>
          ) : null}
          {tracksExpiration && Number(openingQty) > 0 ? (
            <>
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
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("inventory.openingExpiryHint")}
              </p>
            </>
          ) : null}
          <Button
            type="button"
            className="min-h-11"
            disabled={enableMutation.isPending}
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
              {t("inventory.addOpeningStock")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("inventory.addOpeningStockHint")}
            </p>
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
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("openingStock.unitCostHelper")}
            </p>
            {openingStockValue !== null ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("inventory.stockValue")}: ₱{openingStockValue.toFixed(2)}
              </p>
            ) : null}
            {tracksExpiration ? (
              <>
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
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.openingExpiryHint")}
                </p>
              </>
            ) : null}
            <Button
              type="button"
              className="min-h-11"
              disabled={addOpeningStockMutation.isPending}
              onClick={() => addOpeningStockMutation.mutate()}
              data-testid="inventory-add-opening-stock-submit"
            >
              {t("inventory.addOpeningStock")}
            </Button>
          </Card>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11 w-fit"
            disabled={disableMutation.isPending || !canDisableInventory}
            onClick={() => disableMutation.mutate()}
            data-testid="inventory-disable"
          >
            {t("inventory.disable")}
          </Button>
        </>
      ) : (
        <>
          {lotsSection}

          {allowManageInventory ? (
            <>
              <Card className="flex flex-col gap-3 p-3" data-testid="inventory-adjust-form">
                <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
                  {t("inventory.stockAdjustment")}
                </h2>

            <fieldset className="m-0 border-0 p-0">
              <legend className="mb-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                {t("inventory.direction")}
              </legend>
              <div className="flex flex-wrap gap-2" data-testid="inventory-adjust-direction">
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

            <Input
              label={t("inventory.adjustQuantityRequired")}
              name="adjustQuantity"
              inputMode="decimal"
              value={adjustQty}
              onChange={(e) => setAdjustQty(e.target.value)}
            />

            {tracksExpiration && adjustDirection === "In" ? (
              <div className="flex flex-col gap-3" data-testid="inventory-stock-details">
                <h3 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                  {t("inventory.stockDetails")}
                </h3>
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
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.stockInExpiryHint")}
                </p>
              </div>
            ) : null}

            {tracksExpiration && adjustDirection === "Out" ? (
              <fieldset className="m-0 border-0 p-0" data-testid="inventory-deduct-mode">
                <legend className="mb-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("inventory.deductFrom")}
                </legend>
                <div className="flex flex-col gap-2">
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
                <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.deductAutoHint")}
                </p>
                {deductMode === "manual" && lots.length > 0 ? (
                  <div className="mt-3">
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
                  <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
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
            />

            <Button
              type="button"
              className="min-h-11"
              disabled={adjusting || statusLocked || !adjustQty.trim()}
              onClick={() => void onAdjust()}
              data-testid="inventory-adjust"
            >
              {adjusting
                ? t("checkout.confirmingTransaction")
                : tracksExpiration && adjustDirection === "In"
                  ? t("inventory.addStock")
                  : t("inventory.applyAdjustment")}
            </Button>
          </Card>

          <Button
            type="button"
            variant="ghost"
            className="min-h-11 w-fit"
            disabled={disableMutation.isPending || !canDisableInventory}
            onClick={() => disableMutation.mutate()}
            data-testid="inventory-disable"
          >
            {t("inventory.disable")}
          </Button>
            </>
          ) : null}
        </>
      )}

      <div data-testid="inventory-movements">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {t("inventory.movements")}
        </h2>
        {movementsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        <ul className="mt-2 mb-0 flex list-none flex-col gap-2 p-0">
          {movementsQuery.data?.items.map((movement) => (
            <li key={movement.movementId}>
              <Card className="p-3">
                <p className="m-0 font-semibold">
                  {movement.quantityEffect > 0 ? "+" : ""}
                  {movement.quantityEffect} {account.unitOfMeasure}
                </p>
                <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t(inventoryMovementTypeLabelKey(movement.movementType))}
                </p>
                {movement.unitCost != null ? (
                  <dl
                    className="mt-2 mb-0 grid gap-1 text-[length:var(--exits-text-sm)]"
                    data-testid={`inventory-movement-cost-${movement.movementId}`}
                  >
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("inventory.unitPurchaseCost")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={movement.unitCost} />
                        <span className="text-muted"> / {account.unitOfMeasure}</span>
                      </dd>
                    </div>
                    {resolveMovementStockValue(movement) != null ? (
                      <div className="flex flex-wrap items-baseline justify-between gap-2">
                        <dt className="text-muted">{t("inventory.stockValue")}</dt>
                        <dd className="m-0">
                          <MoneyDisplay amount={resolveMovementStockValue(movement)!} />
                        </dd>
                      </div>
                    ) : null}
                  </dl>
                ) : null}
                {movement.expirationDate ? (
                  <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("inventory.movementExpiry")}: {movement.expirationDate}
                    {movement.lotNumber
                      ? ` · ${t("inventory.movementLot")}: ${movement.lotNumber}`
                      : ""}
                  </p>
                ) : null}
                <div className="mt-2">
                  <ActorAttribution
                    labelKey="common.recordedBy"
                    actorId={movement.recordedBy}
                    occurredAtUtc={movement.recordedAtUtc}
                    resolved={actors.resolve(movement.recordedBy)}
                    isLoading={actors.isResolving}
                    testId={`inventory-movement-actor-${movement.movementId}`}
                  />
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
