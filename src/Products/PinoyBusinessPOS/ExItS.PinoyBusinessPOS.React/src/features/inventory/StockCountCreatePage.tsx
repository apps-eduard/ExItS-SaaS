import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listInventory, type PosInventoryAccountDto } from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createStockCount,
  STOCK_COUNT_MAX_LINES,
} from "@/api/pos/pos-stock-count-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { todayDateOnly } from "@/features/inventory/stock-count-labels";
import {
  nextTitleAfterSuggestionInputsChange,
  STOCK_COUNT_PERIOD_TYPES,
  suggestStockCountTitle,
  type StockCountPeriodType,
} from "@/features/inventory/stock-count-title-suggestion";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type SelectedProduct = {
  productId: string;
  name: string;
  unitOfMeasure: string;
  onHand: number;
};

const DEFAULT_PERIOD: StockCountPeriodType = "Monthly";

function periodLabelKey(
  period: StockCountPeriodType,
):
  | "stockCount.period.weekly"
  | "stockCount.period.monthly"
  | "stockCount.period.quarterly"
  | "stockCount.period.annual"
  | "stockCount.period.custom" {
  switch (period) {
    case "Weekly":
      return "stockCount.period.weekly";
    case "Monthly":
      return "stockCount.period.monthly";
    case "Quarterly":
      return "stockCount.period.quarterly";
    case "Annual":
      return "stockCount.period.annual";
    case "Custom":
      return "stockCount.period.custom";
  }
}

