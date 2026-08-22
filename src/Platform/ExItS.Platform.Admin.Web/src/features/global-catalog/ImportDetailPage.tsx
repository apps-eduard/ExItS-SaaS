import { useState, type ReactNode } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft } from "lucide-react";

import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE,
  type GlobalCatalogImportStatus,
} from "@/api/global-catalog/global-catalog-types";
import { AdminTable } from "@/components/exits/AdminTable";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import {
  formatGlobalCatalogFileSize,
  formatGlobalCatalogInstant,
  globalCatalogImportStatusTone,
} from "@/features/global-catalog/global-catalog-presentation";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import {
  useGlobalCatalogImportDetailQuery,
  useGlobalCatalogImportErrorsQuery,
} from "@/features/global-catalog/use-global-import-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<GlobalCatalogImportStatus, MessageKey> = {
  Validated: "globalCatalog.imports.status.Validated",
  Queued: "globalCatalog.imports.status.Queued",
  Processing: "globalCatalog.imports.status.Processing",
  Completed: "globalCatalog.imports.status.Completed",
  CompletedWithWarnings: "globalCatalog.imports.status.CompletedWithWarnings",
  Failed: "globalCatalog.imports.status.Failed",
};

export function ImportDetailPage() {
  const { jobId = "" } = useParams();
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const { confirmImport } = useGlobalCatalogMutations();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [errorsPage, setErrorsPage] = useState(1);
  const [confirmError, setConfirmError] = useState<string | null>(null);

  const canImport =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.importGlobalProducts);

  const detailQuery = useGlobalCatalogImportDetailQuery(jobId, canImport);
  const job = detailQuery.data;
  const showErrors = Boolean(
    job && (job.failedCount > 0 || job.skippedCount > 0 || job.status === "Failed"),
  );
  const errorsQuery = useGlobalCatalogImportErrorsQuery(
    jobId,
    { page: errorsPage, pageSize: GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE },
    canImport && showErrors,
  );

  if (authorization.status === "loading") {
    return <DashboardWidgetSkeleton rows={6} />;
  }

  if (!canImport) {
    return <ShellNotFoundPage />;
  }

  const diagnostic = detailQuery.error
    ? normalizeDiagnosticError({ error: detailQuery.error, operation: "Load import job" })
    : null;

  async function onConfirm() {
    setConfirmError(null);
    try {
      await confirmImport.mutateAsync({ jobId });
      setConfirmOpen(false);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      setConfirmError(
        globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)),
      );
    }
  }

  const errorsTotalCount = errorsQuery.data?.totalCount ?? 0;
  const errorsPageSize = errorsQuery.data?.pageSize ?? GLOBAL_CATALOG_IMPORT_ERRORS_PAGE_SIZE;
  const errorsTotalPages = Math.max(1, Math.ceil(errorsTotalCount / errorsPageSize));

  return (
    <section className="grid gap-4">
      <div className="flex flex-wrap items-center gap-2">
        <Button asChild size="sm" variant="outline">
          <Link to="/admin/global-catalog/imports">
            <ArrowLeft aria-hidden="true" className="mr-1.5 size-4" />
            {t("globalCatalog.imports.backToList")}
          </Link>
        </Button>
      </div>

      {detailQuery.isPending ? <DashboardWidgetSkeleton rows={6} /> : null}
      {detailQuery.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.imports.detailError")}
          headingLevel="h2"
          onRetry={() => void detailQuery.refetch()}
        />
      ) : null}

      {job ? (
        <>
          <PageHeader
            title={job.fileName}
            description={t("globalCatalog.imports.detailDescription")}
            actions={
              job.status === "Validated" ? (
                <Button size="sm" onClick={() => setConfirmOpen(true)}>
                  {t("globalCatalog.imports.confirm")}
                </Button>
              ) : null
            }
          />

          <div className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 md:grid-cols-2">
            <DetailField label={t("globalCatalog.column.status")}>
              <StatusIndicator
                tone={globalCatalogImportStatusTone(job.status)}
                label={t(STATUS_LABELS[job.status])}
              />
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.stage")}>
              {job.currentStage ?? "—"}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.format")}>
              {job.fileFormat}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.fileSize")}>
              {formatGlobalCatalogFileSize(job.fileSizeBytes)}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.total")}>{job.totalCount}</DetailField>
            <DetailField label={t("globalCatalog.imports.column.validProducts")}>
              {job.validProductCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.existingCategories")}>
              {job.existingCategoriesReferencedCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.newCategories")}>
              {job.newCategoriesToCreateCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.processed")}>
              {job.processedCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.imported")}>
              {job.importedCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.skipped")}>
              {job.skippedCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.failed")}>
              {job.failedCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.pending")}>
              {job.pendingCount}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.warnings")}>
              {job.warningCount}
            </DetailField>
            <DetailField label={t("globalCatalog.column.created")}>
              {formatGlobalCatalogInstant(job.createdAtUtc, language) ?? "—"}
            </DetailField>
            <DetailField label={t("globalCatalog.imports.column.completed")}>
              {formatGlobalCatalogInstant(job.completedAtUtc, language) ?? "—"}
            </DetailField>
            {job.targetTemplateId ? (
              <>
                <DetailField label={t("globalCatalog.imports.column.targetTemplateId")}>
                  <span className="font-mono text-[length:var(--exits-text-xs)]">
                    {job.targetTemplateId}
                  </span>
                </DetailField>
                {job.targetTemplateName ? (
                  <DetailField label={t("globalCatalog.imports.column.targetTemplateName")}>
                    {job.targetTemplateName}
                  </DetailField>
                ) : null}
              </>
            ) : null}
          </div>

          {job.previewSummary ? (
            <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 text-[length:var(--exits-text-sm)] text-muted">
              {job.previewSummary}
            </p>
          ) : null}

          {job.errorSummary ? (
            <p
              className="rounded-[var(--exits-density-radius)] border border-warning/30 bg-warning/5 p-4 text-[length:var(--exits-text-sm)] text-warning"
              role="alert"
            >
              {job.errorSummary}
            </p>
          ) : null}

          {job.status === "Queued" || job.status === "Processing" ? (
            <p className="text-[length:var(--exits-text-sm)] text-muted">
              {t("globalCatalog.imports.polling")}
            </p>
          ) : null}

          {job.previewItems.length > 0 ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <h2 className="mb-3 text-[length:var(--exits-text-sm)] font-semibold">
                {t("globalCatalog.imports.previewTitle")}
              </h2>
              <AdminTable
                caption={t("globalCatalog.imports.previewCaption")}
                empty={t("globalCatalog.imports.previewEmpty")}
                rows={job.previewItems}
                columns={[
                  {
                    id: "rowNumber",
                    header: "#",
                    cell: (item) => <span className="tabular-nums">{item.rowNumber}</span>,
                  },
                  {
                    id: "name",
                    header: t("globalCatalog.column.name"),
                    cell: (item) => item.name,
                  },
                  {
                    id: "categoryName",
                    header: t("globalCatalog.column.category"),
                    cell: (item) => item.categoryName ?? "—",
                  },
                  {
                    id: "sku",
                    header: t("globalCatalog.column.sku"),
                    cell: (item) => item.sku ?? "—",
                  },
                  {
                    id: "barcode",
                    header: t("globalCatalog.column.barcode"),
                    cell: (item) => item.barcode ?? "—",
                  },
                  {
                    id: "unit",
                    header: t("globalCatalog.column.unit"),
                    cell: (item) => item.unit,
                  },
                  {
                    id: "status",
                    header: t("globalCatalog.column.status"),
                    cell: (item) => item.status,
                  },
                  {
                    id: "errorMessage",
                    header: t("globalCatalog.imports.column.notes"),
                    cell: (item) => (
                      <span className="max-w-xs truncate text-[length:var(--exits-text-sm)] text-muted">
                        {item.errorMessage ?? "—"}
                      </span>
                    ),
                  },
                ]}
              />
            </div>
          ) : null}

          {showErrors ? (
            <div className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
                {t("globalCatalog.imports.errorsTitle")}
              </h2>
              {errorsQuery.isPending ? <DashboardWidgetSkeleton rows={3} /> : null}
              {errorsQuery.isSuccess && errorsQuery.data.items.length === 0 ? (
                <p className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("globalCatalog.imports.errorsEmpty")}
                </p>
              ) : null}
              {errorsQuery.isSuccess && errorsQuery.data.items.length > 0 ? (
                <AdminTable
                  caption={t("globalCatalog.imports.errorsCaption")}
                  empty={t("globalCatalog.imports.errorsEmpty")}
                  rows={errorsQuery.data.items}
                  columns={[
                    {
                      id: "rowNumber",
                      header: "#",
                      cell: (item) => <span className="tabular-nums">{item.rowNumber}</span>,
                    },
                    {
                      id: "name",
                      header: t("globalCatalog.column.name"),
                      cell: (item) => item.name,
                    },
                    {
                      id: "sku",
                      header: t("globalCatalog.column.sku"),
                      cell: (item) => item.sku ?? "—",
                    },
                    {
                      id: "status",
                      header: t("globalCatalog.column.status"),
                      cell: (item) => item.status,
                    },
                    {
                      id: "errorCode",
                      header: t("globalCatalog.imports.column.errorCode"),
                      cell: (item) => item.errorCode ?? "—",
                    },
                    {
                      id: "errorMessage",
                      header: t("globalCatalog.imports.column.errorMessage"),
                      cell: (item) => item.errorMessage ?? "—",
                    },
                  ]}
                />
              ) : null}
              {errorsTotalPages > 1 ? (
                <div className="flex flex-wrap items-center gap-2">
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={errorsPage <= 1}
                    onClick={() => setErrorsPage((page) => page - 1)}
                  >
                    {t("globalCatalog.previous")}
                  </Button>
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("globalCatalog.page")} {errorsPage} / {errorsTotalPages}
                  </span>
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={errorsPage >= errorsTotalPages}
                    onClick={() => setErrorsPage((page) => page + 1)}
                  >
                    {t("globalCatalog.next")}
                  </Button>
                </div>
              ) : null}
            </div>
          ) : null}
        </>
      ) : null}

      <ConfirmActionDialog
        open={confirmOpen}
        title={t("globalCatalog.imports.confirmTitle")}
        description={t("globalCatalog.imports.confirmBody")}
        confirmLabel={t("globalCatalog.imports.confirm")}
        cancelLabel={t("globalCatalog.cancel")}
        pendingLabel={t("globalCatalog.imports.confirming")}
        pending={confirmImport.isPending}
        error={
          confirmError ? (
            <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
              {confirmError}
            </p>
          ) : undefined
        }
        onCancel={() => {
          setConfirmOpen(false);
          setConfirmError(null);
        }}
        onConfirm={() => void onConfirm()}
      />
    </section>
  );
}

function DetailField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid gap-1">
      <span className="text-[length:var(--exits-text-xs)] font-medium text-muted">{label}</span>
      <span className="text-[length:var(--exits-text-sm)] text-foreground">{children}</span>
    </div>
  );
}
