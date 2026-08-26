import { useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { organizationWorkspaceHref } from "@/api/organizations/organization-id";
import { subscriptionDetailHref } from "@/api/subscriptions/subscription-portfolio-query";
import { paymentDetailHref } from "@/api/payments/payment-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardSection } from "@/components/exits/dashboard/DashboardSection";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import {
  resolveSupportLookup,
  type SupportLookupMode,
} from "@/features/support/support-lookup";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

const LOOKUP_MODES: SupportLookupMode[] = [
  "organization",
  "userEmail",
  "subscriptionId",
  "paymentId",
  "paymentReference",
  "deviceId",
  "publicOrganizationId",
  "publicUserId",
];

const LOOKUP_LABELS: Record<SupportLookupMode, MessageKey> = {
  organization: "support.lookup.organization",
  userEmail: "support.lookup.userEmail",
  subscriptionId: "support.lookup.subscriptionId",
  paymentId: "support.lookup.paymentId",
  paymentReference: "support.lookup.paymentReference",
  deviceId: "support.lookup.deviceId",
  publicOrganizationId: "support.lookup.publicOrganizationId",
  publicUserId: "support.lookup.publicUserId",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

export function SupportConsolePage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const [mode, setMode] = useState<SupportLookupMode>("organization");
  const [queryDraft, setQueryDraft] = useState("");
  const [paymentMethodDraft, setPaymentMethodDraft] = useState("");
  const [submitted, setSubmitted] = useState<{
    mode: SupportLookupMode;
    query: string;
    paymentMethod?: string;
  } | null>(null);
  const canView =
    authorization.status === "loaded" && authorization.isPlatformAdministrator;

  const lookupQuery = useQuery({
    queryKey: ["support-console", submitted?.mode, submitted?.query, submitted?.paymentMethod],
    enabled: canView && submitted != null,
    queryFn: ({ signal }) =>
      resolveSupportLookup(
        env.platformApiBaseUrl,
        submitted!.mode,
        submitted!.query,
        signal,
        submitted!.paymentMethod,
      ),
  });

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <DashboardWidgetSkeleton rows={4} />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    setSubmitted({
      mode,
      query: queryDraft.trim(),
      paymentMethod: mode === "paymentReference" ? paymentMethodDraft.trim() : undefined,
    });
  }

  const diagnostic = lookupQuery.error
    ? normalizeDiagnosticError({ error: lookupQuery.error, operation: "Support lookup" })
    : null;
  const result = lookupQuery.data?.kind === "success" ? lookupQuery.data.result : null;

  return (
    <section className="grid min-w-0 gap-4">
      <PageHeader title={t("support.title")} description={t("support.description")} />

      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[12rem_minmax(0,1fr)_auto]"
        onSubmit={onSubmit}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="support-lookup-mode"
        >
          {t("support.lookup.mode")}
          <select
            id="support-lookup-mode"
            className={controlClass}
            value={mode}
            onChange={(event) => setMode(event.target.value as SupportLookupMode)}
          >
            {LOOKUP_MODES.map((item) => (
              <option key={item} value={item}>
                {t(LOOKUP_LABELS[item])}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="support-lookup-query"
        >
          {t("support.lookup.query")}
          <Input
            id="support-lookup-query"
            value={queryDraft}
            onChange={(event) => setQueryDraft(event.target.value)}
            placeholder={t("support.lookup.queryPlaceholder")}
            autoComplete="off"
          />
        </label>
        <div className="flex items-end">
          <Button type="submit">{t("support.lookup.search")}</Button>
        </div>
      </form>

      {mode === "paymentReference" ? (
        <label
          className="grid max-w-md gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="support-payment-method"
        >
          {t("support.lookup.paymentMethod")}
          <Input
            id="support-payment-method"
            value={paymentMethodDraft}
            onChange={(event) => setPaymentMethodDraft(event.target.value)}
            placeholder={t("support.lookup.paymentMethodPlaceholder")}
            autoComplete="off"
          />
        </label>
      ) : null}

      {lookupQuery.isFetching ? (
        <div role="status" aria-busy="true" aria-label={t("support.loading")}>
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {lookupQuery.isError && diagnostic ? (
        <ErrorState diagnostic={diagnostic} headingLevel="h2" onRetry={() => void lookupQuery.refetch()} />
      ) : null}

      {lookupQuery.data?.kind === "notFound" ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("support.lookup.notFound")}
        </p>
      ) : null}

      {result ? (
        <div className="grid min-w-0 gap-4">
          {result.organization ? (
            <DashboardSection title={t("support.section.organization")}>
              <dl className="grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
                <dt className="text-muted">{t("support.field.displayName")}</dt>
                <dd className="min-w-0 break-words">{result.organization.displayName}</dd>
                <dt className="text-muted">{t("support.field.organizationId")}</dt>
                <dd className="min-w-0 break-all font-mono">{result.organization.id}</dd>
                <dt className="text-muted">{t("support.field.status")}</dt>
                <dd>{result.organization.status}</dd>
              </dl>
              <p className="mt-3 text-[length:var(--exits-text-sm)]">
                <Link
                  className="text-primary hover:underline"
                  to={organizationWorkspaceHref(result.organization.id)}
                >
                  {t("support.link.organizationWorkspace")}
                </Link>
              </p>
            </DashboardSection>
          ) : null}

          {result.user ? (
            <DashboardSection title={t("support.section.account")}>
              <dl className="grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
                <dt className="text-muted">{t("support.field.displayName")}</dt>
                <dd>{result.user.displayName}</dd>
                <dt className="text-muted">{t("support.field.email")}</dt>
                <dd className="break-all">{result.user.email}</dd>
                <dt className="text-muted">{t("support.field.status")}</dt>
                <dd>{result.user.status}</dd>
              </dl>
              <p className="mt-3 text-[length:var(--exits-text-sm)]">
                <Link className="text-primary hover:underline" to={`/admin/users/${result.user.id}`}>
                  {t("support.link.userDetail")}
                </Link>
              </p>
            </DashboardSection>
          ) : null}

          {result.subscription ? (
            <DashboardSection title={t("support.section.subscription")}>
              <dl className="grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
                <dt className="text-muted">{t("support.field.product")}</dt>
                <dd>{result.subscription.productDisplayName || result.subscription.productCode}</dd>
                <dt className="text-muted">{t("support.field.plan")}</dt>
                <dd>
                  {result.subscription.planDisplayName ||
                    result.subscription.planKey ||
                    result.subscription.planId}
                </dd>
                <dt className="text-muted">{t("support.field.status")}</dt>
                <dd>
                  <StatusIndicator
                    tone={organizationSubscriptionStatusTone(result.subscription.status)}
                    label={organizationSubscriptionStatusLabel(result.subscription.status, t)}
                  />
                </dd>
              </dl>
              <p className="mt-3 text-[length:var(--exits-text-sm)]">
                <Link
                  className="text-primary hover:underline"
                  to={subscriptionDetailHref(result.subscription.id)}
                >
                  {t("support.link.subscriptionDetail")}
                </Link>
              </p>
            </DashboardSection>
          ) : null}

          {result.payment ? (
            <DashboardSection title={t("support.section.payment")}>
              <dl className="grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
                <dt className="text-muted">{t("support.field.product")}</dt>
                <dd>{result.payment.productCode}</dd>
                <dt className="text-muted">{t("support.field.status")}</dt>
                <dd>{result.payment.status}</dd>
                <dt className="text-muted">{t("support.field.reference")}</dt>
                <dd className="break-all">
                  {result.payment.externalReference ?? t("support.value.unavailable")}
                </dd>
              </dl>
              <p className="mt-3 text-[length:var(--exits-text-sm)]">
                <Link className="text-primary hover:underline" to={paymentDetailHref(result.payment.id)}>
                  {t("support.link.paymentDetail")}
                </Link>
              </p>
            </DashboardSection>
          ) : null}

          {result.device ? (
            <DashboardSection title={t("support.section.device")}>
              <dl className="grid grid-cols-[8rem_minmax(0,1fr)] gap-x-3 gap-y-2 text-[length:var(--exits-text-sm)]">
                <dt className="text-muted">{t("support.field.deviceName")}</dt>
                <dd>{result.device.friendlyName}</dd>
                <dt className="text-muted">{t("support.field.deviceId")}</dt>
                <dd className="break-all font-mono">{result.device.id}</dd>
                <dt className="text-muted">{t("support.field.installationDeviceId")}</dt>
                <dd className="break-all font-mono">{result.device.installationDeviceId}</dd>
                <dt className="text-muted">{t("support.field.status")}</dt>
                <dd>{result.device.status}</dd>
              </dl>
            </DashboardSection>
          ) : null}

          {result.devices.length > 0 ? (
            <DashboardSection title={t("support.section.devices")}>
              <AdminTable
                caption={t("support.table.devices")}
                empty={t("support.table.empty")}
                rows={result.devices.map((item) => ({ ...item, id: item.id }))}
                columns={[
                  {
                    id: "name",
                    header: t("support.field.deviceName"),
                    cell: (row) => row.friendlyName,
                  },
                  {
                    id: "installation",
                    header: t("support.field.installationDeviceId"),
                    cell: (row) => (
                      <span className="break-all font-mono text-[length:var(--exits-text-xs)]">
                        {row.installationDeviceId}
                      </span>
                    ),
                  },
                  {
                    id: "status",
                    header: t("support.field.status"),
                    cell: (row) => row.status,
                  },
                ]}
              />
            </DashboardSection>
          ) : null}

          {result.commercialSummary ? (
            <>
              <DashboardSection title={t("support.section.subscriptions")}>
                <AdminTable
                  caption={t("support.table.subscriptions")}
                  empty={t("support.table.empty")}
                  rows={result.commercialSummary.subscriptions.map((item) => ({ ...item, id: item.id }))}
                  columns={[
                    {
                      id: "product",
                      header: t("support.field.product"),
                      cell: (row) => row.productCode,
                    },
                    {
                      id: "status",
                      header: t("support.field.status"),
                      cell: (row) => (
                        <StatusIndicator
                          tone={organizationSubscriptionStatusTone(row.status)}
                          label={organizationSubscriptionStatusLabel(row.status, t)}
                        />
                      ),
                    },
                  ]}
                />
              </DashboardSection>

              <DashboardSection title={t("support.section.entitlements")}>
                <AdminTable
                  caption={t("support.table.entitlements")}
                  empty={t("support.table.empty")}
                  rows={result.commercialSummary.latestEntitlements.map((item) => ({
                    ...item,
                    id: item.id,
                  }))}
                  columns={[
                    {
                      id: "product",
                      header: t("support.field.product"),
                      cell: (row) => row.productDisplayName || row.productCode,
                    },
                    {
                      id: "status",
                      header: t("support.field.status"),
                      cell: (row) => row.subscriptionStatus,
                    },
                    {
                      id: "version",
                      header: t("support.field.snapshotVersion"),
                      cell: (row) =>
                        row.snapshotVersion != null ? String(row.snapshotVersion) : t("support.value.unavailable"),
                    },
                  ]}
                />
              </DashboardSection>
            </>
          ) : null}

          {result.auditRecords.length > 0 ? (
            <DashboardSection title={t("support.section.audit")}>
              <AdminTable
                caption={t("support.table.audit")}
                empty={t("support.table.empty")}
                rows={result.auditRecords}
                columns={[
                  {
                    id: "occurred",
                    header: t("support.field.occurredAt"),
                    cell: (row) => row.occurredAtUtc,
                  },
                  {
                    id: "action",
                    header: t("support.field.action"),
                    cell: (row) => row.actionCode,
                  },
                  {
                    id: "outcome",
                    header: t("support.field.outcome"),
                    cell: (row) => row.outcome,
                  },
                  {
                    id: "summary",
                    header: t("support.field.summary"),
                    cell: (row) => row.summary ?? t("support.value.unavailable"),
                  },
                ]}
              />
            </DashboardSection>
          ) : null}
        </div>
      ) : null}
    </section>
  );
}
