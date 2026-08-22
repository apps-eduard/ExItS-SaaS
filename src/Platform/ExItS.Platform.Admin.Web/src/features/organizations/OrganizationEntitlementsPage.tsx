import { useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import {
  ORGANIZATION_ENTITLEMENT_PAGE_SIZE,
  organizationEntitlementSearchParams,
  parseOrganizationEntitlementSearchParams,
  sanitizeEntitlementProduct,
  uniqueEntitlementProductOptions,
  type EntitlementGrant,
  type EntitlementProductOption,
  type OrganizationEntitlementUrlState,
} from "@/api/organizations/entitlement-list-query";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import {
  useOrganizationCommercialSummaryQuery,
  useOrganizationDetailQuery,
  useOrganizationEntitlementSnapshotsQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import { OrganizationEntitlementOperator } from "@/features/organizations/OrganizationEntitlementOperator";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useMediaQuery } from "@/hooks/use-media-query";
import { usePreferences } from "@/hooks/use-preferences";
import {
  organizationSubscriptionStatusLabel,
  organizationSubscriptionStatusTone,
} from "@/features/organizations/organization-subscription-status";
import { grantSourceLabel } from "@/features/organizations/entitlement-operator-utils";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import type { MessageKey } from "@/lib/i18n/messages";

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
    timeStyle: "short",
  }).format(date);
}

function productLabel(option: EntitlementProductOption): string {
  return option.productDisplayName || option.productCode;
}

function grantCounts(grants: EntitlementGrant[]): { enabled: number; disabled: number } {
  let enabled = 0;
  let disabled = 0;
  for (const grant of grants) {
    if (grant.enabled) {
      enabled += 1;
    } else {
      disabled += 1;
    }
  }
  return { enabled, disabled };
}

function grantSummaryLabel(grants: EntitlementGrant[], t: (key: MessageKey) => string): string {
  const { enabled, disabled } = grantCounts(grants);
  return t("organization.entitlements.grant.summary")
    .replace("{enabled}", String(enabled))
    .replace("{disabled}", String(disabled));
}

function EntitlementGrantDetails({
  grants,
  t,
}: {
  grants: EntitlementGrant[];
  t: (key: MessageKey) => string;
}) {
  return (
    <ul className="grid gap-1.5">
      {grants.map((grant) => (
        <li
          key={grant.featureCode}
          className="flex flex-wrap items-baseline gap-x-2 gap-y-0.5 text-[length:var(--exits-text-xs)]"
        >
          <span className="break-all font-mono text-foreground">{grant.featureCode}</span>
          <span className={grant.enabled ? "text-muted" : "font-medium text-foreground"}>
            {grant.enabled
              ? t("organization.entitlements.grant.enabled")
              : t("organization.entitlements.grant.disabled")}
          </span>
          {grant.numericLimit != null ? (
            <span className="text-muted">
              {t("organization.entitlements.grant.limit").replace(
                "{value}",
                String(grant.numericLimit),
              )}
            </span>
          ) : null}
          {grant.source ? (
            <span className="text-muted">{grantSourceLabel(grant.source, t)}</span>
          ) : null}
        </li>
      ))}
    </ul>
  );
}

