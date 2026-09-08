import { useEffect, useMemo, useRef, useState } from "react";
import { Check, Plus, Trash2 } from "lucide-react";
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
import { useMediaMin } from "@/hooks/useMediaQuery";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type SelectedProduct = {
  productId: string;
  name: string;
  unitOfMeasure: string;
  onHand: number;
};

function toSelectedProduct(row: PosInventoryAccountDto): SelectedProduct {
  return {
    productId: row.productId,
    name: row.name,
    unitOfMeasure: row.unitOfMeasure,
    onHand: row.onHandQuantity,
  };
}

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
  const isLargeScreen = useMediaMin(1024);

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
  const [showAddProducts, setShowAddProducts] = useState(true);
  const productsScrollRef = useRef<HTMLDivElement | null>(null);

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
    enabled: Boolean(workspace) && online && allowManage && showAddProducts,
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
    setSelected((prev) => [...prev, toSelectedProduct(row)]);
  }

  function removeProduct(productId: string) {
    setSelected((prev) => {
      const next = prev.filter((p) => p.productId !== productId);
      if (next.length === 0) {
        setShowAddProducts(true);
      }
      return next;
    });
  }

  function hideAddProductsPanel() {
    setShowAddProducts(false);
    setSearch("");
    setDebounced("");
  }

  async function countAllTracked() {
    if (!workspace || loadingAll || saving || !online) {
      return;
    }

    // Hide search + picker immediately so the selected list is the focus.
    hideAddProductsPanel();
    // Instant feedback from rows already loaded in the picker (if any).
    if (pickerRows.length > 0) {
      setSelected(
        pickerRows.slice(0, STOCK_COUNT_MAX_LINES).map(toSelectedProduct),
      );
      productsScrollRef.current?.scrollTo({ top: 0 });
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
        const items = result.items ?? [];
        totalCount = Number(result.totalCount) || 0;
        for (const row of items) {
          if (!row.isTracked || seen.has(row.productId)) {
            continue;
          }
          seen.add(row.productId);
          collected.push(toSelectedProduct(row));
          if (collected.length >= STOCK_COUNT_MAX_LINES) {
            break;
          }
        }
        if (items.length === 0 || page * pageSize >= totalCount || collected.length >= totalCount) {
          break;
        }
        page += 1;
      }

      if (collected.length === 0) {
        setSelected([]);
        setShowAddProducts(true);
        setError(t("stockCount.noTrackedProducts"));
        return;
      }

      setSelected(collected);
      hideAddProductsPanel();
      productsScrollRef.current?.scrollTo({ top: 0 });
      if (totalCount > STOCK_COUNT_MAX_LINES) {
        setNotice(
          t("stockCount.countAllCapped").replace("{max}", String(STOCK_COUNT_MAX_LINES)),
        );
      }
    } catch (err) {
      if (pickerRows.length === 0) {
        setShowAddProducts(true);
      }
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
    "stock-count-product-row flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5 sm:flex-row sm:items-center sm:justify-between sm:gap-2";
  const actionIconClass = "stock-count-action-btn shrink-0";
  const actionIconSoftClass = "stock-count-action-btn stock-count-action-btn--soft shrink-0";

  return (
    <div
      className="stock-count-create-page exits-page mx-auto flex h-full min-h-0 w-full max-w-[80rem] min-w-0 flex-col gap-2.5 overflow-hidden"
      data-testid="stock-count-create-page"
    >
      <div className="stock-count-create-chrome flex shrink-0 min-w-0 flex-col gap-2.5">
        <PageHeader
          title={t("stockCount.newTitle")}
          description={t("stockCount.newLede")}
          backTo="/inventory/stock-counts"
          backLabel={t("stockCount.backList")}
          backTestId="page-header-back-stock-counts"
        />

        <p
          className="stock-count-create-scope m-0"
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

        <section
          className="catalog-form-section stock-count-section exits-animate-panel"
          data-testid="stock-count-create-fields"
        >
          <div className="stock-count-create-meta grid min-w-0 grid-cols-1 gap-2.5 sm:grid-cols-2 lg:grid-cols-[minmax(10rem,13rem)_minmax(0,1fr)_minmax(10rem,14rem)] lg:items-start">
            <label className="flex min-w-0 flex-col gap-1">
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                {t("stockCount.countPeriod")}
              </span>
              <select
                className="exits-select catalog-form-select"
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

            <label className="flex min-w-0 flex-col gap-1 sm:col-span-2 lg:col-span-1">
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                {t("stockCount.fieldTitle")}
              </span>
              <input
                className="exits-input"
                value={title}
                onChange={(e) => onTitleChange(e.target.value)}
                maxLength={80}
                autoComplete="off"
                data-testid="stock-count-title"
              />
              <span className="text-[length:var(--exits-text-xs)] text-muted">
                {t("stockCount.titleSuggestedHint")}
              </span>
            </label>

            <label className="flex min-w-0 flex-col gap-1 sm:col-start-2 sm:row-start-1 lg:col-start-auto lg:row-start-auto">
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                {t("stockCount.countDate")}
              </span>
              <input
                type="date"
                className="exits-input"
                value={countDate}
                onChange={(e) => applyCountDate(e.target.value)}
                data-testid="stock-count-date"
              />
            </label>
          </div>

          <label className="flex min-w-0 flex-col gap-1">
            <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
              {t("stockCount.notes")}{" "}
              <span className="font-normal">({t("stockCount.notesOptional")})</span>
            </span>
            <textarea
              className="exits-input stock-count-notes-input"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              maxLength={512}
              rows={1}
              data-testid="stock-count-notes"
            />
          </label>
        </section>
      </div>

      <section
        className="catalog-form-section stock-count-section stock-count-section--products exits-animate-panel"
        data-testid="stock-count-selected"
      >
        <div className="flex shrink-0 flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <h2 className="catalog-form-section__title m-0 flex min-w-0 items-baseline gap-2">
            <span>{t("stockCount.productsToCount")}</span>
            {selected.length > 0 ? (
              <span className="text-[length:var(--exits-text-sm)] font-medium text-muted">
                ({selected.length})
              </span>
            ) : null}
          </h2>
          <div className="stock-count-create-toolbar flex w-full min-w-0 flex-col gap-2 sm:w-auto sm:flex-row sm:items-center">
            <Button
              type="button"
              variant="outline"
              className="w-full shrink-0 sm:w-auto"
              disabled={!online || loadingAll || saving}
              onClick={() => void countAllTracked()}
              data-testid="stock-count-count-all"
            >
              {loadingAll ? t("stockCount.loadingAll") : t("stockCount.countAll")}
            </Button>
            <Button
              type="button"
              variant="default"
              className="w-full shrink-0 sm:w-auto"
              disabled={!online || saving || selected.length === 0}
              onClick={() => void saveDraft()}
              data-testid="stock-count-save-draft"
            >
              {saving ? t("stockCount.saving") : t("stockCount.saveDraft")}
            </Button>
          </div>
        </div>

        <div
          ref={productsScrollRef}
          className="stock-count-create-products-scroll"
          data-testid="stock-count-products-scroll"
        >
        {selected.length === 0 ? (
          <p className="stock-count-empty m-0">{t("stockCount.draftEmpty")}</p>
        ) : isLargeScreen ? (
          <div
            className="stock-count-create-table-shell min-w-0 overflow-x-auto"
            data-testid="stock-count-selected-table"
          >
            <table className="stock-count-create-table w-full min-w-[32rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead>
                <tr className="stock-count-create-table__head border-b border-border">
                  <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("stockCount.product")}
                  </th>
                  <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("stockCount.unit")}
                  </th>
                  <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("inventory.onHand")}
                  </th>
                  <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("catalog.col.actions")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {selected.map((product) => (
                  <tr
                    key={product.productId}
                    className="stock-count-create-table__row border-b border-border"
                    data-testid={`stock-count-selected-${product.productId}`}
                  >
                    <td className="max-w-[18rem] px-3 py-2.5 align-middle">
                      <span className="block truncate font-semibold text-foreground">
                        {product.name}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-3 py-2.5 align-middle text-muted">
                      {product.unitOfMeasure}
                    </td>
                    <td className="whitespace-nowrap px-3 py-2.5 align-middle text-right tabular-nums text-muted">
                      {product.onHand}
                    </td>
                    <td className="px-3 py-2.5 align-middle">
                      <div className="flex justify-end">
                        <Button
                          type="button"
                          variant="outline"
                          size="icon"
                          className={actionIconSoftClass}
                          aria-label={t("stockCount.removeProduct")}
                          onClick={() => removeProduct(product.productId)}
                          data-testid={`stock-count-remove-${product.productId}`}
                        >
                          <Trash2 className="size-4 shrink-0" aria-hidden />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
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
                  size="icon"
                  className={actionIconSoftClass}
                  aria-label={t("stockCount.removeProduct")}
                  onClick={() => removeProduct(product.productId)}
                  data-testid={`stock-count-remove-${product.productId}`}
                >
                  <Trash2 className="size-4 shrink-0" aria-hidden />
                </Button>
              </li>
            ))}
          </ul>
        )}

        {showAddProducts ? (
          <div className="stock-count-picker flex min-w-0 flex-col gap-2 border-t border-border pt-2.5">
            <h3 className="m-0 text-[length:var(--exits-text-sm)] font-medium text-muted">
              {t("stockCount.addProducts")}
            </h3>
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

            {pickerRows.length > 0 ? (
              isLargeScreen ? (
                <div
                  className="stock-count-create-table-shell min-w-0 overflow-x-auto"
                  data-testid="stock-count-product-picker"
                >
                  <table className="stock-count-create-table w-full min-w-[32rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                    <thead>
                      <tr className="stock-count-create-table__head border-b border-border">
                        <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("stockCount.product")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("stockCount.unit")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("inventory.onHand")}
                        </th>
                        <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("catalog.col.actions")}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {pickerRows.map((row) => {
                        const already = selectedIds.has(row.productId);
                        return (
                          <tr
                            key={row.productId}
                            className="stock-count-create-table__row border-b border-border"
                          >
                            <td className="max-w-[18rem] px-3 py-2.5 align-middle">
                              <span className="block truncate font-semibold text-foreground">
                                {row.name}
                              </span>
                            </td>
                            <td className="whitespace-nowrap px-3 py-2.5 align-middle text-muted">
                              {row.unitOfMeasure}
                            </td>
                            <td className="whitespace-nowrap px-3 py-2.5 align-middle text-right tabular-nums text-muted">
                              {row.onHandQuantity}
                            </td>
                            <td className="px-3 py-2.5 align-middle">
                              <div className="flex justify-end">
                                <Button
                                  type="button"
                                  variant={already ? "outline" : "default"}
                                  size="icon"
                                  className={already ? actionIconSoftClass : actionIconClass}
                                  disabled={already || !online}
                                  aria-label={
                                    already ? t("stockCount.alreadyAdded") : t("stockCount.addProduct")
                                  }
                                  onClick={() => addProduct(row)}
                                  data-testid={`stock-count-add-${row.productId}`}
                                >
                                  {already ? (
                                    <Check className="size-4 shrink-0" aria-hidden />
                                  ) : (
                                    <Plus className="size-4 shrink-0" aria-hidden />
                                  )}
                                </Button>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              ) : (
                <ul
                  className="m-0 flex list-none flex-col gap-1.5 p-0"
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
                          size="icon"
                          className={already ? actionIconSoftClass : actionIconClass}
                          disabled={already || !online}
                          aria-label={
                            already ? t("stockCount.alreadyAdded") : t("stockCount.addProduct")
                          }
                          onClick={() => addProduct(row)}
                          data-testid={`stock-count-add-${row.productId}`}
                        >
                          {already ? (
                            <Check className="size-4 shrink-0" aria-hidden />
                          ) : (
                            <Plus className="size-4 shrink-0" aria-hidden />
                          )}
                        </Button>
                      </li>
                    );
                  })}
                </ul>
              )
            ) : null}
          </div>
        ) : (
          <div className="stock-count-picker-toggle flex min-w-0 justify-start border-t border-border pt-2.5">
            <Button
              type="button"
              variant="ghost"
              className="px-0 text-[length:var(--exits-text-sm)] font-medium text-primary hover:bg-transparent"
              onClick={() => setShowAddProducts(true)}
              data-testid="stock-count-show-add-products"
            >
              <Plus className="size-4 shrink-0" aria-hidden />
              {t("stockCount.addProducts")}
            </Button>
          </div>
        )}
        </div>
      </section>
    </div>
  );
}
