import { useEffect, useMemo, useState } from "react";
import { Loader2, Save } from "lucide-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { listCatalogProducts, updateCatalogProductPrices } from "@/api/pos/pos-catalog-client";
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
  applySuccessfulPriceSave,
  canSavePriceDraft,
  isPriceDraftDirty,
  mergePriceDraftMap,
  parseDraftPrice,
  type PriceDraft,
} from "@/features/catalog/todays-prices-draft";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function conflictMessage(errorCode: string | null | undefined, fallback: string, conflictLabel: string): string {
  if ((errorCode ?? "").toLowerCase().includes("concurrency")) {
    return conflictLabel;
  }
  return fallback;
}

export function TodaysPricesPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const workspace = usePosWorkspaceScope();
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

  useEffect(() => {
    if (!query.data) {
      return;
    }
    setDraftById((previous) => mergePriceDraftMap(previous, query.data.items));
    setVisibleIds(query.data.items.map((item) => item.productId));
  }, [query.data]);

  const visibleDrafts = useMemo(
    () => visibleIds.map((id) => draftById[id]).filter((row): row is PriceDraft => Boolean(row)),
    [visibleIds, draftById],
  );

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
    if (!row || savingIds[productId] || !canSavePriceDraft(row)) {
      return;
    }
    const parsed = parseDraftPrice(row.draftPrice);
    if (!parsed.ok) {
      return;
    }

    setSavingIds((current) => ({ ...current, [productId]: true }));
    try {
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

  return (
    <div
      className="catalog-prices-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="todays-prices-page"
    >
      <PageHeader
        title={t("prices.title")}
        description={t("prices.lede")}
        backTo={pageBackNav.catalog.to}
        backLabel={t(pageBackNav.catalog.labelKey)}
        backTestId="page-header-back-catalog"
      />

      <SearchField
        label={t("catalog.searchProducts")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalog.searchProducts")}
        data-testid="catalog-prices-search"
        containerClassName="catalog-prices-page__search exits-page__search exits-animate-toolbar"
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && visibleDrafts.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("prices.emptyDetail")} />
      ) : null}

      <ul className="catalog-prices-list exits-list m-0 grid list-none gap-2 p-0">
        {visibleDrafts.map((row) => {
          const dirty = isPriceDraftDirty(row);
          const parsed = parseDraftPrice(row.draftPrice);
          const canSave = canSavePriceDraft(row);
          const saving = Boolean(savingIds[row.productId]);
          const invalidDirty = dirty && !parsed.ok;

          return (
            <li key={row.productId}>
              <article
                className={cn(
                  "catalog-prices-row exits-list__card",
                  dirty && "catalog-prices-row--dirty",
                  row.rowError && "catalog-prices-row--error",
                )}
                data-testid={`price-row-${row.productId}`}
              >
                <div className="catalog-prices-row__main min-w-0">
                  <p className="catalog-prices-row__name exits-list__name m-0 font-semibold">
                    {row.name}
                  </p>
                  {row.brandName ? (
                    <p className="catalog-prices-row__brand m-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
                      {row.brandName}
                    </p>
                  ) : null}
                  <p className="catalog-prices-row__current m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {t("prices.current")}: {formatPeso(row.currentPrice)}
                  </p>
                </div>

                <div className="catalog-prices-row__editor">
                  <div className="catalog-prices-row__edit-row">
                    <div className="catalog-prices-row__input-wrap min-w-0 flex-1">
                      <Input
                        label={t("prices.newPrice")}
                        name={`price-${row.productId}`}
                        inputMode="decimal"
                        autoComplete="off"
                        value={row.draftPrice}
                        aria-invalid={Boolean(row.rowError) || invalidDirty}
                        aria-describedby={
                          row.rowError || invalidDirty
                            ? `price-error-${row.productId}`
                            : undefined
                        }
                        onChange={(event) =>
                          updateDraft(row.productId, (current) => ({
                            ...current,
                            draftPrice: event.target.value,
                            rowError: null,
                          }))
                        }
                        onKeyDown={(event) => {
                          if (event.key !== "Enter") {
                            return;
                          }
                          event.preventDefault();
                          void saveProduct(row.productId);
                        }}
                      />
                    </div>
                    {dirty ? (
                      <Button
                        type="button"
                        className="catalog-prices-row__save min-h-11 shrink-0"
                        disabled={!canSave || saving}
                        onClick={() => void saveProduct(row.productId)}
                        data-testid={`price-save-${row.productId}`}
                        aria-label={t("prices.saveOneAria").replace("{product}", row.name)}
                      >
                        {saving ? (
                          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                        ) : (
                          <Save className="size-4 shrink-0" aria-hidden />
                        )}
                        {saving ? t("prices.savingOne") : t("prices.saveOne")}
                      </Button>
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
              </article>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
