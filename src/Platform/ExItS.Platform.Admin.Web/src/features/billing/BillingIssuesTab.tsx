import { useMemo, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  BILLING_ISSUES_PAGE_SIZE,
  billingIssueHref,
  listBillingIssues,
  type BillingIssue,
  type BillingIssueType,
} from "@/api/billing/billing-operations-client";
import { organizationWorkspaceHref } from "@/api/organizations/organization-id";
import { subscriptionDetailHref } from "@/api/subscriptions/subscription-portfolio-query";
import { paymentDetailHref } from "@/api/payments/payment-client";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";
import type { MessageKey } from "@/lib/i18n/messages";

const ISSUE_TYPES: BillingIssueType[] = [
  "pending-payment",
  "rejected-payment",
  "voided-payment",
  "past-due-subscription",
  "grace-period-subscription",
];

const ISSUE_LABELS: Record<BillingIssueType, MessageKey> = {
  "pending-payment": "billing.issues.type.pendingPayment",
  "rejected-payment": "billing.issues.type.rejectedPayment",
  "voided-payment": "billing.issues.type.voidedPayment",
  "past-due-subscription": "billing.issues.type.pastDueSubscription",
  "grace-period-subscription": "billing.issues.type.gracePeriodSubscription",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function parseIssueType(raw: string | null): BillingIssueType | "" {
  if (!raw) return "";
  return ISSUE_TYPES.includes(raw as BillingIssueType) ? (raw as BillingIssueType) : "";
}

function severityTone(severity: BillingIssue["severity"]): "success" | "warning" | "danger" | "neutral" {
  if (severity === "danger") return "danger";
  if (severity === "warning") return "warning";
  return "neutral";
}

export function BillingIssuesTab() {
  const { t, language } = usePreferences();
  const [searchParams, setSearchParams] = useSearchParams();
  const issueType = parseIssueType(searchParams.get("issueType"));
  const page = Math.max(1, Number(searchParams.get("page") ?? "1") || 1);

  const query = useQuery({
    queryKey: ["billing-operations", "issues", issueType, page],
    queryFn: ({ signal }) =>
      listBillingIssues(env.platformApiBaseUrl, {
        issueType,
        page,
        pageSize: BILLING_ISSUES_PAGE_SIZE,
        signal,
      }),
  });

  function replaceFilters(nextIssueType: BillingIssueType | "", nextPage = 1) {
    const params = new URLSearchParams();
    if (nextIssueType) {
      params.set("issueType", nextIssueType);
    }
    if (nextPage > 1) {
      params.set("page", String(nextPage));
    }
    setSearchParams(params, { replace: true });
  }

  function onFilterSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    replaceFilters(parseIssueType(String(data.get("issueType") ?? "")));
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load billing issues" })
    : null;
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / BILLING_ISSUES_PAGE_SIZE))
    : 1;

  const rows = useMemo(
    () =>
      (query.data?.items ?? []).map((issue, index) => ({
        ...issue,
        id: `${issue.issueType}:${issue.paymentId ?? issue.subscriptionId ?? index}`,
      })),
    [query.data?.items],
  );

  return (
    <div className="grid min-w-0 gap-3">
      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-[minmax(12rem,16rem)_auto]"
        onSubmit={onFilterSubmit}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="billing-issue-type"
        >
          {t("billing.issues.filter.type")}
          <select
            id="billing-issue-type"
            name="issueType"
            className={controlClass}
            defaultValue={issueType}
          >
            <option value="">{t("billing.issues.filter.all")}</option>
            {ISSUE_TYPES.map((type) => (
              <option key={type} value={type}>
                {t(ISSUE_LABELS[type])}
              </option>
            ))}
          </select>
        </label>
        <div className="flex items-end gap-2">
          <Button type="submit">{t("billing.issues.filter.apply")}</Button>
          <Button type="button" variant="outline" onClick={() => void query.refetch()}>
            {t("billing.issues.filter.refresh")}
          </Button>
        </div>
      </form>

      {query.isPending ? (
        <div role="status" aria-busy="true" aria-label={t("billing.issues.loading")}>
          <DashboardWidgetSkeleton rows={6} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState diagnostic={diagnostic} headingLevel="h2" onRetry={() => void query.refetch()} />
      ) : null}

      {query.isSuccess ? (
        <div className="min-w-0 rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
          <AdminTable
            caption={t("billing.issues.table.caption")}
            empty={t("billing.issues.table.empty")}
            rows={rows}
            columns={[
              {
                id: "type",
                header: t("billing.issues.column.type"),
                cell: (row) => t(ISSUE_LABELS[row.issueType]),
              },
              {
                id: "summary",
                header: t("billing.issues.column.summary"),
                cell: (row) => row.summary,
              },
              {
                id: "detail",
                header: t("billing.issues.column.detail"),
                cell: (row) => row.detail ?? t("billing.issues.value.unavailable"),
              },
              {
                id: "organization",
                header: t("billing.issues.column.organization"),
                cell: (row) =>
                  row.organizationId ? (
                    <Link
                      className="text-primary hover:underline"
                      to={organizationWorkspaceHref(row.organizationId)}
                    >
                      {row.organizationDisplayName ?? row.organizationId}
                    </Link>
                  ) : (
                    t("billing.issues.value.unavailable")
                  ),
              },
              {
                id: "product",
                header: t("billing.issues.column.product"),
                cell: (row) => row.productDisplayName ?? row.productCode ?? t("billing.issues.value.unavailable"),
              },
              {
                id: "severity",
                header: t("billing.issues.column.severity"),
                cell: (row) => (
                  <StatusIndicator
                    tone={severityTone(row.severity)}
                    label={
                      row.severity === "danger"
                        ? t("billing.issues.severity.danger")
                        : t("billing.issues.severity.warning")
                    }
                  />
                ),
              },
              {
                id: "occurred",
                header: t("billing.issues.column.occurredAt"),
                cell: (row) => {
                  if (!row.occurredAtUtc) {
                    return t("billing.issues.value.unavailable");
                  }
                  const date = new Date(row.occurredAtUtc);
                  return Number.isNaN(date.getTime())
                    ? row.occurredAtUtc
                    : new Intl.DateTimeFormat(language === "fil-PH" ? "fil-PH" : "en-GB", {
                        dateStyle: "medium",
                        timeStyle: "short",
                      }).format(date);
                },
              },
              {
                id: "actions",
                header: t("billing.issues.column.actions"),
                cell: (row) => (
                  <div className="flex flex-wrap gap-2 text-[length:var(--exits-text-sm)]">
                    {row.paymentId ? (
                      <Link className="text-primary hover:underline" to={paymentDetailHref(row.paymentId)}>
                        {t("billing.issues.link.payment")}
                      </Link>
                    ) : null}
                    {row.subscriptionId ? (
                      <Link
                        className="text-primary hover:underline"
                        to={subscriptionDetailHref(row.subscriptionId)}
                      >
                        {t("billing.issues.link.subscription")}
                      </Link>
                    ) : null}
                    {!row.paymentId && !row.subscriptionId ? (
                      <Link className="text-primary hover:underline" to={billingIssueHref(row)}>
                        {t("billing.issues.link.open")}
                      </Link>
                    ) : null}
                  </div>
                ),
              },
            ]}
          />
          {totalPages > 1 ? (
            <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
              <p className="text-[length:var(--exits-text-sm)] text-muted">
                {t("billing.issues.pagination.page")
                  .replace("{page}", String(page))
                  .replace("{totalPages}", String(totalPages))}
              </p>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={page <= 1}
                  onClick={() => replaceFilters(issueType, page - 1)}
                >
                  {t("billing.issues.pagination.previous")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={page >= totalPages}
                  onClick={() => replaceFilters(issueType, page + 1)}
                >
                  {t("billing.issues.pagination.next")}
                </Button>
              </div>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}
