import { useQueryClient } from "@tanstack/react-query";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { PlatformApiError } from "@/api/platform-http";
import {
  privacyRequirementExportPdfPath,
  updatePrivacyComplianceRequirementDetails,
  updatePrivacyComplianceRequirementStatus,
} from "@/api/privacy-compliance/privacy-client";
import type { ComplianceRequirementDto } from "@/api/privacy-compliance/privacy-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { PrivacyStatusTag } from "@/features/privacy-compliance/PrivacyStatusTag";
import {
  privacyOverviewQueryKey,
  privacyRequirementDetailQueryKey,
  privacyRequirementsQueryKey,
  usePrivacyRequirementDetailQuery,
} from "@/features/privacy-compliance/use-privacy-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";
import { PRIVACY_REQUIREMENT_STATUS_OPTIONS } from "@/features/privacy-compliance/privacy-status-options";

const STATUS_LABEL_KEYS: Record<(typeof PRIVACY_REQUIREMENT_STATUS_OPTIONS)[number], MessageKey> = {
  NotStarted: "privacy.status.NotStarted",
  InProgress: "privacy.status.InProgress",
  ReadyForReview: "privacy.status.ReadyForReview",
  Approved: "privacy.status.Approved",
  NeedsUpdate: "privacy.status.NeedsUpdate",
};

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid gap-0.5 border-b border-border py-2 last:border-b-0">
      <dt className="text-[length:var(--exits-text-xs)] text-muted">{label}</dt>
      <dd className="break-words text-[length:var(--exits-text-sm)] text-foreground">{children}</dd>
    </div>
  );
}

function applyAuthoritativeRequirement(
  queryClient: ReturnType<typeof useQueryClient>,
  updated: ComplianceRequirementDto,
) {
  queryClient.setQueryData(privacyRequirementDetailQueryKey(updated.id), updated);
  queryClient.setQueryData(
    privacyRequirementsQueryKey,
    (current: ComplianceRequirementDto[] | undefined) => {
      if (!current) {
        return current;
      }
      return current.map((row) => (row.id === updated.id ? updated : row));
    },
  );
  void queryClient.invalidateQueries({ queryKey: privacyOverviewQueryKey });
}