function EntitlementGrantDisclosure({
  grants,
  expanded,
  onToggle,
  panelId,
  t,
}: {
  grants: EntitlementGrant[];
  expanded: boolean;
  onToggle: () => void;
  panelId: string;
  t: (key: MessageKey) => string;
}) {
  if (grants.length === 0) {
    return (
      <span className="text-[length:var(--exits-text-xs)] text-muted">
        {t("organization.entitlements.grant.noGrants")}
      </span>
    );
  }

  return (
    <div className="grid gap-1">
      <p className="text-[length:var(--exits-text-xs)] text-muted">
        {grantSummaryLabel(grants, t)}
      </p>
      <Button
        type="button"
        size="sm"
        variant="ghost"
        className="h-auto min-h-8 w-fit justify-start px-0 font-medium text-primary hover:bg-transparent hover:underline"
        aria-expanded={expanded}
        aria-controls={panelId}
        onClick={onToggle}
      >
        {expanded
          ? t("organization.entitlements.grant.hide")
          : t("organization.entitlements.grant.show")}
      </Button>
    </div>
  );
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function OrganizationEntitlementsPage() {
  const { t, language } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const showTable = useMediaQuery("(min-width: 768px)");
  const [searchParams, setSearchParams] = useSearchParams();
  const [expandedIds, setExpandedIds] = useState<Set<string>>(() => new Set());
  const state = useMemo(
    () => parseOrganizationEntitlementSearchParams(searchParams),
    [searchParams],
  );
  const commercialQuery = useOrganizationCommercialSummaryQuery(organizationId);
  const organizationQuery = useOrganizationDetailQuery(organizationId);
  const products = useMemo(
    () => uniqueEntitlementProductOptions(commercialQuery.data?.latestEntitlements ?? []),
    [commercialQuery.data],
  );
  const sanitizedProduct = sanitizeEntitlementProduct(state.product, products);
  const expansionScope = `${sanitizedProduct ?? ""}:${state.page}`;
  const [activeExpansionScope, setActiveExpansionScope] = useState(expansionScope);
  if (activeExpansionScope !== expansionScope) {
    setActiveExpansionScope(expansionScope);
    setExpandedIds(new Set());
  }
  const snapshotsQuery = useOrganizationEntitlementSnapshotsQuery(
    organizationId,
    sanitizedProduct,
    state.page,
  );

  useEffect(() => {
    if (state.product || products.length === 0) {
      return;
    }
    setSearchParams(
      organizationEntitlementSearchParams({ product: products[0]!.productCode, page: 1 }),
      { replace: true },
    );
  }, [products, setSearchParams, state.product]);

  if (isForbidden(commercialQuery.error)) {
    return <ShellNotFoundPage />;
  }

  function replaceState(patch: Partial<OrganizationEntitlementUrlState>) {
    const current = parseOrganizationEntitlementSearchParams(
      new URLSearchParams(window.location.search),
    );
    setSearchParams(organizationEntitlementSearchParams({ ...current, ...patch }), {
      replace: true,
    });
  }

  function toggleExpanded(id: string) {
    setExpandedIds((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }

  const totalPages = snapshotsQuery.data
    ? Math.max(1, Math.ceil(snapshotsQuery.data.totalCount / ORGANIZATION_ENTITLEMENT_PAGE_SIZE))
    : 1;
  const commercialDiagnostic = commercialQuery.error
    ? normalizeDiagnosticError({
        error: commercialQuery.error,
        operation: "Load organization product access",
      })
    : null;
  const snapshotsDiagnostic = snapshotsQuery.error
    ? normalizeDiagnosticError({
        error: snapshotsQuery.error,
        operation: "Load organization entitlement snapshots",
      })
    : null;
  const invalidProduct = Boolean(state.product) && sanitizedProduct == null && products.length > 0;

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.entitlements.title")}
        description={t("organization.entitlements.description")}
      />

      {commercialQuery.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.entitlements.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {commercialQuery.isError && commercialDiagnostic ? (
        <ErrorState
          diagnostic={commercialDiagnostic}
          title={t("organization.entitlements.error")}
          headingLevel="h2"
          onRetry={() => void commercialQuery.refetch()}
        />
      ) : null}

      {commercialQuery.data && products.length === 0 ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.entitlements.emptyProducts")}
        </p>
      ) : null}

      {commercialQuery.data && products.length > 0 ? (
        <label
          className="grid max-w-sm gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted"
          htmlFor="org-entitlement-product"
        >
          {t("organization.entitlements.product")}
          <select
            id="org-entitlement-product"
            className={controlClass}
            value={sanitizedProduct ?? ""}
            onChange={(event) =>
              replaceState({
                product: event.target.value,
                page: 1,
              })
            }
          >
            {invalidProduct ? (
              <option value="">{t("organization.entitlements.product.choose")}</option>
            ) : null}
            {products.map((option) => (
              <option key={option.productCode} value={option.productCode}>
                {productLabel(option)}
              </option>
            ))}
          </select>
        </label>
      ) : null}

      {invalidProduct ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.entitlements.invalidProduct")}
        </p>
      ) : null}

      {sanitizedProduct ? (
        <OrganizationEntitlementOperator
          organizationId={organizationId!}
          productCode={sanitizedProduct}
          organizationName={organizationQuery.data?.displayName ?? organizationId!}
        />
      ) : null}

      {sanitizedProduct && snapshotsQuery.isPending ? (
        <div
          className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3"
          role="status"
          aria-busy="true"
          aria-label={t("organization.entitlements.snapshots.loading")}
        >
          <DashboardWidgetSkeleton rows={5} />
        </div>
      ) : null}

      {snapshotsQuery.isError && isForbidden(snapshotsQuery.error) ? (
        <p className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3 text-[length:var(--exits-text-sm)] text-muted">
          {t("organization.entitlements.unavailable")}
        </p>
      ) : null}

      {snapshotsQuery.isError && !isForbidden(snapshotsQuery.error) && snapshotsDiagnostic ? (
        <ErrorState
          diagnostic={snapshotsDiagnostic}
          title={t("organization.entitlements.snapshots.error")}
          headingLevel="h2"
          onRetry={() => void snapshotsQuery.refetch()}
        />
      ) : null}

      {snapshotsQuery.data ? (
        <>
          <h2 className="text-[length:var(--exits-text-base)] font-semibold">
            {t("organization.entitlements.history.title")}
          </h2>
          {showTable ? (
            <div className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-4 py-3">
              <AdminTable
                caption={t("organization.entitlements.caption")}
                empty={t("organization.entitlements.empty")}
                columns={[
                  {
                    id: "revision",
                    header: t("organization.entitlements.column.revision"),
                    cell: (item) => (
                      <span className="font-medium">
                        {item.snapshotVersion}
                        {item.schemaVersion != null ? (
                          <span className="mt-0.5 block font-mono text-[length:var(--exits-text-xs)] font-normal text-muted">
                            {item.schemaVersion}
                          </span>
                        ) : null}
                      </span>
                    ),
                  },
                  {
                    id: "plan",
                    header: t("organization.entitlements.column.plan"),
                    cell: (item) => (
                      <span className="break-words">
                        {item.planCode}
                        {item.planVersionNumber != null ? (
                          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                            {item.planVersionNumber}
                          </span>
                        ) : null}
                      </span>
                    ),
                  },
                  {
                    id: "status",
                    header: t("organization.entitlements.column.status"),
                    cell: (item) => (
                      <StatusIndicator
                        tone={organizationSubscriptionStatusTone(item.subscriptionStatus)}
                        label={organizationSubscriptionStatusLabel(item.subscriptionStatus, t)}
                      />
                    ),
                  },
                  {
                    id: "generated",
                    header: t("organization.entitlements.column.generated"),
                    cell: (item) => formatInstant(item.generatedAtUtc, language) || "—",
                  },
                  {
                    id: "grants",
                    header: t("organization.entitlements.column.grants"),
                    cell: (item) => (
                      <EntitlementGrantDisclosure
                        grants={item.grants}
                        expanded={expandedIds.has(item.id)}
                        onToggle={() => toggleExpanded(item.id)}
                        panelId={`entitlement-grants-${item.id}`}
                        t={t}
                      />
                    ),
                  },
                ]}
                rows={snapshotsQuery.data.items}
                expandedContent={(item) =>
                  item.grants.length > 0 && expandedIds.has(item.id) ? (
                    <div id={`entitlement-grants-${item.id}`} className="px-1">
                      <EntitlementGrantDetails grants={item.grants} t={t} />
                    </div>
                  ) : null
                }
              />
            </div>
          ) : (
            <ul className="grid gap-2">
              {snapshotsQuery.data.items.length === 0 ? (
                <li className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-3 text-[length:var(--exits-text-sm)] text-muted">
                  {t("organization.entitlements.empty")}
                </li>
              ) : (
                snapshotsQuery.data.items.map((item) => {
                  const panelId = `entitlement-grants-${item.id}`;
                  const expanded = expandedIds.has(item.id);
                  return (
                    <li
                      key={item.id}
                      className="rounded-[var(--exits-density-radius)] border border-border bg-surface px-3 py-2.5"
                    >
                      <p className="font-medium">
                        {t("organization.entitlements.column.revision")} {item.snapshotVersion}
                      </p>
                      <p className="mt-0.5 break-words text-[length:var(--exits-text-xs)] text-muted">
                        {item.planCode}
                      </p>
                      <div className="mt-1.5">
                        <StatusIndicator
                          tone={organizationSubscriptionStatusTone(item.subscriptionStatus)}
                          label={organizationSubscriptionStatusLabel(item.subscriptionStatus, t)}
                        />
                      </div>
                      <div className="mt-2">
                        <p className="text-[length:var(--exits-text-xs)] font-medium text-muted">
                          {t("organization.entitlements.column.grants")}
                        </p>
                        <div className="mt-1">
                          <EntitlementGrantDisclosure
                            grants={item.grants}
                            expanded={expanded}
                            onToggle={() => toggleExpanded(item.id)}
                            panelId={panelId}
                            t={t}
                          />
                        </div>
                        {item.grants.length > 0 && expanded ? (
                          <div id={panelId} className="mt-2 border-t border-border/70 pt-2">
                            <EntitlementGrantDetails grants={item.grants} t={t} />
                          </div>
                        ) : null}
                      </div>
                    </li>
                  );
                })
              )}
            </ul>
          )}
          {totalPages > 1 ? (
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
          ) : null}
        </>
      ) : null}
    </section>
  );
}
