import { useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  hasOrganizationManagementAuthority,
  isOrganizationOwnerMembership,
} from "@/access/pos-capabilities";
import {
  canRequestComplianceReview,
  getOrganizationComplianceStatus,
  requestOrganizationComplianceReview,
  type OrganizationComplianceStatus,
} from "@/api/platform/organization-compliance-client";
import {
  acknowledgeSalesDocumentEducation,
  getSalesDocumentEducationStatus,
  type SalesDocumentEducationStatus,
} from "@/api/platform/organization-sales-document-education-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ExperienceAccessDeniedPage } from "@/features/role/ExperienceAccessDeniedPage";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function orgSalesDocumentsQueryKey(organizationId: string) {
  return ["org", "sales-documents", organizationId] as const;
}

function problemText(error: unknown, fallback: string): string {
  if (error instanceof PlatformApiError) {
    return error.problem.detail ?? error.problem.title ?? fallback;
  }
  return fallback;
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 403;
}

export function OrgSalesDocumentsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canView = hasOrganizationManagementAuthority(sessionGrant);
  const canEdit = isOrganizationOwnerMembership(sessionGrant);
  const [understood, setUnderstood] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const query = useQuery({
    queryKey: organizationId
      ? orgSalesDocumentsQueryKey(organizationId)
      : ["org", "sales-documents", "none"],
    enabled: Boolean(organizationId) && canView,
    queryFn: async ({ signal }) => {
      const id = organizationId!;
      const education = await getSalesDocumentEducationStatus(id, signal);
      let compliance: OrganizationComplianceStatus | null = null;
      try {
        compliance = await getOrganizationComplianceStatus(id, signal);
      } catch {
        compliance = null;
      }
      return { education, compliance };
    },
  });

  const acknowledge = useMutation({
    mutationFn: () => acknowledgeSalesDocumentEducation(organizationId!),
    onSuccess: (education) => {
      setActionError(null);
      setActionSuccess(t("orgSales.acknowledged"));
      setUnderstood(false);
      queryClient.setQueryData(
        orgSalesDocumentsQueryKey(organizationId!),
        (current: { education: SalesDocumentEducationStatus; compliance: OrganizationComplianceStatus | null } | undefined) =>
          current ? { ...current, education } : { education, compliance: null },
      );
    },
    onError: (error) => setActionError(problemText(error, t("orgSales.loadErrorDetail"))),
  });

  const requestReview = useMutation({
    mutationFn: () => requestOrganizationComplianceReview(organizationId!),
    onSuccess: (compliance) => {
      setActionError(null);
      setActionSuccess(t("orgSales.requested"));
      queryClient.setQueryData(
        orgSalesDocumentsQueryKey(organizationId!),
        (current: { education: SalesDocumentEducationStatus; compliance: OrganizationComplianceStatus | null } | undefined) =>
          current ? { ...current, compliance } : current,
      );
    },
    onError: (error) => setActionError(problemText(error, t("orgSales.loadErrorDetail"))),
  });

  if (!organizationId) {
    return (
      <div data-testid="sales-no-organization">
        <PageHeader title={t("orgSales.title")} description={t("orgSales.subtitle")} />
        <EmptyState title={t("orgSales.noOrganization")} />
      </div>
    );
  }

  if (!canView || isForbidden(query.error)) {
    return <ExperienceAccessDeniedPage testId="sales-documents-denied" />;
  }

  const education = query.data?.education;
  const compliance = query.data?.compliance ?? null;
  const busy = acknowledge.isPending || requestReview.isPending;
  const showRequest =
    canEdit && (compliance == null || canRequestComplianceReview(compliance.complianceEligibilityStatus));

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="sales-documents-page">
      <PageHeader title={t("orgSales.title")} description={t("orgSales.subtitle")} />
      {query.isLoading ? (
        <div data-testid="sales-loading">
          <LoadingSkeleton />
        </div>
      ) : null}
      {query.isError && !isForbidden(query.error) ? (
        <div data-testid="sales-load-error">
          <ErrorState
            title={t("orgSales.loadError")}
            detail={problemText(query.error, t("orgSales.loadErrorDetail"))}
            error={query.error}
            operation="load sales-document education"
          />
          <Button type="button" data-testid="sales-retry" onClick={() => void query.refetch()}>
            {t("orgSales.retry")}
          </Button>
        </div>
      ) : null}
      {actionError ? (
        <p role="alert" data-testid="sales-action-error">
          {actionError}
        </p>
      ) : null}
      {actionSuccess ? <p data-testid="sales-action-success">{actionSuccess}</p> : null}
      {education ? (
        <>
          <Card>
            <CardHeader>
              <CardTitle>{t("orgSales.title")}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <StatusChip data-testid="sales-education-status">
                {education.requiresOwnerAction ? t("orgSales.actionNeeded") : t("orgSales.acknowledged")}
              </StatusChip>
              {education.requiresOwnerAction ? (
                <p data-testid="sales-owner-required">{t("orgSales.ownerRequired")}</p>
              ) : null}
              <p data-testid="sales-document-mode">{t("orgSales.currentDocument")}</p>
              <p data-testid="sales-disclaimer">{t("orgSales.disclaimer")}</p>
              <p data-testid="sales-invoicing">{t("orgSales.invoicingNotEnabled")}</p>
              <p>{t("orgSales.body1")}</p>
              <p>{t("orgSales.body2")}</p>
              <p>{t("orgSales.body3")}</p>
              <p>{t("orgSales.body4")}</p>
              <p data-testid="sales-version">
                {t("orgSales.version")}: {education.currentVersion}
              </p>
              {education.acknowledgedAtUtc ? (
                <p data-testid="sales-acknowledged-at">
                  {t("orgSales.acknowledged")} · {new Date(education.acknowledgedAtUtc).toLocaleString()}
                </p>
              ) : null}
              {canEdit && education.requiresOwnerAction ? (
                <form
                  className="flex flex-col gap-3"
                  onSubmit={(event) => {
                    event.preventDefault();
                    if (!understood) return;
                    acknowledge.mutate();
                  }}
                >
                  <label className="flex items-start gap-2">
                    <input
                      type="checkbox"
                      data-testid="sales-ack-checkbox"
                      checked={understood}
                      disabled={busy}
                      onChange={(event) => setUnderstood(event.target.checked)}
                    />
                    <span>{t("orgSales.checkbox")}</span>
                  </label>
                  <Button type="submit" disabled={!understood || busy} data-testid="sales-ack-submit">
                    {t("orgSales.continue")}
                  </Button>
                </form>
              ) : null}
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <CardTitle>{t("orgSales.complianceTitle")}</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-3">
              <p data-testid="sales-compliance-status">
                {t("orgSales.complianceReview")}:{" "}
                {compliance?.complianceEligibilityStatus ?? "NotRequested"}
              </p>
              <p data-testid="sales-issuance">
                {t("orgSales.issuance")}:{" "}
                {compliance?.taxDocumentIssuanceEnabled || education.taxDocumentIssuanceEnabled
                  ? t("orgSales.issuanceEnabledUnavailable")
                  : t("orgSales.issuanceDisabled")}
              </p>
              <p data-testid="sales-implementation">
                {t("orgSales.implementation")}:{" "}
                {compliance?.taxDocumentImplementationAvailable
                  ? t("orgSales.implementationAvailable")
                  : t("orgSales.implementationUnavailable")}
              </p>
              <p data-testid="sales-tax-configuration">
                {t("orgSales.taxConfiguration")}:{" "}
                {compliance?.taxConfigurationEnabled
                  ? t("orgSales.taxConfigurationOn")
                  : t("orgSales.taxConfigurationOff")}
              </p>
              <p>{t("orgSales.requestHint")}</p>
              {showRequest ? (
                <Button
                  type="button"
                  disabled={busy}
                  data-testid="sales-request-review"
                  onClick={() => requestReview.mutate()}
                >
                  {t("orgSales.requestReview")}
                </Button>
              ) : compliance && !canRequestComplianceReview(compliance.complianceEligibilityStatus) ? (
                <p data-testid="sales-request-recorded">{t("orgSales.requestRecorded")}</p>
              ) : null}
              <Link to="/org/tax-compliance" data-testid="sales-tax-link">
                {t("orgSales.taxLink")}
              </Link>
            </CardContent>
          </Card>
        </>
      ) : null}
    </div>
  );
}
