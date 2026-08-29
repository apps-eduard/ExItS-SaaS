import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { PackageOpen } from "lucide-react";
import { canManageSuppliers } from "@/access/pos-capabilities";
import { getBusinessCustomer } from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatRelativeOrDate } from "@/features/devices/device-presentation";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function statusTone(status: string): "success" | "warning" | "info" {
  const s = status.toLowerCase();
  if (s === "active") return "success";
  if (s === "disconnected") return "warning";
  return "info";
}

function catalogModeLabel(mode: string, allEligible: string, selectedOnly: string): string {
  return mode === "AllEligible" ? allEligible : selectedOnly;
}

export function BusinessCustomerDetailPage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const { connectionId } = useParams<{ connectionId: string }>();
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const allowManage = canManageSuppliers(sessionGrant);

  const detailQuery = useQuery({
    queryKey: ["business-customers", "detail", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId),
    queryFn: ({ signal }) => getBusinessCustomer(workspace!, connectionId!, signal),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (detailQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (detailQuery.isError) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="business-customer-detail-error">
        <PageHeader
          title={t("customers.business.detailTitle")}
          backTo={pageBackNav.customers.to}
          backLabel={t(pageBackNav.customers.labelKey)}
          backTestId="page-header-back-customers"
        />
        <ErrorState
          title={t("customers.business.loadFailed")}
          detail={
            detailQuery.error instanceof PosApiError
              ? (detailQuery.error.problem.detail ?? detailQuery.error.message)
              : t("customers.business.loadFailedHelp")
          }
          error={detailQuery.error}
          operation="getBusinessCustomer"
        />
        <button
          type="button"
          className="exits-btn exits-btn--secondary self-start"
          data-testid="business-customer-detail-retry"
          onClick={() => void detailQuery.refetch()}
        >
          {t("customers.business.retry")}
        </button>
      </div>
    );
  }

  if (!detailQuery.data) {
    return (
      <EmptyState
        title={t("customers.business.notFound")}
        detail={t("customers.business.notFoundHelp")}
      />
    );
  }

  const customer = detailQuery.data;
  const name =
    customer.organizationDisplayName.trim() || t("customers.business.unknown");
  const since = customer.connectedSinceUtc
    ? formatRelativeOrDate(customer.connectedSinceUtc, new Date(), preferences.locale)
    : null;
  const isActive = customer.relationshipStatus.toLowerCase() === "active";
  const discountLabel =
    customer.customerDiscountPercent != null && customer.customerDiscountPercent > 0
      ? t("customers.business.discountOff").replace(
          "{percent}",
          String(customer.customerDiscountPercent),
        )
      : t("customers.business.noDiscount");

  return (
    <div
      className="business-customer-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="business-customer-detail"
    >
      <PageHeader
        title={t("customers.business.detailTitle")}
        description={name}
        backTo="/customers?kind=businesses"
        backLabel={t("customers.business.back")}
        backTestId="page-header-back-customers"
      />

      <section className="catalog-form-section">
        <div className="flex min-w-0 flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("customers.business.connectedOrg")}
            </p>
            {customer.organizationPublicId ? (
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {customer.organizationPublicId}
              </p>
            ) : null}
            {since ? (
              <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("customers.business.connectedSince").replace("{when}", since)}
              </p>
            ) : null}
          </div>
          <StatusChip tone={statusTone(customer.relationshipStatus)}>
            {customer.relationshipStatus}
          </StatusChip>
        </div>
      </section>

      <section className="catalog-form-section" data-testid="business-customer-catalog-summary">
        <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
          {t("customers.business.catalogPricing")}
        </h2>
        <dl className="m-0 mt-2 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div className="flex justify-between gap-3">
            <dt className="text-muted">{t("customers.business.catalogMode")}</dt>
            <dd className="m-0 font-medium">
              {catalogModeLabel(
                customer.catalogSharingMode,
                t("customers.business.modeAllEligible"),
                t("customers.business.modeSelectedOnly"),
              )}
            </dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted">{t("customers.business.shared")}</dt>
            <dd className="m-0 font-medium">{customer.sharedCount}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted">{t("customers.business.excluded")}</dt>
            <dd className="m-0 font-medium">{customer.excludedCount}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted">{t("customers.business.overrides")}</dt>
            <dd className="m-0 font-medium">{customer.overrideCount}</dd>
          </div>
          <div className="flex justify-between gap-3">
            <dt className="text-muted">{t("customers.business.customerPricing")}</dt>
            <dd className="m-0 font-medium">{discountLabel}</dd>
          </div>
        </dl>
      </section>

      {allowManage && isActive ? (
        <ExitsChipBar
          variant="actions"
          ariaLabel={t("customers.business.detailTitle")}
          testId="business-customer-toolbar"
          className="exits-animate-toolbar"
          items={[
            {
              key: "catalog",
              label: t("customers.business.manageCatalog"),
              icon: <PackageOpen />,
              href: `/suppliers/connected/buyers/${customer.connectionId}/shared-products`,
              testId: "business-customer-manage-catalog",
              emphasis: "primary",
            },
          ]}
        />
      ) : null}

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("customers.business.identityNote")}
      </p>

      <p className="m-0">
        <Link
          className="text-[length:var(--exits-text-sm)]"
          to="/suppliers/connected/requests"
          data-testid="business-customer-incoming-link"
        >
          {t("customers.business.incomingRequests")}
        </Link>
      </p>
    </div>
  );
}
