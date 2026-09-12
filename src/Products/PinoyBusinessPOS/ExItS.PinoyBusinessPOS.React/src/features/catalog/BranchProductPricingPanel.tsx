import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, RotateCcw, Save } from "lucide-react";
import {
  getBranchProductPricing,
  removeBranchProductPriceOverride,
  setBranchProductPriceOverride,
} from "@/api/pos/pos-catalog-client";
import type {
  BranchProductPricingItemDto,
  PosCatalogProductDto,
} from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useToast } from "@/components/exits/ToastProvider";
import {
  type BranchPriceMode,
  resolveBranchPriceMode,
} from "@/features/catalog/branch-pricing-ux";
import { isOrganizationStandardProduct } from "@/features/catalog/catalog-product-scope";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";

type RowKey = "base" | string;

function pricingRowKey(productUnitId: string | null | undefined): RowKey {
  return productUnitId ?? "base";
}

type PriceRowDraft = {
  productUnitId: string | null;
  organizationDefaultPrice: number;
  branchOverridePrice: string;
  effectivePrice: number;
  hasBranchPriceOverride: boolean;
};

function itemToDraft(item: BranchProductPricingItemDto): PriceRowDraft {
  return {
    productUnitId: item.productUnitId ?? null,
    organizationDefaultPrice: item.organizationDefaultPrice,
    branchOverridePrice:
      item.branchOverridePrice != null ? String(item.branchOverridePrice) : "",
    effectivePrice: item.effectivePrice,
    hasBranchPriceOverride: item.hasBranchPriceOverride,
  };
}

export function parseBranchOverridePrice(
  raw: string,
): { ok: true; value: number } | { ok: false } {
  const trimmed = raw.trim();
  if (!trimmed) {
    return { ok: false };
  }
  const value = Number(trimmed);
  if (Number.isNaN(value) || value < 0) {
    return { ok: false };
  }
  return { ok: true, value };
}

function isDraftDirty(draft: PriceRowDraft, mode: BranchPriceMode): boolean {
  if (mode === "inherit") {
    return draft.hasBranchPriceOverride;
  }
  const parsed = parseBranchOverridePrice(draft.branchOverridePrice);
  if (!parsed.ok) {
    return draft.branchOverridePrice.trim().length > 0;
  }
  if (!draft.hasBranchPriceOverride) {
    return true;
  }
  const saved = draft.hasBranchPriceOverride ? Number(draft.branchOverridePrice) : null;
  return saved == null || saved !== parsed.value;
}

