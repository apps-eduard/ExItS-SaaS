import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { listCatalogProducts, updateCatalogProductPrices } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type PriceDraft = {
  productId: string;
  name: string;
  currentPrice: number;
  draftPrice: string;
  expectedUpdatedAtUtc: string;
  rowError: string | null;
};

function toDrafts(products: PosCatalogProductDto[]): PriceDraft[] {
  return products.map((product) => ({
    productId: product.productId,
    name: product.name,
    currentPrice: product.sellingPrice,
    draftPrice: String(product.sellingPrice),
    expectedUpdatedAtUtc: product.updatedAtUtc,
    rowError: null,
  }));
}

export function TodaysPricesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [drafts, setDrafts] = useState<PriceDraft[]>([]);
  const [banner, setBanner] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace
        ? {
            organizationId: boundWorkspace.organizationId,
            branchId: boundWorkspace.branchId,
          }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["catalog", "prices", workspace?.organizationId, workspace?.branchId, debounced],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogProducts(workspace!, { search: debounced || undefined, pageSize: 100 }, signal),
  });

  useEffect(() => {
    if (query.data) {
      setDrafts(toDrafts(query.data.items));
      setBanner(null);
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
        setBanner(
          t("prices.partialFailure")
            .replace("{failed}", String(response.failedCount))
            .replace("{succeeded}", String(response.succeededCount)),
        );
        // Preserve dirty failed rows and concurrency tokens for retry.
        return;
      }
      setBanner(t("prices.success").replace("{changed}", String(response.changedCount)));
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
    onError: (err) => {
      setBanner(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4 pb-24" data-testid="todays-prices-page">
      <PageHeader title={t("prices.title")} description={t("prices.lede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/catalog">{t("catalog.back")}</Link>
      </Button>
      <SearchField
        label={t("catalog.searchProducts")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalog.searchProducts")}
      />
      {banner ? <ErrorState title={t("prices.resultTitle")} detail={banner} /> : null}
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && drafts.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("prices.emptyDetail")} />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {drafts.map((row) => {
          const isDirty = Number(row.draftPrice) !== row.currentPrice;
          return (
            <li key={row.productId}>
              <Card className="flex flex-col gap-2 p-3" data-testid={`price-row-${row.productId}`}>
                <div className="min-w-0">
                  <p className="m-0 truncate font-semibold">{row.name}</p>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("prices.current")}: {row.currentPrice}
                    {isDirty ? ` · ${t("prices.dirty")}` : ""}
                  </p>
                </div>
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
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">
                    {row.rowError}
                  </p>
                ) : null}
              </Card>
            </li>
          );
        })}
      </ul>
      <div className="sticky bottom-0 z-10 -mx-1 border-t border-border bg-background/95 p-3 backdrop-blur">
        <Button
          type="button"
          className="min-h-11 w-full"
          disabled={dirty.length === 0 || saveMutation.isPending}
          onClick={() => saveMutation.mutate()}
          data-testid="prices-save"
        >
          {saveMutation.isPending
            ? t("catalog.saving")
            : t("prices.save").replace("{count}", String(dirty.length))}
        </Button>
      </div>
    </div>
  );
}
