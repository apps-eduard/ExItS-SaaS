import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Save, Trash2 } from "lucide-react";
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
import { isOrganizationStandardProduct } from "@/features/catalog/catalog-product-scope";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";

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

function parseOverridePrice(raw: string): { ok: true; value: number } | { ok: false } {
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

function isDraftDirty(draft: PriceRowDraft): boolean {
  const saved =
    draft.hasBranchPriceOverride && draft.branchOverridePrice.trim()
      ? draft.branchOverridePrice.trim()
      : "";
  const current = draft.branchOverridePrice.trim();
  return current !== saved;
}

function BranchPricingRow(props: {
  label: string;
  draft: PriceRowDraft;
  disabled: boolean;
  onDraftChange: (value: string) => void;
  onSave: () => void;
  onRemove: () => void;
  saving: boolean;
  removing: boolean;
  saveLabel: string;
  savingLabel: string;
  removeLabel: string;
  removingLabel: string;
  organizationDefaultLabel: string;
  branchOverrideLabel: string;
  effectivePriceLabel: string;
  hasOverrideLabel: string;
  invalidPriceLabel: string;
}) {
  const parsed = parseOverridePrice(props.draft.branchOverridePrice);
  const canSave = isDraftDirty(props.draft) && parsed.ok;

  return (
    <div
      className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] px-3 py-3"
      data-testid={
        props.draft.productUnitId
          ? `branch-pricing-unit-${props.draft.productUnitId}`
          : "branch-pricing-base"
      }
    >
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="font-semibold">{props.label}</span>
        {props.draft.hasBranchPriceOverride ? (
          <span className="text-[length:var(--exits-text-sm)] text-muted">{props.hasOverrideLabel}</span>
        ) : null}
      </div>
      <div className="grid gap-2 sm:grid-cols-2">
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{props.organizationDefaultLabel}: </span>
          {formatPeso(props.draft.organizationDefaultPrice)}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)]">
          <span className="text-muted">{props.effectivePriceLabel}: </span>
          <span data-testid={`${props.draft.productUnitId ?? "base"}-effective-price`}>
            {formatPeso(props.draft.effectivePrice)}
          </span>
        </p>
      </div>
      <Input
        label={props.branchOverrideLabel}
        name={`branchOverride-${props.draft.productUnitId ?? "base"}`}
        inputMode="decimal"
        value={props.draft.branchOverridePrice}
        disabled={props.disabled}
        onChange={(event) => props.onDraftChange(event.target.value)}
      />
      {!parsed.ok && props.draft.branchOverridePrice.trim() ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">
          {props.invalidPriceLabel}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="outline"
          className="min-h-11"
          disabled={props.disabled || !canSave || props.saving || props.removing}
          data-testid={`${props.draft.productUnitId ?? "base"}-save-override`}
          onClick={props.onSave}
        >
          {props.saving ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : (
            <Save className="size-4 shrink-0" aria-hidden />
          )}
          {props.saving ? props.savingLabel : props.saveLabel}
        </Button>
        {props.draft.hasBranchPriceOverride ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={props.disabled || props.saving || props.removing}
            data-testid={`${props.draft.productUnitId ?? "base"}-remove-override`}
            onClick={props.onRemove}
          >
            {props.removing ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Trash2 className="size-4 shrink-0" aria-hidden />
            )}
            {props.removing ? props.removingLabel : props.removeLabel}
          </Button>
        ) : null}
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
  const [error, setError] = useState<string | null>(null);
  const [pendingKey, setPendingKey] = useState<{ key: RowKey; action: "save" | "remove" } | null>(
    null,
  );

  useEffect(() => {
    if (!pricingQuery.data) {
      return;
    }
    const next: Record<RowKey, PriceRowDraft> = {
      base: itemToDraft(pricingQuery.data.basePrice),
    };
    for (const unit of pricingQuery.data.unitPrices) {
      next[pricingRowKey(unit.productUnitId)] = itemToDraft(unit);
    }
    setDrafts(next);
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
      const parsed = parseOverridePrice(input.draft.branchOverridePrice);
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
    saveLabel: t("catalog.branchPricing.save"),
    savingLabel: t("catalog.branchPricing.saving"),
    removeLabel: t("catalog.branchPricing.remove"),
    removingLabel: t("catalog.branchPricing.removing"),
    organizationDefaultLabel: t("catalog.branchPricing.organizationDefault"),
    branchOverrideLabel: t("catalog.branchPricing.branchOverride"),
    effectivePriceLabel: t("catalog.branchPricing.effectivePrice"),
    hasOverrideLabel: t("catalog.branchPricing.hasOverride"),
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
      <h2 className="catalog-form-section__title">{t("catalog.branchPricing.title")}</h2>
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
            if (!draft) {
              return null;
            }
            const label =
              key === "base"
                ? t("catalog.branchPricing.basePrice")
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
                draft={draft}
                disabled={saveMutation.isPending || removeMutation.isPending}
                saving={saving}
                removing={removing}
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
                onSave={() => saveMutation.mutate({ key, draft })}
                onRemove={() => removeMutation.mutate({ key, draft })}
                {...rowLabels}
              />
            );
          })}
        </div>
      ) : null}
    </section>
  );
}