function BranchPricingRow(props: {
  /** Optional subsection title (e.g. alternate unit). Base selling price has no nested title. */
  label: string | null;
  branchLabel: string;
  draft: PriceRowDraft;
  mode: BranchPriceMode;
  disabled: boolean;
  onModeChange: (mode: BranchPriceMode) => void;
  onDraftChange: (value: string) => void;
  onSaveCustom: () => void;
  onUseOrganizationDefault: () => void;
  saving: boolean;
  removing: boolean;
  saveLabel: string;
  savingLabel: string;
  useOrganizationDefaultLabel: string;
  removingLabel: string;
  organizationDefaultLabel: string;
  organizationDefaultHint: string;
  branchSellingPriceLabel: string;
  useOrganizationDefaultModeLabel: string;
  customBranchPriceModeLabel: string;
  effectivePriceLabel: string;
  customPriceInputLabel: string;
  invalidPriceLabel: string;
}) {
  const parsed = parseBranchOverridePrice(props.draft.branchOverridePrice);
  const showCustomInput = props.mode === "custom";
  const canSaveCustom =
    props.mode === "custom" && isDraftDirty(props.draft, props.mode) && parsed.ok;
  const rowId = props.draft.productUnitId ?? "base";

  return (
    <div
      className={cn(
        "branch-pricing-row flex flex-col gap-3",
        props.label && "border-t border-[color:var(--exits-border)] pt-3",
      )}
      data-testid={
        props.draft.productUnitId
          ? `branch-pricing-unit-${props.draft.productUnitId}`
          : "branch-pricing-base"
      }
    >
      {props.label ? (
        <span className="text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {props.label}
        </span>
      ) : null}

      <div className="branch-pricing-row__org-default flex flex-col gap-0.5">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-foreground">
          {props.organizationDefaultLabel}
        </p>
        <p
          className="m-0 text-[length:var(--exits-text-lg)] font-semibold tabular-nums text-foreground"
          data-testid={`${rowId}-organization-default`}
        >
          {formatPeso(props.draft.organizationDefaultPrice)}
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {props.organizationDefaultHint}
        </p>
      </div>

      <div className="flex flex-col gap-2">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-medium text-foreground">
          {props.branchSellingPriceLabel.replace("{branch}", props.branchLabel)}
        </p>

        <fieldset className="m-0 border-0 p-0" disabled={props.disabled}>
          <div className="catalog-choice-options catalog-choice-options--2">
            <label
              className={cn(
                "catalog-choice-options__item",
                props.mode === "inherit" && "catalog-choice-options__item--selected",
              )}
            >
              <input
                type="radio"
                name={`branch-price-mode-${rowId}`}
                checked={props.mode === "inherit"}
                onChange={() => props.onModeChange("inherit")}
                className="mt-1 shrink-0"
                data-testid={`${rowId}-mode-inherit`}
              />
              <span className="block min-w-0 text-[length:var(--exits-text-sm)] font-semibold">
                {props.useOrganizationDefaultModeLabel}
              </span>
            </label>

            <label
              className={cn(
                "catalog-choice-options__item",
                props.mode === "custom" && "catalog-choice-options__item--selected",
              )}
            >
              <input
                type="radio"
                name={`branch-price-mode-${rowId}`}
                checked={props.mode === "custom"}
                onChange={() => props.onModeChange("custom")}
                className="mt-1 shrink-0"
                data-testid={`${rowId}-mode-custom`}
              />
              <span className="block text-[length:var(--exits-text-sm)] font-semibold">
                {props.customBranchPriceModeLabel}
              </span>
            </label>
          </div>
        </fieldset>

        {showCustomInput ? (
          <Input
            label={props.customPriceInputLabel}
            name={`branchOverride-${rowId}`}
            inputMode="decimal"
            value={props.draft.branchOverridePrice}
            disabled={props.disabled}
            onChange={(event) => props.onDraftChange(event.target.value)}
            data-testid={`${rowId}-custom-price-input`}
          />
        ) : null}

        {!parsed.ok && showCustomInput && props.draft.branchOverridePrice.trim() ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">
            {props.invalidPriceLabel}
          </p>
        ) : null}

        <div className="flex flex-col gap-0.5">
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {props.effectivePriceLabel}
          </span>
          <span
            className="text-[length:var(--exits-text-lg)] font-semibold tabular-nums text-foreground"
            data-testid={`${rowId}-effective-price`}
          >
            {formatPeso(
              props.mode === "custom" && parsed.ok
                ? parsed.value
                : props.draft.effectivePrice,
            )}
          </span>
        </div>

        <div className="flex flex-wrap gap-2">
          {showCustomInput ? (
            <Button
              type="button"
              variant="outline"
              disabled={props.disabled || !canSaveCustom || props.saving || props.removing}
              data-testid={`${rowId}-save-override`}
              onClick={props.onSaveCustom}
            >
              {props.saving ? (
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
              ) : (
                <Save className="size-4 shrink-0" aria-hidden />
              )}
              {props.saving ? props.savingLabel : props.saveLabel}
            </Button>
          ) : null}
          {props.draft.hasBranchPriceOverride || props.mode === "custom" ? (
            <Button
              type="button"
              variant="outline"
              disabled={props.disabled || props.saving || props.removing}
              data-testid={`${rowId}-use-organization-default`}
              onClick={props.onUseOrganizationDefault}
            >
              {props.removing ? (
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
              ) : (
                <RotateCcw className="size-4 shrink-0" aria-hidden />
              )}
              {props.removing ? props.removingLabel : props.useOrganizationDefaultLabel}
            </Button>
          ) : null}
        </div>
      </div>
    </div>
  );
}

