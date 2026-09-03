import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { MoreHorizontal, Plus } from "lucide-react";
import {
  canInviteOrganizationStaff,
  canManageBranchFulfillment,
} from "@/access/pos-capabilities";
import {
  getBranchCapacity,
  listBranchManagementSummaries,
  type BranchManagementSummaryItemDto,
} from "@/api/platform/organization-branches-client";
import { listOrganizationAreas } from "@/api/platform/organization-areas-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StatusFilter = "all" | "active" | "suspended" | "archived";

function statusTone(status: string): "success" | "warning" | "info" | "danger" {
  switch (normalizeBranchStatusFilter(status)) {
    case "Active":
      return "success";
    case "Suspended":
      return "warning";
    case "Archived":
      return "info";
    default:
      return "danger";
  }
}

function statusLabel(status: string, t: (key: MessageKey) => string): string {
  switch (normalizeBranchStatusFilter(status)) {
    case "Active":
      return t("branches.mgmt.status.active");
    case "Suspended":
      return t("branches.mgmt.status.suspended");
    case "Archived":
      return t("branches.mgmt.status.archived");
    default:
      return status;
  }
}

function matchesFilter(branch: BranchManagementSummaryItemDto, filter: StatusFilter): boolean {
  if (filter === "all") {
    return true;
  }
  const normalized = normalizeBranchStatusFilter(branch.status).toLowerCase();
  return normalized === filter;
}

