import { useEffect, useState } from "react";
import { Ban, Loader2, Pencil, Plus, RotateCcw, Save, Tags } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageCatalog } from "@/access/pos-capabilities";
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
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/** Matches product-category name max length on the API / DB. */
const CATALOG_CATEGORY_NAME_MAX = 128;

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

/**
 * Product categories admin — same layout/interaction pattern as CatalogBrandsPage.
 * Route: `/catalog/categories` (RequireManageCatalog).
 */
export function CatalogCategoriesPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const workspace = usePosWorkspaceScope();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const allowManage = canManageCatalog(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;

  const [name, setName] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [status, setStatus] = useState<StatusFilter>("Active");
  const [error, setError] = useState<string | null>(null);
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [renameDraft, setRenameDraft] = useState("");
  const [renameOriginal, setRenameOriginal] = useState("");
  const [actingId, setActingId] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setStatus("Active");
    setName("");
    setSearch("");
    setDebounced("");
    setRenamingId(null);
    setRenameDraft("");
    setRenameOriginal("");
    setError(null);
  }, [organizationId]);

  const query = useQuery({
    queryKey: [
      "catalog",
      "categories",
      "all",
      organizationId,
      workspace?.branchId,
      debounced,
      status,
    ],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listCatalogCategories(
        workspace!,
        {
          search: debounced || undefined,
          // Empty string omits the query param (All). Do not pass undefined — client defaults to Active.
          status: status === "" ? "" : status,
          page: 1,
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
        err instanceof PosApiError
          ? (err.problem.detail ?? t("catalog.categoryCreateFailed"))
          : t("catalog.categoryCreateFailed"),
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
    }) =>
      updateCatalogCategory(workspace!, categoryId, {
        name: nextName,
        expectedUpdatedAtUtc,
      }),
    onSuccess: async () => {
      setRenamingId(null);
      setRenameDraft("");
      setRenameOriginal("");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("catalog.categoryUpdateFailed"))
          : t("catalog.categoryUpdateFailed"),
      );
    },
  });

  async function handleStatusToggle(category: PosProductCategoryDto, isActive: boolean) {
    if (!workspace || !allowManage || !online) return;
    if (isActive && !window.confirm(t("catalog.category.deactivateConfirm"))) {
      return;
    }
    setActingId(category.categoryId);
    setError(null);
    try {
      if (isActive) {
        await deactivateCatalogCategory(workspace, category.categoryId);
      } else {
        await reactivateCatalogCategory(workspace, category.categoryId);
      }
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ??
              (isActive
                ? t("catalog.categoryDeactivateFailed")
                : t("catalog.categoryReactivateFailed")))
          : isActive
            ? t("catalog.categoryDeactivateFailed")
            : t("catalog.categoryReactivateFailed"),
      );
    } finally {
      setActingId(null);
    }
  }

  function beginRename(categoryId: string, currentName: string) {
    setRenamingId(categoryId);
    setRenameDraft(currentName);
    setRenameOriginal(currentName);
    setError(null);
  }

  function cancelRename() {
    setRenamingId(null);
    setRenameDraft("");
    setRenameOriginal("");
  }

  function resetRename() {
    setRenameDraft(renameOriginal);
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const items = query.data?.items ?? [];
  const canCreate = allowManage && online && !createMutation.isPending && Boolean(name.trim());

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

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("catalog.offline")}</p>
      ) : null}

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      {allowManage ? (
        <section className="catalog-form-section exits-animate-panel" data-testid="catalog-category-create">
          <h2 className="catalog-form-section__title">{t("catalog.sectionCategoryQuickAdd")}</h2>
          <form
            className="catalog-form-quick-add__row"
            onSubmit={(event) => {
              event.preventDefault();
              if (!canCreate) {
                if (!name.trim()) {
                  setError(t("catalog.validation.categoryNameRequired"));
                }
                return;
              }
              createMutation.mutate();
            }}
          >
            <div className="catalog-form-quick-add__field">
              <Input
                label={t("catalog.newCategoryPlaceholder")}
                name="newCategoryName"
                required
                maxLength={CATALOG_CATEGORY_NAME_MAX}
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder={t("catalog.newCategoryPlaceholder")}
                data-testid="catalog-category-name"
              />
            </div>
            <Button
              type="submit"
              variant="default"
              className="catalog-form-quick-add__button catalog-form-quick-add__button--primary"
              data-testid="catalog-category-create-submit"
              disabled={!canCreate}
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
      ) : null}

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
        <ErrorState title={t("error.title")} detail={t("catalog.categoriesLoadFailed")} />
      ) : null}
      {query.isSuccess && items.length === 0 ? (
        <EmptyState
              align="center"
              icon={<Tags className="size-5" strokeWidth={1.75} />}
          title={t("catalog.emptyCategories")}
          detail={
            allowManage ? t("catalog.emptyCategoriesDetail") : t("catalog.emptyCategoriesReadonly")
          }
        />
      ) : null}

      {items.length > 0 ? (
        <div className="catalog-categories-results">
          <ul
            className="catalog-categories-list m-0 grid list-none gap-2 p-0 lg:hidden"
            data-testid="catalog-category-list"
          >
            {items.map((category) => (
              <li key={category.categoryId}>
                <CategoryCard
                  category={category}
                  t={t}
                  allowManage={allowManage}
                  isRenaming={renamingId === category.categoryId}
                  renameDraft={renameDraft}
                  renameOriginal={renameOriginal}
                  isActing={actingId === category.categoryId}
                  renamePending={renameMutation.isPending}
                  onRenameDraftChange={setRenameDraft}
                  onBeginRename={() => beginRename(category.categoryId, category.name)}
                  onCancelRename={cancelRename}
                  onResetRename={resetRename}
                  onSaveRename={() =>
                    renameMutation.mutate({
                      categoryId: category.categoryId,
                      nextName: renameDraft.trim(),
                      expectedUpdatedAtUtc: category.updatedAtUtc,
                    })
                  }
                  onToggleStatus={() =>
                    void handleStatusToggle(category, category.status === "Active")
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
                            renameOriginal={renameOriginal}
                            renamePending={renameMutation.isPending}
                            t={t}
                            onRenameDraftChange={setRenameDraft}
                            onCancelRename={cancelRename}
                            onResetRename={resetRename}
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
                        {!isRenaming && allowManage ? (
                          <div className="catalog-category-row__actions catalog-category-row__actions--table justify-end">
                            <CategoryActionButtons
                              categoryId={category.categoryId}
                              isActive={isActive}
                              isActing={isActing}
                              t={t}
                              onBeginRename={() => beginRename(category.categoryId, category.name)}
                              onToggleStatus={() => void handleStatusToggle(category, isActive)}
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
  renameOriginal,
  renamePending,
  t,
  onRenameDraftChange,
  onCancelRename,
  onResetRename,
  onSaveRename,
}: {
  categoryId: string;
  renameDraft: string;
  renameOriginal: string;
  renamePending: boolean;
  t: Translate;
  onRenameDraftChange: (value: string) => void;
  onCancelRename: () => void;
  onResetRename: () => void;
  onSaveRename: () => void;
}) {
  const isDirty = renameDraft !== renameOriginal;
  const canSave = Boolean(renameDraft.trim()) && isDirty && !renamePending;

  return (
    <div className="catalog-category-row__rename">
      <div className="catalog-category-row__rename-field min-w-0">
        <Input
          label={t("catalog.renamePrompt")}
          name={`rename-${categoryId}`}
          maxLength={CATALOG_CATEGORY_NAME_MAX}
          value={renameDraft}
          onChange={(event) => onRenameDraftChange(event.target.value)}
          data-testid={`catalog-category-rename-input-${categoryId}`}
        />
      </div>
      <div className="catalog-category-row__rename-actions">
        <Button
          type="button"
          variant="default"
          size="icon"
          className="catalog-category-row__rename-save"
          data-testid={`catalog-category-rename-save-${categoryId}`}
          disabled={!canSave}
          aria-label={t("catalog.saveRename")}
          onClick={onSaveRename}
        >
          {renamePending ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : (
            <Save className="size-4 shrink-0" aria-hidden />
          )}
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="catalog-category-row__rename-reset"
          data-testid={`catalog-category-rename-reset-${categoryId}`}
          disabled={!isDirty || renamePending}
          aria-label={t("catalog.resetRename")}
          onClick={onResetRename}
        >
          <RotateCcw className="size-4 shrink-0" aria-hidden />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="catalog-category-row__rename-cancel"
          data-testid={`catalog-category-rename-cancel-${categoryId}`}
          disabled={renamePending}
          aria-label={t("catalog.cancelRename")}
          onClick={onCancelRename}
        >
          <Ban className="size-4 shrink-0" aria-hidden />
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
  allowManage,
  isRenaming,
  renameDraft,
  renameOriginal,
  isActing,
  renamePending,
  onRenameDraftChange,
  onBeginRename,
  onCancelRename,
  onResetRename,
  onSaveRename,
  onToggleStatus,
}: {
  category: PosProductCategoryDto;
  t: Translate;
  allowManage: boolean;
  isRenaming: boolean;
  renameDraft: string;
  renameOriginal: string;
  isActing: boolean;
  renamePending: boolean;
  onRenameDraftChange: (value: string) => void;
  onBeginRename: () => void;
  onCancelRename: () => void;
  onResetRename: () => void;
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
            renameOriginal={renameOriginal}
            renamePending={renamePending}
            t={t}
            onRenameDraftChange={onRenameDraftChange}
            onCancelRename={onCancelRename}
            onResetRename={onResetRename}
            onSaveRename={onSaveRename}
          />
        ) : (
          <div className="catalog-category-row__heading">
            <p className="exits-list__name m-0 min-w-0 truncate font-semibold">{category.name}</p>
            <StatusChip tone={isActive ? "success" : "warning"}>{category.status}</StatusChip>
          </div>
        )}
      </div>

      {!isRenaming && allowManage ? (
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
