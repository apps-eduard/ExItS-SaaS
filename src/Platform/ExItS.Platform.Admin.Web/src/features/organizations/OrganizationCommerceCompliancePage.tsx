import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  birComplianceDisplayLabel,
  isBirComplianceTransitionAllowed,
  isOnlineSupplierPaymentsTransitionAllowed,
  onlineSupplierPaymentsDisplayLabel,
  type BirComplianceStatus,
  type OnlineSupplierPaymentsStatus,
} from "@/api/organizations/commerce-compliance-client";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  useOnlineSupplierPaymentsQuery,
  useOrganizationComplianceStatusQuery,
  useTransitionBirComplianceMutation,
  useTransitionOnlineSupplierPaymentsMutation,
} from "@/features/organizations/use-commerce-compliance-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

type PaymentsConfirm =
  | { kind: "enable" }
  | { kind: "disable" }
  | { kind: "suspend" }
  | { kind: "restore" };

type BirConfirm = {
  target: BirComplianceStatus;
  titleKey: MessageKey;
  descriptionKey: MessageKey;
  confirmKey: MessageKey;
  destructive?: boolean;
};

function formatInstant(value: string | undefined, language: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function paymentsTone(
  status: OnlineSupplierPaymentsStatus,
): "success" | "neutral" | "warning" | "danger" {
  switch (status) {
    case "Available":
      return "success";
    case "Suspended":
      return "warning";
    default:
      return "neutral";
  }
}

function birTone(status: BirComplianceStatus): "success" | "neutral" | "warning" | "danger" {
  switch (status) {
    case "Approved":
      return "success";
    case "Rejected":
    case "Revoked":
      return "danger";
    case "Suspended":
    case "Requested":
    case "DocumentsRequired":
    case "UnderReview":
      return "warning";
    default:
      return "neutral";
  }
}

function MetaBlock({
  updatedAt,
  updatedBy,
  reason,
  language,
  t,
}: {
  updatedAt?: string;
  updatedBy?: string;
  reason?: string;
  language: string;
  t: (key: MessageKey) => string;
}) {
  const when = formatInstant(updatedAt, language);
  if (!when && !updatedBy && !reason) {
    return (
      <p className="text-[length:var(--exits-text-xs)] text-muted">
        {t("organization.commerceCompliance.meta.none")}
      </p>
    );
  }
  return (
    <dl className="grid gap-1 text-[length:var(--exits-text-xs)] text-muted">
      {when ? (
        <div className="flex flex-wrap gap-x-2">
          <dt>{t("organization.commerceCompliance.meta.changedAt")}</dt>
          <dd className="text-foreground">{when}</dd>
        </div>
      ) : null}
      {updatedBy ? (
        <div className="flex flex-wrap gap-x-2">
          <dt>{t("organization.commerceCompliance.meta.changedBy")}</dt>
          <dd className="break-all font-mono text-foreground">{updatedBy}</dd>
        </div>
      ) : null}
      {reason ? (
        <div className="flex flex-wrap gap-x-2">
          <dt>{t("organization.commerceCompliance.meta.reason")}</dt>
          <dd className="text-foreground">{reason}</dd>
        </div>
      ) : null}
    </dl>
  );
}

export function OrganizationCommerceCompliancePage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageOrganizations);

  const paymentsQuery = useOnlineSupplierPaymentsQuery(organizationId);
  const complianceQuery = useOrganizationComplianceStatusQuery(organizationId);
  const paymentsMutation = useTransitionOnlineSupplierPaymentsMutation(organizationId ?? "");
  const birMutation = useTransitionBirComplianceMutation(organizationId ?? "");

  const [paymentsConfirm, setPaymentsConfirm] = useState<PaymentsConfirm | null>(null);
  const [paymentsReason, setPaymentsReason] = useState("");
  const [birConfirm, setBirConfirm] = useState<BirConfirm | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const payments = paymentsQuery.data;
  const compliance = complianceQuery.data;

  const paymentsActions = useMemo(() => {
    if (!payments) {
      return { enable: false, disable: false, suspend: false, restore: false };
    }
    return {
      enable: isOnlineSupplierPaymentsTransitionAllowed(payments.status, "Available")
        && payments.status === "Disabled",
      disable: isOnlineSupplierPaymentsTransitionAllowed(payments.status, "Disabled"),
      suspend: isOnlineSupplierPaymentsTransitionAllowed(payments.status, "Suspended"),
      restore: payments.status === "Suspended"
        && isOnlineSupplierPaymentsTransitionAllowed(payments.status, "Available"),
    };
  }, [payments]);

  const birActions = useMemo(() => {
    const status = compliance?.complianceEligibilityStatus;
    if (!status) {
      return {
        approve: false,
        reject: false,
        suspend: false,
        revoke: false,
        restoreReview: false,
      };
    }
    return {
      approve: isBirComplianceTransitionAllowed(status, "Approved"),
      reject: isBirComplianceTransitionAllowed(status, "Rejected"),
      suspend: isBirComplianceTransitionAllowed(status, "Suspended"),
      revoke: isBirComplianceTransitionAllowed(status, "Revoked"),
      restoreReview: isBirComplianceTransitionAllowed(status, "UnderReview"),
    };
  }, [compliance]);

  if (
    (paymentsQuery.error instanceof PlatformApiError
      && (paymentsQuery.error.status === 401 || paymentsQuery.error.status === 403))
    || (complianceQuery.error instanceof PlatformApiError
      && (complianceQuery.error.status === 401 || complianceQuery.error.status === 403))
  ) {
    return <ShellNotFoundPage />;
  }

  const paymentsDiagnostic = paymentsQuery.error
    ? normalizeDiagnosticError({
        error: paymentsQuery.error,
        operation: "Load online supplier payments capability",
      })
    : null;
  const complianceDiagnostic = complianceQuery.error
    ? normalizeDiagnosticError({
        error: complianceQuery.error,
        operation: "Load BIR compliance status",
      })
    : null;

  async function runPaymentsTransition(target: OnlineSupplierPaymentsStatus) {
    if (!organizationId) {
      return;
    }
    setActionError(null);
    try {
      await paymentsMutation.mutateAsync({
        status: target,
        reason: paymentsReason.trim() || undefined,
      });
      setPaymentsConfirm(null);
      setPaymentsReason("");
    } catch (error) {
      const message =
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : t("organization.commerceCompliance.actionFailed");
      setActionError(message);
    }
  }

  async function runBirTransition(target: BirComplianceStatus) {
    if (!organizationId) {
      return;
    }
    setActionError(null);
    try {
      await birMutation.mutateAsync(target);
      setBirConfirm(null);
    } catch (error) {
      const message =
        error instanceof PlatformApiError
          ? (error.problem.detail ?? error.message)
          : t("organization.commerceCompliance.actionFailed");
      setActionError(message);
    }
  }

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.commerceCompliance.title")}
        description={t("organization.commerceCompliance.description")}
      />

      {actionError ? (
        <Alert title={t("organization.commerceCompliance.actionFailed")} tone="danger">
          {actionError}
        </Alert>
      ) : null}

      <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <h2 className="text-[length:var(--exits-text-base)] font-semibold">
          {t("organization.commerceCompliance.payments.title")}
        </h2>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.commerceCompliance.payments.description")}
        </p>

        {paymentsQuery.isPending ? (
          <div
            className="mt-3"
            role="status"
            aria-busy="true"
            aria-label={t("organization.commerceCompliance.payments.loading")}
          >
            <DashboardWidgetSkeleton rows={3} />
          </div>
        ) : null}

        {paymentsQuery.isError && paymentsDiagnostic ? (
          <div className="mt-3">
            <ErrorState
              diagnostic={paymentsDiagnostic}
              title={t("organization.commerceCompliance.payments.error")}
              headingLevel="h3"
              onRetry={() => void paymentsQuery.refetch()}
            />
          </div>
        ) : null}

        {payments ? (
          <div className="mt-3 grid gap-3">
            <StatusIndicator
              tone={paymentsTone(payments.status)}
              label={onlineSupplierPaymentsDisplayLabel(payments.status)}
            />
            <MetaBlock
              updatedAt={payments.updatedAtUtc}
              updatedBy={payments.updatedByActorReference}
              reason={payments.reason}
              language={language}
              t={t}
            />
            {canManage ? (
              <div className="flex flex-wrap gap-2">
                {paymentsActions.enable ? (
                  <Button
                    type="button"
                    size="sm"
                    onClick={() => {
                      setActionError(null);
                      setPaymentsConfirm({ kind: "enable" });
                    }}
                  >
                    {t("organization.commerceCompliance.payments.enable")}
                  </Button>
                ) : null}
                {paymentsActions.restore ? (
                  <Button
                    type="button"
                    size="sm"
                    onClick={() => {
                      setActionError(null);
                      setPaymentsConfirm({ kind: "restore" });
                    }}
                  >
                    {t("organization.commerceCompliance.payments.restore")}
                  </Button>
                ) : null}
                {paymentsActions.disable ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() => {
                      setActionError(null);
                      setPaymentsConfirm({ kind: "disable" });
                    }}
                  >
                    {t("organization.commerceCompliance.payments.disable")}
                  </Button>
                ) : null}
                {paymentsActions.suspend ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    onClick={() => {
                      setActionError(null);
                      setPaymentsConfirm({ kind: "suspend" });
                    }}
                  >
                    {t("organization.commerceCompliance.payments.suspend")}
                  </Button>
                ) : null}
              </div>
            ) : (
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("organization.commerceCompliance.readOnly")}
              </p>
            )}
          </div>
        ) : null}
      </div>

      <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
        <h2 className="text-[length:var(--exits-text-base)] font-semibold">
          {t("organization.commerceCompliance.bir.title")}
        </h2>
        <p className="mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.commerceCompliance.bir.description")}
        </p>

        {complianceQuery.isPending ? (
          <div
            className="mt-3"
            role="status"
            aria-busy="true"
            aria-label={t("organization.commerceCompliance.bir.loading")}
          >
            <DashboardWidgetSkeleton rows={3} />
          </div>
        ) : null}

        {complianceQuery.isError && complianceDiagnostic ? (
          <div className="mt-3">
            <ErrorState
              diagnostic={complianceDiagnostic}
              title={t("organization.commerceCompliance.bir.error")}
              headingLevel="h3"
              onRetry={() => void complianceQuery.refetch()}
            />
          </div>
        ) : null}

        {compliance ? (
          <div className="mt-3 grid gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <StatusIndicator
                tone={birTone(compliance.complianceEligibilityStatus)}
                label={birComplianceDisplayLabel(compliance.complianceEligibilityStatus)}
              />
              <span className="font-mono text-[length:var(--exits-text-xs)] text-muted">
                {compliance.complianceEligibilityStatus}
              </span>
            </div>
            <MetaBlock
              updatedAt={compliance.updatedAtUtc}
              updatedBy={compliance.updatedByActorReference}
              language={language}
              t={t}
            />
            {canManage ? (
              <div className="flex flex-wrap gap-2">
                {birActions.approve ? (
                  <Button
                    type="button"
                    size="sm"
                    onClick={() =>
                      setBirConfirm({
                        target: "Approved",
                        titleKey: "organization.commerceCompliance.bir.approve.title",
                        descriptionKey: "organization.commerceCompliance.bir.approve.description",
                        confirmKey: "organization.commerceCompliance.bir.approve.confirm",
                      })
                    }
                  >
                    {t("organization.commerceCompliance.bir.approve")}
                  </Button>
                ) : null}
                {birActions.reject ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    onClick={() =>
                      setBirConfirm({
                        target: "Rejected",
                        titleKey: "organization.commerceCompliance.bir.reject.title",
                        descriptionKey: "organization.commerceCompliance.bir.reject.description",
                        confirmKey: "organization.commerceCompliance.bir.reject.confirm",
                        destructive: true,
                      })
                    }
                  >
                    {t("organization.commerceCompliance.bir.reject")}
                  </Button>
                ) : null}
                {birActions.suspend ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    onClick={() =>
                      setBirConfirm({
                        target: "Suspended",
                        titleKey: "organization.commerceCompliance.bir.suspend.title",
                        descriptionKey: "organization.commerceCompliance.bir.suspend.description",
                        confirmKey: "organization.commerceCompliance.bir.suspend.confirm",
                        destructive: true,
                      })
                    }
                  >
                    {t("organization.commerceCompliance.bir.suspend")}
                  </Button>
                ) : null}
                {birActions.revoke ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="destructive"
                    onClick={() =>
                      setBirConfirm({
                        target: "Revoked",
                        titleKey: "organization.commerceCompliance.bir.revoke.title",
                        descriptionKey: "organization.commerceCompliance.bir.revoke.description",
                        confirmKey: "organization.commerceCompliance.bir.revoke.confirm",
                        destructive: true,
                      })
                    }
                  >
                    {t("organization.commerceCompliance.bir.revoke")}
                  </Button>
                ) : null}
                {birActions.restoreReview ? (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    onClick={() =>
                      setBirConfirm({
                        target: "UnderReview",
                        titleKey: "organization.commerceCompliance.bir.restoreReview.title",
                        descriptionKey:
                          "organization.commerceCompliance.bir.restoreReview.description",
                        confirmKey: "organization.commerceCompliance.bir.restoreReview.confirm",
                      })
                    }
                  >
                    {t("organization.commerceCompliance.bir.restoreReview")}
                  </Button>
                ) : null}
              </div>
            ) : (
              <p className="text-[length:var(--exits-text-xs)] text-muted">
                {t("organization.commerceCompliance.readOnly")}
              </p>
            )}
          </div>
        ) : null}
      </div>

      <ConfirmActionDialog
        open={paymentsConfirm != null}
        title={
          paymentsConfirm?.kind === "enable"
            ? t("organization.commerceCompliance.payments.enable.title")
            : paymentsConfirm?.kind === "restore"
              ? t("organization.commerceCompliance.payments.restore.title")
              : paymentsConfirm?.kind === "disable"
                ? t("organization.commerceCompliance.payments.disable.title")
                : t("organization.commerceCompliance.payments.suspend.title")
        }
        description={
          paymentsConfirm?.kind === "enable"
            ? t("organization.commerceCompliance.payments.enable.description")
            : paymentsConfirm?.kind === "restore"
              ? t("organization.commerceCompliance.payments.restore.description")
              : paymentsConfirm?.kind === "disable"
                ? t("organization.commerceCompliance.payments.disable.description")
                : t("organization.commerceCompliance.payments.suspend.description")
        }
        confirmLabel={
          paymentsConfirm?.kind === "enable"
            ? t("organization.commerceCompliance.payments.enable.confirm")
            : paymentsConfirm?.kind === "restore"
              ? t("organization.commerceCompliance.payments.restore.confirm")
              : paymentsConfirm?.kind === "disable"
                ? t("organization.commerceCompliance.payments.disable.confirm")
                : t("organization.commerceCompliance.payments.suspend.confirm")
        }
        cancelLabel={t("organization.commerceCompliance.cancel")}
        pendingLabel={t("organization.commerceCompliance.pending")}
        destructive={paymentsConfirm?.kind === "suspend" || paymentsConfirm?.kind === "disable"}
        pending={paymentsMutation.isPending}
        onCancel={() => {
          if (!paymentsMutation.isPending) {
            setPaymentsConfirm(null);
            setPaymentsReason("");
          }
        }}
        onConfirm={() => {
          const target: OnlineSupplierPaymentsStatus =
            paymentsConfirm?.kind === "disable"
              ? "Disabled"
              : paymentsConfirm?.kind === "suspend"
                ? "Suspended"
                : "Available";
          void runPaymentsTransition(target);
        }}
      >
        <label className="grid gap-1 text-[length:var(--exits-text-sm)]">
          <span>{t("organization.commerceCompliance.reasonOptional")}</span>
          <textarea
            className="min-h-[4.5rem] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 py-2 text-[length:var(--exits-text-sm)]"
            value={paymentsReason}
            onChange={(event) => setPaymentsReason(event.target.value)}
            disabled={paymentsMutation.isPending}
          />
        </label>
      </ConfirmActionDialog>

      <ConfirmActionDialog
        open={birConfirm != null}
        title={birConfirm ? t(birConfirm.titleKey) : ""}
        description={birConfirm ? t(birConfirm.descriptionKey) : ""}
        confirmLabel={birConfirm ? t(birConfirm.confirmKey) : ""}
        cancelLabel={t("organization.commerceCompliance.cancel")}
        pendingLabel={t("organization.commerceCompliance.pending")}
        destructive={birConfirm?.destructive === true}
        pending={birMutation.isPending}
        onCancel={() => {
          if (!birMutation.isPending) {
            setBirConfirm(null);
          }
        }}
        onConfirm={() => {
          if (birConfirm) {
            void runBirTransition(birConfirm.target);
          }
        }}
      />
    </section>
  );
}
