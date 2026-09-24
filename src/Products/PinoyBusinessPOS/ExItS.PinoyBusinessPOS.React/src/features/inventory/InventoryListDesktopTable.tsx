import {
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Check, Eye, X } from "lucide-react";
import {
  enableInventoryTracking,
  type PosInventoryAccountDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import {
  comparePurchaseCostToSellingPrice,
  resolveEffectiveSellingPriceView,
} from "@/features/inventory/inventory-opening-price-feedback";
import {
  formatInventoryQty,
  InventoryInTransitBadge,
  InventoryPendingReturnBadge,
  InventoryReservedBadge,
  resolveAvailableQuantity,
  resolveInTransitInboundQuantity,
  resolveInTransitOutboundQuantity,
  resolvePendingReturnQuantity,
  resolveReservedQuantity,
} from "@/features/inventory/inventory-reservation-display";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { cn } from "@/lib/cn";
import { AppLinkWithReturn } from "@/navigation/AppLinkWithReturn";

type EnableDraft = {
  productId: string;
  openingQty: string;
  unitCost: string;
};

export function InventoryListDesktopTable({
  items,
  workspace,
  allowManage,
  onOpenReservations,
}: {
  items: PosInventoryAccountDto[];
  workspace: PosWorkspaceScope;
  allowManage: boolean;
  onOpenReservations: (product: { productId: string; name: string }) => void;
}) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const costInputRef = useRef<HTMLInputElement | null>(null);
  const [enableDraft, setEnableDraft] = useState<EnableDraft | null>(null);
  const [enableError, setEnableError] = useState<string | null>(null);

  useEffect(() => {
    if (!enableDraft) {
      return;
    }
    const frame = window.requestAnimationFrame(() => {
      const qtyInput = document.querySelector<HTMLInputElement>(
        `[data-testid="inventory-table-opening-qty-${enableDraft.productId}"]`,
      );
      qtyInput?.focus();
      qtyInput?.select();
    });
    return () => window.cancelAnimationFrame(frame);
  }, [enableDraft?.productId]);

  useEffect(() => {
    if (!enableDraft) {
      return;
    }
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        setEnableDraft(null);
        setEnableError(null);
      }
    }
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [enableDraft]);

  const enableMutation = useMutation({
    mutationFn: async (draft: EnableDraft) => {
      const qty = Number(draft.openingQty);
      const openingQuantity =
        draft.openingQty.trim() === "" || Number.isNaN(qty) || qty <= 0 ? null : qty;
      if (openingQuantity != null && openingQuantity < 0) {
        throw new Error(t("openingStock.quantityInvalid"));
      }
      if (openingQuantity) {
        const unitCost = parseMoneyAmountInput(draft.unitCost);
        if (unitCost == null || unitCost <= 0) {
          throw new Error(t("openingStock.unitCostRequired"));
        }
        return enableInventoryTracking(workspace, draft.productId, {
          openingQuantity,
          unitCost,
        });
      }
      return enableInventoryTracking(workspace, draft.productId, {
        openingQuantity: 0,
      });
    },
    onSuccess: async () => {
      setEnableError(null);
      setEnableDraft(null);
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
    onError: (err) => {
      setEnableError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  function beginEnable(item: PosInventoryAccountDto) {
    if (!allowManage || item.isTracked) {
      return;
    }
    setEnableError(null);
    setEnableDraft({
      productId: item.productId,
      openingQty: "0",
      unitCost: "",
    });
  }

  function cancelEnable() {
    setEnableDraft(null);
    setEnableError(null);
  }

  function submitEnable(event?: FormEvent) {
    event?.preventDefault();
    if (!enableDraft || enableMutation.isPending) {
      return;
    }
    enableMutation.mutate(enableDraft);
  }

  function onCostKeyDown(event: ReactKeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter") {
      event.preventDefault();
      const enableBtn = document.querySelector<HTMLButtonElement>(
        `[data-testid="inventory-table-enable-stock-${enableDraft?.productId}"]`,
      );
      enableBtn?.focus();
    }
  }

  if (items.length === 0) {
    return null;
  }

  return (
    <div
      className="inventory-list-table-shell hidden min-w-0 lg:block"
      data-testid="inventory-list-table"
    >
      <table className="inventory-list-table w-full min-w-[64rem] border-separate border-spacing-0 text-left text-[length:var(--exits-text-sm)]">
        <thead className="inventory-list-table__head">
          <tr>
            <th className="inventory-list-table__th sticky top-0 z-10 w-full whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.product")}
            </th>
            <th className="inventory-list-table__th sticky top-0 z-10 w-auto whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.unit")}
            </th>
            <th className="inventory-list-table__th sticky top-0 z-10 w-auto whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.tracking")}
            </th>
            <th className="inventory-list-table__th sticky top-0 z-10 whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-right text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.purchaseCost")}
            </th>
            <th className="inventory-list-table__th sticky top-0 z-10 whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-right text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.sellingPrice")}
            </th>
            <th className="inventory-list-table__th sticky top-0 z-10 whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-center text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.available")}
            </th>
            <th className="inventory-list-table__th sticky top-0 z-10 w-auto whitespace-nowrap border-b border-border bg-[var(--exits-surface)] px-3 py-2.5 text-end text-[length:var(--exits-text-sm)] font-bold text-foreground">
              {t("inventory.col.action")}
            </th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => {
            const tracked = item.isTracked;
            const editing = enableDraft?.productId === item.productId;
            const lowStock = tracked && item.isLowStock;
            const stockStatus = tracked ? item.stockStatus?.trim() ?? "" : "";
            const stockStatusKey = stockStatus.toLowerCase();
            const outOfStock = stockStatusKey.includes("out");
            const showStockChip =
              tracked &&
              Boolean(stockStatus) &&
              (lowStock || outOfStock || stockStatusKey.includes("low"));
            const tracksExpiry = tracked && item.tracksExpiration === true;
            const availableQty = resolveAvailableQuantity(item);
            const reservedQty = resolveReservedQuantity(item);
            const pendingReturnQty = resolvePendingReturnQuantity(item);
            const inTransitOutQty = resolveInTransitOutboundQuantity(item);
            const inTransitInQty = resolveInTransitInboundQuantity(item);
            const sellingView =
              item.sellingPrice != null || item.effectiveSellingPrice != null
                ? resolveEffectiveSellingPriceView({
                    sellingPrice: item.sellingPrice ?? item.effectiveSellingPrice ?? 0,
                    effectiveSellingPrice: item.effectiveSellingPrice,
                    hasBranchPriceOverride: item.hasBranchPriceOverride,
                  })
                : null;
            const costFeedback =
              editing && enableDraft
                ? comparePurchaseCostToSellingPrice(
                    String(parseMoneyAmountInput(enableDraft.unitCost) ?? ""),
                    sellingView?.amount,
                  )
                : { kind: "none" as const };

            return (
              <tr
                key={item.productId}
                className={cn(
                  "inventory-list-table__row",
                  !tracked && "inventory-list-table__row--untracked",
                  editing &&
                    "inventory-list-table__row--enabling bg-[color-mix(in_srgb,var(--exits-primary-soft,#e8f5e9)_35%,transparent)]",
                )}
                data-testid={`inventory-table-row-${item.productId}`}
              >
                <td className="max-w-[16rem] px-3 py-2.5 align-middle">
                  <AppLinkWithReturn
                    to={`/inventory/${item.productId}`}
                    className="block truncate font-semibold text-foreground no-underline hover:underline"
                    data-testid={`inventory-table-product-link-${item.productId}`}
                  >
                    {item.name}
                  </AppLinkWithReturn>
                  {tracksExpiry || showStockChip ? (
                    <div className="mt-1 flex flex-wrap gap-1">
                      {tracksExpiry ? (
                        <span className="inventory-row__badge inventory-row__badge--expiry">
                          {t("inventory.tracksExpirationShort")}
                        </span>
                      ) : null}
                      {showStockChip ? (
                        <span
                          className={
                            outOfStock
                              ? "inventory-row__badge inventory-row__badge--out"
                              : "inventory-row__badge inventory-row__badge--low"
                          }
                        >
                          {outOfStock ? stockStatus : t("inventory.lowStock")}
                        </span>
                      ) : null}
                    </div>
                  ) : null}
                </td>

                <td
                  className="whitespace-nowrap px-3 py-2.5 align-middle text-muted"
                  data-testid={`inventory-table-unit-${item.productId}`}
                >
                  {item.unitOfMeasure}
                </td>

                <td className="w-auto whitespace-nowrap px-3 py-2.5 align-middle">
                  {tracked ? (
                    <span
                      className="text-muted"
                      data-testid={`inventory-table-tracking-${item.productId}`}
                    >
                      {t("inventory.tracked")}
                    </span>
                  ) : editing ? (
                    <span
                      className="font-medium text-primary"
                      data-testid={`inventory-table-tracking-${item.productId}`}
                    >
                      {t("inventory.enablingTracking")}
                    </span>
                  ) : allowManage ? (
                    <Button
                      type="button"
                      intent="primary"
                      appearance="outline"
                      data-testid={`inventory-table-untracked-${item.productId}`}
                      onClick={() => beginEnable(item)}
                    >
                      {t("inventory.notTracked")}
                    </Button>
                  ) : (
                    <span
                      className="text-muted"
                      data-testid={`inventory-table-tracking-${item.productId}`}
                    >
                      {t("inventory.notTracked")}
                    </span>
                  )}
                </td>

                <td className="px-3 py-2.5 align-middle text-right">
                  {editing && enableDraft ? (
                    <div className="inline-flex flex-col items-stretch gap-1.5 sm:items-end">
                      <div className="flex flex-wrap items-start justify-end gap-3">
                        <label className="flex flex-col items-stretch gap-1 text-start">
                          <span className="text-[length:var(--exits-text-xs)] font-medium text-muted">
                            {t("inventory.col.openingQty")}
                          </span>
                          <QuantityStepper
                            compact
                            variant="auto"
                            min={0}
                            step={1}
                            precision={4}
                            unitOfMeasure={item.unitOfMeasure}
                            sellingMode="PerItem"
                            value={Number(enableDraft.openingQty) || 0}
                            onChange={(next) =>
                              setEnableDraft({
                                ...enableDraft,
                                openingQty: String(next),
                              })
                            }
                            decreaseLabel={t("transfer.decreaseQuantity")}
                            increaseLabel={t("transfer.increaseQuantity")}
                            ariaLabel={t("inventory.col.openingQty")}
                            valueTestId={`inventory-table-opening-qty-${item.productId}`}
                            className="inventory-list-table__qty-stepper"
                            disabled={enableMutation.isPending}
                          />
                        </label>
                        <label className="flex flex-col items-stretch gap-1 text-start">
                          <span className="text-[length:var(--exits-text-xs)] font-medium text-muted">
                            {t("inventory.col.purchaseCost")}
                          </span>
                          <div className="exits-currency-field inventory-list-table__purchase-cost">
                            <span className="exits-currency-field__prefix" aria-hidden>
                              ₱
                            </span>
                            <input
                              ref={costInputRef}
                              type="text"
                              inputMode="decimal"
                              value={enableDraft.unitCost}
                              onChange={(event) =>
                                setEnableDraft({
                                  ...enableDraft,
                                  unitCost: normalizeMoneyAmountTyping(event.target.value),
                                })
                              }
                              onBlur={() => {
                                const parsed = parseMoneyAmountInput(enableDraft.unitCost);
                                if (parsed !== null) {
                                  setEnableDraft({
                                    ...enableDraft,
                                    unitCost: formatMoneyAmountInput(parsed),
                                  });
                                }
                              }}
                              onKeyDown={onCostKeyDown}
                              aria-label={t("inventory.col.purchaseCost")}
                              data-testid={`inventory-table-unit-cost-${item.productId}`}
                              className="exits-currency-field__input"
                              disabled={enableMutation.isPending}
                            />
                          </div>
                        </label>
                      </div>
                      {costFeedback.kind === "zeroMargin" ? (
                        <p
                          className="m-0 max-w-[16rem] text-end text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`inventory-table-cost-zero-margin-${item.productId}`}
                        >
                          {t("inventory.purchaseCostZeroMargin")}
                        </p>
                      ) : null}
                      {costFeedback.kind === "higherCost" ? (
                        <p
                          className="m-0 max-w-[16rem] text-end text-[length:var(--exits-text-xs)] text-[var(--exits-warning,#b45309)]"
                          role="status"
                          data-testid={`inventory-table-cost-high-warning-${item.productId}`}
                        >
                          {t("inventory.purchaseCostHigherThanSelling")}
                        </p>
                      ) : null}
                    </div>
                  ) : tracked && item.unitCost != null && Number.isFinite(item.unitCost) ? (
                    <span data-testid={`inventory-table-unit-cost-value-${item.productId}`}>
                      <MoneyDisplay amount={item.unitCost} className="font-medium" />
                    </span>
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </td>

                <td
                  className="whitespace-nowrap px-3 py-2.5 align-middle text-right tabular-nums"
                  data-testid={`inventory-table-selling-price-${item.productId}`}
                >
                  {sellingView ? (
                    <MoneyDisplay amount={sellingView.amount} className="font-medium" />
                  ) : (
                    <span className="text-muted">—</span>
                  )}
                </td>

                <td className="px-3 py-2.5 align-middle text-center">
                  {tracked ? (
                    <div className="inline-flex flex-col items-center gap-1">
                      <span
                        className={cn(
                          "tabular-nums font-semibold",
                          lowStock && "text-[var(--exits-warning,#b45309)]",
                          outOfStock && "text-danger",
                        )}
                        data-testid={`inventory-table-available-${item.productId}`}
                      >
                        {formatInventoryQty(availableQty)}
                      </span>
                      <div className="flex flex-wrap items-center justify-center gap-1">
                        <InventoryReservedBadge
                          reservedQuantity={reservedQty}
                          onClick={() =>
                            onOpenReservations({
                              productId: item.productId,
                              name: item.name,
                            })
                          }
                          testId={`inventory-table-reserved-${item.productId}`}
                        />
                        <InventoryInTransitBadge
                          quantity={inTransitOutQty}
                          branchName={item.inTransitOutboundBranchName}
                          direction="outbound"
                          onClick={() =>
                            onOpenReservations({
                              productId: item.productId,
                              name: item.name,
                            })
                          }
                          testId={`inventory-table-in-transit-out-${item.productId}`}
                        />
                        <InventoryInTransitBadge
                          quantity={inTransitInQty}
                          branchName={item.inTransitInboundBranchName}
                          direction="inbound"
                          onClick={() =>
                            onOpenReservations({
                              productId: item.productId,
                              name: item.name,
                            })
                          }
                          testId={`inventory-table-in-transit-in-${item.productId}`}
                        />
                        <InventoryPendingReturnBadge
                          pendingReturnQuantity={pendingReturnQty}
                          testId={`inventory-table-pending-return-${item.productId}`}
                        />
                      </div>
                    </div>
                  ) : (
                    <span className="text-muted" aria-hidden>
                      —
                    </span>
                  )}
                </td>

                <td className="px-3 py-2.5 align-middle">
                  <div className="flex items-center justify-end gap-1.5">
                    {editing ? (
                      <>
                        <Button
                          type="button"
                          size="icon"
                          intent="success"
                          appearance="outline"
                          className="size-9 shrink-0"
                          disabled={enableMutation.isPending}
                          aria-label={t("inventory.enableStock")}
                          title={t("inventory.enableStock")}
                          data-testid={`inventory-table-enable-stock-${item.productId}`}
                          onClick={() => submitEnable()}
                        >
                          <Check className="size-4" aria-hidden />
                        </Button>
                        <Button
                          type="button"
                          size="icon"
                          intent="danger"
                          appearance="outline"
                          className="size-9 shrink-0"
                          disabled={enableMutation.isPending}
                          aria-label={t("inventory.enableCancel")}
                          title={t("inventory.enableCancel")}
                          data-testid={`inventory-table-enable-cancel-${item.productId}`}
                          onClick={cancelEnable}
                        >
                          <X className="size-4" aria-hidden />
                        </Button>
                      </>
                    ) : null}
                    <Button
                      asChild
                      type="button"
                      size="icon"
                      appearance="outline"
                      className="size-9 shrink-0"
                      data-testid={`inventory-table-view-${item.productId}`}
                    >
                      <AppLinkWithReturn
                        to={`/inventory/${item.productId}`}
                        aria-label={t("inventory.viewInventoryDetails")}
                        title={t("inventory.viewInventoryDetails")}
                      >
                        <Eye className="size-4" aria-hidden />
                      </AppLinkWithReturn>
                    </Button>
                  </div>
                  {editing && enableError ? (
                    <p
                      className="m-0 mt-1 text-end text-[length:var(--exits-text-xs)] text-danger"
                      data-testid={`inventory-table-enable-error-${item.productId}`}
                    >
                      {enableError}
                    </p>
                  ) : null}
                  {editing && enableDraft && Number(enableDraft.openingQty) > 0 ? (
                    <p className="m-0 mt-1 text-end text-[length:var(--exits-text-xs)] text-muted">
                      {t("inventory.stockValue")}:{" "}
                      {formatPeso(
                        Number(enableDraft.openingQty) *
                          (parseMoneyAmountInput(enableDraft.unitCost) ?? 0),
                      )}
                    </p>
                  ) : null}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
