import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState, type ReactNode } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Pencil } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import type { GlobalBusinessTypeDetail } from "@/api/global-catalog/global-catalog-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { ForbiddenState } from "@/components/exits/ForbiddenState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { BusinessTypeLifecycleActions } from "@/features/global-catalog/BusinessTypeLifecycleActions";
import {
  createBusinessTypeSchema,
  editBusinessTypeSchema,
  type CreateBusinessTypeFormValues,
  type EditBusinessTypeFormValues,
} from "@/features/global-catalog/business-type-form-schema";
import {
  formatGlobalCatalogInstant,
  globalCatalogControlClass,
  globalCatalogDetailCardClass,
  globalCatalogFieldLabelClass,
  globalCatalogStatusTone,
} from "@/features/global-catalog/global-catalog-presentation";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalBusinessTypeDetailQuery } from "@/features/global-catalog/use-global-business-type-queries";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<GlobalBusinessTypeDetail["status"], MessageKey> = {
  Active: "globalCatalog.status.Active",
  Inactive: "globalCatalog.status.Inactive",
  Archived: "globalCatalog.status.Archived",
};

export function BusinessTypeDetailPage() {
  const { businessTypeId = "" } = useParams();
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewGlobalCatalog);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalCategories);

  const query = useGlobalBusinessTypeDetailQuery(businessTypeId, canView);

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
    ? normalizeDiagnosticError({ error: query.error, operation: "Load business type" })
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
        <BusinessTypeDetailContent
          businessType={query.data}
          language={language}
          canManage={canManage}
          onRefresh={() => void query.refetch()}
        />
      ) : null}
    </section>
  );
}

function BusinessTypeDetailContent({
  businessType,
  language,
  canManage,
  onRefresh,
}: {
  businessType: GlobalBusinessTypeDetail;
  language: string;
  canManage: boolean;
  onRefresh: () => void;
}) {
  const { t } = usePreferences();
  const created = formatGlobalCatalogInstant(businessType.createdAtUtc, language);
  const updated = formatGlobalCatalogInstant(businessType.updatedAtUtc, language);

  return (
    <>
      <PageHeader
        title={businessType.name}
        description={t("globalCatalog.businessTypes.detailDescription")}
        actions={
          canManage ? (
            <Button asChild size="sm" variant="outline">
              <Link to={`/admin/global-catalog/business-types/${businessType.id}/edit`}>
                <Pencil aria-hidden="true" className="mr-1.5 size-4" />
                {t("globalCatalog.edit")}
              </Link>
            </Button>
          ) : null
        }
      />

      <dl className={globalCatalogDetailCardClass}>
        <DetailRow label={t("globalCatalog.column.code")} value={<span className="font-mono">{businessType.code}</span>} />
        <DetailRow
          label={t("globalCatalog.column.status")}
          value={
            <StatusIndicator
              label={t(STATUS_LABELS[businessType.status])}
              tone={globalCatalogStatusTone(businessType.status)}
            />
          }
        />
        <DetailRow label={t("globalCatalog.column.sortOrder")} value={String(businessType.sortOrder)} />
        <DetailRow label={t("globalCatalog.column.description")} value={businessType.description ?? "—"} />
        <DetailRow
          label={t("globalCatalog.field.iconReference")}
          value={
            <span className="font-mono text-[length:var(--exits-text-sm)]">
              {businessType.iconReference ?? "—"}
            </span>
          }
        />
        {created ? <DetailRow label={t("globalCatalog.column.created")} value={created} /> : null}
        {updated ? <DetailRow label={t("globalCatalog.column.updated")} value={updated} /> : null}
      </dl>

      <BusinessTypeLifecycleActions
        businessType={businessType}
        canManage={canManage}
        onStatusChanged={onRefresh}
      />
    </>
  );
}

function DetailRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="grid gap-1 sm:grid-cols-[10rem_1fr] sm:gap-3">
      <dt className="text-muted">{label}</dt>
      <dd>{value}</dd>
    </div>
  );
}

