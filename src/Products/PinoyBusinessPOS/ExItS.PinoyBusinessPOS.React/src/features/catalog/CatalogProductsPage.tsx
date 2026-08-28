import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  Award,
  CircleDollarSign,
  Globe,
  LayoutTemplate,
  Plus,
  Tags,
} from "lucide-react";
import {
  listCatalogBrands,
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

const PAGE_SIZE = 20;

type StatusFilter = "Active" | "Inactive" | "";

const STATUS_FILTERS: Array<{
  value: StatusFilter;
  key: string;
  labelKey: "catalog.statusActive" | "catalog.statusInactive" | "catalog.statusAll";
}> = [
  { value: "Active", key: "Active", labelKey: "catalog.statusActive" },
  { value: "Inactive", key: "Inactive", labelKey: "catalog.statusInactive" },
  { value: "", key: "all", labelKey: "catalog.statusAll" },
];

export function CatalogProductsPage() {
  const { t } = useI18n();
  const workspace = usePosWorkspaceScope();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
  const [categoryId, setCategoryId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [page, setPage] = useState(1);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debounced, status, categoryId, brandId]);

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", "filter", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    staleTime: 60_000,
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", "filter", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    staleTime: 60_000,
    queryFn: ({ signal }) =>
      listCatalogBrands(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const category of categoriesQuery.data?.items ?? []) {
      map.set(category.categoryId, category.name);
    }
    return map;
  }, [categoriesQuery.data?.items]);

  const query = useQuery({
    queryKey: [
      "catalog",
      "products",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
      categoryId,
      brandId,
      page,
    ],
    enabled: Boolean(workspace),
    staleTime: 30_000,
    meta: { suppressGlobalError: true, operation: "list catalog products" },
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        {
          search: debounced || undefined,
          status: status || undefined,
          categoryId: categoryId || undefined,
          brandId: brandId || undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;

  return (
    <div className="catalog-page exits-page flex min-w-0 flex-col gap-3" data-testid="catalog-products-page">
      <PageHeader
        title={t("catalog.productsTitle")}
        description={t("catalog.productsLede")}
        backTo={pageBackNav.managerHome.to}
        backLabel={t(pageBackNav.managerHome.labelKey)}
        backTestId="page-header-back-catalog"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("catalog.productsTitle")}
        testId="catalog-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "new",
            label: t("catalog.newProduct"),
            icon: <Plus />,
            href: "/catalog/products/new",
            testId: "catalog-new-product",
            emphasis: "primary",
          },
          {
            key: "templates",
            label: t("catalog.businessTemplate"),
            icon: <LayoutTemplate />,
            href: "/catalog/templates",
            testId: "catalog-open-templates",
          },
          {
            key: "global",
            label: t("catalog.globalCatalog"),
            icon: <Globe />,
            href: "/catalog/global-catalog",
            testId: "catalog-open-global-catalog",
          },
          {
            key: "categories",
            label: t("catalog.categoriesTitle"),
            icon: <Tags />,
            href: "/catalog/categories",
            testId: "catalog-open-categories",
          },
          {
            key: "brands",
            label: t("catalog.brandsTitle"),
            icon: <Award />,
            href: "/catalog/brands",
            testId: "catalog-open-brands",
          },
          {
            key: "prices",
            label: t("prices.title"),
            icon: <CircleDollarSign />,
            href: "/catalog/todays-prices",
            testId: "catalog-open-prices",
          },
        ]}
      />

      <SearchField
        label={t("catalog.searchProducts")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalog.searchProducts")}
        data-testid="catalog-search"
        containerClassName="catalog-page__search exits-page__search"
      />

      <div className="catalog-page__filters flex flex-wrap gap-2">
        <label className="catalog-form-field flex min-w-[10rem] flex-1 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("catalog.category")}
          <select
            className="catalog-form-select"
            data-testid="catalog-filter-category"
            value={categoryId}
            onChange={(event) => setCategoryId(event.target.value)}
          >
            <option value="">{t("catalog.allCategories")}</option>
            {(categoriesQuery.data?.items ?? []).map((category) => (
              <option key={category.categoryId} value={category.categoryId}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
        <label className="catalog-form-field flex min-w-[10rem] flex-1 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("catalog.brand")}
          <select
            className="catalog-form-select"
            data-testid="catalog-filter-brand"
            value={brandId}
            onChange={(event) => setBrandId(event.target.value)}
          >
            <option value="">{t("catalog.allBrands")}</option>
            {(brandsQuery.data?.items ?? []).map((brand) => (
              <option key={brand.brandId} value={brand.brandId}>
                {brand.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("catalog.statusFilter")}
        testId="catalog-status-filters"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: (status || "all") === filter.key ? "active" : "idle",
          testId: `catalog-status-${filter.key === "all" ? "all" : filter.key}`,
          onSelect: () => setStatus(filter.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("catalog.emptyProductsDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="catalog-products-list">
        {query.data?.items.map((product) => {
          const categoryName = product.categoryId
            ? categoryNameById.get(product.categoryId)
            : undefined;
          const secondaryMeta = [product.brandName, categoryName].filter(Boolean).join(" · ");
          const idsMeta = [product.sku, product.barcode].filter(Boolean).join(" · ");
          const isActive = product.status.toLowerCase() === "active";

          return (
            <li key={product.productId}>
              <Link
                className="exits-list__card catalog-product-row block min-w-0 text-foreground no-underline"
                to={`/catalog/products/${product.productId}/edit`}
                data-testid={`catalog-product-row-${product.productId}`}
              >
                <span className="catalog-product-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">{product.name}</span>
                  {secondaryMeta ? (
                    <span className="catalog-product-row__meta mt-1 block truncate text-muted">
                      {secondaryMeta}
                    </span>
                  ) : null}
                  {idsMeta ? (
                    <span className="catalog-product-row__ids mt-0.5 block truncate text-muted">
                      {idsMeta}
                    </span>
                  ) : null}
                </span>
                <span className="catalog-product-row__aside">
                  {product.sellingPrice != null ? (
                    <span className="catalog-product-row__price">{formatPeso(product.sellingPrice)}</span>
                  ) : null}
                  <span
                    className={
                      isActive
                        ? "catalog-product-row__badge catalog-product-row__badge--active"
                        : "catalog-product-row__badge catalog-product-row__badge--inactive"
                    }
                  >
                    {product.status}
                  </span>
                </span>
              </Link>
            </li>
          );
        })}
      </ul>

      {query.isSuccess && totalCount > 0 ? (
        <div className="exits-pagination" data-testid="catalog-pagination">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("catalog.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </p>
          <div className="exits-pagination__actions flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              data-testid="catalog-prev"
              disabled={!canPrev}
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("catalog.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-9"
              data-testid="catalog-next"
              disabled={!canNext}
              onClick={() => setPage((current) => current + 1)}
            >
              {t("catalog.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
