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
import { canGovernOrganizationCatalog } from "@/access/pos-capabilities";
import { listOrganizationBranches } from "@/api/platform/platform-auth-client";
import {
  listCatalogBrands,
  listCatalogCategories,
  listCatalogProducts,
} from "@/api/pos/pos-catalog-client";
import type { CatalogProductScopeCode } from "@/api/pos/pos-catalog-types";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { FilterButton, FilterChips } from "@/components/exits/ListToolbar";
import { LoadingState } from "@/components/exits/LoadingState";
import { BackgroundRefreshIndicator } from "@/components/exits/loading/BackgroundRefreshIndicator";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { BottomSheet } from "@/components/exits/SheetDialog";
import {
  buildCatalogActiveFilterChips,
  countCatalogSheetFilters,
  defaultCatalogSheetFilters,
  type CatalogActiveFilterChipId,
  type CatalogStatusFilter,
  type CatalogUsageFilter,
} from "@/features/catalog/catalog-products-filter-helpers";
import {
  isBranchLocalProduct,
  isOrganizationStandardProduct,
  type CatalogScopeFilter,
} from "@/features/catalog/catalog-product-scope";
import {
  businessUsageLabelKey,
  resolveBusinessUsage,
} from "@/features/catalog/product-business-usage";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useMediaMin } from "@/hooks/useMediaQuery";
import { cn } from "@/lib/cn";
import { formatPeso } from "@/lib/format-money";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

const PAGE_SIZE = 20;

type StatusFilter = CatalogStatusFilter;
type UsageFilter = CatalogUsageFilter;

const STATUS_FILTERS: Array<{
  value: StatusFilter;
  key: string;
  labelKey: "catalog.statusActive" | "catalog.statusInactive" | "catalog.statusAll";
}> = [
  { value: "Active", key: "Active", labelKey: "catalog.statusActive" },
  { value: "Inactive", key: "Inactive", labelKey: "catalog.statusInactive" },
  { value: "", key: "all", labelKey: "catalog.statusAll" },
];

const USAGE_FILTERS: Array<{
  value: UsageFilter;
  key: string;
  labelKey:
    | "catalog.businessUsage.filterAll"
    | "catalog.businessUsage.filterResale"
    | "catalog.businessUsage.filterIngredient"
    | "catalog.businessUsage.filterInternal"
    | "catalog.businessUsage.filterProduced";
}> = [
  { value: "all", key: "all", labelKey: "catalog.businessUsage.filterAll" },
  { value: "Resale", key: "Resale", labelKey: "catalog.businessUsage.filterResale" },
  { value: "Ingredient", key: "Ingredient", labelKey: "catalog.businessUsage.filterIngredient" },
  { value: "InternalUse", key: "InternalUse", labelKey: "catalog.businessUsage.filterInternal" },
  {
    value: "ProducedItem",
    key: "ProducedItem",
    labelKey: "catalog.businessUsage.filterProduced",
  },
];

const SCOPE_FILTERS: Array<{
  value: CatalogScopeFilter;
  key: string;
  labelKey:
    | "catalog.governance.scopeAll"
    | "catalog.governance.scopeOrganization"
    | "catalog.governance.scopeBranch";
  testId: string;
}> = [
  { value: "", key: "all", labelKey: "catalog.governance.scopeAll", testId: "catalog-scope-all" },
  {
    value: "OrganizationStandard",
    key: "OrganizationStandard",
    labelKey: "catalog.governance.scopeOrganization",
    testId: "catalog-scope-OrganizationStandard",
  },
  {
    value: "BranchLocal",
    key: "BranchLocal",
    labelKey: "catalog.governance.scopeBranch",
    testId: "catalog-scope-BranchLocal",
  },
];

