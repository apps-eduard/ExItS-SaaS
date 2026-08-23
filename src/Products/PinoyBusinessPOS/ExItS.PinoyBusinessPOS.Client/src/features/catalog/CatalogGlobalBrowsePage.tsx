import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

export function CatalogGlobalBrowsePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [importError, setImportError] = useState<string | null>(null);

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

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className="flex flex-col gap-4" data-testid="catalog-global-page">
        <PageHeader title={t("catalogGlobal.title")} description={t("catalogGlobal.lede")} />
        <OnlineRequiredCard code={ONLINE_REQUIRED_CODES.CatalogImport} />
        <Button asChild variant="ghost" className="min-h-11 self-start">
          <Link to="/catalog">{t("catalogImport.backToProducts")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4 pb-24" data-testid="catalog-global-page">
      <PageHeader title={t("catalogGlobal.title")} description={t("catalogGlobal.lede")} />

      <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
        <SearchField
          label={t("catalogGlobal.search")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("catalogGlobal.search")}
        />
        <label className="grid min-w-0 flex-1 gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("catalogGlobal.category")}
          <select
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground"
            value={categoryId}
            data-testid="catalog-global-category"
            onChange={(event) => setCategoryId(event.target.value)}
          >
            <option value="">{t("catalogGlobal.allCategories")}</option>
            {categoriesQuery.data?.items.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </label>
      </div>

      {productsQuery.isLoading || importedQuery.isLoading ? (
        <LoadingState label={t("loading.label")} />
      ) : null}
      {productsQuery.isError ? (
        <ErrorState title={t("error.title")} detail={(productsQuery.error as Error).message} />
      ) : null}
      {productsQuery.isSuccess && productsQuery.data.items.length === 0 ? (
        <EmptyState
          title={t("catalogGlobal.empty")}
          detail={t("catalogGlobal.emptyDetail")}
        />
      ) : null}

      <ul
        className="m-0 grid list-none grid-cols-1 gap-2 p-0 sm:grid-cols-2 lg:grid-cols-3"
        data-testid="catalog-global-list"
      >
        {productsQuery.data?.items.map((product) => {
          const already = importedSet.has(product.id);
          const isSelected = selected.has(product.id);
          const categoryName = product.globalCategoryId
            ? categoryNameById.get(product.globalCategoryId)
            : null;
          return (
            <li key={product.id}>
              <Card
                className={cn(
                  "flex h-full flex-col gap-2 p-3",
                  already && "opacity-70",
                  isSelected && !already && "ring-2 ring-primary",
                )}
              >
                <div className="flex gap-3">
                  {product.hasImage ? (
                    <img
                      src={globalProductImageUrl(product.id, "thumb", product.imageVersion)}
                      alt=""
                      className="size-16 shrink-0 rounded-[var(--exits-radius-md)] object-cover"
                      loading="lazy"
                    />
                  ) : (
                    <div
                      className="flex size-16 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)] text-[length:var(--exits-text-xs)] text-muted"
                      aria-hidden
                    >
                      —
                    </div>
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="m-0 truncate font-semibold">{product.name}</p>
                    <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {[categoryName, product.unit, product.sellingMode]
                        .filter(Boolean)
                        .join(" · ")}
                    </p>
                    <p className="mb-0 mt-1 truncate text-[length:var(--exits-text-xs)] text-muted">
                      {[product.sku, product.barcode].filter(Boolean).join(" · ") || "—"}
                    </p>
                  </div>
                </div>
                <div className="mt-auto flex items-center justify-between gap-2">
                  {already ? (
                    <StatusChip>{t("catalogImport.alreadyAdded")}</StatusChip>
                  ) : (
                    <label className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)]">
                      <input
                        type="checkbox"
                        className="size-5"
                        checked={isSelected}
                        data-testid={`catalog-global-select-${product.id}`}
                        onChange={() => toggleSelect(product.id, already)}
                      />
                      {t("catalogGlobal.select")}
                    </label>
                  )}
                </div>
              </Card>
            </li>
          );
        })}
      </ul>

      {importError ? <ErrorState title={t("error.title")} detail={importError} /> : null}

      <div className="fixed inset-x-0 bottom-0 z-20 border-t border-border bg-[var(--exits-bg)] p-3 md:static md:border-0 md:bg-transparent md:p-0">
        <div className="mx-auto flex max-w-5xl flex-wrap items-center justify-between gap-2">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("catalogGlobal.selectedCount").replace("{count}", String(selectedNewCount))}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button asChild variant="ghost" className="min-h-11">
              <Link to="/catalog">{t("catalogImport.backToProducts")}</Link>
            </Button>
            <Button
              type="button"
              className="min-h-11"
              data-testid="catalog-global-import"
              disabled={selectedNewCount === 0 || importMutation.isPending}
              onClick={() => {
                setImportError(null);
                importMutation.mutate();
              }}
            >
              {importMutation.isPending
                ? t("catalogImport.starting")
                : t("catalogGlobal.importSelected")}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
