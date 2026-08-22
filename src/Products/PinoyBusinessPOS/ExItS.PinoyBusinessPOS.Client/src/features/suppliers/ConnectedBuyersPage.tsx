import { useMemo } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageSuppliers } from "@/access/pos-capabilities";
import {
  isRelationshipActive,
  listBuyerProductShares,
  listRelationships,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ConnectedBuyersPage() {
  const { t } = useI18n();
  const { relationshipId } = useParams<{ relationshipId?: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);

  const listQuery = useQuery({
    queryKey: ["connected-suppliers", "buyers", workspace?.organizationId],
    enabled: Boolean(workspace) && !relationshipId,
    queryFn: async ({ signal }) => {
      const rows = await listRelationships(workspace!, "supplier", signal);
      return rows.filter((row) => isRelationshipActive(row));
    },
  });

  const detailQuery = useQuery({
    queryKey: ["connected-suppliers", "buyer", workspace?.organizationId, relationshipId],
    enabled: Boolean(workspace) && Boolean(relationshipId),
    queryFn: async ({ signal }) => {
      const rows = await listRelationships(workspace!, "supplier", signal);
      return rows.find((row) => row.relationshipId === relationshipId) ?? null;
    },
  });

  const sharedCountQuery = useQuery({
    queryKey: ["connected-suppliers", "shared-count", relationshipId],
    enabled: Boolean(workspace) && Boolean(relationshipId) && Boolean(detailQuery.data),
    queryFn: async ({ signal }) => {
      const shares = await listBuyerProductShares(workspace!, relationshipId!, signal);
      return shares.filter((share) => share.isShared).length;
    },
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (relationshipId) {
    if (detailQuery.isLoading) {
      return <LoadingState label={t("loading.label")} />;
    }
    if (detailQuery.isError) {
      return (
        <ErrorState
          title={t("error.title")}
          detail={
            detailQuery.error instanceof PosApiError
              ? (detailQuery.error.problem.detail ?? detailQuery.error.message)
              : t("connected.loadFailed")
          }
        />
      );
    }
    if (!detailQuery.data) {
      return (
        <EmptyState
          title={t("connected.buyerNotFound")}
          detail={t("connected.buyerNotFoundHelp")}
        />
      );
    }
    const buyer = detailQuery.data;
    const name = buyer.counterpartyDisplayName?.trim() || t("connected.unknownBusiness");
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="connected-buyer-detail">
        <PageHeader
          title={t("connected.buyerDetailTitle")}
          description={name}
          backTo={pageBackNav.connectedBuyers.to}
          backLabel={t(pageBackNav.connectedBuyers.labelKey)}
          backTestId="page-header-back-suppliers"
        />
        <div className="flex flex-wrap items-center gap-2">
          <StatusChip tone={isRelationshipActive(buyer) ? "success" : "warning"}>
            {buyer.status}
          </StatusChip>
          {buyer.counterpartyPublicOrganizationId ? (
            <span className="text-[length:var(--exits-text-sm)] text-muted">
              {buyer.counterpartyPublicOrganizationId}
            </span>
          ) : null}
        </div>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("connected.notCustomerNote")}
        </p>
        {typeof sharedCountQuery.data === "number" ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)]"
            data-testid="connected-shared-count"
          >
            {t("connected.productsSharedCount").replace("{count}", String(sharedCountQuery.data))}
          </p>
        ) : null}
        {allowManage && isRelationshipActive(buyer) ? (
          <Button asChild className="min-h-11 self-start" data-testid="connected-manage-shared">
            <Link to={`/suppliers/connected/buyers/${buyer.relationshipId}/shared-products`}>
              {t("connected.manageSharedProducts")}
            </Link>
          </Button>
        ) : null}
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="connected-buyers-page">
      <PageHeader
        title={t("connected.buyersTitle")}
        description={t("connected.buyersHelp")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t("connected.backToSuppliers")}
        backTestId="page-header-back-suppliers"
      />
      {listQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {listQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("connected.loadFailed")} />
      ) : null}
      {listQuery.isSuccess && listQuery.data.length === 0 ? (
        <EmptyState title={t("connected.buyersEmpty")} detail={t("connected.buyersEmptyHelp")} />
      ) : null}
      <ul className="m-0 grid list-none gap-2 p-0" data-testid="connected-buyers-list">
        {listQuery.data?.map((buyer) => {
          const name = buyer.counterpartyDisplayName?.trim() || t("connected.unknownBusiness");
          return (
            <li key={buyer.relationshipId}>
              <Card className="p-3">
                <Link
                  className="block text-foreground no-underline"
                  to={`/suppliers/connected/buyers/${buyer.relationshipId}`}
                  data-testid={`connected-buyer-${buyer.relationshipId}`}
                >
                  <span className="block font-semibold">{name}</span>
                  <span className="mt-1 flex flex-wrap gap-2 text-[length:var(--exits-text-sm)] text-muted">
                    {buyer.counterpartyPublicOrganizationId}
                    <StatusChip tone="success">{buyer.status}</StatusChip>
                  </span>
                </Link>
              </Card>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
