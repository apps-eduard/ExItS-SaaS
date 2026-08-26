import { useParams } from "react-router-dom";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import { PlatformApiError } from "@/api/platform-http";
import { PageHeader } from "@/components/exits/PageHeader";
import { OrganizationBillingLifecycle } from "@/features/organizations/OrganizationBillingLifecycle";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useOrganizationPaymentsQuery } from "@/features/organizations/use-organization-workspace-queries";
import { usePreferences } from "@/hooks/use-preferences";
import { useMemo } from "react";
import { parseOrganizationBillingSearchParams } from "@/api/organizations/billing-list-query";
import { useSearchParams } from "react-router-dom";

export function OrganizationBillingPage() {
  const { t } = usePreferences();
  const params = useParams();
  const organizationId = parseOrganizationId(params.organizationId);
  const [searchParams] = useSearchParams();
  const state = useMemo(() => parseOrganizationBillingSearchParams(searchParams), [searchParams]);
  const query = useOrganizationPaymentsQuery(organizationId, state);

  if (
    query.error instanceof PlatformApiError &&
    (query.error.status === 401 || query.error.status === 403)
  ) {
    return <ShellNotFoundPage />;
  }

  if (!organizationId) {
    return <ShellNotFoundPage />;
  }

  return (
    <section className="grid max-w-3xl gap-4">
      <PageHeader
        title={t("organization.billing.title")}
        description={t("organization.billing.description")}
      />
      <OrganizationBillingLifecycle organizationId={organizationId} />
    </section>
  );
}
