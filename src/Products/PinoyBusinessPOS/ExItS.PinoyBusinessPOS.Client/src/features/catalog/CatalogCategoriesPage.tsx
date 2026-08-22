import { useMemo, useState } from "react";
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
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function CatalogCategoriesPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["catalog", "categories", "all", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "", pageSize: 100 }, signal),
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

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="catalog-categories-page">
      <PageHeader
        title={t("catalog.categoriesTitle")}
        description={t("catalog.categoriesLede")}
        backTo={pageBackNav.catalog.to}
        backLabel={t(pageBackNav.catalog.labelKey)}
        backTestId="page-header-back-catalog"
      />
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}
      <Card>
        <form
          className="flex flex-col gap-2 sm:flex-row sm:items-end"
          onSubmit={(event) => {
            event.preventDefault();
            createMutation.mutate();
          }}
        >
          <div className="min-w-0 flex-1">
            <Input
              label={t("catalog.newCategoryPlaceholder")}
              name="newCategoryName"
              required
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={t("catalog.newCategoryPlaceholder")}
            />
          </div>
          <Button type="submit" className="min-h-11" disabled={createMutation.isPending}>
            {t("catalog.addCategory")}
          </Button>
        </form>
      </Card>
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState
          title={t("catalog.emptyCategories")}
          detail={t("catalog.emptyCategoriesDetail")}
        />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {query.data?.items.map((category) => (
          <li key={category.categoryId}>
            <Card className="flex flex-wrap items-center justify-between gap-2 p-3">
              <div className="min-w-0">
                <p className="m-0 truncate font-semibold">{category.name}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {category.status}
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  onClick={() => {
                    const next = window.prompt(t("catalog.renamePrompt"), category.name);
                    if (!next?.trim() || !workspace) {
                      return;
                    }
                    void updateCatalogCategory(workspace, category.categoryId, {
                      name: next.trim(),
                      expectedUpdatedAtUtc: category.updatedAtUtc,
                    })
                      .then(() =>
                        queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] }),
                      )
                      .catch((err) =>
                        setError(
                          err instanceof PosApiError
                            ? (err.problem.detail ?? err.message)
                            : (err as Error).message,
                        ),
                      );
                  }}
                >
                  {t("catalog.rename")}
                </Button>
                {category.status === "Active" ? (
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    onClick={() => {
                      void deactivateCatalogCategory(workspace, category.categoryId).then(() =>
                        queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] }),
                      );
                    }}
                  >
                    {t("catalog.deactivate")}
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    onClick={() => {
                      void reactivateCatalogCategory(workspace, category.categoryId).then(() =>
                        queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] }),
                      );
                    }}
                  >
                    {t("catalog.reactivate")}
                  </Button>
                )}
              </div>
            </Card>
          </li>
        ))}
      </ul>
    </div>
  );
}
