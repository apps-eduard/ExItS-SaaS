import { useEffect, useState } from "react";
import { Ban, Loader2, Pencil, Plus, RotateCcw, Save, Tags } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCatalogBrand,
  deactivateCatalogBrand,
  listCatalogBrands,
  reactivateCatalogBrand,
  updateCatalogBrand,
} from "@/api/pos/pos-catalog-client";
import type { PosProductBrandDto } from "@/api/pos/pos-catalog-types";
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

export function CatalogBrandsPage() {
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
  const [renameOriginal, setRenameOriginal] = useState("");
  const [actingId, setActingId] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const query = useQuery({
    queryKey: [
      "catalog",
      "brands",
      "all",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
      status,
    ],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogBrands(
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
    mutationFn: () => createCatalogBrand(workspace!, { name: name.trim() }),
    onSuccess: async () => {
      setName("");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
    },
    onError: (err) => {
      if (
        err instanceof PosApiError &&
        (err.errorCode?.includes("brand.name.conflict") ||
          /brand.*already exists/i.test(err.problem.detail ?? err.message))
      ) {
        setError(t("catalog.brandAlreadyExists"));
        return;
      }
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const renameMutation = useMutation({
    mutationFn: async ({
      brandId,
      nextName,
      expectedUpdatedAtUtc,
    }: {
      brandId: string;
      nextName: string;
      expectedUpdatedAtUtc: string;
    }) => updateCatalogBrand(workspace!, brandId, { name: nextName, expectedUpdatedAtUtc }),
    onSuccess: async () => {
      setRenamingId(null);
      setRenameDraft("");
      setRenameOriginal("");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
    },
    onError: (err) => {
      if (
        err instanceof PosApiError &&
        (err.errorCode?.includes("brand.name.conflict") ||
          /brand.*already exists/i.test(err.problem.detail ?? err.message))
      ) {
        setError(t("catalog.brandAlreadyExists"));
        return;
      }
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  async function handleStatusToggle(brandId: string, isActive: boolean) {
    if (!workspace) return;
    setActingId(brandId);
    setError(null);
    try {
      if (isActive) {
        await deactivateCatalogBrand(workspace, brandId);
      } else {
        await reactivateCatalogBrand(workspace, brandId);
      }
      await queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
    } catch (err) {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    } finally {
      setActingId(null);
    }
  }

  function beginRename(brandId: string, currentName: string) {
    setRenamingId(brandId);
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

  return (
    <div
      className="catalog-brands-page catalog-page exits-page flex min-w-0 flex-col gap-2.5"
      data-testid="catalog-brands-page"
    >
      <PageHeader
        title={t("catalog.brandsTitle")}
        description={t("catalog.brandsLede")}
        backTo={pageBackNav.catalog.to}
        backLabel={t(pageBackNav.catalog.labelKey)}
        backTestId="page-header-back-catalog"
      />

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <section className="catalog-form-section exits-animate-panel">
        <h2 className="catalog-form-section__title">{t("catalog.sectionBrandQuickAdd")}</h2>
        <form
          className="catalog-form-quick-add__row"
          onSubmit={(event) => {
            event.preventDefault();
            createMutation.mutate();
          }}
        >
          <div className="catalog-form-quick-add__field">
            <Input
              label={t("catalog.newBrandPlaceholder")}
              name="newBrandName"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={t("catalog.newBrandPlaceholder")}
            />
          </div>
          <Button
            type="submit"
            variant="default"
            className="catalog-form-quick-add__button catalog-form-quick-add__button--primary"
            data-testid="catalog-add-brand"
            disabled={!name.trim() || createMutation.isPending}
          >
            {createMutation.isPending ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : (
              <Plus className="size-4 shrink-0" aria-hidden />
            )}
            {createMutation.isPending ? t("catalog.addingBrand") : t("catalog.addBrand")}
          </Button>
        </form>
      </section>

      <div className="catalog-brands-toolbar" data-testid="catalog-brands-toolbar">
        <SearchField
          label={t("catalog.searchBrands")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("catalog.searchBrands")}
          data-testid="catalog-brands-search"
          containerClassName="catalog-brands-page__search exits-page__search min-w-0 flex-1"
        />
        <ExitsChipBar
          variant="filter"
          ariaLabel={t("catalog.brandStatusFilter")}
          testId="catalog-brand-status-filters"
          className="catalog-brands-toolbar__filters shrink-0"
          items={STATUS_FILTERS.map((filter) => ({
            key: filter.key,
            label: t(filter.labelKey),
            state: (status || "all") === filter.key ? "active" : "idle",
            testId: `catalog-brand-status-${filter.key === "all" ? "all" : filter.key}`,
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
              align="center"
              icon={<Tags className="size-5" strokeWidth={1.75} />} title={t("catalog.emptyBrands")} detail={t("catalog.emptyBrandsDetail")} />
      ) : null}

      {items.length > 0 ? (
        <div className="catalog-brands-results">
          <ul
            className="catalog-brands-list m-0 grid list-none gap-2 p-0 lg:hidden"
            data-testid="catalog-brands-list"
          >
            {items.map((brand) => (
              <li key={brand.brandId}>
                <BrandCard
                  brand={brand}
                  t={t}
                  isRenaming={renamingId === brand.brandId}
                  renameDraft={renameDraft}
                  renameOriginal={renameOriginal}
                  isActing={actingId === brand.brandId}
                  renamePending={renameMutation.isPending}
                  onRenameDraftChange={setRenameDraft}
                  onBeginRename={() => beginRename(brand.brandId, brand.name)}
                  onCancelRename={cancelRename}
                  onResetRename={resetRename}
                  onSaveRename={() =>
                    renameMutation.mutate({
                      brandId: brand.brandId,
                      nextName: renameDraft.trim(),
                      expectedUpdatedAtUtc: brand.updatedAtUtc,
                    })
                  }
                  onToggleStatus={() =>
                    void handleStatusToggle(brand.brandId, brand.status === "Active")
                  }
                />
              </li>
            ))}
          </ul>

          <div
            className="catalog-brands-table-shell hidden min-w-0 overflow-x-auto lg:block"
            data-testid="catalog-brands-table"
          >
            <table className="catalog-brands-table w-full min-w-[36rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead>
                <tr className="catalog-brands-table__head border-b border-border">
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
                {items.map((brand) => {
                  const isActive = brand.status === "Active";
                  const isRenaming = renamingId === brand.brandId;
                  const isActing = actingId === brand.brandId;
                  return (
                    <tr
                      key={brand.brandId}
                      className="catalog-brands-table__row border-b border-border"
                      data-testid={`catalog-brand-table-row-${brand.brandId}`}
                    >
                      <td className="max-w-[20rem] px-3 py-2.5 align-middle">
                        {isRenaming ? (
                          <BrandRenameEditor
                            brandId={brand.brandId}
                            renameDraft={renameDraft}
                            renameOriginal={renameOriginal}
                            renamePending={renameMutation.isPending}
                            t={t}
                            onRenameDraftChange={setRenameDraft}
                            onCancelRename={cancelRename}
                            onResetRename={resetRename}
                            onSaveRename={() =>
                              renameMutation.mutate({
                                brandId: brand.brandId,
                                nextName: renameDraft.trim(),
                                expectedUpdatedAtUtc: brand.updatedAtUtc,
                              })
                            }
                          />
                        ) : (
                          <span className="block truncate font-semibold text-foreground">
                            {brand.name}
                          </span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-3 py-2.5 align-middle">
                        <StatusChip tone={isActive ? "success" : "warning"}>
                          {brand.status}
                        </StatusChip>
                      </td>
                      <td className="px-3 py-2.5 align-middle">
                        {!isRenaming ? (
                          <div className="catalog-brand-row__actions catalog-brand-row__actions--table justify-end">
                            <BrandActionButtons
                              brandId={brand.brandId}
                              isActive={isActive}
                              isActing={isActing}
                              t={t}
                              onBeginRename={() => beginRename(brand.brandId, brand.name)}
                              onToggleStatus={() =>
                                void handleStatusToggle(brand.brandId, isActive)
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

function BrandRenameEditor({
  brandId,
  renameDraft,
  renameOriginal,
  renamePending,
  t,
  onRenameDraftChange,
  onCancelRename,
  onResetRename,
  onSaveRename,
}: {
  brandId: string;
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
    <div className="catalog-brand-row__rename">
      <div className="catalog-brand-row__rename-field min-w-0">
        <Input
          label={t("catalog.renameBrandPrompt")}
          name={`rename-${brandId}`}
          value={renameDraft}
          onChange={(event) => onRenameDraftChange(event.target.value)}
          data-testid={`catalog-brand-rename-input-${brandId}`}
        />
      </div>
      <div className="catalog-brand-row__rename-actions">
        <Button
          type="button"
          variant="default"
          size="icon"
          className="catalog-brand-row__rename-save"
          data-testid={`catalog-brand-rename-save-${brandId}`}
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
          className="catalog-brand-row__rename-reset"
          data-testid={`catalog-brand-rename-reset-${brandId}`}
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
          className="catalog-brand-row__rename-cancel"
          data-testid={`catalog-brand-rename-cancel-${brandId}`}
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

function BrandActionButtons({
  brandId,
  isActive,
  isActing,
  t,
  onBeginRename,
  onToggleStatus,
}: {
  brandId: string;
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
        className="catalog-brand-row__action"
        data-testid={`catalog-brand-rename-${brandId}`}
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
          className="catalog-brand-row__action"
          data-testid={`catalog-brand-deactivate-${brandId}`}
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
          className="catalog-brand-row__action catalog-form-actions__restore"
          data-testid={`catalog-brand-reactivate-${brandId}`}
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

function BrandCard({
  brand,
  t,
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
  brand: PosProductBrandDto;
  t: Translate;
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
  const isActive = brand.status === "Active";

  return (
    <article
      className="catalog-brand-row exits-list__card"
      data-testid={`catalog-brand-row-${brand.brandId}`}
    >
      <div className="catalog-brand-row__main min-w-0">
        {isRenaming ? (
          <BrandRenameEditor
            brandId={brand.brandId}
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
          <div className="catalog-brand-row__heading">
            <p className="exits-list__name m-0 min-w-0 truncate font-semibold">{brand.name}</p>
            <StatusChip tone={isActive ? "success" : "warning"}>{brand.status}</StatusChip>
          </div>
        )}
      </div>

      {!isRenaming ? (
        <div className="catalog-brand-row__actions">
          <BrandActionButtons
            brandId={brand.brandId}
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
