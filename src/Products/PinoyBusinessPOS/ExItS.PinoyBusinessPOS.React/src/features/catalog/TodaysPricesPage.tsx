import { useEffect, useState } from "react";
import { Loader2, Save } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { listCatalogProducts, updateCatalogProductPrices } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

type PriceDraft = {
  productId: string;
  name: string;
  brandName?: string | null;
  currentPrice: number;
  draftPrice: string;
  expectedUpdatedAtUtc: string;
  rowError: string | null;
};

function toDrafts(products: PosCatalogProductDto[]): PriceDraft[] {
  return products.map((product) => ({
    productId: product.productId,
    name: product.name,
    brandName: product.brandName,
    currentPrice: product.sellingPrice,
    draftPrice: String(product.sellingPrice),
    expectedUpdatedAtUtc: product.updatedAtUtc,
    rowError: null,
  }));
}

export function TodaysPricesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const workspace = usePosWorkspaceScope();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [drafts, setDrafts] = useState<PriceDraft[]>([]);
  const [bannerError, setBannerError] = useState<string | null>(null);
  const [bannerSuccess, setBannerSuccess] = useState<string | null>(null);

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
    if (query.data) {
      setDrafts(toDrafts(query.data.items));
      setBannerError(null);
      setBannerSuccess(null);
    }
  }, [query.data]);

  const dirty = drafts.filter(
    (row) => Number(row.draftPrice) !== row.currentPrice && row.draftPrice.trim() !== "",
  );

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace) {
        throw new Error("Workspace required");
      }
      const items = dirty.map((row) => ({
        productId: row.productId,
        sellingPrice: Number(row.draftPrice),
        expectedUpdatedAtUtc: row.expectedUpdatedAtUtc,
      }));
      if (items.length === 0) {
        throw new Error(t("prices.nothingToSave"));
      }
      return updateCatalogProductPrices(workspace, { items });
    },
    onSuccess: async (response) => {
      setDrafts((current) =>
        current.map((row) => {
          const result = response.results.find((item) => item.productId === row.productId);
          if (!result) {
            return row;
          }
          if (!result.succeeded) {
            return {
              ...row,
              rowError: result.errorMessage ?? t("prices.itemFailed"),
            };
          }
          const nextPrice = result.product?.sellingPrice ?? Number(row.draftPrice);
          const nextToken = result.product?.updatedAtUtc ?? row.expectedUpdatedAtUtc;
          return {
            ...row,
            currentPrice: nextPrice,
            draftPrice: String(nextPrice),
            expectedUpdatedAtUtc: nextToken,
            rowError: null,
          };
        }),
      );
      if (response.failedCount > 0) {
        setBannerSuccess(null);
        setBannerError(
          t("prices.partialFailure")
            .replace("{failed}", String(response.failedCount))
            .replace("{succeeded}", String(response.succeededCount)),
        );
        return;
      }
      setBannerError(null);
      setBannerSuccess(t("prices.success").replace("{changed}", String(response.changedCount)));
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
    onError: (err) => {
      setBannerSuccess(null);
      setBannerError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

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

      {bannerError ? <ErrorState title={t("prices.resultTitle")} detail={bannerError} /> : null}
      {bannerSuccess ? (
        <div className="exits-alert exits-alert--success" role="status">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{bannerSuccess}</p>
        </div>
      ) : null}

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && drafts.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("prices.emptyDetail")} />
      ) : null}

      <ul className="catalog-prices-list exits-list m-0 grid list-none gap-2 p-0">
        {drafts.map((row) => {
          const isDirty = Number(row.draftPrice) !== row.currentPrice;
          return (
            <li key={row.productId}>
              <article
                className={cn(
                  "catalog-prices-row exits-list__card",
                  isDirty && "catalog-prices-row--dirty",
                  row.rowError && "catalog-prices-row--error",
                )}
                data-testid={`price-row-${row.productId}`}
              >
                <div className="catalog-prices-row__main min-w-0">
                  <p className="exits-list__name m-0 truncate font-semibold">{row.name}</p>
                  {row.brandName ? (
                    <p className="m-0 mt-0.5 truncate text-[length:var(--exits-text-sm)] text-muted">
                      {row.brandName}
                    </p>
                  ) : null}
                  <div className="catalog-prices-row__meta mt-1 flex flex-wrap items-center gap-2">
                    <span className="text-[length:var(--exits-text-sm)] text-muted">
                      {t("prices.current")}: {formatPeso(row.currentPrice)}
                    </span>
                    {isDirty ? <StatusChip tone="info">{t("prices.dirty")}</StatusChip> : null}
                  </div>
                </div>

                <div className="catalog-prices-row__editor">
                  <Input
                    label={t("prices.newPrice")}
                    name={`price-${row.productId}`}
                    inputMode="decimal"
                    value={row.draftPrice}
                    onChange={(event) =>
                      setDrafts((current) =>
                        current.map((item) =>
                          item.productId === row.productId
                            ? { ...item, draftPrice: event.target.value, rowError: null }
                            : item,
                        ),
                      )
                    }
                  />
                  {row.rowError ? (
                    <p className="catalog-prices-row__error m-0 text-[length:var(--exits-text-sm)] text-destructive">
                      {row.rowError}
                    </p>
                  ) : null}
                </div>
              </article>
            </li>
          );
        })}
      </ul>

      <div className="catalog-form-actions catalog-prices-actions">
        <div className="catalog-form-actions__primary">
          <p className="catalog-prices-actions__summary m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {dirty.length > 0
              ? t("prices.pendingCount").replace("{count}", String(dirty.length))
              : t("prices.noChanges")}
          </p>
        </div>
        <div className="catalog-form-actions__secondary">
          <Button
            type="button"
            className="catalog-form-actions__save min-h-11"
            disabled={dirty.length === 0 || saveMutation.isPending}
            onClick={() => saveMutation.mutate()}
            data-testid="prices-save"
          >
            {saveMutation.isPending ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Save className="size-4 shrink-0" aria-hidden />
            )}
            {saveMutation.isPending
              ? t("catalog.saving")
              : t("prices.save").replace("{count}", String(dirty.length))}
          </Button>
        </div>
      </div>
    </div>
  );
}