function BusinessTypeFormShell({
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

export function BusinessTypeFormPage({ mode }: { mode: "create" | "edit" }) {
  const { businessTypeId = "" } = useParams();
  const navigate = useNavigate();
  const authorization = useAuthorization();
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalCategories);
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewGlobalCatalog);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (!canView || !canManage) {
    return (
      <ForbiddenState
        requiredPermission={
          !canView
            ? PLATFORM_PERMISSIONS.viewGlobalCatalog
            : PLATFORM_PERMISSIONS.manageGlobalCategories
        }
      />
    );
  }

  if (mode === "edit") {
    return (
      <BusinessTypeEditForm
        businessTypeId={businessTypeId}
        onSaved={(id) => navigate(`/admin/global-catalog/business-types/${id}`)}
      />
    );
  }

  return (
    <BusinessTypeCreateForm onSaved={(id) => navigate(`/admin/global-catalog/business-types/${id}`)} />
  );
}

function BusinessTypeCreateForm({ onSaved }: { onSaved: (id: string) => void }) {
  const { t } = usePreferences();
  const { createBusinessType } = useGlobalCatalogMutations();
  const form = useForm<CreateBusinessTypeFormValues>({
    defaultValues: { code: "", name: "", description: "", sortOrder: 0, iconReference: "" },
    resolver: zodResolver(createBusinessTypeSchema),
  });
  const [serverError, setServerError] = useState<string | null>(null);

  async function onSubmit(values: CreateBusinessTypeFormValues) {
    setServerError(null);
    try {
      const created = await createBusinessType.mutateAsync({
        code: values.code.trim(),
        name: values.name.trim(),
        description: values.description?.trim() || undefined,
        sortOrder: values.sortOrder,
        iconReference: values.iconReference?.trim() || undefined,
      });
      onSaved(created.id);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      setServerError(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  return (
    <BusinessTypeFormShell
      title={t("globalCatalog.businessTypes.create")}
      description={t("globalCatalog.businessTypes.createDescription")}
    >
      <form className="grid max-w-2xl gap-3" onSubmit={form.handleSubmit(onSubmit)}>
        <FormField label={t("globalCatalog.column.code")} htmlFor="gc-bt-code" error={form.formState.errors.code?.message}>
          <Input id="gc-bt-code" {...form.register("code")} autoComplete="off" />
        </FormField>
        <FormField label={t("globalCatalog.field.name")} htmlFor="gc-bt-name" error={form.formState.errors.name?.message}>
          <Input id="gc-bt-name" {...form.register("name")} required />
        </FormField>
        <FormField
          label={t("globalCatalog.column.description")}
          htmlFor="gc-bt-description"
          error={form.formState.errors.description?.message}
        >
          <textarea
            id="gc-bt-description"
            className={`${globalCatalogControlClass} min-h-24 py-2`}
            {...form.register("description")}
          />
        </FormField>
        <FormField
          label={t("globalCatalog.field.sortOrder")}
          htmlFor="gc-bt-sort-order"
          error={form.formState.errors.sortOrder?.message}
        >
          <Input id="gc-bt-sort-order" inputMode="numeric" {...form.register("sortOrder", { valueAsNumber: true })} />
        </FormField>
        <FormField
          label={t("globalCatalog.field.iconReference")}
          htmlFor="gc-bt-icon"
          error={form.formState.errors.iconReference?.message}
        >
          <Input id="gc-bt-icon" {...form.register("iconReference")} autoComplete="off" />
        </FormField>
        {serverError ? (
          <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {serverError}
          </p>
        ) : null}
        <FormActions pending={createBusinessType.isPending} />
      </form>
    </BusinessTypeFormShell>
  );
}

function BusinessTypeEditForm({
  businessTypeId,
  onSaved,
}: {
  businessTypeId: string;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const query = useGlobalBusinessTypeDetailQuery(businessTypeId, true);

  if (query.isPending) {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (query.isError || !query.data) {
    const diagnostic = query.error
      ? normalizeDiagnosticError({ error: query.error, operation: "Load business type" })
      : null;
    return diagnostic ? (
      <ErrorState
        diagnostic={diagnostic}
        title={t("globalCatalog.error")}
        headingLevel="h1"
        onRetry={() => void query.refetch()}
      />
    ) : null;
  }

  return <BusinessTypeEditFormBody businessType={query.data} onSaved={onSaved} />;
}

function BusinessTypeEditFormBody({
  businessType,
  onSaved,
}: {
  businessType: GlobalBusinessTypeDetail;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const { updateBusinessType } = useGlobalCatalogMutations();
  const detailQuery = useGlobalBusinessTypeDetailQuery(businessType.id, true);
  const form = useForm<EditBusinessTypeFormValues>({
    defaultValues: {
      name: businessType.name,
      description: businessType.description ?? "",
      sortOrder: businessType.sortOrder,
      iconReference: businessType.iconReference ?? "",
    },
    resolver: zodResolver(editBusinessTypeSchema),
  });
  const [serverError, setServerError] = useState<string | null>(null);

  useEffect(() => {
    form.reset({
      name: businessType.name,
      description: businessType.description ?? "",
      sortOrder: businessType.sortOrder,
      iconReference: businessType.iconReference ?? "",
    });
  }, [businessType, form]);

  async function onSubmit(values: EditBusinessTypeFormValues) {
    setServerError(null);
    const expectedUpdatedAtUtc = detailQuery.data?.updatedAtUtc ?? businessType.updatedAtUtc;
    if (!expectedUpdatedAtUtc) {
      setServerError(t("globalCatalog.mutation.error.unknown"));
      return;
    }
    try {
      const updated = await updateBusinessType.mutateAsync({
        businessTypeId: businessType.id,
        input: {
          name: values.name.trim(),
          description: values.description?.trim() || undefined,
          sortOrder: values.sortOrder,
          iconReference: values.iconReference?.trim() || undefined,
          expectedUpdatedAtUtc,
        },
      });
      onSaved(updated.id);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      if (failure.kind === "conflict") {
        await detailQuery.refetch();
      }
      setServerError(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  return (
    <BusinessTypeFormShell
      title={t("globalCatalog.businessTypes.edit")}
      description={t("globalCatalog.businessTypes.editDescription")}
    >
      <form className="grid max-w-2xl gap-3" onSubmit={form.handleSubmit(onSubmit)}>
        <FormField label={t("globalCatalog.column.code")} htmlFor="gc-bt-code-readonly">
          <Input id="gc-bt-code-readonly" value={businessType.code} readOnly disabled className="font-mono" />
        </FormField>
        <FormField label={t("globalCatalog.field.name")} htmlFor="gc-bt-name" error={form.formState.errors.name?.message}>
          <Input id="gc-bt-name" {...form.register("name")} required />
        </FormField>
        <FormField
          label={t("globalCatalog.column.description")}
          htmlFor="gc-bt-description"
          error={form.formState.errors.description?.message}
        >
          <textarea
            id="gc-bt-description"
            className={`${globalCatalogControlClass} min-h-24 py-2`}
            {...form.register("description")}
          />
        </FormField>
        <FormField
          label={t("globalCatalog.field.sortOrder")}
          htmlFor="gc-bt-sort-order"
          error={form.formState.errors.sortOrder?.message}
        >
          <Input id="gc-bt-sort-order" inputMode="numeric" {...form.register("sortOrder", { valueAsNumber: true })} />
        </FormField>
        <FormField
          label={t("globalCatalog.field.iconReference")}
          htmlFor="gc-bt-icon"
          error={form.formState.errors.iconReference?.message}
        >
          <Input id="gc-bt-icon" {...form.register("iconReference")} autoComplete="off" />
        </FormField>
        {serverError ? (
          <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {serverError}
          </p>
        ) : null}
        <FormActions pending={updateBusinessType.isPending} />
      </form>
    </BusinessTypeFormShell>
  );
}

function FormField({
  label,
  htmlFor,
  error,
  children,
}: {
  label: string;
  htmlFor: string;
  error?: string;
  children: ReactNode;
}) {
  return (
    <div className="grid gap-1">
      <Label htmlFor={htmlFor} className={globalCatalogFieldLabelClass}>
        {label}
      </Label>
      {children}
      {error ? (
        <p className="text-[length:var(--exits-text-xs)] text-danger" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}

function FormActions({ pending }: { pending: boolean }) {
  const { t } = usePreferences();
  const navigate = useNavigate();
  return (
    <div className="flex flex-wrap gap-2">
      <Button type="submit" size="sm" disabled={pending} aria-busy={pending}>
        {pending ? t("globalCatalog.saving") : t("globalCatalog.save")}
      </Button>
      <Button type="button" size="sm" variant="outline" onClick={() => navigate(-1)}>
        {t("globalCatalog.cancel")}
      </Button>
    </div>
  );
}
