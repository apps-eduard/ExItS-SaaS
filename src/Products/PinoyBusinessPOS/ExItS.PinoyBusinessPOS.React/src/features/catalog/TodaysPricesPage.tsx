import { useEffect, useMemo, useState } from "react";
import { Loader2, RotateCcw, Save } from "lucide-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canGovernOrganizationCatalog } from "@/access/pos-capabilities";
import {
  listCatalogProducts,
  removeBranchProductPriceOverride,
  setBranchProductPriceOverride,
  updateCatalogProductPrices,
} from "@/api/pos/pos-catalog-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useToast } from "@/components/exits/ToastProvider";
import {
  isBranchLocalProduct,
  isOrganizationStandardProduct,
  isStandardMasterReadOnlyForActor,
} from "@/features/catalog/catalog-product-scope";
import {
  applySuccessfulBranchPriceSave,
  applySuccessfulPriceSave,
  canSavePriceDraft,
  isPriceDraftDirty,
  mergePriceDraftMap,
  parseDraftPrice,
  pricesEqual,
  resetPriceDraft,
  type PriceDraft,
} from "@/features/catalog/todays-prices-draft";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function conflictMessage(errorCode: string | null | undefined, fallback: string, conflictLabel: string): string {
  if ((errorCode ?? "").toLowerCase().includes("concurrency")) {
    return conflictLabel;
  }
  return fallback;
}

function isPriceRowEditable(canGovern: boolean, row: PriceDraft): boolean {
  return !isStandardMasterReadOnlyForActor({
    canGovernOrganizationCatalog: canGovern,
    product: { scope: row.scope ?? undefined },
  });
}

type Translate = (key: MessageKey) => string;

type PriceRowViewModel = {
  row: PriceDraft;
  editable: boolean;
  dirty: boolean;
  canSave: boolean;
  saving: boolean;
  invalidDirty: boolean;
  isStandard: boolean;
  isLocal: boolean;
  priceLabel: string;
  inputLabel: string;
  scopeLabel: string;
};

function buildPriceRowViewModel(
  row: PriceDraft,
  options: {
    canGovern: boolean;
    saving: boolean;
    t: Translate;
  },
): PriceRowViewModel {
  const editable = isPriceRowEditable(options.canGovern, row);
  const dirty = editable && isPriceDraftDirty(row);
  const parsed = parseDraftPrice(row.draftPrice);
  const isStandard = isOrganizationStandardProduct({ scope: row.scope ?? undefined });
  const isLocal = isBranchLocalProduct({ scope: row.scope ?? undefined });
  const priceLabel =
    row.priceEditScope === "branch"
      ? options.t("prices.branchEffectivePrice")
      : isStandard
        ? options.t("prices.organizationPrice")
        : isLocal
          ? options.t("catalog.governance.currentBranchProductPrice")
          : options.t("prices.current");
  const inputLabel =
    row.priceEditScope === "branch"
      ? options.t("prices.branchPrice")
      : editable
        ? options.t("prices.newPrice")
        : options.t("prices.organizationPrice");
  const scopeLabel = isStandard
    ? options.t("catalog.governance.organizationProduct")
    : isLocal
      ? options.t("catalog.governance.branchProduct")
      : options.t("catalog.governance.productType");

  return {
    row,
    editable,
    dirty,
    canSave: editable && canSavePriceDraft(row),
    saving: options.saving,
    invalidDirty: dirty && !parsed.ok,
    isStandard,
    isLocal,
    priceLabel,
    inputLabel,
    scopeLabel,
  };
}