export function CatalogProductsPage() {
  const { t } = useI18n();
  const isDesktopFilters = useMediaMin(768);
  const workspace = usePosWorkspaceScope();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canGovern = canGovernOrganizationCatalog(sessionGrant);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
  const [usageFilter, setUsageFilter] = useState<UsageFilter>("all");
  const [scopeFilter, setScopeFilter] = useState<CatalogScopeFilter>("");
  const [categoryId, setCategoryId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [page, setPage] = useState(1);
  const [filtersSheetOpen, setFiltersSheetOpen] = useState(false);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
  }, [debounced, status, categoryId, brandId, usageFilter, scopeFilter]);

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

  const branchesQuery = useQuery({
    queryKey: ["catalog", "org-branches", workspace?.organizationId],
    enabled: Boolean(workspace?.organizationId),
    staleTime: 60_000,
    queryFn: async () => {
      const result = await listOrganizationBranches(workspace!.organizationId);
      if (!result.ok) {
        return [];
      }
      return result.branches;
    },
  });

  const branchNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const branch of branchesQuery.data ?? []) {
      map.set(branch.id, branch.name);
    }
    return map;
  }, [branchesQuery.data]);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const category of categoriesQuery.data?.items ?? []) {
      map.set(category.categoryId, category.name);
    }
    return map;
  }, [categoriesQuery.data?.items]);

  const brandNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const brand of brandsQuery.data?.items ?? []) {
      map.set(brand.brandId, brand.name);
    }
    return map;
  }, [brandsQuery.data?.items]);

  const sheetFilterCount = countCatalogSheetFilters({
    scopeFilter,
    usageFilter,
    categoryId,
    brandId,
  });

  const activeFilterChips = useMemo(
    () =>
      buildCatalogActiveFilterChips({
        scopeFilter,
        usageFilter,
        status,
        categoryId,
        brandId,
        categoryName: categoryId ? categoryNameById.get(categoryId) : undefined,
        brandName: brandId ? brandNameById.get(brandId) : undefined,
        labels: {
          scopeOrganization: t("catalog.governance.scopeOrganization"),
          scopeBranch: t("catalog.governance.scopeBranch"),
          statusActive: t("catalog.statusActive"),
          statusInactive: t("catalog.statusInactive"),
          statusAll: t("catalog.statusAll"),
          usageResale: t("catalog.businessUsage.filterResale"),
          usageIngredient: t("catalog.businessUsage.filterIngredient"),
          usageInternal: t("catalog.businessUsage.filterInternal"),
          usageProduced: t("catalog.businessUsage.filterProduced"),
          categoryPrefix: t("catalog.category"),
          brandPrefix: t("catalog.brand"),
        },
      }),
    [
      scopeFilter,
      usageFilter,
      status,
      categoryId,
      brandId,
      categoryNameById,
      brandNameById,
      t,
    ],
  );

  const clearSheetFilters = () => {
    const defaults = defaultCatalogSheetFilters();
    setScopeFilter(defaults.scopeFilter);
    setUsageFilter(defaults.usageFilter);
    setCategoryId(defaults.categoryId);
    setBrandId(defaults.brandId);
  };

  const removeActiveFilter = (id: string) => {
    switch (id as CatalogActiveFilterChipId) {
      case "scope":
        setScopeFilter("");
        break;
      case "usage":
        setUsageFilter("all");
        break;
      case "status":
        setStatus("Active");
        break;
      case "category":
        setCategoryId("");
        break;
      case "brand":
        setBrandId("");
        break;
      default:
        break;
    }
  };

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
      scopeFilter,
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
          scope: (scopeFilter || undefined) as CatalogProductScopeCode | undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  const filteredItems = useMemo(() => {
    const items = query.data?.items ?? [];
    if (usageFilter === "all") {
      return items;
    }
    return items.filter((product) => resolveBusinessUsage(product) === usageFilter);
  }, [query.data?.items, usageFilter]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));
  const canPrev = page > 1;
  const canNext = page < totalPages && totalCount > 0;
  const currentBranchName = boundWorkspace?.branchName ?? null;

  const toolbarItems = [
    {
      key: "new",
      label: t("catalog.newProduct"),
      icon: <Plus />,
      href: "/catalog/products/new",
      testId: "catalog-new-product",
      emphasis: "primary" as const,
    },
    ...(canGovern
      ? [
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
        ]
      : []),
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
  ];

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
        items={toolbarItems}
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

      {isDesktopFilters ? (
        <div className="catalog-page__filters-desktop flex min-w-0 flex-col gap-2">
          <div className="catalog-page__filters flex flex-wrap gap-2">
            <CatalogCategoryBrandSelects
              categoryId={categoryId}
              brandId={brandId}
              categories={categoriesQuery.data?.items ?? []}
              brands={brandsQuery.data?.items ?? []}
              onCategoryChange={setCategoryId}
              onBrandChange={setBrandId}
              categoryLabel={t("catalog.category")}
              brandLabel={t("catalog.brand")}
              allCategoriesLabel={t("catalog.allCategories")}
              allBrandsLabel={t("catalog.allBrands")}
            />
          </div>

          <CatalogProductFilterChipBars
            scopeFilter={scopeFilter}
            status={status}
            usageFilter={usageFilter}
            onScopeChange={setScopeFilter}
            onStatusChange={setStatus}
            onUsageChange={setUsageFilter}
            t={t}
          />
        </div>
      ) : (
        <div className="catalog-page__filters-mobile flex min-w-0 flex-col gap-2">
          <div className="catalog-page__filters-mobile-toolbar flex min-w-0 items-center gap-2.5">
            <FilterButton
              activeCount={sheetFilterCount}
              className="catalog-page__filter-open h-[var(--exits-chip-min-height)] min-h-[var(--exits-chip-min-height)] shrink-0 px-3 py-0"
              data-testid="catalog-open-filters"
              onClick={() => setFiltersSheetOpen(true)}
            >
              {t("catalog.filters")}
            </FilterButton>
            <div className="catalog-page__status-quick min-w-0 flex-1">
              <CatalogFilterScrollRow
                ariaLabel={t("catalog.statusFilter")}
                testId="catalog-status-filters-mobile"
                items={STATUS_FILTERS.map((filter) => ({
                  key: filter.key,
                  label: t(filter.labelKey),
                  active: (status || "all") === filter.key,
                  testId: `catalog-status-${filter.key === "all" ? "all" : filter.key}`,
                  onSelect: () => setStatus(filter.value),
                }))}
              />
            </div>
          </div>
          <FilterChips
            items={activeFilterChips}
            listLabel={t("catalog.activeFilters")}
            onRemove={removeActiveFilter}
          />
        </div>
      )}

      {!isDesktopFilters ? (
        <BottomSheet
          open={filtersSheetOpen}
          onClose={() => setFiltersSheetOpen(false)}
          title={t("catalog.filtersTitle")}
          panelId="catalog-products-filters-sheet"
          testId="catalog-filters-sheet"
          closeLabel={t("catalog.filtersDone")}
          panelClassName="catalog-page__filters-sheet"
        >
          <div className="catalog-page__filters-sheet-layout flex min-h-0 min-w-0 flex-1 flex-col gap-0 overflow-hidden">
            <div className="catalog-page__filters-sheet-scroll shrink-0 overflow-x-hidden overflow-y-auto overscroll-y-contain">
              <div
                className="catalog-page__filter-section catalog-page__filter-section--taxonomy flex min-w-0 flex-col gap-3"
                data-testid="catalog-filter-taxonomy"
              >
                <p className="catalog-page__filter-section-label m-0">{t("catalog.filtersTaxonomy")}</p>
                <CatalogCategoryBrandSelects
                  categoryId={categoryId}
                  brandId={brandId}
                  categories={categoriesQuery.data?.items ?? []}
                  brands={brandsQuery.data?.items ?? []}
                  onCategoryChange={setCategoryId}
                  onBrandChange={setBrandId}
                  categoryLabel={t("catalog.category")}
                  brandLabel={t("catalog.brand")}
                  allCategoriesLabel={t("catalog.allCategories")}
                  allBrandsLabel={t("catalog.allBrands")}
                  stacked
                />
              </div>
            </div>

            <div className="catalog-page__filters-sheet-chips shrink-0 border-t border-border">
              <CatalogProductFilterChipBars
                scopeFilter={scopeFilter}
                status={status}
                usageFilter={usageFilter}
                onScopeChange={setScopeFilter}
                onStatusChange={setStatus}
                onUsageChange={setUsageFilter}
                t={t}
                includeStatus={false}
                layout="sheet"
              />
              {sheetFilterCount > 0 ? (
                <Button
                  type="button"
                  variant="ghost"
                  className="mt-3 min-h-11 self-start"
                  data-testid="catalog-clear-filters"
                  onClick={clearSheetFilters}
                >
                  {t("catalog.clearFilters")}
                </Button>
              ) : null}
            </div>
          </div>
        </BottomSheet>
      ) : null}

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isFetching && !query.isLoading && query.data ? (
        <BackgroundRefreshIndicator active label={t("loading.updating")} />
      ) : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && filteredItems.length === 0 ? (
        <EmptyState title={t("catalog.emptyProducts")} detail={t("catalog.emptyProductsDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="catalog-products-list">
        {filteredItems.map((product) => {
          const categoryName = product.categoryId
            ? categoryNameById.get(product.categoryId)
            : undefined;
          const usage = resolveBusinessUsage(product);
          const secondaryMeta = [product.brandName, categoryName].filter(Boolean).join(" · ");
          const idsMeta = [product.sku, product.barcode].filter(Boolean).join(" · ");
          const isActive = product.status.toLowerCase() === "active";
          const isStandard = isOrganizationStandardProduct(product);
          const isLocal = isBranchLocalProduct(product);
          const originName = product.originBranchId
            ? (branchNameById.get(product.originBranchId) ?? null)
            : null;
          const scopeBadge = isStandard
            ? t("catalog.governance.organizationProduct")
            : isLocal
              ? originName &&
                  currentBranchName &&
                  product.originBranchId === workspace.branchId
                ? t("catalog.governance.branchProductThisBranch")
                : originName
                  ? t("catalog.governance.branchProductOrigin").replace("{branch}", originName)
                  : t("catalog.governance.branchProduct")
              : null;
          const offeringLabel =
            isStandard && product.isOfferedAtBranch === false
              ? t("catalog.governance.notOfferedAtBranch")
              : null;

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
                  <span className="catalog-product-row__usage mt-0.5 block truncate text-muted">
                    {t(businessUsageLabelKey(usage))}
                  </span>
                  {scopeBadge ? (
                    <span
                      className="catalog-product-row__scope mt-1 inline-flex"
                      data-testid="catalog-product-scope-badge"
                    >
                      <span className="catalog-product-row__badge catalog-product-row__badge--scope">
                        {scopeBadge}
                      </span>
                    </span>
                  ) : null}
                  {offeringLabel ? (
                    <span
                      className="catalog-product-row__offering mt-1 block text-muted"
                      data-testid="catalog-product-offering"
                    >
                      {offeringLabel}
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

type CatalogCategoryBrandSelectsProps = {
  categoryId: string;
  brandId: string;
  categories: ReadonlyArray<{ categoryId: string; name: string }>;
  brands: ReadonlyArray<{ brandId: string; name: string }>;
  onCategoryChange: (value: string) => void;
  onBrandChange: (value: string) => void;
  categoryLabel: string;
  brandLabel: string;
  allCategoriesLabel: string;
  allBrandsLabel: string;
  stacked?: boolean;
};

function CatalogCategoryBrandSelects({
  categoryId,
  brandId,
  categories,
  brands,
  onCategoryChange,
  onBrandChange,
  categoryLabel,
  brandLabel,
  allCategoriesLabel,
  allBrandsLabel,
  stacked = false,
}: CatalogCategoryBrandSelectsProps) {
  const fields = (
    <>
      <label
        className={
          stacked
            ? "catalog-form-field flex w-full flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold"
            : "catalog-form-field flex min-w-[10rem] flex-1 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold"
        }
      >
        {categoryLabel}
        <select
          className="catalog-form-select"
          data-testid="catalog-filter-category"
          value={categoryId}
          onChange={(event) => onCategoryChange(event.target.value)}
        >
          <option value="">{allCategoriesLabel}</option>
          {categories.map((category) => (
            <option key={category.categoryId} value={category.categoryId}>
              {category.name}
            </option>
          ))}
        </select>
      </label>
      <label
        className={
          stacked
            ? "catalog-form-field flex w-full flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold"
            : "catalog-form-field flex min-w-[10rem] flex-1 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold"
        }
      >
        {brandLabel}
        <select
          className="catalog-form-select"
          data-testid="catalog-filter-brand"
          value={brandId}
          onChange={(event) => onBrandChange(event.target.value)}
        >
          <option value="">{allBrandsLabel}</option>
          {brands.map((brand) => (
            <option key={brand.brandId} value={brand.brandId}>
              {brand.name}
            </option>
          ))}
        </select>
      </label>
    </>
  );

  if (stacked) {
    return <div className="flex min-w-0 flex-col gap-2">{fields}</div>;
  }

  return fields;
}

type CatalogFilterScrollRowProps = {
  ariaLabel: string;
  testId: string;
  items: ReadonlyArray<{
    key: string;
    label: string;
    active: boolean;
    testId?: string;
    onSelect: () => void;
  }>;
};

/** Same scroll model as SellCategoryFilter — chips are direct children of the track. */
function CatalogFilterScrollRow({ ariaLabel, testId, items }: CatalogFilterScrollRowProps) {
  return (
    <div className="catalog-filter-scroll-row min-w-0" aria-label={ariaLabel}>
      <div
        role="tablist"
        aria-label={ariaLabel}
        data-testid={testId}
        className="sell-categories-track catalog-page__filter-scroll-track flex w-full min-w-0 gap-1.5 overflow-x-auto overscroll-x-contain pb-0.5"
      >
        {items.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            aria-selected={item.active}
            data-testid={item.testId}
            className={cn("exits-chip shrink-0", item.active && "exits-chip--active")}
            onClick={item.onSelect}
          >
            <span className="exits-chip__label whitespace-nowrap">{item.label}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

type CatalogProductFilterChipBarsProps = {
  scopeFilter: CatalogScopeFilter;
  status: StatusFilter;
  usageFilter: UsageFilter;
  onScopeChange: (value: CatalogScopeFilter) => void;
  onStatusChange: (value: StatusFilter) => void;
  onUsageChange: (value: UsageFilter) => void;
  t: (key: MessageKey) => string;
  includeStatus?: boolean;
  layout?: "inline" | "sheet";
};

function CatalogProductFilterChipBars({
  scopeFilter,
  status,
  usageFilter,
  onScopeChange,
  onStatusChange,
  onUsageChange,
  t,
  includeStatus = true,
  layout = "inline",
}: CatalogProductFilterChipBarsProps) {
  const scopeItems = SCOPE_FILTERS.map((filter) => ({
    key: filter.key,
    label: t(filter.labelKey),
    active: (scopeFilter || "all") === filter.key,
    testId: filter.testId,
    onSelect: () => onScopeChange(filter.value),
  }));

  const statusItems = STATUS_FILTERS.map((filter) => ({
    key: filter.key,
    label: t(filter.labelKey),
    active: (status || "all") === filter.key,
    testId: `catalog-status-${filter.key === "all" ? "all" : filter.key}`,
    onSelect: () => onStatusChange(filter.value),
  }));

  const usageItems = USAGE_FILTERS.map((filter) => ({
    key: filter.key,
    label: t(filter.labelKey),
    active: usageFilter === filter.value,
    testId: `catalog-usage-${filter.key}`,
    onSelect: () => onUsageChange(filter.value),
  }));

  if (layout === "sheet") {
    return (
      <div
        className="catalog-page__filter-groups catalog-page__filter-groups--sheet flex min-w-0 flex-col gap-1.5"
        data-testid="catalog-filter-groups-sheet"
      >
        <section className="catalog-page__filter-section catalog-page__filter-section--scope">
          <p className="catalog-page__filter-section-label">{t("catalog.governance.scopeFilter")}</p>
          <CatalogFilterScrollRow
            ariaLabel={t("catalog.governance.scopeFilter")}
            testId="catalog-scope-filters"
            items={scopeItems}
          />
        </section>
        <section className="catalog-page__filter-section catalog-page__filter-section--usage">
          <p className="catalog-page__filter-section-label">{t("catalog.businessUsage.label")}</p>
          <CatalogFilterScrollRow
            ariaLabel={t("catalog.businessUsage.label")}
            testId="catalog-usage-filters"
            items={usageItems}
          />
        </section>
      </div>
    );
  }

  const rows: Array<{ ariaLabel: string; testId: string; items: CatalogFilterScrollRowProps["items"] }> =
    [
      {
        ariaLabel: t("catalog.governance.scopeFilter"),
        testId: "catalog-scope-filters",
        items: scopeItems,
      },
    ];

  if (includeStatus) {
    rows.push({
      ariaLabel: t("catalog.statusFilter"),
      testId: "catalog-status-filters",
      items: statusItems,
    });
  }

  rows.push({
    ariaLabel: t("catalog.businessUsage.label"),
    testId: "catalog-usage-filters",
    items: usageItems,
  });

  return (
    <div className="catalog-page__filter-groups flex min-w-0 flex-col gap-2">
      {rows.map((row) => (
        <CatalogFilterScrollRow
          key={row.testId}
          ariaLabel={row.ariaLabel}
          testId={row.testId}
          items={row.items}
        />
      ))}
    </div>
  );
}
