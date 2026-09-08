import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  createCatalogProduct,
  getCatalogProduct,
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  DEFAULT_CATALOG_SELLING_MODE,
  type PosUnitOfMeasureCode,
} from "@/api/pos/pos-catalog-options";
import { PosApiError } from "@/api/pos/pos-http";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { enableInventoryTracking, getInventoryProduct } from "@/api/pos/pos-inventory-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { SearchField } from "@/components/exits/SearchField";
import {
  buildRecipeMaterialCostEstimate,
  estimatedMaterialMargin,
  estimatedUnitMaterialCost,
  resolveLatestAcquisitionUnitCost,
  toBaseQuantityForCost,
  type RecipeMaterialCostEstimate,
} from "@/features/inventory/production-recipe-cost";
import {
  DEFAULT_TARGET_GROSS_MARGIN,
  TARGET_GROSS_MARGIN_PRESETS,
  formatGrossMarginPercent,
  isLowGrossMargin,
  isSellingBelowCost,
  resolveMaterialCostBasis,
  suggestSellingPriceFromUnitCost,
} from "@/features/inventory/production-suggested-selling-price";
import {
  materialBaseUomLabel,
  type ProductionMaterialDraft,
} from "@/features/inventory/production-material-uom";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";

export type RecipeOutputLinkResult = {
  outputProduct: PosCatalogProductDto;
  createdNew: boolean;
};

type Props = {
  open: boolean;
  workspace: PosWorkspaceScope;
  recipeName: string;
  standardYieldQty: number;
  /** Catalog UOM code for the new product / yield display. */
  standardYieldUom: PosUnitOfMeasureCode | string;
  materials: ProductionMaterialDraft[];
  onCancel: () => void;
  onLinked: (result: RecipeOutputLinkResult) => void;
};

type OutputMode = "create" | "existing";

function isEligibleExistingOutput(product: PosCatalogProductDto): boolean {
  return product.isProduced === true && product.status === "Active";
}

function sellingModeForUom(uom: string): string {
  const normalized = uom.trim().toLowerCase();
  if (
    normalized === "kilogram" ||
    normalized === "kg" ||
    normalized === "gram" ||
    normalized === "g"
  ) {
    return "ByWeight";
  }
  return DEFAULT_CATALOG_SELLING_MODE;
}

function marginPresetLabel(margin: number): string {
  return `${Math.round(margin * 100)}%`;
}

