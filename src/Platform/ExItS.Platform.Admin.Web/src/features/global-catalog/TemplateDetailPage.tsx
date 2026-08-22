import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState, type ReactNode } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Pencil } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import {
  CATALOG_TEMPLATE_SELECTION_MODES,
  type GlobalBusinessTypeItem,
  type GlobalCatalogTemplateDetail,
  type GlobalCatalogTemplateStatus,
} from "@/api/global-catalog/global-catalog-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { ForbiddenState } from "@/components/exits/ForbiddenState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { BusinessTypePicker } from "@/features/global-catalog/BusinessTypePicker";
import { TemplateCompositionPanel } from "@/features/global-catalog/TemplateCompositionPanel";
import { TemplateLifecycleActions } from "@/features/global-catalog/TemplateLifecycleActions";
import {
  createTemplateSchema,
  editTemplateSchema,
  type CreateTemplateFormValues,
  type EditTemplateFormValues,
} from "@/features/global-catalog/template-form-schema";
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
import { useGlobalBusinessTypesQuery } from "@/features/global-catalog/use-global-business-types-query";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { useGlobalCatalogTemplateDetailQuery } from "@/features/global-catalog/use-global-template-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<GlobalCatalogTemplateStatus, MessageKey> = {
  Draft: "globalCatalog.status.Draft",
  Published: "globalCatalog.templates.status.Published",
  Archived: "globalCatalog.status.Archived",
};

const SELECTION_MODE_LABELS: Record<(typeof CATALOG_TEMPLATE_SELECTION_MODES)[number], MessageKey> = {
  Curated: "globalCatalog.templates.selectionMode.Curated",
  Auto: "globalCatalog.templates.selectionMode.Auto",
  Hybrid: "globalCatalog.templates.selectionMode.Hybrid",
};

export function TemplateDetailPage() {
  const { templateId = "" } = useParams();
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewGlobalCatalog);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalogTemplates);
  const canPublish =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.publishCatalogTemplates);

  const query = useGlobalCatalogTemplateDetailQuery(templateId, canView);

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
    ? normalizeDiagnosticError({ error: query.error, operation: "Load catalog template" })
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
        <TemplateDetailContent
          template={query.data}
          language={language}
          canManage={canManage}
          canPublish={canPublish}
          onRefresh={() => void query.refetch()}
        />
      ) : null}
    </section>
  );
}

function TemplateDetailContent({
  template,
  language,
  canManage,
  canPublish,
  onRefresh,
}: {
  template: GlobalCatalogTemplateDetail;
  language: string;
  canManage: boolean;
  canPublish: boolean;
  onRefresh: () => void;
}) {
  const { t } = usePreferences();
  const readOnly = template.status === "Archived";
  const canEdit = canManage && !readOnly;
  const created = formatGlobalCatalogInstant(template.createdAtUtc, language);
  const updated = formatGlobalCatalogInstant(template.updatedAtUtc, language);
  const published = formatGlobalCatalogInstant(template.publishedAtUtc, language);

  return (
    <>
      <PageHeader
        title={template.name}
        description={t("globalCatalog.templates.detailDescription")}
        actions={
          canEdit ? (
            <Button asChild size="sm" variant="outline">
              <Link to={`/admin/global-catalog/templates/${template.id}/edit`}>
                <Pencil aria-hidden="true" className="mr-1.5 size-4" />
                {t("globalCatalog.edit")}
              </Link>
            </Button>
          ) : null
        }
      />

      <dl className={globalCatalogDetailCardClass}>
        <DetailRow label={t("globalCatalog.templates.column.slug")} value={<span className="font-mono">{template.slug}</span>} />
        <DetailRow
          label={t("globalCatalog.column.status")}
          value={
            <StatusIndicator
              label={t(STATUS_LABELS[template.status])}
              tone={globalCatalogStatusTone(template.status)}
            />
          }
        />
        <DetailRow
          label={t("globalCatalog.templates.column.primaryBusinessType")}
          value={template.primaryBusinessType}
        />
        <DetailRow label={t("globalCatalog.column.description")} value={template.description ?? "—"} />
        <DetailRow
          label={t("globalCatalog.field.iconReference")}
          value={
            <span className="font-mono text-[length:var(--exits-text-sm)]">
              {template.iconReference ?? "—"}
            </span>
          }
        />
        <DetailRow
          label={t("globalCatalog.templates.column.defaultBatchSize")}
          value={String(template.defaultBatchSize)}
        />
        <DetailRow
          label={t("globalCatalog.templates.column.selectionMode")}
          value={t(SELECTION_MODE_LABELS[template.selectionMode])}
        />
        <DetailRow
          label={t("globalCatalog.templates.column.productCount")}
          value={String(template.productCount)}
        />
        {published ? (
          <DetailRow label={t("globalCatalog.templates.column.publishedAt")} value={published} />
        ) : null}
        {created ? <DetailRow label={t("globalCatalog.column.created")} value={created} /> : null}
        {updated ? <DetailRow label={t("globalCatalog.column.updated")} value={updated} /> : null}
      </dl>

      <TemplateLifecycleActions template={template} canPublish={canPublish} onChanged={onRefresh} />

      <TemplateCompositionPanel
        template={template}
        canManage={canManage}
        onChanged={onRefresh}
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

function TemplateFormShell({
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

export function TemplateFormPage({ mode }: { mode: "create" | "edit" }) {
  const { templateId = "" } = useParams();
  const navigate = useNavigate();
  const authorization = useAuthorization();
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageCatalogTemplates);
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
            : PLATFORM_PERMISSIONS.manageCatalogTemplates
        }
      />
    );
  }

  if (mode === "edit") {
    return (
      <TemplateEditForm
        templateId={templateId}
        onSaved={(id) => navigate(`/admin/global-catalog/templates/${id}`)}
      />
    );
  }

  return (
    <TemplateCreateForm onSaved={(id) => navigate(`/admin/global-catalog/templates/${id}`)} />
  );
}