function PrivacyRequirementManageForm({
  requirement,
  onAuthoritativeUpdate,
}: {
  requirement: ComplianceRequirementDto;
  onAuthoritativeUpdate: (updated: ComplianceRequirementDto) => void;
}) {
  const { t } = usePreferences();
  const [editStatus, setEditStatus] = useState(requirement.status);
  const [editNotes, setEditNotes] = useState(requirement.notes ?? "");
  const [saving, setSaving] = useState(false);
  const [feedback, setFeedback] = useState<{
    tone: "success" | "danger";
    title: string;
    detail?: string;
  } | null>(null);

  async function handleSave() {
    if (saving || editStatus.trim().length === 0) {
      return;
    }

    const statusChanged =
      editStatus.localeCompare(requirement.status, undefined, { sensitivity: "accent" }) !== 0;
    const notesChanged = (editNotes ?? "") !== (requirement.notes ?? "");

    if (!statusChanged && !notesChanged) {
      setFeedback({
        tone: "success",
        title: t("privacy.requirement.saveSucceeded"),
      });
      return;
    }

    setSaving(true);
    setFeedback(null);
    try {
      let updated: ComplianceRequirementDto = requirement;

      if (statusChanged) {
        updated = await updatePrivacyComplianceRequirementStatus(
          env.platformApiBaseUrl,
          requirement.id,
          editStatus,
        );
      }

      if (notesChanged) {
        updated = await updatePrivacyComplianceRequirementDetails(
          env.platformApiBaseUrl,
          requirement.id,
          { notes: editNotes },
        );
      }

      onAuthoritativeUpdate(updated);
      setEditStatus(updated.status);
      setEditNotes(updated.notes ?? "");
      setFeedback({
        tone: "success",
        title: t("privacy.requirement.saveSucceeded"),
      });
    } catch (error) {
      const detail =
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : error instanceof Error
            ? error.message
            : t("privacy.requirement.saveFailed");
      setFeedback({
        tone: "danger",
        title: t("privacy.requirement.saveFailed"),
        detail,
      });
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="grid gap-3" data-testid="privacy-requirement-manage">
      <label
        className="grid gap-1 text-[length:var(--exits-text-sm)]"
        htmlFor="privacy-requirement-status"
      >
        {t("privacy.requirement.updateStatus")}
        <select
          id="privacy-requirement-status"
          data-testid="privacy-requirement-status"
          className="h-9 rounded-[var(--exits-density-radius)] border border-border bg-surface px-2"
          value={editStatus}
          disabled={saving}
          onChange={(event) => setEditStatus(event.target.value)}
        >
          {PRIVACY_REQUIREMENT_STATUS_OPTIONS.map((status) => (
            <option key={status} value={status}>
              {t(STATUS_LABEL_KEYS[status])}
            </option>
          ))}
        </select>
      </label>

      <label
        className="grid gap-1 text-[length:var(--exits-text-sm)]"
        htmlFor="privacy-requirement-notes"
      >
        {t("privacy.column.notes")}
        <textarea
          id="privacy-requirement-notes"
          data-testid="privacy-requirement-notes"
          className="min-h-24 rounded-[var(--exits-density-radius)] border border-border bg-surface px-2 py-1.5"
          value={editNotes}
          disabled={saving}
          onChange={(event) => setEditNotes(event.target.value)}
        />
      </label>

      {feedback ? (
        <Alert
          title={feedback.title}
          tone={feedback.tone}
          data-testid={
            feedback.tone === "success"
              ? "privacy-requirement-save-success"
              : "privacy-requirement-save-error"
          }
        >
          {feedback.detail}
        </Alert>
      ) : null}

      <Button
        type="button"
        size="sm"
        className="justify-self-start"
        disabled={saving}
        data-testid="privacy-requirement-save"
        onClick={() => void handleSave()}
      >
        {saving ? t("privacy.requirement.saving") : t("privacy.requirement.save")}
      </Button>
    </div>
  );
}

export function PrivacyRequirementDrawer({
  requirementId,
  open,
  onOpenChange,
}: {
  requirementId: string | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { t, language, theme, density } = usePreferences();
  const authorization = useAuthorization();
  const queryClient = useQueryClient();
  const query = usePrivacyRequirementDetailQuery(open ? requirementId : null);

  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.managePrivacyCompliance);

  const title =
    query.data?.title ??
    (query.isPending ? t("privacy.requirement.loading") : t("privacy.requirement.detail"));

  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <DialogPrimitive.Portal>
        <DialogPrimitive.Overlay className="fixed inset-0 z-[var(--exits-z-overlay)] bg-[var(--exits-overlay)]" />
        <DialogPrimitive.Content
          className="fixed inset-y-0 right-0 z-[var(--exits-z-drawer)] flex w-full max-w-md flex-col border-l border-border bg-surface p-[var(--exits-density-card-padding)] shadow-lg"
          data-testid="privacy-requirement-drawer"
        >
          <DialogPrimitive.Title className="text-[length:var(--exits-text-lg)] font-bold">
            {title}
          </DialogPrimitive.Title>
          <DialogPrimitive.Description className="sr-only">
            {t("privacy.requirement.detail")}
          </DialogPrimitive.Description>

          <div className="mt-4 min-h-0 flex-1 overflow-y-auto">
            {query.isPending ? <DashboardWidgetSkeleton /> : null}

            {query.isError ? (
              query.error instanceof PlatformApiError && query.error.status === 404 ? (
                <p className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("privacy.requirement.notFound")}
                </p>
              ) : query.error instanceof PlatformApiError &&
                (query.error.status === 401 || query.error.status === 403) ? (
                <Alert
                  title={t("privacy.requirement.forbidden")}
                  tone="danger"
                  data-testid="privacy-requirement-forbidden"
                />
              ) : (
                <ErrorState
                  diagnostic={normalizeDiagnosticError({
                    error: query.error,
                    operation: "Load privacy compliance requirement",
                    environment: { locale: language, theme, density },
                  })}
                  description={t("privacy.requirement.error")}
                  onRetry={() => void query.refetch()}
                />
              )
            ) : null}

            {query.data ? (
              <div className="grid gap-4">
                <dl>
                  <Field label={t("privacy.column.code")}>{query.data.code}</Field>
                  <Field label={t("privacy.column.category")}>{query.data.category}</Field>
                  <Field label={t("privacy.column.level")}>
                    <PrivacyStatusTag value={query.data.requirementLevel} />
                  </Field>
                  <Field label={t("privacy.column.status")}>
                    <PrivacyStatusTag value={query.data.status} />
                  </Field>
                  <Field label={t("privacy.column.owner")}>{query.data.ownerRole}</Field>
                  <Field label={t("privacy.column.evidence")}>{query.data.evidenceCount}</Field>
                  <Field label={t("privacy.column.version")}>{query.data.version}</Field>
                  <Field label={t("privacy.column.description")}>{query.data.description}</Field>
                </dl>

                {canManage ? (
                  <PrivacyRequirementManageForm
                    key={query.data.id}
                    requirement={query.data}
                    onAuthoritativeUpdate={(updated) => {
                      applyAuthoritativeRequirement(queryClient, updated);
                    }}
                  />
                ) : (
                  <Field label={t("privacy.column.notes")}>
                    {query.data.notes?.trim() ? query.data.notes : "—"}
                  </Field>
                )}
              </div>
            ) : null}
          </div>

          <div className="mt-4 flex flex-wrap gap-2 border-t border-border pt-3">
            {query.data ? (
              <>
                <Button asChild variant="outline" size="sm">
                  <a
                    href={`${env.platformApiBaseUrl}${privacyRequirementExportPdfPath(query.data.id)}`}
                    target="_blank"
                    rel="noreferrer"
                    data-testid="privacy-requirement-pdf"
                  >
                    {t("privacy.requirement.exportPdf")}
                  </a>
                </Button>
                <Button asChild variant="outline" size="sm">
                  <Link to={`/admin/privacy-compliance/evidence?requirementId=${query.data.id}`}>
                    {t("privacy.requirement.viewEvidence")}
                  </Link>
                </Button>
              </>
            ) : null}
            <Button type="button" variant="secondary" size="sm" onClick={() => onOpenChange(false)}>
              {t("privacy.requirement.close")}
            </Button>
          </div>
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
}
