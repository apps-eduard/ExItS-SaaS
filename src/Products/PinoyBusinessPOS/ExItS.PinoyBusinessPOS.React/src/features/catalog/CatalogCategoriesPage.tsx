import { useEffect, useMemo, useState } from "react";
import { Ban, Check, Loader2, Pencil, Plus, RotateCcw, X } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCatalogCategory,
  deactivateCatalogCategory,
  listCatalogCategories,
  reactivateCatalogCategory,
  updateCatalogCategory,
} from "@/api/pos/pos-catalog-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { Input } from "@/components/ui/input";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

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

export function CatalogCategoriesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const workspace = usePosWorkspaceScope();
  const [name, setName] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
  const [error, setError] = useState<string | null>(null);
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState("");
  const [actingId, setActingId] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const query = useQuery({
    queryKey: [
      "catalog",
      "categories",
      "all",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogCategories(
        workspace!,
        {
          search: debounced || undefined,
          status: status === "" ? "" : status,
          pageSize: 100,
        },
        signal,
      ),
  });

  const createMutation = useMutation({
    mutationFn: () => createCatalogCategory(workspace!, { name: name.trim() }),
    onSuccess: async () => {
      setName("");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const renameMutation = useMutation({
    mutationFn: async ({
      categoryId,
      nextName,
      expectedUpdatedAtUtc,
    }: {
      categoryId: string;
      nextName: string;
      expectedUpdatedAtUtc: string;
    }) => updateCatalogCategory(workspace!, categoryId, { name: nextName, expectedUpdatedAtUtc }),
    onSuccess: async () => {
      setRenamingId(null);
      setRenameDraft("");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  async function handleStatusToggle(categoryId: string, isActive: boolean) {
    if (!workspace) return;
    setActingId(categoryId);
    setError(null);
    try {
      if (isActive) {
        await deactivateCatalogCategory(workspace, categoryId);
      } else {
        await reactivateCatalogCategory(workspace, categoryId);
      }
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    } catch (err) {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    } finally {
      setActingId(null);
    }
  }

  function beginRename(categoryId: string, currentName: string) {
    setRenamingId(categoryId);
    setRenameDraft(currentName);
    setError(null);
  }

  function cancelRename() {
    setRenamingId(null);
    setRenameDraft("");
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div
      className="catalog-categories-page catalog-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="catalog-categories-page"
    >
      <PageHeader
        title={t("catalog.categoriesTitle")}
        description={t("catalog.categoriesLede")}
        backTo={pageBackNav.catalog.to}
        backLabel={t(pageBackNav.catalog.labelKey)}
        backTestId="page-header-back-catalog"
      />

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <section className="catalog-form-section exits-animate-panel">
        <h2 className="catalog-form-section__title">{t("catalog.sectionCategoryQuickAdd")}</h2>
        <form
          className="catalog-form-quick-add__row"
          onSubmit={(event) => {
            event.preventDefault();
            createMutation.mutate();
          }}
        >
          <div className="catalog-form-quick-add__field">
            <Input
              label={t("catalog.newCategoryPlaceholder")}
              name="newCategoryName"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={t("catalog.newCategoryPlaceholder")}
            />
          </div>
          <Button
            type="submit"
            variant="outline"
            className="catalog-form-quick-add__button"
            data-testid="catalog-add-category"
            disabled={!name.trim() || createMutation.isPending}
          >
            {createMutation.isPending ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Plus className="size-4 shrink-0" aria-hidden />
            )}
            {createMutation.isPending ? t("catalog.addingCategory") : t("catalog.addCategory")}
          </Button>
        </form>
      </section>

      <SearchField
        label={t("catalog.searchCategories")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalog.searchCategories")}
        data-testid="catalog-categories-search"
        containerClassName="catalog-categories-page__search exits-page__search exits-animate-toolbar"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("catalog.categoryStatusFilter")}
        testId="catalog-category-status-filters"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: (status || "all") === filter.key ? "active" : "idle",
          testId: `catalog-category-status-${filter.key === "all" ? "all" : filter.key}`,
          onSelect: () => setStatus(filter.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState
          title={t("catalog.emptyCategories")}
          detail={t("catalog.emptyCategoriesDetail")}
        />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="catalog-categories-list">
        {query.data?.items.map((category) => {
          const isActive = category.status === "Active";
          const isRenaming = renamingId === category.categoryId;
          const isActing = actingId === category.categoryId;

          return (
            <li key={category.categoryId}>
              <article
                className="catalog-category-row exits-list__card"
                data-testid={`catalog-category-row-${category.categoryId}`}
              >
                <div className="catalog-category-row__main min-w-0">
                  {isRenaming ? (
                    <div className="catalog-category-row__rename">
                      <Input
                        label={t("catalog.renamePrompt")}
                        name={`rename-${category.categoryId}`}
                        value={renameDraft}
                        onChange={(event) => setRenameDraft(event.target.value)}
                        data-testid={`catalog-category-rename-input-${category.categoryId}`}
                      />
                      <div className="catalog-category-row__rename-actions">
                        <Button
                          type="button"
                          variant="outline"
                          className="min-h-11"
                          data-testid={`catalog-category-rename-save-${category.categoryId}`}
                          disabled={!renameDraft.trim() || renameMutation.isPending}
                          onClick={() =>
                            renameMutation.mutate({
                              categoryId: category.categoryId,
                              nextName: renameDraft.trim(),
                              expectedUpdatedAtUtc: category.updatedAtUtc,
                            })
                          }
                        >
                          {renameMutation.isPending ? (
                            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                          ) : (
                            <Check className="size-4 shrink-0" aria-hidden />
                          )}
                          {t("catalog.saveRename")}
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          className="min-h-11"
                          aria-label={t("catalog.cancelRename")}
                          onClick={cancelRename}
                        >
                          <X className="size-4 shrink-0" aria-hidden />
                        </Button>
                      </div>
                    </div>
                  ) : (
                    <>
                      <p className="exits-list__name m-0 truncate font-semibold">{category.name}</p>
                      <div className="catalog-category-row__status mt-1">
                        <StatusChip tone={isActive ? "success" : "warning"}>
                          {category.status}
                        </StatusChip>
                      </div>
                    </>
                  )}
                </div>

                {!isRenaming ? (
                  <div className="catalog-category-row__actions">
                    <Button
                      type="button"
                      variant="outline"
                      className="catalog-category-row__action min-h-11"
                      data-testid={`catalog-category-rename-${category.categoryId}`}
                      disabled={isActing}
                      onClick={() => beginRename(category.categoryId, category.name)}
                    >
                      <Pencil className="size-4 shrink-0" aria-hidden />
                      {t("catalog.rename")}
                    </Button>
                    {isActive ? (
                      <Button
                        type="button"
                        variant="destructive"
                        className="catalog-category-row__action min-h-11"
                        data-testid={`catalog-category-deactivate-${category.categoryId}`}
                        disabled={isActing}
                        onClick={() => void handleStatusToggle(category.categoryId, true)}
                      >
                        {isActing ? (
                          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                        ) : (
                          <Ban className="size-4 shrink-0" aria-hidden />
                        )}
                        {t("catalog.deactivate")}
                      </Button>
                    ) : (
                      <Button
                        type="button"
                        variant="outline"
                        className="catalog-category-row__action catalog-form-actions__restore min-h-11"
                        data-testid={`catalog-category-reactivate-${category.categoryId}`}
                        disabled={isActing}
                        onClick={() => void handleStatusToggle(category.categoryId, false)}
                      >
                        {isActing ? (
                          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                        ) : (
                          <RotateCcw className="size-4 shrink-0" aria-hidden />
                        )}
                        {t("catalog.reactivate")}
                      </Button>
                    )}
                  </div>
                ) : null}
              </article>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
