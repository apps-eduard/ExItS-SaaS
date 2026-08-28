import { useEffect, useState } from "react";
import { Ban, Check, Loader2, Pencil, Plus, RotateCcw, X } from "lucide-react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createCatalogBrand,
  deactivateCatalogBrand,
  listCatalogBrands,
  reactivateCatalogBrand,
  updateCatalogBrand,
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
      className="catalog-brands-page catalog-page exits-page flex min-w-0 flex-col gap-3"
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
            variant="outline"
            className="catalog-form-quick-add__button"
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

      <SearchField
        label={t("catalog.searchBrands")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("catalog.searchBrands")}
        data-testid="catalog-brands-search"
        containerClassName="catalog-brands-page__search exits-page__search exits-animate-toolbar"
      />

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("catalog.brandStatusFilter")}
        testId="catalog-brand-status-filters"
        items={STATUS_FILTERS.map((filter) => ({
          key: filter.key,
          label: t(filter.labelKey),
          state: (status || "all") === filter.key ? "active" : "idle",
          testId: `catalog-brand-status-${filter.key === "all" ? "all" : filter.key}`,
          onSelect: () => setStatus(filter.value),
        }))}
      />

      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState title={t("error.title")} detail={(query.error as Error).message} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState title={t("catalog.emptyBrands")} detail={t("catalog.emptyBrandsDetail")} />
      ) : null}

      <ul
        className="catalog-brands-list m-0 grid list-none gap-2 p-0"
        data-testid="catalog-brands-list"
      >
        {query.data?.items.map((brand) => {
          const isActive = brand.status === "Active";
          const isRenaming = renamingId === brand.brandId;
          const isActing = actingId === brand.brandId;

          return (
            <li key={brand.brandId}>
              <article
                className="catalog-brand-row exits-list__card"
                data-testid={`catalog-brand-row-${brand.brandId}`}
              >
                <div className="catalog-brand-row__main min-w-0">
                  {isRenaming ? (
                    <div className="catalog-brand-row__rename">
                      <Input
                        label={t("catalog.renameBrandPrompt")}
                        name={`rename-${brand.brandId}`}
                        value={renameDraft}
                        onChange={(event) => setRenameDraft(event.target.value)}
                        data-testid={`catalog-brand-rename-input-${brand.brandId}`}
                      />
                      <div className="catalog-brand-row__rename-actions">
                        <Button
                          type="button"
                          variant="outline"
                          className="min-h-11"
                          data-testid={`catalog-brand-rename-save-${brand.brandId}`}
                          disabled={!renameDraft.trim() || renameMutation.isPending}
                          onClick={() =>
                            renameMutation.mutate({
                              brandId: brand.brandId,
                              nextName: renameDraft.trim(),
                              expectedUpdatedAtUtc: brand.updatedAtUtc,
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
                    <div className="catalog-brand-row__heading">
                      <p className="exits-list__name m-0 min-w-0 truncate font-semibold">{brand.name}</p>
                      <StatusChip tone={isActive ? "success" : "warning"}>{brand.status}</StatusChip>
                    </div>
                  )}
                </div>

                {!isRenaming ? (
                  <div className="catalog-brand-row__actions">
                    <Button
                      type="button"
                      variant="outline"
                      className="catalog-brand-row__action min-h-11"
                      data-testid={`catalog-brand-rename-${brand.brandId}`}
                      disabled={isActing}
                      onClick={() => beginRename(brand.brandId, brand.name)}
                    >
                      <Pencil className="size-4 shrink-0" aria-hidden />
                      {t("catalog.rename")}
                    </Button>
                    {isActive ? (
                      <Button
                        type="button"
                        variant="destructive"
                        className="catalog-brand-row__action min-h-11"
                        data-testid={`catalog-brand-deactivate-${brand.brandId}`}
                        disabled={isActing}
                        onClick={() => void handleStatusToggle(brand.brandId, true)}
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
                        className="catalog-brand-row__action catalog-form-actions__restore min-h-11"
                        data-testid={`catalog-brand-reactivate-${brand.brandId}`}
                        disabled={isActing}
                        onClick={() => void handleStatusToggle(brand.brandId, false)}
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
