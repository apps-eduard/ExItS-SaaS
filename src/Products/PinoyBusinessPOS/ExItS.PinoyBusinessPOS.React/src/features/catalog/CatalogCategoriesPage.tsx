import { useEffect, useState } from "react";
import { Ban, Check, Loader2, Pencil, Plus, RotateCcw, X } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCatalogCategory,
  deactivateCatalogCategory,
  listCatalogCategories,
  reactivateCatalogCategory,
  updateCatalogCategory,
} from "@/api/pos/pos-catalog-client";
import type { PosProductCategoryDto } from "@/api/pos/pos-catalog-types";
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
import type { MessageKey } from "@/i18n/messages";
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

  const items = query.data?.items ?? [];

  return (
    <div
      className="catalog-categories-page catalog-page exits-page flex min-w-0 flex-col gap-2.5"
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
            variant="default"
            className="catalog-form-quick-add__button catalog-form-quick-add__button--primary"
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

      <div className="catalog-categories-toolbar" data-testid="catalog-categories-toolbar">
        <SearchField
          label={t("catalog.searchCategories")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("catalog.searchCategories")}
          data-testid="catalog-categories-search"
          containerClassName="catalog-categories-page__search exits-page__search min-w-0 flex-1"
        />
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("catalog.categoryStatusFilter")}
          testId="catalog-category-status-filters"
          className="catalog-categories-toolbar__filters shrink-0"
          items={STATUS_FILTERS.map((filter) => ({
            key: filter.key,
            label: t(filter.labelKey),
            state: (status || "all") === filter.key ? "active" : "idle",
            testId: `catalog-category-status-${filter.key === "all" ? "all" : filter.key}`,
            onSelect: () => setStatus(filter.value),
          }))}
        />
      </div>

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState
          title={t("catalog.emptyCategories")}
          detail={t("catalog.emptyCategoriesDetail")}
        />
      ) : null}

      {items.length > 0 ? (
        <div className="catalog-categories-results">
          <ul
            className="catalog-categories-list m-0 grid list-none gap-2 p-0 lg:hidden"
            data-testid="catalog-categories-list"
          >
            {items.map((category) => (
              <li key={category.categoryId}>
                <CategoryCard
                  category={category}
                  t={t}
                  isRenaming={renamingId === category.categoryId}
                  renameDraft={renameDraft}
                  isActing={actingId === category.categoryId}
                  renamePending={renameMutation.isPending}
                  onRenameDraftChange={setRenameDraft}
                  onBeginRename={() => beginRename(category.categoryId, category.name)}
                  onCancelRename={cancelRename}
                  onSaveRename={() =>
                    renameMutation.mutate({
                      categoryId: category.categoryId,
                      nextName: renameDraft.trim(),
                      expectedUpdatedAtUtc: category.updatedAtUtc,
                    })
                  }
                  onToggleStatus={() =>
                    void handleStatusToggle(category.categoryId, category.status === "Active")
                  }
                />
              </li>
            ))}
          </ul>

          <div
            className="catalog-categories-table-shell hidden min-w-0 overflow-x-auto lg:block"
            data-testid="catalog-categories-table"
          >
            <table className="catalog-categories-table w-full min-w-[36rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead>
                <tr className="catalog-categories-table__head border-b border-border">
                  <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("catalog.name")}
                  </th>
                  <th className="whitespace-nowrap px-3 py-2.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("catalog.statusFilter")}
                  </th>
                  <th className="whitespace-nowrap px-3 py-2.5 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                    {t("catalog.col.actions")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.map((category) => {
                  const isActive = category.status === "Active";
                  const isRenaming = renamingId === category.categoryId;
                  const isActing = actingId === category.categoryId;
                  return (
                    <tr
                      key={category.categoryId}
                      className="catalog-categories-table__row border-b border-border"
                      data-testid={`catalog-category-table-row-${category.categoryId}`}
                    >
                      <td className="max-w-[20rem] px-3 py-2.5 align-middle">
                        {isRenaming ? (
                          <CategoryRenameEditor
                            categoryId={category.categoryId}
                            renameDraft={renameDraft}
                            renamePending={renameMutation.isPending}
                            t={t}
                            onRenameDraftChange={setRenameDraft}
                            onCancelRename={cancelRename}
                            onSaveRename={() =>
                              renameMutation.mutate({
                                categoryId: category.categoryId,
                                nextName: renameDraft.trim(),
                                expectedUpdatedAtUtc: category.updatedAtUtc,
                              })
                            }
                          />
                        ) : (
                          <span className="block truncate font-semibold text-foreground">
                            {category.name}
                          </span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-3 py-2.5 align-middle">
                        <StatusChip tone={isActive ? "success" : "warning"}>
                          {category.status}
                        </StatusChip>
                      </td>
                      <td className="px-3 py-2.5 align-middle">
                        {!isRenaming ? (
                          <div className="catalog-category-row__actions catalog-category-row__actions--table justify-end">
                            <CategoryActionButtons
                              categoryId={category.categoryId}
                              isActive={isActive}
                              isActing={isActing}
                              t={t}
                              onBeginRename={() => beginRename(category.categoryId, category.name)}
                              onToggleStatus={() =>
                                void handleStatusToggle(category.categoryId, isActive)
                              }
                            />
                          </div>
                        ) : null}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </div>
  );
}

type Translate = (key: MessageKey) => string;

function CategoryRenameEditor({
  categoryId,
  renameDraft,
  renamePending,
  t,
  onRenameDraftChange,
  onCancelRename,
  onSaveRename,
}: {
  categoryId: string;
  renameDraft: string;
  renamePending: boolean;
  t: Translate;
  onRenameDraftChange: (value: string) => void;
  onCancelRename: () => void;
  onSaveRename: () => void;
}) {
  return (
    <div className="catalog-category-row__rename">
      <Input
        label={t("catalog.renamePrompt")}
        name={`rename-${categoryId}`}
        value={renameDraft}
        onChange={(event) => onRenameDraftChange(event.target.value)}
        data-testid={`catalog-category-rename-input-${categoryId}`}
      />
      <div className="catalog-category-row__rename-actions">
        <Button
          type="button"
          variant="default"
          className="catalog-category-row__rename-save"
          data-testid={`catalog-category-rename-save-${categoryId}`}
          disabled={!renameDraft.trim() || renamePending}
          onClick={onSaveRename}
        >
          {renamePending ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : (
            <Check className="size-4 shrink-0" aria-hidden />
          )}
          {t("catalog.saveRename")}
        </Button>
        <Button
          type="button"
          variant="outline"
          className="catalog-category-row__rename-cancel"
          data-testid={`catalog-category-rename-cancel-${categoryId}`}
          disabled={renamePending}
          onClick={onCancelRename}
        >
          <X className="size-4 shrink-0" aria-hidden />
          {t("catalog.cancelRename")}
        </Button>
      </div>
    </div>
  );
}

function CategoryActionButtons({
  categoryId,
  isActive,
  isActing,
  t,
  onBeginRename,
  onToggleStatus,
}: {
  categoryId: string;
  isActive: boolean;
  isActing: boolean;
  t: Translate;
  onBeginRename: () => void;
  onToggleStatus: () => void;
}) {
  return (
    <>
      <Button
        type="button"
        variant="outline"
        className="catalog-category-row__action"
        data-testid={`catalog-category-rename-${categoryId}`}
        disabled={isActing}
        onClick={onBeginRename}
      >
        <Pencil className="size-4 shrink-0" aria-hidden />
        {t("catalog.rename")}
      </Button>
      {isActive ? (
        <Button
          type="button"
          variant="destructive"
          className="catalog-category-row__action"
          data-testid={`catalog-category-deactivate-${categoryId}`}
          disabled={isActing}
          onClick={onToggleStatus}
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
          className="catalog-category-row__action catalog-form-actions__restore"
          data-testid={`catalog-category-reactivate-${categoryId}`}
          disabled={isActing}
          onClick={onToggleStatus}
        >
          {isActing ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : (
            <RotateCcw className="size-4 shrink-0" aria-hidden />
          )}
          {t("catalog.reactivate")}
        </Button>
      )}
    </>
  );
}

function CategoryCard({
  category,
  t,
  isRenaming,
  renameDraft,
  isActing,
  renamePending,
  onRenameDraftChange,
  onBeginRename,
  onCancelRename,
  onSaveRename,
  onToggleStatus,
}: {
  category: PosProductCategoryDto;
  t: Translate;
  isRenaming: boolean;
  renameDraft: string;
  isActing: boolean;
  renamePending: boolean;
  onRenameDraftChange: (value: string) => void;
  onBeginRename: () => void;
  onCancelRename: () => void;
  onSaveRename: () => void;
  onToggleStatus: () => void;
}) {
  const isActive = category.status === "Active";

  return (
    <article
      className="catalog-category-row exits-list__card"
      data-testid={`catalog-category-row-${category.categoryId}`}
    >
      <div className="catalog-category-row__main min-w-0">
        {isRenaming ? (
          <CategoryRenameEditor
            categoryId={category.categoryId}
            renameDraft={renameDraft}
            renamePending={renamePending}
            t={t}
            onRenameDraftChange={onRenameDraftChange}
            onCancelRename={onCancelRename}
            onSaveRename={onSaveRename}
          />
        ) : (
          <div className="catalog-category-row__heading">
            <p className="exits-list__name m-0 min-w-0 truncate font-semibold">{category.name}</p>
            <StatusChip tone={isActive ? "success" : "warning"}>{category.status}</StatusChip>
          </div>
        )}
      </div>

      {!isRenaming ? (
        <div className="catalog-category-row__actions">
          <CategoryActionButtons
            categoryId={category.categoryId}
            isActive={isActive}
            isActing={isActing}
            t={t}
            onBeginRename={onBeginRename}
            onToggleStatus={onToggleStatus}
          />
        </div>
      ) : null}
    </article>
  );
}