export function ProductionRecipeOutputSheet({
  open,
  workspace,
  recipeName,
  standardYieldQty,
  standardYieldUom,
  materials,
  onCancel,
  onLinked,
}: Props) {
  const { t } = useI18n();
  const [mode, setMode] = useState<OutputMode>("create");
  const [productName, setProductName] = useState(recipeName);
  const [baseUnit, setBaseUnit] = useState(standardYieldUom);
  const [canBeSold, setCanBeSold] = useState(true);
  const [canBeIngredient, setCanBeIngredient] = useState(false);
  const [sellingPrice, setSellingPrice] = useState("");
  const [userEditedSellingPrice, setUserEditedSellingPrice] = useState(false);
  const [targetMargin, setTargetMargin] = useState(DEFAULT_TARGET_GROSS_MARGIN);
  const [customMarginRaw, setCustomMarginRaw] = useState("");
  const [usingCustomMargin, setUsingCustomMargin] = useState(false);
  const [categoryId, setCategoryId] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [busy, setBusy] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);
  const [costEstimate, setCostEstimate] = useState<RecipeMaterialCostEstimate | null>(null);
  const [costLoading, setCostLoading] = useState(false);

  useEffect(() => {
    if (!open) {
      return;
    }
    setMode("create");
    setProductName(recipeName.trim() || "");
    setBaseUnit(standardYieldUom);
    setCanBeSold(true);
    setCanBeIngredient(false);
    setSellingPrice("");
    setUserEditedSellingPrice(false);
    setTargetMargin(DEFAULT_TARGET_GROSS_MARGIN);
    setCustomMarginRaw("");
    setUsingCustomMargin(false);
    setCategoryId("");
    setSearch("");
    setLocalError(null);
    setBusy(false);
  }, [open, recipeName, standardYieldUom]);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    if (!open) {
      return;
    }
    let cancelled = false;
    setCostLoading(true);
    void (async () => {
      try {
        const lines = await Promise.all(
          materials.map(async (material) => {
            const [unitCost, product] = await Promise.all([
              resolveLatestAcquisitionUnitCost(workspace, material.materialProductId),
              getCatalogProduct(workspace, material.materialProductId).catch(() => null),
            ]);
            let multiplier = 1;
            if (product && material.productUnitId) {
              const unit = (product.units ?? []).find((u) => u.unitId === material.productUnitId);
              if (unit?.multiplierToBase && unit.multiplierToBase > 0) {
                multiplier = unit.multiplierToBase;
              }
            }
            return {
              materialProductId: material.materialProductId,
              name: material.name,
              baseQuantity: toBaseQuantityForCost(material.quantity, multiplier),
              unitCost,
            };
          }),
        );
        if (!cancelled) {
          setCostEstimate(buildRecipeMaterialCostEstimate(lines));
        }
      } catch {
        if (!cancelled) {
          setCostEstimate(null);
        }
      } finally {
        if (!cancelled) {
          setCostLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [open, materials, workspace]);

  const categoriesQuery = useQuery({
    queryKey: ["catalog-categories", "recipe-output", workspace.organizationId],
    enabled: open && mode === "create",
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace, { status: "Active", pageSize: 100 }, signal),
  });

  const existingQuery = useQuery({
    queryKey: [
      "catalog-products",
      "recipe-output-existing",
      workspace.organizationId,
      debounced,
    ],
    enabled: open && mode === "existing",
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace,
        { search: debounced || undefined, status: "Active", pageSize: 40 },
        signal,
      ),
  });

  const existingCandidates = useMemo(() => {
    return (existingQuery.data?.items ?? []).filter(isEligibleExistingOutput);
  }, [existingQuery.data?.items]);

  const unitMaterialCost = estimatedUnitMaterialCost(
    costEstimate?.batchCost ?? null,
    standardYieldQty,
  );

  const costBasis = useMemo(
    () =>
      resolveMaterialCostBasis({
        unitCost: unitMaterialCost,
        knownLineCount: costEstimate?.knownLineCount ?? 0,
        missingLineCount: costEstimate?.missingLineCount ?? 0,
      }),
    [unitMaterialCost, costEstimate?.knownLineCount, costEstimate?.missingLineCount],
  );

  const suggestion = useMemo(() => {
    if (!canBeSold || !costBasis.complete || costBasis.unitCost == null) {
      return null;
    }
    return suggestSellingPriceFromUnitCost(costBasis.unitCost, targetMargin);
  }, [canBeSold, costBasis, targetMargin]);

  // Auto-fill suggested price only while the user has not manually edited.
  useEffect(() => {
    if (!open || !canBeSold || userEditedSellingPrice || !suggestion) {
      return;
    }
    setSellingPrice(String(suggestion.rounded));
  }, [open, canBeSold, userEditedSellingPrice, suggestion]);

  const priceNum = Number(sellingPrice);
  const margin =
    canBeSold && Number.isFinite(priceNum)
      ? estimatedMaterialMargin(priceNum, unitMaterialCost)
      : null;

  const belowCost =
    canBeSold &&
    unitMaterialCost != null &&
    Number.isFinite(priceNum) &&
    isSellingBelowCost(priceNum, unitMaterialCost);

  const lowMargin =
    canBeSold &&
    unitMaterialCost != null &&
    Number.isFinite(priceNum) &&
    isLowGrossMargin(priceNum, unitMaterialCost);

  if (!open) {
    return null;
  }

  function applySuggestedPrice() {
    if (!suggestion) {
      return;
    }
    setSellingPrice(String(suggestion.rounded));
    setUserEditedSellingPrice(false);
  }

  function onSellingPriceChange(value: string) {
    setUserEditedSellingPrice(true);
    setSellingPrice(value);
  }

  function selectPresetMargin(marginValue: number) {
    setUsingCustomMargin(false);
    setCustomMarginRaw("");
    setTargetMargin(marginValue);
  }

  function onCustomMarginChange(raw: string) {
    setUsingCustomMargin(true);
    setCustomMarginRaw(raw);
    const parsed = Number(raw);
    if (!Number.isFinite(parsed) || parsed <= 0 || parsed >= 100) {
      return;
    }
    setTargetMargin(parsed / 100);
  }

  async function createAndLink() {
    const trimmed = productName.trim();
    if (!trimmed) {
      setLocalError(t("production.recipes.needOutputName"));
      return;
    }
    const price = canBeSold ? Number(sellingPrice) : 0;
    if (canBeSold && (!Number.isFinite(price) || price < 0 || sellingPrice.trim() === "")) {
      setLocalError(t("production.recipes.invalidSellingPrice"));
      return;
    }
    setBusy(true);
    setLocalError(null);
    try {
      // Omit scope so Owner/Admin create OrganizationStandard; branch managers create BranchLocal.
      const product = await createCatalogProduct(workspace, {
        name: trimmed,
        unitOfMeasure: baseUnit,
        sellingPrice: price,
        sellingMode: sellingModeForUom(String(baseUnit)),
        canBeSold,
        canBeUsedAsIngredient: canBeIngredient,
        isProduced: true,
        categoryId: categoryId || null,
      });
      try {
        // Opening 0 — stock rises only after Produce. Do not send unitCost without qty.
        await enableInventoryTracking(workspace, product.productId, {
          openingQuantity: 0,
        });
      } catch (enableErr) {
        let tracked = false;
        try {
          const account = await getInventoryProduct(workspace, product.productId);
          tracked = account.isTracked === true;
        } catch {
          tracked = false;
        }
        if (!tracked) {
          setLocalError(
            enableErr instanceof PosApiError
              ? (enableErr.problem.detail ?? t("production.recipes.enableTrackingFailed"))
              : t("production.recipes.enableTrackingFailed"),
          );
          return;
        }
      }
      onLinked({ outputProduct: product, createdNew: true });
    } catch (err) {
      setLocalError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.recipes.createOutputFailed"))
          : t("production.recipes.createOutputFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  function selectExisting(product: PosCatalogProductDto) {
    if (materials.some((m) => m.materialProductId === product.productId)) {
      setLocalError(t("production.setups.materialAsOutputForbidden"));
      return;
    }
    onLinked({ outputProduct: product, createdNew: false });
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-0 sm:items-center sm:p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="production-recipe-output-title"
      data-testid="production-recipe-output-sheet"
    >
      <Card className="flex max-h-[92dvh] w-full max-w-lg flex-col gap-3 overflow-y-auto rounded-t-xl p-4 sm:rounded-xl">
        <div className="flex items-start justify-between gap-2">
          <h2
            id="production-recipe-output-title"
            className="m-0 text-[length:var(--exits-text-md)] font-semibold"
          >
            {t("production.recipes.createOutputTitle")}
          </h2>
          <Button type="button" variant="ghost" disabled={busy} onClick={onCancel}>
            {t("sell.cancel")}
          </Button>
        </div>

        <div
          className="flex flex-wrap gap-2"
          role="tablist"
          aria-label={t("production.recipes.outputMode")}
          data-testid="production-recipe-output-mode"
        >
          <Button
            type="button"
            variant={mode === "create" ? "default" : "outline"}
            data-testid="production-recipe-output-mode-create"
            onClick={() => setMode("create")}
          >
            {t("production.recipes.createNewOutput")}
          </Button>
          <Button
            type="button"
            variant={mode === "existing" ? "default" : "outline"}
            data-testid="production-recipe-output-mode-existing"
            onClick={() => setMode("existing")}
          >
            {t("production.recipes.useExistingOutput")}
          </Button>
        </div>

        {localError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {localError}
          </p>
        ) : null}

        {mode === "create" ? (
          <>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("production.recipes.outputProductName")}
              <input
                className="rounded-md border border-border bg-background px-3"
                value={productName}
                onChange={(e) => setProductName(e.target.value)}
                disabled={busy}
                data-testid="production-recipe-output-name"
              />
            </label>

            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("production.recipes.standardProducedQty")}: {standardYieldQty} {baseUnit}
            </p>

            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("production.recipes.baseUnit")}
              <select
                className="rounded-md border border-border bg-background px-2"
                value={baseUnit}
                onChange={(e) => setBaseUnit(e.target.value)}
                disabled={busy}
                data-testid="production-recipe-output-base-unit"
              >
                {["Piece", "Kilogram", "Gram", "Liter", "Pack", "Box"].map((code) => (
                  <option key={code} value={code}>
                    {code}
                  </option>
                ))}
              </select>
            </label>

            <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
              <legend className="mb-1 text-[length:var(--exits-text-sm)] font-medium">
                {t("production.recipes.canBeSold")}
              </legend>
              <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <input
                  type="radio"
                  name="recipe-can-sell"
                  checked={canBeSold}
                  onChange={() => setCanBeSold(true)}
                  data-testid="production-recipe-can-sell-yes"
                />
                {t("production.recipes.yes")}
              </label>
              <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <input
                  type="radio"
                  name="recipe-can-sell"
                  checked={!canBeSold}
                  onChange={() => setCanBeSold(false)}
                  data-testid="production-recipe-can-sell-no"
                />
                {t("production.recipes.no")}
              </label>
            </fieldset>

            <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
              <input
                type="checkbox"
                checked={canBeIngredient}
                onChange={(e) => setCanBeIngredient(e.target.checked)}
                data-testid="production-recipe-can-ingredient"
              />
              {t("production.recipes.canBeIngredient")}
            </label>

            <div
              className="rounded-md border border-border bg-[var(--exits-surface-muted)] p-3 text-[length:var(--exits-text-sm)]"
              data-testid="production-recipe-estimated-cost"
            >
              <div className="font-medium">{t("production.recipes.estimatedMaterialCost")}</div>
              {costLoading ? (
                <LoadingState label={t("production.loading")} />
              ) : unitMaterialCost != null ? (
                <>
                  <p className="m-0 mt-1.5 flex flex-wrap justify-between gap-2">
                    <span>{t("production.recipes.estimatedBatchCost")}</span>
                    <span className="tabular-nums font-medium">
                      {formatPeso(costEstimate!.batchCost!)}
                    </span>
                  </p>
                  <p className="m-0 mt-1 flex flex-wrap justify-between gap-2">
                    <span>
                      {t("production.recipes.estimatedUnitCostLabel").replace(
                        "{uom}",
                        String(baseUnit),
                      )}
                    </span>
                    <span
                      className="tabular-nums font-medium"
                      data-testid="production-recipe-unit-material-cost"
                    >
                      {formatPeso(unitMaterialCost)}
                    </span>
                  </p>
                  {(costEstimate?.missingLineCount ?? 0) > 0 ? (
                    <p className="m-0 mt-1 text-muted">
                      {t("production.recipes.estimatedCostPartial")}
                    </p>
                  ) : null}
                </>
              ) : (
                <p className="m-0 mt-1 text-muted">
                  {t("production.recipes.estimatedCostUnavailable")}
                </p>
              )}
            </div>

            {canBeSold ? (
              <div
                className="flex flex-col gap-2"
                data-testid="production-recipe-pricing-section"
              >
                {costBasis.complete && suggestion ? (
                  <>
                    <div className="flex flex-col gap-1.5">
                      <span className="text-[length:var(--exits-text-sm)] font-medium">
                        {t("production.recipes.targetGrossMargin")}
                      </span>
                      <div
                        className="flex flex-wrap gap-1.5"
                        data-testid="production-recipe-target-margin"
                      >
                        {TARGET_GROSS_MARGIN_PRESETS.map((preset) => (
                          <Button
                            key={preset}
                            type="button"
                            variant={
                              !usingCustomMargin && targetMargin === preset ? "default" : "outline"
                            }
                            className="h-8 px-2.5"
                            data-testid={`production-recipe-margin-${Math.round(preset * 100)}`}
                            onClick={() => selectPresetMargin(preset)}
                            disabled={busy}
                          >
                            {marginPresetLabel(preset)}
                          </Button>
                        ))}
                        <Button
                          type="button"
                          variant={usingCustomMargin ? "default" : "outline"}
                          className="h-8 px-2.5"
                          data-testid="production-recipe-margin-custom"
                          onClick={() => {
                            setUsingCustomMargin(true);
                            if (!customMarginRaw) {
                              setCustomMarginRaw(String(Math.round(targetMargin * 100)));
                            }
                          }}
                          disabled={busy}
                        >
                          {t("production.recipes.targetMarginCustom")}
                        </Button>
                      </div>
                      {usingCustomMargin ? (
                        <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                          <input
                            type="number"
                            min={1}
                            max={99}
                            step={1}
                            className="w-20 rounded-md border border-border bg-background px-2 tabular-nums"
                            value={customMarginRaw}
                            onChange={(e) => onCustomMarginChange(e.target.value)}
                            disabled={busy}
                            data-testid="production-recipe-margin-custom-input"
                          />
                          %
                        </label>
                      ) : null}
                    </div>

                    <div
                      className="rounded-md border border-border p-3 text-[length:var(--exits-text-sm)]"
                      data-testid="production-recipe-suggested-price"
                    >
                      <div className="font-medium">
                        {t("production.recipes.suggestedSellingPrice")}
                      </div>
                      <p
                        className="m-0 mt-1 text-[length:var(--exits-text-md)] font-semibold tabular-nums"
                        data-testid="production-recipe-suggested-price-value"
                      >
                        {t("production.recipes.suggestedPriceValue")
                          .replace("{price}", formatPeso(suggestion.rounded))
                          .replace("{uom}", String(baseUnit))}
                      </p>
                      <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                        {t("production.recipes.suggestedPriceBasedOnMaterial")}
                      </p>
                      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                        {t("production.recipes.laborOverheadNotIncluded")}
                      </p>
                      {margin ? (
                        <>
                          <p
                            className="m-0 mt-2"
                            data-testid="production-recipe-gross-profit"
                          >
                            {t("production.recipes.estimatedGrossProfit")}:{" "}
                            {t("production.recipes.estimatedGrossProfitValue")
                              .replace("{amount}", formatPeso(margin.amount))
                              .replace("{uom}", String(baseUnit))}
                          </p>
                          <p
                            className="m-0 mt-0.5 text-muted"
                            data-testid="production-recipe-actual-margin"
                          >
                            {t("production.recipes.estimatedMargin").replace(
                              "{percent}",
                              formatGrossMarginPercent(margin.percent),
                            )}
                          </p>
                        </>
                      ) : null}
                      <Button
                        type="button"
                        variant="outline"
                        className="mt-2 w-fit"
                        disabled={busy}
                        data-testid="production-recipe-use-suggested-price"
                        onClick={applySuggestedPrice}
                      >
                        {t("production.recipes.useSuggestedPrice")}
                      </Button>
                    </div>
                  </>
                ) : (
                  <div
                    className="rounded-md border border-border p-3 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="production-recipe-suggested-unavailable"
                  >
                    <div className="font-medium text-foreground">
                      {t("production.recipes.suggestedPriceUnavailable")}
                    </div>
                    <p className="m-0 mt-1">
                      {costBasis.complete === false && costBasis.reason === "partial"
                        ? t("production.recipes.estimatedCostPartialManual")
                        : t("production.recipes.suggestedPriceUnavailableReason")}
                    </p>
                  </div>
                )}

                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("production.recipes.sellingPrice")}
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    className="rounded-md border border-border bg-background px-3 tabular-nums"
                    value={sellingPrice}
                    onChange={(e) => onSellingPriceChange(e.target.value)}
                    disabled={busy}
                    data-testid="production-recipe-selling-price"
                  />
                </label>

                {belowCost && unitMaterialCost != null ? (
                  <div
                    className="rounded-md border border-[var(--exits-danger)]/40 bg-[var(--exits-danger-soft)] p-3 text-[length:var(--exits-text-sm)] text-destructive"
                    data-testid="production-recipe-below-cost-warning"
                    role="status"
                  >
                    <div className="font-medium">
                      {t("production.recipes.sellingBelowCostWarning")}
                    </div>
                    <p className="m-0 mt-1">
                      {t("production.recipes.sellingBelowCostDetail")
                        .replace("{cost}", formatPeso(unitMaterialCost))
                        .replace("{price}", formatPeso(priceNum))
                        .replace("{loss}", formatPeso(unitMaterialCost - priceNum))
                        .replace("{uom}", String(baseUnit))}
                    </p>
                  </div>
                ) : null}

                {lowMargin && !belowCost ? (
                  <div
                    className="rounded-md border border-border bg-[var(--exits-surface-muted)] p-3 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="production-recipe-low-margin-warning"
                    role="status"
                  >
                    {t("production.recipes.lowMarginWarning")}
                  </div>
                ) : null}
              </div>
            ) : null}

            {(categoriesQuery.data?.items.length ?? 0) > 0 ? (
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("production.recipes.category")}
                <select
                  className="rounded-md border border-border bg-background px-2"
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  disabled={busy}
                  data-testid="production-recipe-category"
                >
                  <option value="">{t("production.recipes.categoryNone")}</option>
                  {categoriesQuery.data!.items.map((cat) => (
                    <option key={cat.categoryId} value={cat.categoryId}>
                      {cat.name}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}

            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="production-recipe-stock-zero-hint"
            >
              {t("production.recipes.stockZeroUntilProduce")}
            </p>

            <Button
              type="button"
              className="w-full"
              disabled={busy}
              onClick={() => void createAndLink()}
              data-testid="production-recipe-create-and-save"
            >
              {busy
                ? t("production.setups.saving")
                : t("production.recipes.createProductAndSave")}
            </Button>
          </>
        ) : (
          <>
            <SearchField
              label={t("production.setups.searchOutput")}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onClear={() => setSearch("")}
              placeholder={t("production.setups.searchOutput")}
              data-testid="production-recipe-existing-search"
            />
            {existingCandidates.length === 0 && existingQuery.isSuccess ? (
              <EmptyState
                title={t("production.setups.noProducts")}
                detail={t("production.setups.noProductsDetail")}
              />
            ) : null}
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {existingCandidates.map((product) => (
                <li key={product.productId}>
                  <button
                    type="button"
                    className={cn(
                      "flex w-full flex-col gap-1 rounded-md border border-border bg-surface p-3 text-left",
                    )}
                    disabled={busy}
                    data-testid={`production-recipe-existing-${product.productId}`}
                    onClick={() => selectExisting(product)}
                  >
                    <span className="font-medium">{product.name}</span>
                    <span className="text-[length:var(--exits-text-sm)] text-muted">
                      {materialBaseUomLabel(product)}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
      </Card>
    </div>
  );
}