function TemplateCreateForm({ onSaved }: { onSaved: (id: string) => void }) {
  const { t } = usePreferences();
  const businessTypesQuery = useGlobalBusinessTypesQuery(true);
  const { createTemplate } = useGlobalCatalogMutations();
  const form = useForm<CreateTemplateFormValues>({
    defaultValues: {
      name: "",
      slug: "",
      description: "",
      primaryBusinessTypeId: "",
      iconReference: "",
      defaultBatchSize: 20,
      selectionMode: "Curated",
    },
    resolver: zodResolver(createTemplateSchema),
  });
  const [serverError, setServerError] = useState<string | null>(null);
  const businessTypes = businessTypesQuery.data?.items ?? [];

  async function onSubmit(values: CreateTemplateFormValues) {
    setServerError(null);
    const selected = businessTypes.find((item) => item.id === values.primaryBusinessTypeId);
    try {
      const created = await createTemplate.mutateAsync({
        name: values.name.trim(),
        slug: values.slug?.trim() || undefined,
        description: values.description?.trim() || undefined,
        primaryBusinessTypeId: values.primaryBusinessTypeId,
        primaryBusinessType: selected?.code,
        iconReference: values.iconReference?.trim() || undefined,
        defaultBatchSize: values.defaultBatchSize,
        selectionMode: values.selectionMode,
      });
      onSaved(created.id);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      setServerError(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  return (
    <TemplateFormShell
      title={t("globalCatalog.templates.create")}
      description={t("globalCatalog.templates.createDescription")}
    >
      <TemplateFormFields
        form={form}
        businessTypes={businessTypes}
        pending={createTemplate.isPending}
        serverError={serverError}
        onSubmit={form.handleSubmit(onSubmit)}
      />
    </TemplateFormShell>
  );
}

function TemplateEditForm({
  templateId,
  onSaved,
}: {
  templateId: string;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const query = useGlobalCatalogTemplateDetailQuery(templateId, true);

  if (query.isPending) {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (query.isError || !query.data) {
    const diagnostic = query.error
      ? normalizeDiagnosticError({ error: query.error, operation: "Load catalog template" })
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

  if (query.data.status === "Archived") {
    return <ShellNotFoundPage />;
  }

  return <TemplateEditFormBody template={query.data} onSaved={onSaved} />;
}

function TemplateEditFormBody({
  template,
  onSaved,
}: {
  template: GlobalCatalogTemplateDetail;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const businessTypesQuery = useGlobalBusinessTypesQuery(true);
  const { updateTemplate } = useGlobalCatalogMutations();
  const detailQuery = useGlobalCatalogTemplateDetailQuery(template.id, true);
  const form = useForm<EditTemplateFormValues>({
    defaultValues: {
      name: template.name,
      slug: template.slug,
      description: template.description ?? "",
      primaryBusinessTypeId: template.primaryBusinessTypeId,
      iconReference: template.iconReference ?? "",
      defaultBatchSize: template.defaultBatchSize,
      selectionMode: template.selectionMode,
    },
    resolver: zodResolver(editTemplateSchema),
  });
  const [serverError, setServerError] = useState<string | null>(null);
  const businessTypes = businessTypesQuery.data?.items ?? [];

  useEffect(() => {
    form.reset({
      name: template.name,
      slug: template.slug,
      description: template.description ?? "",
      primaryBusinessTypeId: template.primaryBusinessTypeId,
      iconReference: template.iconReference ?? "",
      defaultBatchSize: template.defaultBatchSize,
      selectionMode: template.selectionMode,
    });
  }, [template, form]);

  async function onSubmit(values: EditTemplateFormValues) {
    setServerError(null);
    const expectedUpdatedAtUtc = detailQuery.data?.updatedAtUtc ?? template.updatedAtUtc;
    if (!expectedUpdatedAtUtc) {
      setServerError(t("globalCatalog.mutation.error.unknown"));
      return;
    }
    const selected = businessTypes.find((item) => item.id === values.primaryBusinessTypeId);
    try {
      const updated = await updateTemplate.mutateAsync({
        templateId: template.id,
        input: {
          name: values.name.trim(),
          slug: values.slug?.trim() || undefined,
          description: values.description?.trim() || undefined,
          primaryBusinessTypeId: values.primaryBusinessTypeId,
          primaryBusinessType: selected?.code,
          iconReference: values.iconReference?.trim() || undefined,
          defaultBatchSize: values.defaultBatchSize,
          selectionMode: values.selectionMode,
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
    <TemplateFormShell
      title={t("globalCatalog.templates.edit")}
      description={t("globalCatalog.templates.editDescription")}
    >
      <TemplateFormFields
        form={form}
        businessTypes={businessTypes}
        pending={updateTemplate.isPending}
        serverError={serverError}
        onSubmit={form.handleSubmit(onSubmit)}
      />
    </TemplateFormShell>
  );
}

function TemplateFormFields({
  form,
  businessTypes,
  pending,
  serverError,
  onSubmit,
}: {
  form: ReturnType<typeof useForm<CreateTemplateFormValues>>;
  businessTypes: readonly GlobalBusinessTypeItem[];
  pending: boolean;
  serverError: string | null;
  onSubmit: () => void;
}) {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const errors = form.formState.errors;

  return (
    <form className="grid max-w-2xl gap-3" onSubmit={onSubmit}>
      <FormField label={t("globalCatalog.field.name")} htmlFor="gc-tpl-name" error={errors.name?.message}>
        <Input id="gc-tpl-name" {...form.register("name")} required />
      </FormField>
      <FormField label={t("globalCatalog.templates.column.slug")} htmlFor="gc-tpl-slug" error={errors.slug?.message}>
        <Input id="gc-tpl-slug" {...form.register("slug")} autoComplete="off" className="font-mono" />
      </FormField>
      <FormField
        label={t("globalCatalog.column.description")}
        htmlFor="gc-tpl-description"
        error={errors.description?.message}
      >
        <textarea
          id="gc-tpl-description"
          className={`${globalCatalogControlClass} min-h-24 py-2`}
          {...form.register("description")}
        />
      </FormField>
      <FormField
        label={t("globalCatalog.templates.column.primaryBusinessType")}
        htmlFor="gc-tpl-business-type"
        error={errors.primaryBusinessTypeId?.message}
      >
        <BusinessTypePicker
          id="gc-tpl-business-type"
          options={businessTypes}
          value={form.watch("primaryBusinessTypeId")}
          onChange={(value) => form.setValue("primaryBusinessTypeId", value, { shouldValidate: true })}
        />
      </FormField>
      <FormField
        label={t("globalCatalog.field.iconReference")}
        htmlFor="gc-tpl-icon"
        error={errors.iconReference?.message}
      >
        <Input id="gc-tpl-icon" {...form.register("iconReference")} autoComplete="off" />
      </FormField>
      <FormField
        label={t("globalCatalog.templates.column.defaultBatchSize")}
        htmlFor="gc-tpl-batch-size"
        error={errors.defaultBatchSize?.message}
      >
        <Input
          id="gc-tpl-batch-size"
          inputMode="numeric"
          {...form.register("defaultBatchSize", { valueAsNumber: true })}
        />
      </FormField>
      <FormField
        label={t("globalCatalog.templates.column.selectionMode")}
        htmlFor="gc-tpl-selection-mode"
        error={errors.selectionMode?.message}
      >
        <select id="gc-tpl-selection-mode" className={globalCatalogControlClass} {...form.register("selectionMode")}>
          {CATALOG_TEMPLATE_SELECTION_MODES.map((mode) => (
            <option key={mode} value={mode}>
              {t(SELECTION_MODE_LABELS[mode])}
            </option>
          ))}
        </select>
      </FormField>
      {serverError ? (
        <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {serverError}
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
