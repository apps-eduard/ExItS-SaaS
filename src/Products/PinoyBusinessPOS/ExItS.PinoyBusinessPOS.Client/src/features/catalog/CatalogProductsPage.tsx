import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function CatalogProductsPage() {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

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

  const query = useQuery({
    queryKey: ["catalog", "products", workspace?.organizationId, workspace?.branchId, debounced],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogProducts(workspace!, { search: debounced || undefined, pageSize: 50 }, signal),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="catalog-products-page">
      <PageHeader title={t("catalog.productsTitle")} description={t("catalog.productsLede")} />
      <div className="flex flex-wrap gap-2">
        <Button asChild className="min-h-11">
          <Link to="/catalog/products/new">{t("catalog.newProduct")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11" data-testid="catalog-open-templates">
          <Link to="/catalog/templates">{t("catalog.businessTemplate")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11"
          data-testid="catalog-open-global-catalog"
        >
          <Link to="/catalog/global-catalog">{t("catalog.globalCatalog")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/catalog/categories">{t("catalog.categoriesTitle")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/catalog/todays-prices">{t("prices.title")}</Link>
        </Button>
      </div>
      <SearchField
        label={t("catalog.searchProducts")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalog.searchProducts")}
      />
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("catalog.emptyProductsDetail")} />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {query.data?.items.map((product) => (
          <li key={product.productId}>
            <Card className="p-3">
              <Link
                className="block min-w-0 text-foreground no-underline"
                to={`/catalog/products/${product.productId}/edit`}
              >
                <span className="block truncate font-semibold">{product.name}</span>
                <span className="block truncate text-[length:var(--exits-text-sm)] text-muted">
                  {[product.sku, product.barcode].filter(Boolean).join(" · ") || product.status}
                </span>
              </Link>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}
