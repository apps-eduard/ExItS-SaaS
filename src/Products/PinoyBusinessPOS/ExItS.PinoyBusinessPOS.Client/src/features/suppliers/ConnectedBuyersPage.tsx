import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, Inbox, PackageOpen } from "lucide-react";
import { canManageSuppliers } from "@/access/pos-capabilities";
import {
  isRelationshipActive,
  listBuyerProductShares,
  listRelationships,
  type ConnectedSupplierRelationship,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatRelativeOrDate } from "@/features/devices/device-presentation";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function matchesBuyerSearch(item: ConnectedSupplierRelationship, query: string): boolean {
  if (!query) return true;

  const haystack = [
    item.counterpartyDisplayName,
    item.counterpartyPublicOrganizationId,
    item.status,
  ]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();

  return haystack.includes(query.toLowerCase());
}

function connectedSinceLabel(
  buyer: ConnectedSupplierRelationship,
  locale: string,
  template: string,
): string | null {
  const when = formatRelativeOrDate(
    buyer.respondedAtUtc ?? buyer.createdAtUtc,
    new Date(),
    locale,
  );
  return when ? template.replace("{when}", when) : null;
}

export function ConnectedBuyersPage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const { relationshipId } = useParams<{ relationshipId?: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

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

  const listItems = useMemo(() => {
    const rows = listQuery.data ?? [];
    return rows.filter((item) => matchesBuyerSearch(item, debounced));
  }, [listQuery.data, debounced]);

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
    const sinceLabel = connectedSinceLabel(buyer, preferences.locale, t("connected.connectedSince"));

    return (
      <div
        className="connected-buyer-detail-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="connected-buyer-detail"
      >
        <PageHeader
          title={t("connected.buyerDetailTitle")}
          description={name}
          backTo={pageBackNav.connectedBuyers.to}
          backLabel={t(pageBackNav.connectedBuyers.labelKey)}
          backTestId="page-header-back-suppliers"
        />

        <section className="catalog-form-section connected-buyer-detail__overview">
          <div className="connected-buyer-detail__header">
            <div className="min-w-0">
              {buyer.counterpartyPublicOrganizationId ? (
                <p className="connected-buyer-detail__org m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {buyer.counterpartyPublicOrganizationId}
                </p>
              ) : null}
              {sinceLabel ? (
                <p className="connected-buyer-detail__meta m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                  {sinceLabel}
                </p>
              ) : null}
              <p className="connected-buyer-detail__note m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
                {t("connected.notCustomerNote")}
              </p>
            </div>
            <div className="connected-buyer-detail__aside">
              {typeof sharedCountQuery.data === "number" ? (
                <span className="connected-buyer-detail__stat" data-testid="connected-shared-count">
                  {sharedCountQuery.data}
                  <span className="connected-buyer-detail__stat-label">
                    {t("connected.shared")}
                  </span>
                </span>
              ) : null}
              <StatusChip tone={isRelationshipActive(buyer) ? "success" : "warning"}>
                {buyer.status}
              </StatusChip>
            </div>
          </div>
        </section>

        {allowManage && isRelationshipActive(buyer) ? (
          <ExitsChipBar
            variant="actions"
            ariaLabel={t("connected.buyerDetailTitle")}
            testId="connected-buyer-toolbar"
            className="exits-animate-toolbar"
            items={[
              {
                key: "shared",
                label: t("connected.manageSharedProducts"),
                icon: <PackageOpen />,
                href: `/suppliers/connected/buyers/${buyer.relationshipId}/shared-products`,
                testId: "connected-manage-shared",
                emphasis: "primary",
              },
            ]}
          />
        ) : null}
      </div>
    );
  }

  const hasLoaded = listQuery.isSuccess;
  const totalBuyers = listQuery.data?.length ?? 0;
  const showFilteredEmpty = hasLoaded && totalBuyers > 0 && listItems.length === 0;
  const showTrueEmpty = hasLoaded && totalBuyers === 0;

  return (
    <div
      className="connected-buyers-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="connected-buyers-page"
    >
      <PageHeader
        title={t("connected.buyersTitle")}
        description={t("connected.buyersHelp")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t("connected.backToSuppliers")}
        backTestId="page-header-back-suppliers"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("connected.buyersTitle")}
        testId="connected-buyers-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "incoming",
            label: t("connected.incomingRequests"),
            icon: <Inbox />,
            href: "/suppliers/connected/requests",
            testId: "connected-buyers-incoming",
          },
        ]}
      />

      <SearchField
        label={t("connected.searchBuyers")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("connected.searchBuyers")}
        data-testid="connected-buyers-search"
        containerClassName="connected-buyers-page__search exits-page__search"
      />

      {listQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {listQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("connected.loadFailed")} />
      ) : null}
      {showTrueEmpty ? (
        <EmptyState title={t("connected.buyersEmpty")} detail={t("connected.buyersEmptyHelp")} />
      ) : null}
      {showFilteredEmpty ? (
        <EmptyState
          title={t("connected.buyersNoMatch")}
          detail={t("connected.buyersNoMatchHelp")}
        />
      ) : null}

      <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="connected-buyers-list">
        {listItems.map((buyer) => {
          const name = buyer.counterpartyDisplayName?.trim() || t("connected.unknownBusiness");
          const sinceLabel = connectedSinceLabel(
            buyer,
            preferences.locale,
            t("connected.connectedSince"),
          );
          const metaParts = [
            buyer.counterpartyPublicOrganizationId,
            sinceLabel,
          ].filter(Boolean);

          return (
            <li key={buyer.relationshipId}>
              <Link
                className="exits-list__card connected-buyer-row block min-w-0 text-foreground no-underline"
                to={`/suppliers/connected/buyers/${buyer.relationshipId}`}
                data-testid={`connected-buyer-${buyer.relationshipId}`}
              >
                <span className="connected-buyer-row__main min-w-0">
                  <span className="exits-list__name block truncate font-semibold">{name}</span>
                  {metaParts.length > 0 ? (
                    <span className="connected-buyer-row__meta mt-1 block truncate text-[length:var(--exits-text-sm)] text-muted">
                      {metaParts.join(" · ")}
                    </span>
                  ) : null}
                </span>
                <span className="connected-buyer-row__aside">
                  <StatusChip tone="success">{buyer.status}</StatusChip>
                  <ChevronRight className="connected-buyer-row__chevron size-4 shrink-0 text-muted" aria-hidden />
                </span>
              </Link>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
