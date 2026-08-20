import { useMemo, useState, type FormEvent } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import {
  ORGANIZATION_SUBSCRIPTION_PAGE_SIZE,
  ORGANIZATION_SUBSCRIPTION_SORT_BY,
  ORGANIZATION_SUBSCRIPTION_STATUSES,
  hasActiveSubscriptionFilters,
  organizationSubscriptionSearchParams,
  parseOrganizationSubscriptionSearchParams,
  type OrganizationSubscription,
  type OrganizationSubscriptionSortBy,
  type OrganizationSubscriptionUrlState,
} from "@/api/organizations/subscription-list-query";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { useOrganizationSubscriptionsQuery } from "@/features/organizations/use-organization-workspace-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

const SORT_LABELS: Record<OrganizationSubscriptionSortBy, MessageKey> = {
  UpdatedAtUtc: "organization.subscriptions.sort.updatedAtUtc",
  CreatedAtUtc: "organization.subscriptions.sort.createdAtUtc",
  Status: "organization.subscriptions.sort.status",
  ProductCode: "organization.subscriptions.sort.productCode",
  TrialEndUtc: "organization.subscriptions.sort.trialEndUtc",
  PaidPeriodEndUtc: "organization.subscriptions.sort.paidPeriodEndUtc",
  ProductDisplayName: "organization.subscriptions.sort.productDisplayName",
  PlanDisplayName: "organization.subscriptions.sort.planDisplayName",
};

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

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

function productLabel(item: OrganizationSubscription): string {
  return item.productDisplayName || item.productCode;
}

function planLabel(item: OrganizationSubscription): string {
  return item.planDisplayName || item.planKey || item.planId;
}

