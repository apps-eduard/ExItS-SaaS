import { useMemo } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import {
  ORGANIZATION_BILLING_PAGE_SIZE,
  ORGANIZATION_PAYMENT_STATUSES,
  organizationBillingSearchParams,
  parseOrganizationBillingSearchParams,
  type OrganizationBillingUrlState,
  type OrganizationPayment,
  type OrganizationPaymentStatus,
} from "@/api/organizations/billing-list-query";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { useOrganizationPaymentsQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const STATUS_LABELS: Record<string, MessageKey> = {
  PendingConfirmation: "organization.billing.status.PendingConfirmation",
  Confirmed: "organization.billing.status.Confirmed",
  Rejected: "organization.billing.status.Rejected",
  Voided: "organization.billing.status.Voided",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" {
  if (status === "Confirmed") {
    return "success";
  }
  if (status === "PendingConfirmation") {
    return "warning";
  }
  if (status === "Rejected" || status === "Voided") {
    return "danger";
  }
  return "neutral";
}

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
  }).format(date);
}

function formatAmount(item: OrganizationPayment): string {
  return `${item.amount} ${item.currencyCode}`;
}

export function OrganizationBillingPage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseOrganizationBillingSearchParams(searchParams), [searchParams]);
  const query = useOrganizationPaymentsQuery(organizationId, state);

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  function replaceState(patch: Partial<OrganizationBillingUrlState>) {
    const current = parseOrganizationBillingSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(organizationBillingSearchParams({ ...current, ...patch }), {
      replace: true,
    });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_BILLING_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization billing",
      })
    : null;

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.billing.title")}
        description={t("organization.billing.description")}
      />

      <label
        className="grid max-w-sm gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
        htmlFor="org-billing-status"
      >
        {t("organization.billing.status")}
        <select
          id="org-billing-status"
          className={controlClass}
          value={state.status}
          onChange={(event) =>
            replaceState({
              status: event.target.value as OrganizationPaymentStatus | "",
              page: 1,
            })
          }
        >
          <option value="">{t("organization.billing.status.all")}</option>
          {ORGANIZATION_PAYMENT_STATUSES.map((status) => (
            <option key={status} value={status}>
              {t(STATUS_LABELS[status]!)}
            </option>
          ))}
        </select>
      </label>

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.billing.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.billing.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("organization.billing.caption")}
                empty={
                  state.status
                    ? t("organization.billing.zeroResult")
                    : t("organization.billing.empty")
                }
                columns={[
                  {
                    id: "product",
                    header: t("organization.billing.column.product"),
                    cell: (item) => <span className="font-medium">{item.productCode}</span>,
                  },
                  {
                    id: "amount",
                    header: t("organization.billing.column.amount"),
                    cell: (item) => formatAmount(item),
                  },
                  {
                    id: "status",
                    header: t("organization.billing.column.status"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={statusTone(item.status)}
                        label={
                          STATUS_LABELS[item.status] ? t(STATUS_LABELS[item.status]!) : item.status
                        }
                      />
                    ),
                  },
                  {
                    id: "method",
                    header: t("organization.billing.column.method"),
                    cell: (item) => item.method,
                  },
                  {
                    id: "paid",
                    header: t("organization.billing.column.paid"),
                    cell: (item) => formatInstant(item.paidAtUtc, language) || "—",
                  },
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                  {state.status
                    ? t("organization.billing.zeroResult")
                    : t("organization.billing.empty")}
                </li>
              ) : (
                query.data.items.map((item) => (
                  <li
                    key={item.id}
                    className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                  >
                    <p className="font-medium">{item.productCode}</p>
                    <p className="mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                      {formatAmount(item)}
                    </p>
                    <div className="mt-1.5">
                      <StatusIndicator
                        tone={statusTone(item.status)}
                        label={
                          STATUS_LABELS[item.status] ? t(STATUS_LABELS[item.status]!) : item.status
                        }
                      />
                    </div>
                  </li>
                ))
              )}
            </ul>
          )}
          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={state.page <= 1}
              onClick={() => replaceState({ page: state.page - 1 })}
            >
              {t("organizations.previous")}
            </Button>
            <p className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.page")} {state.page} / {totalPages}
            </p>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={state.page >= totalPages}
              onClick={() => replaceState({ page: state.page + 1 })}
            >
              {t("organizations.next")}
            </Button>
          </div>
        </>
      ) : null}
    </section>
  );
}