export function BranchManagementListPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const canCreate = canInviteOrganizationStaff(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;
  const [filter, setFilter] = useState<StatusFilter>("active");
  const [menuBranch, setMenuBranch] = useState<BranchManagementSummaryItemDto | null>(null);

  const summaryQuery = useQuery({
    queryKey: ["branch-management-summary", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  const capacityQuery = useQuery({
    queryKey: ["branch-capacity", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: async ({ signal }) => {
      const result = await getBranchCapacity(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  const branches = useMemo(() => {
    const items = summaryQuery.data ?? [];
    return [...items]
      .filter((branch) => matchesFilter(branch, filter))
      .sort((a, b) => {
        if (a.isPrimary !== b.isPrimary) {
          return a.isPrimary ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
      });
  }, [summaryQuery.data, filter]);

  const capacity = capacityQuery.data;
  const atLimit = capacity != null && capacity.allowed > 0 && capacity.used >= capacity.allowed;

  const areasQuery = useQuery({
    queryKey: ["organization-areas", organizationId],
    enabled: Boolean(organizationId && canCreate),
    queryFn: async ({ signal }) => {
      const result = await listOrganizationAreas(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("areas.loadError"));
      }
      return result.value;
    },
  });

  // Single-branch shops with no areas keep the simple UX: no area setup is ever required.
  const liveBranchCount = (summaryQuery.data ?? []).filter(
    (branch) => normalizeBranchStatusFilter(branch.status) !== "Archived",
  ).length;
  const showAreasLink =
    canCreate && (liveBranchCount > 1 || (areasQuery.data?.areas.length ?? 0) > 0);

  if (!canManage) {
    return (
      <div className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3" data-testid="branch-mgmt-denied">
        <PageHeader
          title={t("branches.mgmt.title")}
          description={t("branches.mgmt.denied")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  return (
    <div className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3" data-testid="branch-mgmt-list">
      <PageHeader
        title={t("branches.mgmt.title")}
        description={t("branches.mgmt.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      {capacity ? (
        <div className="branch-mgmt-capacity" data-testid="branch-mgmt-capacity">
          <p className="branch-mgmt-capacity__label m-0">{t("branches.mgmt.capacity")}</p>
          <p className="branch-mgmt-capacity__value m-0" data-testid="branch-mgmt-capacity-value">
            {t("branches.mgmt.capacityOf")
              .replace("{used}", String(capacity.used))
              .replace("{allowed}", String(capacity.allowed))}
          </p>
          {atLimit ? (
            <p className="branch-mgmt-capacity__limit m-0" data-testid="branch-mgmt-capacity-limit">
              {t("branches.mgmt.capacityLimit").replace("{allowed}", String(capacity.allowed))}
            </p>
          ) : null}
        </div>
      ) : null}

      <div className="branch-mgmt-toolbar">
        <ExitsChipBar
          ariaLabel={t("branches.mgmt.filterLabel")}
          testId="branch-mgmt-filters"
          variant="filter"
          items={[
            {
              key: "active",
              label: t("branches.mgmt.filter.active"),
              state: filter === "active" ? "active" : "idle",
              testId: "branch-mgmt-filter-active",
              onSelect: () => setFilter("active"),
            },
            {
              key: "suspended",
              label: t("branches.mgmt.filter.suspended"),
              state: filter === "suspended" ? "active" : "idle",
              testId: "branch-mgmt-filter-suspended",
              onSelect: () => setFilter("suspended"),
            },
            {
              key: "archived",
              label: t("branches.mgmt.filter.archived"),
              state: filter === "archived" ? "active" : "idle",
              testId: "branch-mgmt-filter-archived",
              onSelect: () => setFilter("archived"),
            },
            {
              key: "all",
              label: t("branches.mgmt.filter.all"),
              state: filter === "all" ? "active" : "idle",
              testId: "branch-mgmt-filter-all",
              onSelect: () => setFilter("all"),
            },
          ]}
        />
        {showAreasLink ? (
          <Button asChild variant="outline" className="min-h-11" data-testid="branch-mgmt-areas">
            <Link to="/org/areas">{t("areas.title")}</Link>
          </Button>
        ) : null}
        {canCreate ? (
          atLimit ? (
            <Button type="button" className="branch-mgmt-add min-h-11" disabled data-testid="branch-mgmt-add">
              <Plus className="size-4" aria-hidden />
              {t("branches.mgmt.add")}
            </Button>
          ) : (
            <Button asChild className="branch-mgmt-add min-h-11" data-testid="branch-mgmt-add">
              <Link to="/org/branches/new">
                <Plus className="size-4" aria-hidden />
                {t("branches.mgmt.add")}
              </Link>
            </Button>
          )
        ) : null}
      </div>

      {summaryQuery.isLoading || capacityQuery.isLoading ? (
        <LoadingSkeleton count={3} label={t("loading.label")} />
      ) : null}

      {summaryQuery.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            summaryQuery.error instanceof Error
              ? summaryQuery.error.message
              : t("branches.mgmt.loadError")
          }
        />
      ) : null}

      {summaryQuery.isSuccess && branches.length === 0 ? (
        <EmptyState title={t("branches.mgmt.emptyTitle")} detail={t("branches.mgmt.emptyDetail")} />
      ) : null}

      {summaryQuery.isSuccess && branches.length > 0 ? (
        <ul className="branch-mgmt-list m-0 grid list-none gap-2 p-0" data-testid="branch-mgmt-items">
          {branches.map((branch) => {
            const location = [branch.city, branch.region].filter(Boolean).join(", ");
            return (
              <li key={branch.id}>
                <article
                  className="exits-list__card branch-mgmt-card min-w-0"
                  data-testid={`branch-mgmt-card-${branch.id}`}
                >
                  <div className="branch-mgmt-card__header">
                    <div className="min-w-0">
                      <h2 className="branch-mgmt-card__title m-0 truncate">{branch.name}</h2>
                      <p className="branch-mgmt-card__code m-0 mt-1 text-muted">{branch.code}</p>
                    </div>
                    <div className="branch-mgmt-card__badges">
                      {branch.isPrimary ? (
                        <span data-testid={`branch-mgmt-primary-${branch.id}`}>
                          <StatusChip tone="info">{t("branches.mgmt.primary")}</StatusChip>
                        </span>
                      ) : null}
                      <StatusChip tone={statusTone(branch.status)}>
                        {statusLabel(branch.status, t)}
                      </StatusChip>
                    </div>
                  </div>

                  {location ? (
                    <p className="branch-mgmt-card__location m-0 text-muted">{location}</p>
                  ) : null}

                  <dl className="branch-mgmt-card__meta">
                    <div>
                      <dt>{t("branches.mgmt.staffAccess")}</dt>
                      <dd data-testid={`branch-mgmt-staff-${branch.id}`}>
                        {branch.isPrimary || branch.assignedStaffCount === 0
                          ? branch.assignedStaffCount
                          : branch.assignedStaffCount}
                      </dd>
                    </div>
                    <div>
                      <dt>{t("branches.mgmt.devices")}</dt>
                      <dd data-testid={`branch-mgmt-devices-${branch.id}`}>
                        {t("branches.mgmt.devicesActive").replace(
                          "{count}",
                          String(branch.activeDeviceCount),
                        )}
                      </dd>
                    </div>
                    {showAreasLink ? (
                      <div>
                        <dt>{t("areas.singular")}</dt>
                        <dd data-testid={`branch-mgmt-area-${branch.id}`}>
                          {branch.areaName ?? t("areas.unassigned")}
                        </dd>
                      </div>
                    ) : null}
                    <div>
                      <dt>{t("branches.mgmt.pickup")}</dt>
                      <dd>{branch.pickupEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")}</dd>
                    </div>
                    <div>
                      <dt>{t("branches.mgmt.delivery")}</dt>
                      <dd>
                        {branch.deliveryEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")}
                      </dd>
                    </div>
                  </dl>

                  <div className="branch-mgmt-card__actions">
                    <Button asChild variant="outline" className="min-h-11" data-testid={`branch-mgmt-open-${branch.id}`}>
                      <Link to={`/org/branches/${branch.id}`}>{t("branches.mgmt.open")}</Link>
                    </Button>
                    <Button
                      asChild
                      variant="outline"
                      className="min-h-11"
                      data-testid={`branch-mgmt-view-qr-${branch.id}`}
                    >
                      <Link to={`/org/branches/${branch.id}?focus=qr#branch-storefront-qr`}>
                        {t("branches.mgmt.viewQr")}
                      </Link>
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      className="min-h-11 min-w-11 px-3"
                      data-testid={`branch-mgmt-more-${branch.id}`}
                      aria-label={t("branches.mgmt.more")}
                      onClick={() => setMenuBranch(branch)}
                    >
                      <MoreHorizontal className="size-4" aria-hidden />
                    </Button>
                  </div>
                </article>
              </li>
            );
          })}
        </ul>
      ) : null}

      <BottomSheet
        open={menuBranch !== null}
        onClose={() => setMenuBranch(null)}
        panelId="branch-mgmt-more-panel"
        testId="branch-mgmt-more-panel"
        title={menuBranch?.name ?? t("branches.mgmt.more")}
        closeLabel={t("branches.cancel")}
      >
        {menuBranch ? (
          <div className="flex flex-col gap-2">
            <Button asChild variant="outline" className="min-h-11 justify-start">
              <Link to={`/org/branches/${menuBranch.id}`} onClick={() => setMenuBranch(null)}>
                {t("branches.mgmt.open")}
              </Link>
            </Button>
            <Button asChild variant="outline" className="min-h-11 justify-start">
              <Link
                to={`/org/branches/${menuBranch.id}?focus=qr#branch-storefront-qr`}
                onClick={() => setMenuBranch(null)}
              >
                {t("branches.mgmt.viewQr")}
              </Link>
            </Button>
            <Button asChild variant="outline" className="min-h-11 justify-start">
              <Link
                to={`/org/branches/${menuBranch.id}/fulfillment`}
                onClick={() => setMenuBranch(null)}
              >
                {t("branches.detail.configureFulfillment")}
              </Link>
            </Button>
            {/* Explicitly no Delete action — archive is lifecycle-only on detail. */}
          </div>
        ) : null}
      </BottomSheet>
    </div>
  );
}