export function OrganizationSubscriptionsPage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(
    () => parseOrganizationSubscriptionSearchParams(searchParams),
    [searchParams],
  );
  const [searchDraft, setSearchDraft] = useState(state.search);
  const [productDraft, setProductDraft] = useState(state.productCode);
  const [appliedSearch, setAppliedSearch] = useState(state.search);
  const [appliedProduct, setAppliedProduct] = useState(state.productCode);
  if (state.search !== appliedSearch) {
    setAppliedSearch(state.search);
    setSearchDraft(state.search);
  }
  if (state.productCode !== appliedProduct) {
    setAppliedProduct(state.productCode);
    setProductDraft(state.productCode);
  }

  const query = useOrganizationSubscriptionsQuery(organizationId, state);

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  function replaceState(patch: Partial<OrganizationSubscriptionUrlState>) {
    const current = parseOrganizationSubscriptionSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(organizationSubscriptionSearchParams({ ...current, ...patch }), {
      replace: true,
    });
  }

  function onSearchSubmit(event: FormEvent) {
    event.preventDefault();
    replaceState({
      search: searchDraft.trim(),
      productCode: productDraft.trim(),
      page: 1,
    });
  }

  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / ORGANIZATION_SUBSCRIPTION_PAGE_SIZE))
    : 1;
  const diagnostic = query.error
    ? normalizeDiagnosticError({
        error: query.error,
        operation: "Load organization subscriptions",
      })
    : null;

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.subscriptions.title")}
        description={t("organization.subscriptions.description")}
      />

      <form
        className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-3 md:grid-cols-2"
        onSubmit={onSearchSubmit}
      >
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-sub-search"
        >
          {t("organization.subscriptions.search")}
          <Input
            id="org-sub-search"
            value={searchDraft}
            onChange={(event) => setSearchDraft(event.target.value)}
            name="search"
            autoComplete="off"
          />
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-sub-product"
        >
          {t("organization.subscriptions.productCode")}
          <Input
            id="org-sub-product"
            value={productDraft}
            onChange={(event) => setProductDraft(event.target.value)}
            name="productCode"
            autoComplete="off"
          />
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-sub-status"
        >
          {t("organization.subscriptions.status")}
          <select
            id="org-sub-status"
            className={controlClass}
            value={state.status}
            onChange={(event) =>
              replaceState({
                status: event.target.value as OrganizationSubscriptionUrlState["status"],
                page: 1,
              })
            }
          >
            <option value="">{t("organization.subscriptions.status.all")}</option>
            {ORGANIZATION_SUBSCRIPTION_STATUSES.map((status) => (
              <option key={status} value={status}>
                {organizationSubscriptionStatusLabel(status, t)}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-sub-trial"
        >
          {t("organization.subscriptions.trial")}
          <select
            id="org-sub-trial"
            className={controlClass}
            value={state.isTrial}
            onChange={(event) =>
              replaceState({
                isTrial: event.target.value as OrganizationSubscriptionUrlState["isTrial"],
                page: 1,
              })
            }
          >
            <option value="">{t("organization.subscriptions.trial.all")}</option>
            <option value="true">{t("organization.subscriptions.trial.yes")}</option>
            <option value="false">{t("organization.subscriptions.trial.no")}</option>
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-sub-sort"
        >
          {t("organization.subscriptions.sort")}
          <select
            id="org-sub-sort"
            className={controlClass}
            value={state.sortBy}
            onChange={(event) =>
              replaceState({
                sortBy: event.target.value as OrganizationSubscriptionSortBy,
                page: 1,
              })
            }
          >
            {ORGANIZATION_SUBSCRIPTION_SORT_BY.map((sortBy) => (
              <option key={sortBy} value={sortBy}>
                {t(SORT_LABELS[sortBy])}
              </option>
            ))}
          </select>
        </label>
        <label
          className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-sub-order"
        >
          {t("organization.subscriptions.sort.direction")}
          <select
            id="org-sub-order"
            className={controlClass}
            value={state.sortDesc ? "desc" : "asc"}
            onChange={(event) =>
              replaceState({
                sortDesc: event.target.value === "desc",
                page: 1,
              })
            }
          >
            <option value="desc">{t("organizations.sort.desc")}</option>
            <option value="asc">{t("organizations.sort.asc")}</option>
          </select>
        </label>
        <div className="flex flex-wrap gap-2 md:col-span-2">
          <Button type="submit" size="sm">
            {t("organizations.searchSubmit")}
          </Button>
          {hasActiveSubscriptionFilters(state) ? (
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => {
                setSearchDraft("");
                setProductDraft("");
                replaceState({
                  page: 1,
                  search: "",
                  status: "",
                  isTrial: "",
                  productCode: "",
                  sortBy: "UpdatedAtUtc",
                  sortDesc: true,
                });
              }}
            >
              {t("organizations.reset")}
            </Button>
          ) : null}
        </div>
      </form>

      {query.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.subscriptions.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.subscriptions.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}

      {query.data ? (
        <>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("organization.subscriptions.caption")}
                empty={
                  hasActiveSubscriptionFilters(state)
                    ? t("organization.subscriptions.zeroResult")
                    : t("organization.subscriptions.empty")
                }
                columns={[
                  {
                    id: "product",
                    header: t("organization.subscriptions.column.product"),
                    cell: (item) => <span className="font-medium">{productLabel(item)}</span>,
                  },
                  {
                    id: "plan",
                    header: t("organization.subscriptions.column.plan"),
                    cell: (item) => (
                      <span className="break-words text-muted">{planLabel(item)}</span>
                    ),
                  },
                  {
                    id: "status",
                    header: t("organization.subscriptions.column.status"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={organizationSubscriptionStatusTone(item.status)}
                        label={organizationSubscriptionStatusLabel(item.status, t)}
                      />
                    ),
                  },
                  {
                    id: "trial",
                    header: t("organization.subscriptions.column.trial"),
                    cell: (item) => formatInstant(item.trialEndUtc, language) || "—",
                  },
                  {
                    id: "period",
                    header: t("organization.subscriptions.column.period"),
                    cell: (item) =>
                      formatInstant(item.currentPeriodEndUtc || item.paidPeriodEndUtc, language) ||
                      "—",
                  },
                ]}
                rows={query.data.items}
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {query.data.items.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                  {hasActiveSubscriptionFilters(state)
                    ? t("organization.subscriptions.zeroResult")
                    : t("organization.subscriptions.empty")}
                </li>
              ) : (
                query.data.items.map((item) => (
                  <li
                    key={item.id}
                    className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                  >
                    <p className="font-medium">{productLabel(item)}</p>
                    <p className="mt-0.5 break-words text-[length:var(--exits-text-xs)] text-muted">
                      {planLabel(item)}
                    </p>
                    <div className="mt-1.5">
                      <StatusIndicator
                        tone={organizationSubscriptionStatusTone(item.status)}
                        label={organizationSubscriptionStatusLabel(item.status, t)}
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