export function StockCountCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const initialDate = todayDateOnly();
  const [period, setPeriod] = useState<StockCountPeriodType>(DEFAULT_PERIOD);
  const [title, setTitle] = useState(() => suggestStockCountTitle(DEFAULT_PERIOD, initialDate));
  const [titleDirty, setTitleDirty] = useState(false);
  const [countDate, setCountDate] = useState(initialDate);
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [selected, setSelected] = useState<SelectedProduct[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [loadingAll, setLoadingAll] = useState(false);

  function applyPeriod(next: StockCountPeriodType) {
    setPeriod(next);
    setTitle((current) =>
      nextTitleAfterSuggestionInputsChange({
        period: next,
        countDate,
        currentTitle: current,
        titleDirty,
      }),
    );
  }

  function applyCountDate(next: string) {
    setCountDate(next);
    setTitle((current) =>
      nextTitleAfterSuggestionInputsChange({
        period,
        countDate: next,
        currentTitle: current,
        titleDirty,
      }),
    );
  }

  function onTitleChange(value: string) {
    setTitle(value);
    setTitleDirty(true);
  }

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const pickerQuery = useQuery({
    queryKey: [
      "inventory",
      "stock-count-picker",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        { search: debounced || undefined, pageSize: 40, tracked: true },
        signal,
      ),
  });

  const selectedIds = useMemo(() => new Set(selected.map((s) => s.productId)), [selected]);

  const pickerRows = useMemo(() => {
    const items = (pickerQuery.data?.items ?? []).filter((row) => row.isTracked);
    return items;
  }, [pickerQuery.data?.items]);

  function addProduct(row: PosInventoryAccountDto) {
    if (selectedIds.has(row.productId)) {
      return;
    }
    if (selected.length >= STOCK_COUNT_MAX_LINES) {
      setError(t("stockCount.maxLines").replace("{max}", String(STOCK_COUNT_MAX_LINES)));
      return;
    }
    setError(null);
    setSelected((prev) => [
      ...prev,
      {
        productId: row.productId,
        name: row.name,
        unitOfMeasure: row.unitOfMeasure,
        onHand: row.onHandQuantity,
      },
    ]);
  }

  function removeProduct(productId: string) {
    setSelected((prev) => prev.filter((p) => p.productId !== productId));
  }

  async function countAllTracked() {
    if (!workspace || loadingAll || saving) {
      return;
    }
    setLoadingAll(true);
    setError(null);
    setNotice(null);
    try {
      const collected: SelectedProduct[] = [];
      const seen = new Set<string>();
      let page = 1;
      const pageSize = 100;
      let totalCount = 0;
      while (collected.length < STOCK_COUNT_MAX_LINES) {
        const result = await listInventory(workspace, {
          page,
          pageSize,
          tracked: true,
        });
        totalCount = result.totalCount;
        for (const row of result.items) {
          if (!row.isTracked || seen.has(row.productId)) {
            continue;
          }
          seen.add(row.productId);
          collected.push({
            productId: row.productId,
            name: row.name,
            unitOfMeasure: row.unitOfMeasure,
            onHand: row.onHandQuantity,
          });
          if (collected.length >= STOCK_COUNT_MAX_LINES) {
            break;
          }
        }
        if (result.items.length === 0 || page * pageSize >= totalCount) {
          break;
        }
        page += 1;
      }
      setSelected(collected);
      if (totalCount > STOCK_COUNT_MAX_LINES) {
        setNotice(
          t("stockCount.countAllCapped").replace("{max}", String(STOCK_COUNT_MAX_LINES)),
        );
      } else if (collected.length === 0) {
        setError(t("stockCount.noTrackedProducts"));
      }
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("stockCount.loadFailed"))
          : t("stockCount.loadFailed"),
      );
    } finally {
      setLoadingAll(false);
    }
  }

  async function saveDraft() {
    if (!workspace || !allowManage || !online || saving) {
      return;
    }
    const trimmedTitle = title.trim();
    if (!trimmedTitle) {
      setError(t("stockCount.titleRequired"));
      return;
    }
    if (selected.length === 0) {
      setError(t("stockCount.draftEmpty"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const created = await createStockCount(workspace, {
        title: trimmedTitle,
        countDate: countDate || null,
        notes: notes.trim() || null,
        lines: selected.map((p) => ({ productId: p.productId })),
      });
      navigate(`/inventory/stock-counts/${created.stockCountId}`, {
        replace: true,
        state: { flash: "created" },
      });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("stockCount.saveFailed"))
          : t("stockCount.saveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!allowManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="stock-count-create-denied">
        <PageHeader
          title={t("stockCount.newTitle")}
          backTo="/inventory/stock-counts"
          backLabel={t("stockCount.backList")}
          backTestId="page-header-back-stock-counts"
        />
        <ErrorState title={t("stockCount.errorTitle")} detail={t("stockCount.manageDenied")} />
      </div>
    );
  }

  const productRowClass =
    "flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3 sm:flex-row sm:items-center sm:justify-between sm:gap-2";
  const productActionClass = "min-h-11 w-full shrink-0 sm:w-auto";

  return (
    <div
      className="stock-count-create-page exits-page flex min-w-0 flex-col gap-4 pb-[calc(5.5rem+env(safe-area-inset-bottom,0px))]"
      data-testid="stock-count-create-page"
    >
      <PageHeader
        title={t("stockCount.newTitle")}
        description={t("stockCount.newLede")}
        backTo="/inventory/stock-counts"
        backLabel={t("stockCount.backList")}
        backTestId="page-header-back-stock-counts"
      />

      <p
        className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] px-3 py-2.5 text-[length:var(--exits-text-sm)] leading-snug text-foreground"
        data-testid="stock-count-create-scope"
      >
        {boundWorkspace?.branchName
          ? t("stockCount.orgScopeNote").replace("{name}", boundWorkspace.branchName)
          : t("stockCount.branchRequired")}
      </p>

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockCount.offline")}</p>
      ) : null}

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert" data-testid="stock-count-create-error">
          {error}
        </p>
      ) : null}
      {notice ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="stock-count-create-notice">
          {notice}
        </p>
      ) : null}

      <section className="flex flex-col gap-3" data-testid="stock-count-create-fields">
        <label className="flex flex-col gap-1.5">
          <span className="text-[length:var(--exits-text-sm)] font-semibold">{t("stockCount.countPeriod")}</span>
          <select
            className="exits-input min-h-11"
            value={period}
            onChange={(e) => applyPeriod(e.target.value as StockCountPeriodType)}
            data-testid="stock-count-period"
          >
            {STOCK_COUNT_PERIOD_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(periodLabelKey(value))}
              </option>
            ))}
          </select>
        </label>

        <label className="flex flex-col gap-1.5">
          <span className="text-[length:var(--exits-text-sm)] font-semibold">{t("stockCount.fieldTitle")}</span>
          <input
            className="exits-input min-h-11"
            value={title}
            onChange={(e) => onTitleChange(e.target.value)}
            maxLength={80}
            autoComplete="off"
            data-testid="stock-count-title"
          />
          <span className="text-[length:var(--exits-text-xs)] text-muted">{t("stockCount.titleSuggestedHint")}</span>
        </label>

        <label className="flex flex-col gap-1.5">
          <span className="text-[length:var(--exits-text-sm)] font-semibold">{t("stockCount.countDate")}</span>
          <input
            type="date"
            className="exits-input min-h-11"
            value={countDate}
            onChange={(e) => applyCountDate(e.target.value)}
            data-testid="stock-count-date"
          />
        </label>

        <label className="flex flex-col gap-1.5">
          <span className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("stockCount.notes")}{" "}
            <span className="font-normal text-muted">({t("stockCount.notesOptional")})</span>
          </span>
          <textarea
            className="exits-input min-h-20"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            maxLength={512}
            data-testid="stock-count-notes"
          />
        </label>
      </section>

      <section className="flex flex-col gap-3" data-testid="stock-count-selected">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <h2 className="m-0 flex min-w-0 items-baseline gap-2 text-[length:var(--exits-text-lg)] font-semibold">
            <span>{t("stockCount.productsToCount")}</span>
            {selected.length > 0 ? (
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                ({selected.length})
              </span>
            ) : null}
          </h2>
          <Button
            type="button"
            variant="outline"
            className={productActionClass}
            disabled={!online || loadingAll || saving}
            onClick={() => void countAllTracked()}
            data-testid="stock-count-count-all"
          >
            {loadingAll ? t("stockCount.loadingAll") : t("stockCount.countAll")}
          </Button>
        </div>
        {selected.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockCount.draftEmpty")}</p>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {selected.map((product) => (
              <li
                key={product.productId}
                className={productRowClass}
                data-testid={`stock-count-selected-${product.productId}`}
              >
                <div className="min-w-0">
                  <p className="m-0 font-medium leading-snug">{product.name}</p>
                  <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                    {product.unitOfMeasure} · {t("inventory.onHand")}: {product.onHand}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="outline"
                  className={productActionClass}
                  onClick={() => removeProduct(product.productId)}
                  data-testid={`stock-count-remove-${product.productId}`}
                >
                  {t("stockCount.removeProduct")}
                </Button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">{t("stockCount.addProducts")}</h2>
        <SearchField
          label={t("stockCount.searchProducts")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("stockCount.searchProducts")}
          data-testid="stock-count-product-search"
        />
        {pickerQuery.isLoading ? <LoadingState label={t("stockCount.loading")} /> : null}
        {!pickerQuery.isLoading && pickerRows.length === 0 ? (
          <EmptyState title={t("stockCount.noProducts")} detail={t("stockCount.noProductsDetail")} />
        ) : null}
        <ul
          className="m-0 flex max-h-[min(50vh,22rem)] list-none flex-col gap-2 overflow-y-auto overscroll-contain p-0"
          data-testid="stock-count-product-picker"
        >
          {pickerRows.map((row) => {
            const already = selectedIds.has(row.productId);
            return (
              <li key={row.productId} className={productRowClass}>
                <div className="min-w-0">
                  <p className="m-0 font-medium leading-snug">{row.name}</p>
                  <p className="m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                    {row.unitOfMeasure} · {t("inventory.onHand")}: {row.onHandQuantity}
                  </p>
                </div>
                <Button
                  type="button"
                  variant={already ? "outline" : "default"}
                  className={productActionClass}
                  disabled={already || !online}
                  onClick={() => addProduct(row)}
                  data-testid={`stock-count-add-${row.productId}`}
                >
                  {already ? t("stockCount.alreadyAdded") : t("stockCount.addProduct")}
                </Button>
              </li>
            );
          })}
        </ul>
      </section>

      <StickyActionBar className="px-3 py-3 sm:px-4">
        <Button
          type="button"
          className="min-h-12 w-full flex-1"
          disabled={!online || saving || selected.length === 0}
          onClick={() => void saveDraft()}
          data-testid="stock-count-save-draft"
        >
          {saving ? t("stockCount.saving") : t("stockCount.saveDraft")}
        </Button>
      </StickyActionBar>
    </div>
  );
}
