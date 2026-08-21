import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  adjustInventoryStock,
  disableInventoryTracking,
  enableInventoryTracking,
  getInventoryProduct,
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
  requiresOpeningExpirationDate,
  resolveLotExpiryLabel,
} from "@/features/inventory/inventory-lot-status";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const LOT_PAGE_SIZE = 50;

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
      return t("inventory.statusOk");
    default:
      return label.status;
  }
}

export function InventoryDetailPage() {
  const { t } = useI18n();
  const { productId } = useParams();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const [openingQty, setOpeningQty] = useState("0");
  const [openingExpiry, setOpeningExpiry] = useState("");
  const [openingLotNumber, setOpeningLotNumber] = useState("");
  const [adjustQty, setAdjustQty] = useState("");
  const [adjustReason, setAdjustReason] = useState("");
  const [adjustDirection, setAdjustDirection] = useState<"In" | "Out">("In");
  const [adjustExpiry, setAdjustExpiry] = useState("");
  const [adjustLotNumber, setAdjustLotNumber] = useState("");
  const [selectedLotId, setSelectedLotId] = useState("");
  const [error, setError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const accountQuery = useQuery({
    queryKey: ["inventory", "product", workspace?.organizationId, productId],
    enabled: Boolean(workspace) && Boolean(productId),
    queryFn: ({ signal }) => getInventoryProduct(workspace!, productId!, signal),
  });

  const tracksExpiration = accountQuery.data?.tracksExpiration === true;

  const movementsQuery = useQuery({
    queryKey: ["inventory", "movements", workspace?.organizationId, productId],
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
    () => lotsQuery.data?.pages.flatMap((page) => page.items) ?? [],
    [lotsQuery.data],
  );

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: ["inventory"] });
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
      return enableInventoryTracking(workspace!, productId!, {
        openingQuantity,
        expirationDate:
          tracksExpiration && openingQuantity && openingExpiry.trim() ? openingExpiry.trim() : null,
        lotNumber: tracksExpiration && openingLotNumber.trim() ? openingLotNumber.trim() : null,
      });
    },
    onSuccess: async () => {
      setError(null);
      setOpeningExpiry("");
      setOpeningLotNumber("");
      await invalidate();
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
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const adjustMutation = useMutation({
    mutationFn: () => {
      const reason = adjustReason.trim();
      if (!reason) {
        throw new Error(t("inventory.reasonRequired"));
      }
      if (tracksExpiration && adjustDirection === "In" && !adjustExpiry.trim()) {
        throw new Error(t("inventory.expirationDateRequired"));
      }
      if (tracksExpiration && adjustDirection === "Out" && lots.length > 0 && !selectedLotId) {
        throw new Error(t("inventory.lotRequired"));
      }
      if (
        tracksExpiration &&
        adjustDirection === "Out" &&
        lots.length === 0 &&
        !adjustExpiry.trim()
      ) {
        throw new Error(t("inventory.expirationDateRequired"));
      }
      return adjustInventoryStock(workspace!, productId!, {
        direction: adjustDirection,
        quantity: Number(adjustQty),
        reason,
        expirationDate:
          tracksExpiration && adjustDirection === "In"
            ? adjustExpiry.trim() || null
            : tracksExpiration && adjustDirection === "Out" && !selectedLotId
              ? adjustExpiry.trim() || null
              : null,
        lotNumber:
          tracksExpiration && adjustDirection === "In" && adjustLotNumber.trim()
            ? adjustLotNumber.trim()
            : null,
        lotId: tracksExpiration && selectedLotId ? selectedLotId : null,
      });
    },
    onSuccess: async () => {
      setAdjustQty("");
      setAdjustReason("");
      setAdjustExpiry("");
      setAdjustLotNumber("");
      setSelectedLotId("");
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  if (!workspace || accountQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const account = accountQuery.data;
  if (!account) {
    return <ErrorState title={t("error.title")} detail={t("inventory.notFound")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="inventory-detail-page">
      <PageHeader title={account.name} description={t("inventory.detailLede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/inventory">{t("inventory.backList")}</Link>
      </Button>
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <Card className="p-3" data-testid="inventory-status">
        <p className="m-0 font-semibold">
          {account.isTracked ? t("inventory.tracked") : t("inventory.notTracked")}
        </p>
        {account.isTracked && tracksExpiration ? (
          <dl
            className="mt-2 mb-0 grid gap-2 text-[length:var(--exits-text-sm)]"
            data-testid="inventory-expiry-totals"
          >
            <div className="flex min-w-0 justify-between gap-3">
              <dt className="text-muted">{t("inventory.totalOnHand")}</dt>
              <dd className="m-0 font-semibold">
                {account.onHandQuantity} {account.unitOfMeasure}
              </dd>
            </div>
            <div className="flex min-w-0 justify-between gap-3">
              <dt className="text-muted">{t("inventory.sellable")}</dt>
              <dd className="m-0 font-semibold">{account.sellableQuantity ?? 0}</dd>
            </div>
            <div className="flex min-w-0 justify-between gap-3">
              <dt className="text-muted">{t("inventory.expiredQty")}</dt>
              <dd className="m-0 font-semibold">{account.expiredQuantity ?? 0}</dd>
            </div>
            <div className="flex min-w-0 justify-between gap-3">
              <dt className="text-muted">{t("inventory.nearExpiryQty")}</dt>
              <dd className="m-0 font-semibold">{account.nearExpiryQuantity ?? 0}</dd>
            </div>
          </dl>
        ) : (
          <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
            {account.isTracked
              ? `${t("inventory.onHand")}: ${account.onHandQuantity} ${account.unitOfMeasure}`
              : t("inventory.untrackedHint")}
          </p>
        )}
      </Card>

      {tracksExpiration && account.isTracked ? (
        <div data-testid="inventory-lots">
          <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
            {t("inventory.lots")}
          </h2>
          {lotsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {lots.length === 0 && !lotsQuery.isLoading ? (
            <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("inventory.lotsEmpty")}
            </p>
          ) : (
            <ul className="mt-2 mb-0 flex list-none flex-col gap-2 p-0">
              {lots.map((lot) => (
                <li key={lot.lotId}>
                  <Card className="p-3" data-testid={`inventory-lot-${lot.lotId}`}>
                    <p className="m-0 truncate font-semibold">
                      {lot.expirationDate}
                      {lot.lotNumber ? ` · ${lot.lotNumber}` : ""}
                    </p>
                    <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("inventory.onHand")}: {lot.quantityOnHand} · {formatLotStatus(lot, t)}
                    </p>
                  </Card>
                </li>
              ))}
            </ul>
          )}
          {lotsQuery.hasNextPage ? (
            <Button
              type="button"
              variant="ghost"
              className="mt-2 min-h-11 w-fit"
              disabled={lotsQuery.isFetchingNextPage}
              onClick={() => void lotsQuery.fetchNextPage()}
              data-testid="inventory-lots-load-more"
            >
              {lotsQuery.isFetchingNextPage ? t("inventory.loadingMore") : t("inventory.loadMore")}
            </Button>
          ) : null}
        </div>
      ) : null}

      {!account.isTracked ? (
        <Card className="flex flex-col gap-3 p-3">
          <Input
            label={t("inventory.openingQuantity")}
            name="openingQuantity"
            inputMode="decimal"
            value={openingQty}
            onChange={(e) => setOpeningQty(e.target.value)}
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.openingHint")}
          </p>
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
                label={t("inventory.lotNumberOptional")}
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
      ) : (
        <>
          <Card className="flex flex-col gap-3 p-3">
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("inventory.direction")}
              <select
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
                value={adjustDirection}
                onChange={(e) => {
                  setAdjustDirection(e.target.value as "In" | "Out");
                  setSelectedLotId("");
                }}
                data-testid="inventory-adjust-direction"
              >
                <option value="In">{t("inventory.adjustIn")}</option>
                <option value="Out">{t("inventory.adjustOut")}</option>
              </select>
            </label>
            <Input
              label={t("inventory.adjustQuantity")}
              name="adjustQuantity"
              inputMode="decimal"
              value={adjustQty}
              onChange={(e) => setAdjustQty(e.target.value)}
            />
            <Input
              label={t("inventory.reason")}
              name="adjustReason"
              value={adjustReason}
              onChange={(e) => setAdjustReason(e.target.value)}
            />
            {tracksExpiration && adjustDirection === "In" ? (
              <>
                <Input
                  label={t("inventory.expirationDate")}
                  name="adjustExpirationDate"
                  type="date"
                  value={adjustExpiry}
                  onChange={(e) => setAdjustExpiry(e.target.value)}
                  data-testid="inventory-adjust-expiry"
                />
                <Input
                  label={t("inventory.lotNumberOptional")}
                  name="adjustLotNumber"
                  value={adjustLotNumber}
                  onChange={(e) => setAdjustLotNumber(e.target.value)}
                />
              </>
            ) : null}
            {tracksExpiration && adjustDirection === "Out" ? (
              <>
                {lots.length > 0 ? (
                  <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("inventory.selectLot")}
                    <select
                      className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
                      value={selectedLotId}
                      onChange={(e) => setSelectedLotId(e.target.value)}
                      data-testid="inventory-adjust-lot"
                    >
                      <option value="">{t("inventory.selectLotPlaceholder")}</option>
                      {lots.map((lot) => (
                        <option key={lot.lotId} value={lot.lotId}>
                          {lot.expirationDate} · {lot.quantityOnHand}
                          {lot.lotNumber ? ` · ${lot.lotNumber}` : ""} · {formatLotStatus(lot, t)}
                        </option>
                      ))}
                    </select>
                  </label>
                ) : (
                  <Input
                    label={t("inventory.expirationDate")}
                    name="adjustOutExpirationDate"
                    type="date"
                    value={adjustExpiry}
                    onChange={(e) => setAdjustExpiry(e.target.value)}
                    data-testid="inventory-adjust-expiry"
                  />
                )}
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("inventory.writeOffHint")}
                </p>
              </>
            ) : null}
            <Button
              type="button"
              className="min-h-11"
              disabled={adjustMutation.isPending || !adjustQty.trim()}
              onClick={() => adjustMutation.mutate()}
              data-testid="inventory-adjust"
            >
              {t("inventory.applyAdjustment")}
            </Button>
          </Card>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11 w-fit"
            disabled={disableMutation.isPending}
            onClick={() => disableMutation.mutate()}
            data-testid="inventory-disable"
          >
            {t("inventory.disable")}
          </Button>
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
                  {movement.movementType} · {movement.quantityEffect}
                </p>
                <p className="mt-1 mb-0 truncate text-[length:var(--exits-text-sm)] text-muted">
                  {movement.reason} · {new Date(movement.recordedAtUtc).toLocaleString()}
                </p>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
