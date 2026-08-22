import { useMemo, useState, type FormEvent, type ReactNode } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Pencil } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import type { GlobalCategoryDetail } from "@/api/global-catalog/global-catalog-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { ForbiddenState } from "@/components/exits/ForbiddenState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { BusinessTypeMultiSelect } from "@/features/global-catalog/BusinessTypeMultiSelect";
import { CategoryLifecycleActions } from "@/features/global-catalog/CategoryLifecycleActions";
import {
  formatGlobalCatalogInstant,
  globalCatalogStatusTone,
} from "@/features/global-catalog/global-catalog-presentation";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalBusinessTypesQuery } from "@/features/global-catalog/use-global-business-types-query";
import {
  useGlobalCategoryDetailQuery,
  useGlobalCategoryLookupQuery,
} from "@/features/global-catalog/use-global-category-queries";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

const STATUS_LABELS = {
  Active: "globalCatalog.status.Active",
  Inactive: "globalCatalog.status.Inactive",
  Archived: "globalCatalog.status.Archived",
} as const;

export function CategoryDetailPage() {
  const { categoryId = "" } = useParams();
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewGlobalCatalog);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalCategories);

  const query = useGlobalCategoryDetailQuery(categoryId, canView);
  const lookupQuery = useGlobalCategoryLookupQuery(canView);
  const parentName = useMemo(() => {
    const parentId = query.data?.parentId;
    if (!parentId) {
      return null;
    }
    return lookupQuery.data?.items.find((item) => item.id === parentId)?.name ?? parentId;
  }, [lookupQuery.data?.items, query.data?.parentId]);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (!canView) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.viewGlobalCatalog} />;
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load global category" })
    : null;

  return (
    <section className="grid gap-4">
      {query.isPending ? <DashboardWidgetSkeleton rows={4} /> : null}
      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.error")}
          headingLevel="h1"
          onRetry={() => void query.refetch()}
        />
      ) : null}
      {query.data ? (
        <CategoryDetailContent
          category={query.data}
          parentName={parentName}
          language={language}
          canManage={canManage}
        />
      ) : null}
    </section>
  );
}

function CategoryDetailContent({
  category,
  parentName,
  language,
  canManage,
}: {
  category: GlobalCategoryDetail;
  parentName: string | null;
  language: string;
  canManage: boolean;
}) {
  const { t } = usePreferences();

  return (
    <>
      <PageHeader
        title={category.name}
        description={t("globalCatalog.categories.detailDescription")}
        actions={
          canManage ? (
            <Button asChild size="sm" variant="outline">
              <Link to={`/admin/global-catalog/categories/${category.id}/edit`}>
                <Pencil aria-hidden="true" className="mr-1.5 size-4" />
                {t("globalCatalog.edit")}
              </Link>
            </Button>
          ) : null
        }
      />
      <div className="grid gap-4 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 md:grid-cols-2">
        <dl className="grid gap-2 text-[length:var(--exits-text-sm)]">
          <div>
            <dt className="text-muted">{t("globalCatalog.column.status")}</dt>
            <dd className="mt-0.5">
              <StatusIndicator
                tone={globalCatalogStatusTone(category.status)}
                label={t(STATUS_LABELS[category.status])}
              />
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("globalCatalog.column.parent")}</dt>
            <dd className="mt-0.5">{parentName ?? t("globalCatalog.parent.root")}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("globalCatalog.column.sortOrder")}</dt>
            <dd className="mt-0.5 tabular-nums">{category.sortOrder}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("globalCatalog.field.businessTypes")}</dt>
            <dd className="mt-0.5">{category.businessTypes.join(", ") || "—"}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("globalCatalog.column.updated")}</dt>
            <dd className="mt-0.5 tabular-nums">
              {formatGlobalCatalogInstant(category.updatedAtUtc, language) ?? "—"}
            </dd>
          </div>
        </dl>
        <CategoryLifecycleActions category={category} canManage={canManage} />
      </div>
    </>
  );
}

export function CategoryFormPage({ mode }: { mode: "create" | "edit" }) {
  const { categoryId = "" } = useParams();
  const navigate = useNavigate();
  const authorization = useAuthorization();
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalCategories);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (!canManage) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.manageGlobalCategories} />;
  }

  if (mode === "edit") {
    return (
      <CategoryEditForm
        categoryId={categoryId}
        onSaved={(id) => navigate(`/admin/global-catalog/categories/${id}`)}
      />
    );
  }

  return <CategoryCreateForm onSaved={(id) => navigate(`/admin/global-catalog/categories/${id}`)} />;
}