export function BranchProductPricingPanel(props: {
  workspace: PosWorkspaceScope;
  productId: string;
  product: Pick<PosCatalogProductDto, "scope" | "units"> | null | undefined;
  canGovern: boolean;
  branchName?: string | null;
}) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const branchId = props.workspace.branchId;
  const enabled =
    props.canGovern &&
    isOrganizationStandardProduct(props.product) &&
    Boolean(branchId) &&
    Boolean(props.productId);

  const pricingQuery = useQuery({
    queryKey: [
      "catalog",
      "branch-pricing",
      props.workspace.organizationId,
      branchId,
      props.productId,
    ],
    enabled,
    queryFn: ({ signal }) =>
      getBranchProductPricing(props.workspace, props.productId, branchId!, signal),
  });

  const [drafts, setDrafts] = useState<Record<RowKey, PriceRowDraft>>({});
  const [modes, setModes] = useState<Record<RowKey, BranchPriceMode>>({});
  const [error, setError] = useState<string | null>(null);
  const [pendingKey, setPendingKey] = useState<{ key: RowKey; action: "save" | "remove" } | null>(
    null,
  );

  useEffect(() => {
    if (!pricingQuery.data) {
      return;
    }
    const nextDrafts: Record<RowKey, PriceRowDraft> = {
      base: itemToDraft(pricingQuery.data.basePrice),
    };
    const nextModes: Record<RowKey, BranchPriceMode> = {
      base: resolveBranchPriceMode(pricingQuery.data.basePrice.hasBranchPriceOverride),
    };
    for (const unit of pricingQuery.data.unitPrices) {
      const key = pricingRowKey(unit.productUnitId);
      nextDrafts[key] = itemToDraft(unit);
      nextModes[key] = resolveBranchPriceMode(unit.hasBranchPriceOverride);
    }
    setDrafts(nextDrafts);
    setModes(nextModes);
  }, [pricingQuery.data]);

  const invalidate = async () => {
    await queryClient.invalidateQueries({
      queryKey: ["catalog", "branch-pricing", props.workspace.organizationId],
    });
    await queryClient.invalidateQueries({ queryKey: ["catalog"] });
  };

  const saveMutation = useMutation({
    mutationFn: async (input: { key: RowKey; draft: PriceRowDraft }) => {
      if (!branchId) {
        throw new Error("Branch required");
      }
      const parsed = parseBranchOverridePrice(input.draft.branchOverridePrice);
      if (!parsed.ok) {
        throw new Error(t("catalog.invalidPrice"));
      }
      await setBranchProductPriceOverride(props.workspace, props.productId, {
        branchId,
        sellingPrice: parsed.value,
        productUnitId: input.draft.productUnitId,
      });
    },
    onMutate: (input) => {
      setPendingKey({ key: input.key, action: "save" });
    },
    onSuccess: async () => {
      setError(null);
      showToast(t("catalog.branchPricing.saved"));
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
    onSettled: () => {
      setPendingKey(null);
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (input: { key: RowKey; draft: PriceRowDraft }) => {
      if (!branchId) {
        throw new Error("Branch required");
      }
      await removeBranchProductPriceOverride(
        props.workspace,
        props.productId,
        branchId,
        input.draft.productUnitId,
      );
    },
    onMutate: (input) => {
      setPendingKey({ key: input.key, action: "remove" });
    },
    onSuccess: async () => {
      setError(null);
      showToast(t("catalog.branchPricing.removed"));
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
    onSettled: () => {
      setPendingKey(null);
    },
  });

  if (!props.canGovern || !isOrganizationStandardProduct(props.product)) {
    return null;
  }

  const branchLabel = props.branchName ?? branchId ?? "—";
  const rowLabels = {
    saveLabel: t("catalog.branchPricing.saveCustom"),
    savingLabel: t("catalog.branchPricing.saving"),
    useOrganizationDefaultLabel: t("catalog.branchPricing.useOrganizationDefault"),
    removingLabel: t("catalog.branchPricing.removing"),
    organizationDefaultLabel: t("catalog.branchPricing.organizationDefault"),
    organizationDefaultHint: t("catalog.branchPricing.inheritedByBranches"),
    branchSellingPriceLabel: t("catalog.branchPricing.branchSellingPrice"),
    useOrganizationDefaultModeLabel: t("catalog.branchPricing.useOrganizationDefaultMode"),
    customBranchPriceModeLabel: t("catalog.branchPricing.customBranchPriceMode"),
    effectivePriceLabel: t("catalog.branchPricing.effectivePrice"),
    customPriceInputLabel: t("catalog.branchPricing.customPriceInput"),
    invalidPriceLabel: t("catalog.invalidPrice"),
  };

  const unitNameById = new Map(
    (props.product?.units ?? []).map((unit) => [unit.unitId, unit.displayName]),
  );

  const orderedKeys: RowKey[] = ["base", ...Object.keys(drafts).filter((key) => key !== "base")];

  return (
    <section
      className="catalog-form-section exits-animate-panel"
      data-testid="catalog-branch-pricing"
    >
      <h2 className="catalog-form-section__title">
        {t("catalog.branchPricing.title").replace("{branch}", branchLabel)}
      </h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("catalog.branchPricing.hint").replace("{branch}", branchLabel)}
      </p>
      {!branchId ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.branchPricing.branchRequired")}
        </p>
      ) : null}
      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">{error}</p>
      ) : null}
      {branchId && pricingQuery.isLoading ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
      ) : null}
      {branchId && pricingQuery.isError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">
          {pricingQuery.error instanceof PosApiError
            ? (pricingQuery.error.problem.detail ?? pricingQuery.error.message)
            : (pricingQuery.error as Error).message}
        </p>
      ) : null}
      {branchId && !pricingQuery.isLoading && !pricingQuery.isError ? (
        <div className="flex flex-col gap-3">
          {orderedKeys.map((key) => {
            const draft = drafts[key];
            const mode = modes[key] ?? "inherit";
            if (!draft) {
              return null;
            }
            // Base selling price sits directly in the branch card — no nested "Base unit price" card.
            const label =
              key === "base"
                ? null
                : t("catalog.branchPricing.unitPrice").replace(
                    "{name}",
                    unitNameById.get(draft.productUnitId ?? "") ?? draft.productUnitId ?? "—",
                  );
            const saving = pendingKey?.key === key && pendingKey.action === "save";
            const removing = pendingKey?.key === key && pendingKey.action === "remove";
            return (
              <BranchPricingRow
                key={key}
                label={label}
                branchLabel={branchLabel}
                draft={draft}
                mode={mode}
                disabled={saveMutation.isPending || removeMutation.isPending}
                saving={saving}
                removing={removing}
                onModeChange={(nextMode) => {
                  setModes((current) => ({ ...current, [key]: nextMode }));
                  if (nextMode === "custom" && !draft.branchOverridePrice.trim()) {
                    setDrafts((current) => {
                      const row = current[key];
                      if (!row) {
                        return current;
                      }
                      return {
                        ...current,
                        [key]: {
                          ...row,
                          branchOverridePrice: String(row.organizationDefaultPrice),
                        },
                      };
                    });
                  }
                }}
                onDraftChange={(value) =>
                  setDrafts((current) => {
                    const row = current[key];
                    if (!row) {
                      return current;
                    }
                    return {
                      ...current,
                      [key]: { ...row, branchOverridePrice: value },
                    };
                  })
                }
                onSaveCustom={() => saveMutation.mutate({ key, draft })}
                onUseOrganizationDefault={() => {
                  if (draft.hasBranchPriceOverride) {
                    removeMutation.mutate({ key, draft });
                    return;
                  }
                  setModes((current) => ({ ...current, [key]: "inherit" }));
                  setDrafts((current) => {
                    const row = current[key];
                    if (!row) {
                      return current;
                    }
                    return {
                      ...current,
                      [key]: { ...row, branchOverridePrice: "" },
                    };
                  });
                }}
                {...rowLabels}
              />
            );
          })}
        </div>
      ) : null}
    </section>
  );
}
