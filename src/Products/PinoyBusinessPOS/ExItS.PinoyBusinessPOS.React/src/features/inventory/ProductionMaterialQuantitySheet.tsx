import { useEffect, useMemo, useState } from "react";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { BottomSheet } from "@/components/exits/SheetDialog";
import {
  formatQuantityDisplay,
  type WeightInputUnit,
} from "@/cart/sell-cart-helpers";
import {
  activeMaterialUnits,
  draftDisplayQuantity,
  formatMaterialAvailableCaption,
  normalizeMaterialQuantityInput,
  resolveMaterialEntryMode,
  type ProductionMaterialDraft,
} from "@/features/inventory/production-material-uom";
import { useI18n } from "@/i18n/I18nProvider";

type ProductionMaterialQuantitySheetProps = {
  open: boolean;
  product: PosCatalogProductDto | null;
  initialDraft?: ProductionMaterialDraft | null;
  onConfirm: (draft: ProductionMaterialDraft) => void;
  onCancel: () => void;
};

export function ProductionMaterialQuantitySheet({
  open,
  product,
  initialDraft = null,
  onConfirm,
  onCancel,
}: ProductionMaterialQuantitySheetProps) {
  const { t } = useI18n();
  const [rawValue, setRawValue] = useState("1");
  const [weightUnit, setWeightUnit] = useState<WeightInputUnit>("kg");
  const [productUnitId, setProductUnitId] = useState<string | null>(null);
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    if (open && product && !opened) {
      const mode = resolveMaterialEntryMode(product);
      if (initialDraft) {
        setRawValue(draftDisplayQuantity(initialDraft));
        setWeightUnit(initialDraft.weightInputUnit === "g" ? "g" : "kg");
        setProductUnitId(initialDraft.productUnitId ?? null);
      } else if (mode === "weight") {
        setWeightUnit("kg");
        setRawValue("");
        setProductUnitId(null);
      } else if (mode === "unit") {
        const units = activeMaterialUnits(product);
        setProductUnitId(units[0]?.unitId ?? null);
        setRawValue("1");
        setWeightUnit("kg");
      } else {
        setRawValue("1");
        setProductUnitId(null);
        setWeightUnit("kg");
      }
      setOpened(true);
    } else if (!open) {
      setOpened(false);
    }
  }, [initialDraft, open, opened, product]);

  const mode = product ? resolveMaterialEntryMode(product) : "base";
  const units = product ? activeMaterialUnits(product) : [];
  const availableCaption = product
    ? formatMaterialAvailableCaption(product, t("transfer.available"))
    : null;

  const parsed = useMemo(() => {
    if (!product) {
      return null;
    }
    const raw = Number(rawValue);
    return normalizeMaterialQuantityInput({
      product,
      rawValue: raw,
      weightUnit,
      productUnitId,
    });
  }, [product, productUnitId, rawValue, weightUnit]);

  const errorMessage =
    parsed && !parsed.ok
      ? parsed.error === "zero"
        ? t("production.setups.invalidQuantity")
        : parsed.error === "precision"
          ? t("sell.weightErrorPrecision")
          : t("production.setups.invalidQuantity")
      : null;

  if (!product) {
    return null;
  }

  return (
    <BottomSheet
      open={open}
      onClose={onCancel}
      title={product.name}
      panelId="production-material-qty-sheet"
      testId="production-material-qty-sheet"
      closeLabel={t("sell.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      panelClassName="max-w-md"
    >
      <div className="flex flex-col gap-3 p-1">
        {availableCaption ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="production-material-available"
          >
            {availableCaption}
          </p>
        ) : null}

        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("production.setups.requiredQuantity")}
          <div className="flex flex-wrap items-stretch gap-2">
            <input
              type="number"
              inputMode="decimal"
              min={mode === "weight" && weightUnit === "g" ? 1 : 0}
              step={mode === "weight" && weightUnit === "g" ? 1 : "any"}
              className="min-w-0 flex-1 rounded-md border border-border bg-background px-3 tabular-nums"
              value={rawValue}
              onChange={(e) => setRawValue(e.target.value)}
              data-testid="production-material-qty-input"
              autoFocus
            />
            {mode === "weight" ? (
              <select
                className="rounded-md border border-border bg-background px-2"
                value={weightUnit}
                onChange={(e) => setWeightUnit(e.target.value as WeightInputUnit)}
                data-testid="production-material-weight-unit"
                aria-label={t("sell.weightUnit")}
              >
                <option value="kg">{t("sell.weightUnitKg")}</option>
                <option value="g">{t("sell.weightUnitG")}</option>
              </select>
            ) : mode === "unit" ? (
              <select
                className="rounded-md border border-border bg-background px-2"
                value={productUnitId ?? ""}
                onChange={(e) => setProductUnitId(e.target.value || null)}
                data-testid="production-material-unit"
                aria-label={t("production.setups.materialQuantity")}
              >
                {units.map((unit) => (
                  <option key={unit.unitId} value={unit.unitId}>
                    {unit.shortLabel || unit.displayName}
                  </option>
                ))}
              </select>
            ) : (
              <span
                className="inline-flex items-center rounded-md border border-border bg-[var(--exits-surface-muted)] px-3 text-[length:var(--exits-text-sm)]"
                data-testid="production-material-base-uom"
              >
                {product.unitOfMeasure}
              </span>
            )}
          </div>
        </label>

        {parsed?.ok && mode === "weight" && weightUnit === "g" ? (
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="production-material-kg-preview">
            = {formatQuantityDisplay(parsed.quantity)} kg
          </p>
        ) : null}

        {errorMessage ? (
          <p role="alert" className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {errorMessage}
          </p>
        ) : null}

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            className="flex-1"
            disabled={!parsed?.ok}
            data-testid="production-material-qty-confirm"
            onClick={() => {
              if (!parsed?.ok) {
                return;
              }
              onConfirm({
                materialProductId: product.productId,
                name: product.name,
                quantity: parsed.quantity,
                displayUom: parsed.displayUom,
                productUnitId: parsed.productUnitId ?? null,
                weightInputUnit: parsed.weightInputUnit ?? null,
              });
            }}
          >
            {initialDraft ? t("sell.weightUpdate") : t("production.setups.addMaterial")}
          </Button>
          <Button type="button" variant="outline" className="flex-1" onClick={onCancel}>
            {t("sell.cancel")}
          </Button>
        </div>
      </div>
    </BottomSheet>
  );
}