function CategoryCreateForm({ onSaved }: { onSaved: (id: string) => void }) {
  const { t } = usePreferences();
  return (
    <CategoryFormShell
      title={t("globalCatalog.categories.create")}
      description={t("globalCatalog.categories.createDescription")}
    >
      <CategoryForm mode="create" category={null} onSaved={onSaved} />
    </CategoryFormShell>
  );
}

function CategoryEditForm({
  categoryId,
  onSaved,
}: {
  categoryId: string;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const query = useGlobalCategoryDetailQuery(categoryId, true);
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load global category" })
    : null;

  return (
    <CategoryFormShell
      title={t("globalCatalog.categories.edit")}
      description={t("globalCatalog.categories.editDescription")}
    >
      {query.isPending ? <DashboardWidgetSkeleton rows={5} /> : null}
      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}
      {query.data ? <CategoryForm mode="edit" category={query.data} onSaved={onSaved} /> : null}
    </CategoryFormShell>
  );
}

function CategoryFormShell({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="grid gap-4">
      <PageHeader title={title} description={description} />
      {children}
    </section>
  );
}

function CategoryForm({
  mode,
  category,
  onSaved,
}: {
  mode: "create" | "edit";
  category: GlobalCategoryDetail | null;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const businessTypesQuery = useGlobalBusinessTypesQuery(true);
  const lookupQuery = useGlobalCategoryLookupQuery(true);
  const detailQuery = useGlobalCategoryDetailQuery(category?.id ?? "", mode === "edit");
  const { createCategory, updateCategory } = useGlobalCatalogMutations();
  const [name, setName] = useState(category?.name ?? "");
  const [parentId, setParentId] = useState(category?.parentId ?? "");
  const [sortOrder, setSortOrder] = useState(String(category?.sortOrder ?? 0));
  const [businessTypeIds, setBusinessTypeIds] = useState<string[]>(
    category?.businessTypeIds ?? [],
  );
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const parentOptions = (lookupQuery.data?.items ?? []).filter(
    (item) => item.id !== category?.id,
  );

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setErrorMessage(null);
    const parsedSortOrder = Number(sortOrder);
    const payload = {
      name: name.trim(),
      parentId: parentId || null,
      sortOrder: Number.isFinite(parsedSortOrder) ? parsedSortOrder : 0,
      businessTypeIds,
    };
    if (!payload.name) {
      setErrorMessage(t("globalCatalog.validation.nameRequired"));
      return;
    }
    if (category && payload.parentId === category.id) {
      setErrorMessage(t("globalCatalog.validation.selfParent"));
      return;
    }
    try {
      if (mode === "create") {
        const created = await createCategory.mutateAsync(payload);
        onSaved(created.id);
        return;
      }
      const expectedUpdatedAtUtc = detailQuery.data?.updatedAtUtc ?? category?.updatedAtUtc;
      if (!category || !expectedUpdatedAtUtc) {
        setErrorMessage(t("globalCatalog.mutation.error.unknown"));
        return;
      }
      const updated = await updateCategory.mutateAsync({
        categoryId: category.id,
        input: { ...payload, expectedUpdatedAtUtc },
      });
      onSaved(updated.id);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      if (failure.kind === "conflict" && mode === "edit") {
        await detailQuery.refetch();
      }
      setErrorMessage(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  const pending = createCategory.isPending || updateCategory.isPending;

  return (
    <form className="grid max-w-2xl gap-3" onSubmit={(event) => void onSubmit(event)}>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.field.name")}
        <input
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          value={name}
          onChange={(event) => setName(event.target.value)}
          required
        />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.field.parent")}
        <select
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          value={parentId}
          onChange={(event) => setParentId(event.target.value)}
        >
          <option value="">{t("globalCatalog.parent.root")}</option>
          {parentOptions.map((option) => (
            <option key={option.id} value={option.id}>
              {option.name}
            </option>
          ))}
        </select>
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.field.sortOrder")}
        <input
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          inputMode="numeric"
          value={sortOrder}
          onChange={(event) => setSortOrder(event.target.value)}
        />
      </label>
      <BusinessTypeMultiSelect
        id="category-business-types"
        options={businessTypesQuery.data?.items ?? []}
        value={businessTypeIds}
        disabled={businessTypesQuery.isPending}
        onChange={setBusinessTypeIds}
      />
      {errorMessage ? (
        <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {errorMessage}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" size="sm" disabled={pending} aria-busy={pending}>
          {pending ? t("globalCatalog.saving") : t("globalCatalog.save")}
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => navigate(-1)}>
          {t("globalCatalog.cancel")}
        </Button>
      </div>
    </form>
  );
}
