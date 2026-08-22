import { useState, type FormEvent } from "react";
import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import type {
  GlobalCatalogTemplateDetail,
  GlobalCatalogTemplateProduct,
} from "@/api/global-catalog/global-catalog-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import { AdminTable } from "@/components/exits/AdminTable";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ErrorState } from "@/components/exits/ErrorState";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { useGlobalCatalogTemplateAvailableProductsQuery } from "@/features/global-catalog/use-global-template-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

export function TemplateCompositionPanel({
  template,
  canManage,
  onChanged,
}: {
  template: GlobalCatalogTemplateDetail;
  canManage: boolean;
  onChanged: () => void;
}) {
  const { t } = usePreferences();
  const readOnly = !canManage || template.status === "Archived";
  const [searchDraft, setSearchDraft] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const [productToRemove, setProductToRemove] = useState<GlobalCatalogTemplateProduct | null>(null);
  const {
    assignTemplateProduct,
    removeTemplateProduct,
    updateTemplateProductFlags,
    reorderTemplateProducts,
  } = useGlobalCatalogMutations();

  const availableQuery = useGlobalCatalogTemplateAvailableProductsQuery(
    template.id,
    { page, search: appliedSearch || undefined, status: "Active" },
    !readOnly,
  );

  const assignedProducts = [...template.products].sort((left, right) => left.sortOrder - right.sortOrder);
  const pending =
    assignTemplateProduct.isPending ||
    removeTemplateProduct.isPending ||
    updateTemplateProductFlags.isPending ||
    reorderTemplateProducts.isPending;

  function handleMutationError(error: unknown) {
    const failure = classifyGlobalCatalogMutationFailure(error);
    if (failure.kind === "conflict") {
      onChanged();
    }
    setMutationError(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
  }

  async function onAssign(globalProductId: string) {
    setMutationError(null);
    try {
      await assignTemplateProduct.mutateAsync({
        templateId: template.id,
        input: {
          globalProductId,
          expectedUpdatedAtUtc: template.updatedAtUtc,
        },
      });
      onChanged();
    } catch (error) {
      handleMutationError(error);
    }
  }

  async function onRemove(product: GlobalCatalogTemplateProduct) {
    setMutationError(null);
    try {
      await removeTemplateProduct.mutateAsync({
        templateId: template.id,
        productId: product.globalProductId,
        expectedUpdatedAtUtc: template.updatedAtUtc,
      });
      setProductToRemove(null);
      onChanged();
    } catch (error) {
      handleMutationError(error);
    }
  }

  async function onToggleFlag(
    product: GlobalCatalogTemplateProduct,
    field: "isFeatured" | "isFirstBatch",
    next: boolean,
  ) {
    setMutationError(null);
    try {
      await updateTemplateProductFlags.mutateAsync({
        templateId: template.id,
        productId: product.globalProductId,
        input: {
          [field]: next,
          expectedUpdatedAtUtc: template.updatedAtUtc,
        },
      });
      onChanged();
    } catch (error) {
      handleMutationError(error);
    }
  }

  async function onMove(product: GlobalCatalogTemplateProduct, direction: "up" | "down") {
    const ids = assignedProducts.map((item) => item.globalProductId);
    const index = ids.indexOf(product.globalProductId);
    if (index < 0) {
      return;
    }
    const swapIndex = direction === "up" ? index - 1 : index + 1;
    if (swapIndex < 0 || swapIndex >= ids.length) {
      return;
    }
    const currentId = ids[index];
    const swapId = ids[swapIndex];
    if (!currentId || !swapId) {
      return;
    }
    ids[index] = swapId;
    ids[swapIndex] = currentId;
    setMutationError(null);
    try {
      await reorderTemplateProducts.mutateAsync({
        templateId: template.id,
        input: {
          orderedGlobalProductIds: ids,
          expectedUpdatedAtUtc: template.updatedAtUtc,
        },
      });
      onChanged();
    } catch (error) {
      handleMutationError(error);
    }
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    setPage(1);
    setAppliedSearch(searchDraft.trim());
  }

  const availableDiagnostic = availableQuery.error
    ? normalizeDiagnosticError({ error: availableQuery.error, operation: "Load available products" })
    : null;
  const availableItems = availableQuery.data?.items ?? [];
  const availableTotalPages = Math.max(
    1,
    Math.ceil((availableQuery.data?.totalCount ?? 0) / (availableQuery.data?.pageSize ?? 10)),
  );

  return (
    <div className="grid gap-4">
      <section className="grid gap-3">
        <h2 className="text-[length:var(--exits-text-base)] font-semibold">
          {t("globalCatalog.templates.assignedProducts")}
        </h2>
        {assignedProducts.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">
            {t("globalCatalog.templates.assignedEmpty")}
          </p>
        ) : (
          <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
            <AdminTable
              caption={t("globalCatalog.templates.assignedProducts")}
              empty={t("globalCatalog.templates.assignedEmpty")}
              rows={assignedProducts}
              columns={[
                {
                  id: "name",
                  header: t("globalCatalog.column.name"),
                  cell: (item) => item.productName ?? item.globalProductId,
                },
                {
                  id: "sku",
                  header: t("globalCatalog.column.sku"),
                  cell: (item) => (
                    <span className="font-mono text-[length:var(--exits-text-sm)]">{item.sku ?? "—"}</span>
                  ),
                },
                {
                  id: "featured",
                  header: t("globalCatalog.templates.column.featured"),
                  cell: (item) =>
                    readOnly ? (
                      item.isFeatured ? t("globalCatalog.templates.yes") : t("globalCatalog.templates.no")
                    ) : (
                      <label className="inline-flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={item.isFeatured}
                          disabled={pending}
                          onChange={(event) => void onToggleFlag(item, "isFeatured", event.target.checked)}
                        />
                        {t("globalCatalog.templates.column.featured")}
                      </label>
                    ),
                },
                {
                  id: "firstBatch",
                  header: t("globalCatalog.templates.column.firstBatch"),
                  cell: (item) =>
                    readOnly ? (
                      item.isFirstBatch ? t("globalCatalog.templates.yes") : t("globalCatalog.templates.no")
                    ) : (
                      <label className="inline-flex items-center gap-2">
                        <input
                          type="checkbox"
                          checked={item.isFirstBatch}
                          disabled={pending}
                          onChange={(event) => void onToggleFlag(item, "isFirstBatch", event.target.checked)}
                        />
                        {t("globalCatalog.templates.column.firstBatch")}
                      </label>
                    ),
                },
                {
                  id: "actions",
                  header: t("globalCatalog.templates.column.actions"),
                  cell: (item) =>
                    readOnly ? (
                      "—"
                    ) : (
                      <div className="flex flex-wrap gap-1">
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={pending || assignedProducts[0]?.globalProductId === item.globalProductId}
                          aria-label={t("globalCatalog.templates.moveUp")}
                          onClick={() => void onMove(item, "up")}
                        >
                          <ArrowUp aria-hidden="true" className="size-4" />
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={
                            pending ||
                            assignedProducts[assignedProducts.length - 1]?.globalProductId ===
                              item.globalProductId
                          }
                          aria-label={t("globalCatalog.templates.moveDown")}
                          onClick={() => void onMove(item, "down")}
                        >
                          <ArrowDown aria-hidden="true" className="size-4" />
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="destructive"
                          disabled={pending}
                          aria-label={t("globalCatalog.templates.removeProduct")}
                          onClick={() => {
                            setMutationError(null);
                            setProductToRemove(item);
                          }}
                        >
                          <Trash2 aria-hidden="true" className="size-4" />
                        </Button>
                      </div>
                    ),
                },
              ]}
            />
          </div>
        )}
      </section>

      {!readOnly ? (
        <section className="grid gap-3">
          <h2 className="text-[length:var(--exits-text-base)] font-semibold">
            {t("globalCatalog.templates.availableProducts")}
          </h2>
          <form className="flex flex-wrap gap-2" onSubmit={onSearchSubmit}>
            <Input
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
              placeholder={t("globalCatalog.products.searchPlaceholder")}
              aria-label={t("globalCatalog.search")}
              autoComplete="off"
            />
            <Button type="submit" size="sm">
              {t("globalCatalog.searchSubmit")}
            </Button>
          </form>
          {availableQuery.isPending ? <DashboardWidgetSkeleton rows={3} /> : null}
          {availableQuery.isError && availableDiagnostic ? (
            <ErrorState
              diagnostic={availableDiagnostic}
              title={t("globalCatalog.error")}
              headingLevel="h2"
              onRetry={() => void availableQuery.refetch()}
            />
          ) : null}
          {availableQuery.isSuccess && availableItems.length === 0 ? (
            <p className="text-[length:var(--exits-text-sm)] text-muted">
              {t("globalCatalog.templates.availableEmpty")}
            </p>
          ) : null}
          {availableQuery.isSuccess && availableItems.length > 0 ? (
            <ul className="grid gap-2">
              {availableItems.map((product) => (
                <li
                  key={product.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3"
                >
                  <div>
                    <p className="font-medium">{product.name}</p>
                    <p className="font-mono text-[length:var(--exits-text-xs)] text-muted">{product.sku}</p>
                  </div>
                  <Button
                    type="button"
                    size="sm"
                    disabled={pending}
                    onClick={() => void onAssign(product.id)}
                  >
                    <Plus aria-hidden="true" className="mr-1 size-4" />
                    {t("globalCatalog.templates.assignProduct")}
                  </Button>
                </li>
              ))}
            </ul>
          ) : null}
          {availableQuery.isSuccess && availableTotalPages > 1 ? (
            <div className="flex flex-wrap items-center gap-2">
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={page <= 1}
                onClick={() => setPage((current) => current - 1)}
              >
                {t("globalCatalog.previous")}
              </Button>
              <span className="text-[length:var(--exits-text-sm)] text-muted">
                {t("globalCatalog.page")} {page} / {availableTotalPages}
              </span>
              <Button
                type="button"
                size="sm"
                variant="outline"
                disabled={page >= availableTotalPages}
                onClick={() => setPage((current) => current + 1)}
              >
                {t("globalCatalog.next")}
              </Button>
            </div>
          ) : null}
        </section>
      ) : null}

      {mutationError ? (
        <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {mutationError}
        </p>
      ) : null}

      <ConfirmActionDialog
        open={productToRemove != null}
        title={t("globalCatalog.templates.removeProductConfirmTitle")}
        description={t("globalCatalog.templates.removeProductConfirmBody")}
        confirmLabel={t("globalCatalog.templates.removeProduct")}
        cancelLabel={t("globalCatalog.cancel")}
        pendingLabel={t("globalCatalog.saving")}
        destructive
        pending={removeTemplateProduct.isPending}
        error={
          mutationError && productToRemove ? (
            <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {mutationError}
            </p>
          ) : undefined
        }
        onCancel={() => {
          if (!removeTemplateProduct.isPending) {
            setProductToRemove(null);
          }
        }}
        onConfirm={() => {
          if (productToRemove) {
            void onRemove(productToRemove);
          }
        }}
      />

    </div>
  );
}
