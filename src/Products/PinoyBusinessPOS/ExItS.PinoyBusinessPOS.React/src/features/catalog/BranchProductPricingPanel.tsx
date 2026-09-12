import { useEffect, useState, type ReactNode } from "react";
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
import { SettingsSelect } from "@/components/ui/settings-select";
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

export type OrganizationPriceEditorProps = {
  value: string;
  onChange: (value: string) => void;
  warning?: string | null;
};

function BranchPricingRow(props: {
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
  priceSourceLabel: string;
  useOrganizationDefaultModeLabel: string;
  customBranchPriceModeLabel: string;
  effectivePriceLabel: string;
  customPriceInputLabel: string;
  invalidPriceLabel: string;
  /** Compact branch column (inside Selling price card); omit branch heading when false for units. */
  showBranchHeading?: boolean;
}) {
  const parsed = parseBranchOverridePrice(props.draft.branchOverridePrice);
  const showCustomInput = props.mode === "custom";
  const canSaveCustom =
    props.mode === "custom" && isDraftDirty(props.draft, props.mode) && parsed.ok;
  const rowId = props.draft.productUnitId ?? "base";
  const showBranchHeading = props.showBranchHeading !== false;

  return (
    <div
      className={cn(
        "branch-pricing-row flex min-w-0 flex-col gap-2.5",
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

      {showBranchHeading ? (
        <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {props.branchLabel}
        </h3>
      ) : null}

      <fieldset className="m-0 min-w-0 border-0 p-0" disabled={props.disabled}>
        <SettingsSelect<BranchPriceMode>
          label={props.priceSourceLabel}
          value={props.mode}
          onChange={props.onModeChange}
          variant="segmented"
          testId={`${rowId}-price-source`}
          options={[
            {
              value: "inherit",
              label: props.useOrganizationDefaultModeLabel,
              testId: `${rowId}-mode-inherit`,
            },
            {
              value: "custom",
              label: props.customBranchPriceModeLabel,
              testId: `${rowId}-mode-custom`,
            },
          ]}
        />
      </fieldset>

      <div
        className="flex items-baseline justify-between gap-3"
        data-testid={`${rowId}-organization-default-summary`}
      >
        <span className="text-[length:var(--exits-text-sm)] text-muted">
          {props.organizationDefaultLabel}
        </span>
        <span
          className="text-[length:var(--exits-text-sm)] font-semibold tabular-nums text-foreground"
          data-testid={`${rowId}-organization-default`}
        >
          {formatPeso(props.draft.organizationDefaultPrice)}
        </span>
      </div>

      {showCustomInput ? (
        <div className="selling-price-field max-w-[17.5rem]">
          <Input
            label={props.customPriceInputLabel}
            name={`branchOverride-${rowId}`}
            inputMode="decimal"
            value={props.draft.branchOverridePrice}
            disabled={props.disabled}
            onChange={(event) => props.onDraftChange(event.target.value)}
            data-testid={`${rowId}-custom-price-input`}
          />
        </div>
      ) : null}

      {!parsed.ok && showCustomInput && props.draft.branchOverridePrice.trim() ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">
          {props.invalidPriceLabel}
        </p>
      ) : null}

      <div className="border-t border-[color:var(--exits-border)] pt-2.5">
        <div className="flex items-baseline justify-between gap-3">
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
              <Loader2 className="size-4 shrink-0 animate-spin text-[var(--exits-primary)]" aria-hidden />
            ) : (
              <Save className="size-4 shrink-0 text-[var(--exits-primary)]" aria-hidden />
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
              <Loader2 className="size-4 shrink-0 animate-spin text-[var(--exits-primary)]" aria-hidden />
            ) : (
              <RotateCcw className="size-4 shrink-0 text-[var(--exits-primary)]" aria-hidden />
            )}
            {props.removing ? props.removingLabel : props.useOrganizationDefaultLabel}
          </Button>
        ) : null}
      </div>
    </div>
  );
}

/**
 * Combined Selling price card: organization default (optional) + branch price source.
 * Persistence / effective-price logic unchanged.
 */
export function BranchProductPricingPanel(props: {
  workspace: PosWorkspaceScope;
  productId: string;
  product: Pick<PosCatalogProductDto, "scope" | "units"> | null | undefined;
  canGovern: boolean;
  branchName?: string | null;
  /** When set, renders editable Organization default in the left column. */
  organizationEditor?: OrganizationPriceEditorProps | null;
  organizationEditorExtra?: ReactNode;
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
    priceSourceLabel: t("catalog.branchPricing.priceSource"),
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
  const hasOrgEditor = Boolean(props.organizationEditor);

  const renderRow = (key: RowKey, options?: { showBranchHeading?: boolean }) => {
    const draft = drafts[key];
    const mode = modes[key] ?? "inherit";
    if (!draft) {
      return null;
    }
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
        showBranchHeading={options?.showBranchHeading}
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
  };

  return (
    <section
      className="catalog-form-section exits-animate-panel selling-price-card"
      data-testid="catalog-selling-price"
    >
      <h2 className="catalog-form-section__title">{t("catalog.sellingPrice.title")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("catalog.sellingPrice.hint")}
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

      <div
        className={cn(
          "selling-price-card__columns",
          hasOrgEditor && "selling-price-card__columns--split",
        )}
      >
        {props.organizationEditor ? (
          <div
            className="selling-price-card__org flex min-w-0 flex-col gap-2.5"
            data-testid="catalog-organization-pricing"
          >
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
              {t("catalog.branchPricing.organizationDefault")}
            </h3>
            <div className="selling-price-field max-w-[17.5rem]">
              <Input
                label={t("catalog.organizationPricing.defaultPrice")}
                name="organizationDefaultSellingPrice"
                inputMode="decimal"
                value={props.organizationEditor.value}
                onChange={(e) => props.organizationEditor?.onChange(e.target.value)}
                data-testid="catalog-organization-default-price"
              />
            </div>
            <p className="m-0 max-w-[17.5rem] text-[length:var(--exits-text-xs)] text-muted">
              {t("catalog.organizationPricing.hint")}
            </p>
            {props.organizationEditor.warning ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="catalog-organization-default-warning"
              >
                {props.organizationEditor.warning}
              </p>
            ) : null}
            {props.organizationEditorExtra}
          </div>
        ) : null}

        {branchId && !pricingQuery.isLoading && !pricingQuery.isError ? (
          <div className="selling-price-card__branch flex min-w-0 flex-col gap-3">
            {renderRow("base", { showBranchHeading: true })}
            {orderedKeys
              .filter((key) => key !== "base")
              .map((key) => renderRow(key, { showBranchHeading: false }))}
          </div>
        ) : null}
      </div>
    </section>
  );
}