export function TodaysPricesPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const workspace = usePosWorkspaceScope();
  const { sessionGrant } = useWorkspace();
  const canGovern = canGovernOrganizationCatalog(sessionGrant);
  const isLargeScreen = useMediaMin(1024);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [draftById, setDraftById] = useState<Record<string, PriceDraft>>({});
  const [visibleIds, setVisibleIds] = useState<string[]>([]);
  const [savingIds, setSavingIds] = useState<Record<string, true>>({});

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const query = useQuery({
    queryKey: ["catalog", "prices", workspace?.organizationId, workspace?.branchId, debounced],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debounced || undefined, canBeSold: true, pageSize: 100 },
        signal,
      ),
  });

  const branchWorkspace = Boolean(workspace?.branchId);

  useEffect(() => {
    if (!query.data) {
      return;
    }
    setDraftById((previous) =>
      mergePriceDraftMap(previous, query.data.items, { branchWorkspace }),
    );
    setVisibleIds(query.data.items.map((item) => item.productId));
  }, [query.data, branchWorkspace]);

  const visibleDrafts = useMemo(
    () => visibleIds.map((id) => draftById[id]).filter((row): row is PriceDraft => Boolean(row)),
    [visibleIds, draftById],
  );

  const showBranchColumns = visibleDrafts.some((row) => row.priceEditScope === "branch");

  function updateDraft(productId: string, updater: (row: PriceDraft) => PriceDraft) {
    setDraftById((current) => {
      const row = current[productId];
      if (!row) {
        return current;
      }
      return { ...current, [productId]: updater(row) };
    });
  }

  async function saveProduct(productId: string) {
    if (!workspace) {
      return;
    }
    const row = draftById[productId];
    if (!row || savingIds[productId] || !canSavePriceDraft(row) || !isPriceRowEditable(canGovern, row)) {
      return;
    }
    const parsed = parseDraftPrice(row.draftPrice);
    if (!parsed.ok) {
      return;
    }

    setSavingIds((current) => ({ ...current, [productId]: true }));
    try {
      if (row.priceEditScope === "branch") {
        if (!workspace.branchId) {
          updateDraft(productId, (current) => ({
            ...current,
            rowError: t("catalog.branchPricing.branchRequired"),
          }));
          return;
        }

        if (pricesEqual(parsed.value, row.organizationDefaultPrice)) {
          if (row.hasBranchPriceOverride) {
            await removeBranchProductPriceOverride(
              workspace,
              productId,
              workspace.branchId,
              null,
            );
          }
          updateDraft(productId, (current) =>
            applySuccessfulBranchPriceSave(
              current,
              row.organizationDefaultPrice,
              false,
            ),
          );
        } else {
          await setBranchProductPriceOverride(workspace, productId, {
            branchId: workspace.branchId,
            sellingPrice: parsed.value,
          });
          updateDraft(productId, (current) =>
            applySuccessfulBranchPriceSave(current, parsed.value, true),
          );
        }

        showToast(
          t("prices.updatedToast")
            .replace("{product}", row.name)
            .replace("{price}", formatPeso(parsed.value)),
        );
        await queryClient.invalidateQueries({ queryKey: ["catalog"] });
        return;
      }

      const response = await updateCatalogProductPrices(workspace, {
        items: [
          {
            productId,
            sellingPrice: parsed.value,
            expectedUpdatedAtUtc: row.expectedUpdatedAtUtc,
          },
        ],
      });
      const result = response.results.find((item) => item.productId === productId);
      if (!result || !result.succeeded) {
        const message = conflictMessage(
          result?.errorCode,
          result?.errorMessage ?? t("prices.itemFailed"),
          t("prices.staleConflict"),
        );
        updateDraft(productId, (current) => ({ ...current, rowError: message }));
        return;
      }

      const nextPrice = result.product?.sellingPrice ?? parsed.value;
      const nextToken = result.product?.updatedAtUtc ?? row.expectedUpdatedAtUtc;
      updateDraft(productId, (current) => applySuccessfulPriceSave(current, nextPrice, nextToken));
      showToast(
        t("prices.updatedToast")
          .replace("{product}", row.name)
          .replace("{price}", formatPeso(nextPrice)),
      );
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    } catch (err) {
      const message =
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message;
      updateDraft(productId, (current) => ({ ...current, rowError: message }));
    } finally {
      setSavingIds((current) => {
        const next = { ...current };
        delete next[productId];
        return next;
      });
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const rowModels = visibleDrafts.map((row) =>
    buildPriceRowViewModel(row, {
      canGovern,
      saving: Boolean(savingIds[row.productId]),
      t,
    }),
  );

  return (
    <div
      className="catalog-prices-page exits-page flex min-w-0 flex-col gap-2.5"
      data-testid="todays-prices-page"
    >
      <PageHeader
        title={t("prices.title")}
        description={t("prices.lede")}
        backTo={pageBackNav.catalog.to}
        backLabel={t(pageBackNav.catalog.labelKey)}
        backTestId="page-header-back-catalog"
      />

      <div className="catalog-prices-toolbar" data-testid="catalog-prices-toolbar">
        <SearchField
          label={t("catalog.searchProducts")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("catalog.searchProducts")}
          data-testid="catalog-prices-search"
          containerClassName="catalog-prices-page__search exits-page__search min-w-0 flex-1"
        />
        {showBranchColumns ? (
          <p className="catalog-prices-toolbar__hint m-0" data-testid="prices-branch-scope-hint">
            {t("prices.branchScopeHint")}
          </p>
        ) : null}
      </div>

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && visibleDrafts.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("prices.emptyDetail")} />
      ) : null}

      {rowModels.length > 0 ? (
        <div className="catalog-prices-results">
          {isLargeScreen ? (
            <div
              className="catalog-prices-table-shell min-w-0 overflow-x-auto"
              data-testid="catalog-prices-table"
            >
              <table className="catalog-prices-table w-full min-w-[48rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="catalog-prices-table__head border-b border-border">
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("catalog.name")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("catalog.col.scope")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {showBranchColumns
                        ? t("prices.branchEffectivePrice")
                        : t("prices.current")}
                    </th>
                    {showBranchColumns ? (
                      <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                        {t("prices.organizationPrice")}
                      </th>
                    ) : null}
                    <th className="min-w-[9rem] whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {showBranchColumns ? t("prices.branchPrice") : t("prices.newPrice")}
                    </th>
                    <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("catalog.col.actions")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {rowModels.map((model) => (
                    <PriceTableRow
                      key={model.row.productId}
                      model={model}
                      showBranchColumns={showBranchColumns}
                      t={t}
                      onDraftChange={(value) => {
                        if (!model.editable) {
                          return;
                        }
                        updateDraft(model.row.productId, (current) => ({
                          ...current,
                          draftPrice: value,
                          rowError: null,
                        }));
                      }}
                      onSave={() => void saveProduct(model.row.productId)}
                      onReset={() =>
                        updateDraft(model.row.productId, (current) => resetPriceDraft(current))
                      }
                    />
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <ul
              className="catalog-prices-list m-0 grid list-none gap-2 p-0"
              data-testid="catalog-prices-list"
            >
              {rowModels.map((model) => (
                <li key={model.row.productId}>
                  <PriceCard
                    model={model}
                    t={t}
                    onDraftChange={(value) => {
                      if (!model.editable) {
                        return;
                      }
                      updateDraft(model.row.productId, (current) => ({
                        ...current,
                        draftPrice: value,
                        rowError: null,
                      }));
                    }}
                    onSave={() => void saveProduct(model.row.productId)}
                    onReset={() =>
                      updateDraft(model.row.productId, (current) => resetPriceDraft(current))
                    }
                  />
                </li>
              ))}
            </ul>
          )}
        </div>
      ) : null}
    </div>
  );
}

function PriceEditorFields({
  model,
  t,
  layout,
  showActions = true,
  onDraftChange,
  onSave,
  onReset,
}: {
  model: PriceRowViewModel;
  t: Translate;
  layout: "card" | "table";
  showActions?: boolean;
  onDraftChange: (value: string) => void;
  onSave: () => void;
  onReset: () => void;
}) {
  const { row, editable, dirty, invalidDirty, inputLabel } = model;

  return (
    <div className={cn("catalog-prices-row__editor", layout === "table" && "catalog-prices-row__editor--table")}>
      <div className="catalog-prices-row__edit-row">
        <div className="catalog-prices-row__input-wrap min-w-0">
          <Input
            label={inputLabel}
            name={`price-${row.productId}`}
            inputMode="decimal"
            autoComplete="off"
            className="catalog-prices-row__price-input"
            value={row.draftPrice}
            readOnly={!editable}
            disabled={!editable}
            aria-invalid={Boolean(row.rowError) || invalidDirty}
            aria-describedby={
              row.rowError || invalidDirty ? `price-error-${row.productId}` : undefined
            }
            onChange={(event) => onDraftChange(event.target.value)}
            onKeyDown={(event) => {
              if (event.key !== "Enter") {
                return;
              }
              event.preventDefault();
              if (!editable) {
                return;
              }
              onSave();
            }}
          />
        </div>
        {showActions && dirty ? (
          <PriceDirtyActions model={model} t={t} onSave={onSave} onReset={onReset} />
        ) : null}
      </div>
      {invalidDirty ? (
        <p
          id={`price-error-${row.productId}`}
          className="catalog-prices-row__error m-0 text-[length:var(--exits-text-sm)] text-destructive"
        >
          {t("prices.invalidPrice")}
        </p>
      ) : null}
      {row.rowError ? (
        <p
          id={`price-error-${row.productId}`}
          className="catalog-prices-row__error m-0 text-[length:var(--exits-text-sm)] text-destructive"
        >
          {row.rowError}
        </p>
      ) : null}
    </div>
  );
}

function PriceDirtyActions({
  model,
  t,
  onSave,
  onReset,
}: {
  model: PriceRowViewModel;
  t: Translate;
  onSave: () => void;
  onReset: () => void;
}) {
  const { row, canSave, saving } = model;
  return (
    <div className="catalog-prices-row__actions shrink-0">
      <Button
        type="button"
        variant="outline"
        size="icon"
        className="catalog-prices-row__reset"
        disabled={saving}
        onClick={onReset}
        data-testid={`price-reset-${row.productId}`}
        aria-label={t("prices.resetOneAria").replace("{product}", row.name)}
      >
        <RotateCcw className="size-4 shrink-0" aria-hidden />
      </Button>
      <Button
        type="button"
        variant="default"
        size="icon"
        className="catalog-prices-row__save"
        disabled={!canSave || saving}
        onClick={onSave}
        data-testid={`price-save-${row.productId}`}
        aria-label={t("prices.saveOneAria").replace("{product}", row.name)}
      >
        {saving ? (
          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
        ) : (
          <Save className="size-4 shrink-0" aria-hidden />
        )}
      </Button>
    </div>
  );
}

function PriceCard({
  model,
  t,
  onDraftChange,
  onSave,
  onReset,
}: {
  model: PriceRowViewModel;
  t: Translate;
  onDraftChange: (value: string) => void;
  onSave: () => void;
  onReset: () => void;
}) {
  const { row, editable, dirty, priceLabel, scopeLabel } = model;

  return (
    <article
      className={cn(
        "catalog-prices-row exits-list__card",
        dirty && "catalog-prices-row--dirty",
        row.rowError && "catalog-prices-row--error",
        !editable && "catalog-prices-row--readonly",
      )}
      data-testid={`price-row-${row.productId}`}
    >
      <div className="catalog-prices-row__main min-w-0">
        <p className="catalog-prices-row__name exits-list__name m-0 font-semibold">{row.name}</p>
        {row.brandName ? (
          <p className="catalog-prices-row__brand m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
            {row.brandName}
          </p>
        ) : null}
        <p className="catalog-prices-row__scope m-0 mt-1" data-testid={`price-scope-${row.productId}`}>
          <span className="catalog-product-row__badge catalog-product-row__badge--scope">
            {scopeLabel}
          </span>
        </p>
        <p className="catalog-prices-row__current m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {priceLabel}: {formatPeso(row.currentPrice)}
        </p>
        {row.priceEditScope === "branch" ? (
          <p
            className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
            data-testid={`price-org-default-${row.productId}`}
          >
            {t("prices.organizationPrice")}: {formatPeso(row.organizationDefaultPrice)}
          </p>
        ) : null}
        {row.priceEditScope === "branch" ? (
          <p
            className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
            data-testid={`price-branch-scope-${row.productId}`}
          >
            {t("prices.branchScopeHint")}
          </p>
        ) : row.priceEditScope === "organization" ? (
          <p
            className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
            data-testid={`price-org-scope-${row.productId}`}
          >
            {t("prices.organizationScopeHint")}
          </p>
        ) : null}
        {!editable ? (
          <p
            className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
            data-testid={`price-managed-${row.productId}`}
          >
            {t("prices.managedByOrganization")}
          </p>
        ) : null}
      </div>

      <PriceEditorFields
        model={model}
        t={t}
        layout="card"
        onDraftChange={onDraftChange}
        onSave={onSave}
        onReset={onReset}
      />
    </article>
  );
}

function PriceTableRow({
  model,
  showBranchColumns,
  t,
  onDraftChange,
  onSave,
  onReset,
}: {
  model: PriceRowViewModel;
  showBranchColumns: boolean;
  t: Translate;
  onDraftChange: (value: string) => void;
  onSave: () => void;
  onReset: () => void;
}) {
  const { row, editable, dirty, scopeLabel } = model;

  return (
    <tr
      className={cn(
        "catalog-prices-table__row border-b border-border",
        dirty && "catalog-prices-table__row--dirty",
        row.rowError && "catalog-prices-table__row--error",
        !editable && "catalog-prices-table__row--readonly",
      )}
      data-testid={`price-row-${row.productId}`}
    >
      <td className="max-w-[16rem] px-3 py-2.5 align-middle">
        <span className="block truncate font-semibold text-foreground">{row.name}</span>
        {row.brandName ? (
          <span className="mt-0.5 block truncate text-[length:var(--exits-text-xs)] text-muted">
            {row.brandName}
          </span>
        ) : null}
        {!editable ? (
          <span
            className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
            data-testid={`price-managed-${row.productId}`}
          >
            {t("prices.managedByOrganization")}
          </span>
        ) : null}
      </td>
      <td className="whitespace-nowrap px-3 py-2.5 align-middle" data-testid={`price-scope-${row.productId}`}>
        <span className="catalog-product-row__badge catalog-product-row__badge--scope">
          {scopeLabel}
        </span>
      </td>
      <td className="whitespace-nowrap px-3 py-2.5 align-middle tabular-nums text-muted">
        {formatPeso(row.currentPrice)}
      </td>
      {showBranchColumns ? (
        <td
          className="whitespace-nowrap px-3 py-2.5 align-middle tabular-nums text-muted"
          data-testid={`price-org-default-${row.productId}`}
        >
          {row.priceEditScope === "branch"
            ? formatPeso(row.organizationDefaultPrice)
            : "—"}
        </td>
      ) : null}
      <td className="min-w-[10rem] px-3 py-2.5 align-middle">
        <PriceEditorFields
          model={model}
          t={t}
          layout="table"
          showActions={false}
          onDraftChange={onDraftChange}
          onSave={onSave}
          onReset={onReset}
        />
      </td>
      <td className="px-3 py-2.5 align-middle">
        {dirty ? (
          <div className="flex justify-end">
            <PriceDirtyActions model={model} t={t} onSave={onSave} onReset={onReset} />
          </div>
        ) : null}
      </td>
    </tr>
  );
}
