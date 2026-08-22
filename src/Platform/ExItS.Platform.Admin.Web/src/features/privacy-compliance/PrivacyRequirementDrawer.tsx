import * as DialogPrimitive from "@radix-ui/react-dialog";
import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { PlatformApiError } from "@/api/platform-http";
import { privacyRequirementExportPdfPath } from "@/api/privacy-compliance/privacy-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { PrivacyStatusTag } from "@/features/privacy-compliance/PrivacyStatusTag";
import { usePrivacyRequirementDetailQuery } from "@/features/privacy-compliance/use-privacy-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid gap-0.5 border-b border-border py-2 last:border-b-0">
      <dt className="text-[length:var(--exits-text-xs)] text-muted">{label}</dt>
      <dd className="break-words text-[length:var(--exits-text-sm)] text-foreground">{children}</dd>
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
  const query = usePrivacyRequirementDetailQuery(open ? requirementId : null);

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
                {query.data.notes ? (
                  <Field label={t("privacy.column.notes")}>{query.data.notes}</Field>
                ) : null}
              </dl>
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
