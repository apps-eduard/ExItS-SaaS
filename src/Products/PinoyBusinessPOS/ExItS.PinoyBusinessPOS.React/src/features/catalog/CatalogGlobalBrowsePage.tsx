import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2, Upload } from "lucide-react";
import {
  globalProductImageUrl,
  listActiveGlobalCategories,
  searchActiveGlobalProducts,
} from "@/api/platform/merchant-catalog-client";
import {
  importSelectedGlobalProducts,
  listImportedGlobalProducts,
} from "@/api/pos/pos-catalog-import-client";
import { PosApiError } from "@/api/pos/pos-http";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { canGovernOrganizationCatalog } from "@/access/pos-capabilities";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

export function CatalogGlobalBrowsePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const workspace = usePosWorkspaceScope();
  const { sessionGrant } = useWorkspace();
  const canGovern = canGovernOrganizationCatalog(sessionGrant);
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [importError, setImportError] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const categoriesQuery = useQuery({
    queryKey: ["merchant-catalog", "categories"],
    enabled: online && Boolean(workspace),
    queryFn: ({ signal }) => listActiveGlobalCategories({ pageSize: 100 }, signal),
  });

  const productsQuery = useQuery({
    queryKey: ["merchant-catalog", "search", debounced, categoryId],
    enabled: online && Boolean(workspace),
    queryFn: ({ signal }) =>
      searchActiveGlobalProducts(
        {
          search: debounced || undefined,
          categoryId: categoryId || undefined,
          pageSize: 40,
        },
        signal,
      ),
  });

  const productIds = productsQuery.data?.items.map((item) => item.id) ?? [];

  const importedQuery = useQuery({
    queryKey: ["catalog-import", "imported", workspace?.organizationId, productIds.join(",")],
    enabled: online && Boolean(workspace) && productIds.length > 0,
    queryFn: ({ signal }) => listImportedGlobalProducts(workspace!, productIds, signal),
  });

  const importedSet = useMemo(
    () => new Set(importedQuery.data?.importedIds ?? []),
    [importedQuery.data?.importedIds],
  );

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const category of categoriesQuery.data?.items ?? []) {
      map.set(category.id, category.name);
    }
    return map;
  }, [categoriesQuery.data?.items]);

  const selectedNewCount = useMemo(() => {
    let count = 0;
    for (const id of selected) {
      if (!importedSet.has(id)) count += 1;
    }
    return count;
  }, [importedSet, selected]);

  const importMutation = useMutation({
    mutationFn: async () => {
      if (!workspace) throw new Error("Missing workspace");
      if (!canGovern) {
        throw new Error(t("catalog.governance.importRequiresOrgGovernance"));
      }
      const ids = [...selected].filter((id) => !importedSet.has(id));
      if (ids.length === 0) {
        throw new Error(t("catalogGlobal.nothingToImport"));
      }
      return importSelectedGlobalProducts(workspace, { platformGlobalProductIds: ids });
    },
    onSuccess: (job) => {
      void queryClient.invalidateQueries({ queryKey: ["catalog-import"] });
      setSelected(new Set());
      navigate(`/catalog/import-jobs/${job.jobId}`);
    },
    onError: (error) => {
      if (error instanceof PosApiError || error instanceof PlatformApiError) {
        setImportError(error.message);
        return;
      }
      setImportError(error instanceof Error ? error.message : t("error.title"));
    },
  });

  function toggleSelect(productId: string, alreadyImported: boolean) {
    if (alreadyImported) return;
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(productId)) next.delete(productId);
      else next.add(productId);
      return next;
    });
  }

  const pageHeader = (
    <PageHeader
      title={t("catalogGlobal.title")}
      description={t("catalogGlobal.lede")}
      backTo={pageBackNav.catalog.to}
      backLabel={t(pageBackNav.catalog.labelKey)}
      backTestId="page-header-back-catalog"
    />
  );

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div
        className="catalog-global-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="catalog-global-page"
      >
        {pageHeader}
        <OnlineRequiredCard code={ONLINE_REQUIRED_CODES.CatalogImport} />
      </div>
    );
  }

  const categoryFilters = [
    { key: "all", value: "", label: t("catalogGlobal.allCategories") },
    ...(categoriesQuery.data?.items.map((category) => ({
      key: category.id,
      value: category.id,
      label: category.name,
    })) ?? []),
  ];

  return (
    <div
      className="catalog-global-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="catalog-global-page"
    >
      {pageHeader}

      <SearchField
        label={t("catalogGlobal.search")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalogGlobal.search")}
        data-testid="catalog-global-search"
        containerClassName="catalog-global-page__search exits-page__search exits-animate-toolbar"
      />

      {categoriesQuery.isSuccess && categoryFilters.length > 1 ? (
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("catalogGlobal.categoryFilter")}
          testId="catalog-global-category-filters"
          items={categoryFilters.map((filter) => ({
            key: filter.key,
            label: filter.label,
            state: (categoryId || "all") === (filter.value || "all") ? "active" : "idle",
            testId: `catalog-global-category-${filter.key}`,
            onSelect: () => setCategoryId(filter.value),
          }))}
        />
      ) : null}

      {productsQuery.isLoading || importedQuery.isLoading ? (
        <LoadingState label={t("loading.label")} />
      ) : null}
      {productsQuery.isError ? (
        <ErrorState title={t("error.title")} detail={(productsQuery.error as Error).message} />
      ) : null}
      {productsQuery.isSuccess && productsQuery.data.items.length === 0 ? (
        <EmptyState title={t("catalogGlobal.empty")} detail={t("catalogGlobal.emptyDetail")} />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="catalog-global-list">
        {productsQuery.data?.items.map((product) => {
          const already = importedSet.has(product.id);
          const isSelected = selected.has(product.id);
          const categoryName = product.globalCategoryId
            ? categoryNameById.get(product.globalCategoryId)
            : null;
          const meta = [categoryName, product.unit, product.sellingMode]
            .filter(Boolean)
            .join(" · ");
          const identity = [product.sku, product.barcode].filter(Boolean).join(" · ");

          return (
            <li key={product.id}>
              <article
                className={cn(
                  "catalog-global-product-row exits-list__card",
                  already && "catalog-global-product-row--added",
                  isSelected && !already && "catalog-global-product-row--selected",
                )}
              >
                <div className="catalog-global-product-row__media">
                  {product.hasImage ? (
                    <img
                      src={globalProductImageUrl(product.id, "thumb", product.imageVersion)}
                      alt=""
                      className="catalog-global-product-row__image"
                      loading="lazy"
                    />
                  ) : (
                    <div className="catalog-global-product-row__placeholder" aria-hidden>
                      —
                    </div>
                  )}
                </div>

                <div className="catalog-global-product-row__main min-w-0">
                  <p className="exits-list__name m-0 truncate font-semibold">{product.name}</p>
                  {meta ? (
                    <p className="catalog-global-product-row__meta m-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                      {meta}
                    </p>
                  ) : null}
                  {identity ? (
                    <p className="catalog-global-product-row__identity m-0 mt-1 truncate text-[length:var(--exits-text-xs)] text-muted">
                      {identity}
                    </p>
                  ) : null}
                </div>

                <div className="catalog-global-product-row__aside">
                  {product.sellingPrice != null ? (
                    <span className="catalog-global-product-row__price">
                      {formatPeso(product.sellingPrice)}
                    </span>
                  ) : null}
                  {already ? (
                    <StatusChip tone="warning">{t("catalogImport.alreadyAdded")}</StatusChip>
                  ) : canGovern ? (
                    <label className="catalog-global-product-row__select catalog-form-check">
                      <input
                        type="checkbox"
                        checked={isSelected}
                        data-testid={`catalog-global-select-${product.id}`}
                        onChange={() => toggleSelect(product.id, already)}
                      />
                      <span>{t("catalogGlobal.select")}</span>
                    </label>
                  ) : null}
                </div>
              </article>
            </li>
          );
        })}
      </ul>

      {importError ? <ErrorState title={t("error.title")} detail={importError} /> : null}

      {!canGovern ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="catalog-import-governance-gate"
        >
          {t("catalog.governance.importRequiresOrgGovernance")}
        </p>
      ) : null}

      <div className="catalog-form-actions catalog-global-actions">
        <div className="catalog-form-actions__primary">
          <p className="catalog-global-actions__count m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("catalogGlobal.selectedCount").replace("{count}", String(selectedNewCount))}
          </p>
        </div>
        <div className="catalog-form-actions__secondary">
          <Button
            type="button"
            className="catalog-form-actions__save min-h-11"
            data-testid="catalog-global-import"
            disabled={!canGovern || selectedNewCount === 0 || importMutation.isPending}
            onClick={() => {
              setImportError(null);
              importMutation.mutate();
            }}
          >
            {importMutation.isPending ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Upload className="size-4 shrink-0" aria-hidden />
            )}
            {importMutation.isPending
              ? t("catalogImport.starting")
              : t("catalogGlobal.importSelected")}
          </Button>
        </div>
      </div>
    </div>
  );
}
