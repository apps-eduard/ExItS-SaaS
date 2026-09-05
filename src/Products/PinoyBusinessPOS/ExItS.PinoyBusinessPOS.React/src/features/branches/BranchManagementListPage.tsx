import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown, LockKeyhole, MapPin, MoreHorizontal, Plus, Warehouse } from "lucide-react";
import {
  canInviteOrganizationStaff,
  canManageBranchFulfillment,
  canUseWarehouseBranches,
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
import { DropdownMenu, MenuItem } from "@/components/ui/dropdown-menu";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StatusFilter = "all" | "active" | "suspended" | "archived";
type TypeFilter = "all" | "retail" | "warehouse";

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

function matchesStatusFilter(branch: BranchManagementSummaryItemDto, filter: StatusFilter): boolean {
  if (filter === "all") {
    return true;
  }
  const normalized = normalizeBranchStatusFilter(branch.status).toLowerCase();
  return normalized === filter;
}

function matchesTypeFilter(branch: BranchManagementSummaryItemDto, filter: TypeFilter): boolean {
  if (filter === "all") {
    return true;
  }
  if (filter === "warehouse") {
    return isWarehouseBranch(branch.branchType);
  }
  return !isWarehouseBranch(branch.branchType);
}

export function BranchManagementListPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const canCreate = canInviteOrganizationStaff(sessionGrant);
  const warehouseAllowed = canUseWarehouseBranches(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("active");
  const [typeFilter, setTypeFilter] = useState<TypeFilter>("all");
  const [addMenuOpen, setAddMenuOpen] = useState(false);
  const [mobileMenuBranch, setMobileMenuBranch] = useState<BranchManagementSummaryItemDto | null>(
    null,
  );
  const [desktopMenuId, setDesktopMenuId] = useState<string | null>(null);

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

  const locationCounts = useMemo(() => {
    const items = summaryQuery.data ?? [];
    const live = items.filter(
      (branch) => normalizeBranchStatusFilter(branch.status) !== "Archived",
    );
    const retail = live.filter((branch) => !isWarehouseBranch(branch.branchType)).length;
    const warehouse = live.filter((branch) => isWarehouseBranch(branch.branchType)).length;
    return { retail, warehouse, live: live.length };
  }, [summaryQuery.data]);

  const branches = useMemo(() => {
    const items = summaryQuery.data ?? [];
    return [...items]
      .filter(
        (branch) =>
          matchesStatusFilter(branch, statusFilter) && matchesTypeFilter(branch, typeFilter),
      )
      .sort((a, b) => {
        if (a.isPrimary !== b.isPrimary) {
          return a.isPrimary ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
      });
  }, [summaryQuery.data, statusFilter, typeFilter]);

  const capacity = capacityQuery.data;
  const atLimit = capacity != null && capacity.allowed > 0 && capacity.used >= capacity.allowed;
  const showWarehouseHint =
    warehouseAllowed && summaryQuery.isSuccess && locationCounts.warehouse === 0;

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

  const liveBranchCount = locationCounts.live;
  const showAreasLink =
    canCreate && (liveBranchCount > 1 || (areasQuery.data?.areas.length ?? 0) > 0);

  function goCreate(type: "retail" | "warehouse") {
    setAddMenuOpen(false);
    navigate(`/org/branches/new?type=${type}`);
  }

  function branchSecondaryActions(branch: BranchManagementSummaryItemDto) {
    const warehouse = isWarehouseBranch(branch.branchType);
    const actions: Array<{ testId: string; label: string; to: string }> = [
      {
        testId: `branch-mgmt-more-staff-${branch.id}`,
        label: t("branches.detail.staff"),
        to: `/org/branches/${branch.id}?tab=staff`,
      },
      {
        testId: `branch-mgmt-more-devices-${branch.id}`,
        label: t("branches.detail.devices"),
        to: `/org/branches/${branch.id}?tab=devices`,
      },
    ];
    if (!warehouse) {
      actions.push({
        testId: `branch-mgmt-more-fulfillment-${branch.id}`,
        label: t("branches.detail.configureFulfillment"),
        to: `/org/branches/${branch.id}/fulfillment`,
      });
    }
    return actions;
  }

  const addLocationControl =
    canCreate ? (
      atLimit ? (
        <Button type="button" className="branch-mgmt-add" disabled data-testid="branch-mgmt-add">
              <Plus className="size-4 shrink-0" aria-hidden />
              <span>{t("branches.mgmt.add")}</span>
              <ChevronDown className="size-3.5 shrink-0 opacity-70" aria-hidden />
            </Button>
          ) : (
        <DropdownMenu
          open={addMenuOpen}
          onOpenChange={setAddMenuOpen}
          align="end"
          menuLabel={t("branches.mgmt.addMenuLabel")}
          className="branch-mgmt-add-menu"
          trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
            <Button
              type="button"
              id={id}
              className="branch-mgmt-add"
              data-testid="branch-mgmt-add"
              aria-haspopup="menu"
              aria-expanded={expanded}
              aria-controls={controls}
              onClick={onClick}
              onKeyDown={onKeyDown}
            >
              <Plus className="size-4 shrink-0" aria-hidden />
              <span>{t("branches.mgmt.add")}</span>
              <ChevronDown className="size-3.5 shrink-0 opacity-70" aria-hidden />
            </Button>
          )}
        >
          <MenuItem
            data-testid="branch-mgmt-add-retail"
            onSelect={() => goCreate("retail")}
          >
            <MapPin className="size-4 shrink-0" aria-hidden />
            {t("branches.mgmt.addRetail")}
          </MenuItem>
          {warehouseAllowed ? (
            <MenuItem
              data-testid="branch-mgmt-add-warehouse"
              onSelect={() => goCreate("warehouse")}
            >
              <Warehouse className="size-4 shrink-0" aria-hidden />
              {t("branches.mgmt.addWarehouse")}
            </MenuItem>
          ) : (
            <MenuItem
              data-testid="branch-mgmt-add-warehouse-locked"
              disabled
              onSelect={() => undefined}
            >
              <LockKeyhole className="size-4 shrink-0" aria-hidden />
              <span className="flex min-w-0 flex-col gap-0.5">
                <span>{t("branches.mgmt.addWarehouse")}</span>
                <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
                  {t("branches.mgmt.addWarehouseLocked")}
                </span>
              </span>
            </MenuItem>
          )}
        </DropdownMenu>
      )
    ) : null;

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
        trailing={addLocationControl}
      />

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="outline" size="sm" data-testid="branch-mgmt-supply-routes">
          <Link to="/org/supply-routes">{t("branches.mgmt.supplyRoutes")}</Link>
        </Button>
      </div>

      {capacity ? (
        <div className="branch-mgmt-capacity" data-testid="branch-mgmt-capacity">
          <p className="branch-mgmt-capacity__label m-0">{t("branches.mgmt.capacity")}</p>
          <p className="branch-mgmt-capacity__value m-0" data-testid="branch-mgmt-capacity-value">
            {t("branches.mgmt.capacityOf")
              .replace("{used}", String(capacity.used))
              .replace("{allowed}", String(capacity.allowed))}
          </p>
          <p
            className="branch-mgmt-capacity__breakdown m-0"
            data-testid="branch-mgmt-capacity-breakdown"
          >
            {t("branches.mgmt.capacityBreakdown")
              .replace("{retail}", String(locationCounts.retail))
              .replace("{warehouse}", String(locationCounts.warehouse))}
          </p>
          {atLimit ? (
            <p className="branch-mgmt-capacity__limit m-0" data-testid="branch-mgmt-capacity-limit">
              {t("branches.mgmt.capacityLimit").replace("{allowed}", String(capacity.allowed))}
            </p>
          ) : null}
        </div>
      ) : null}

      {showWarehouseHint ? (
        <div className="branch-mgmt-warehouse-hint" data-testid="branch-mgmt-warehouse-hint">
          <div className="branch-mgmt-warehouse-hint__copy">
            <p className="branch-mgmt-warehouse-hint__title m-0">
              {t("branches.mgmt.warehouseHintTitle")}
            </p>
            <p className="branch-mgmt-warehouse-hint__detail m-0">
              {t("branches.mgmt.warehouseHintDetail")}
            </p>
          </div>
          {canCreate && !atLimit ? (
            <Button asChild variant="outline" data-testid="branch-mgmt-warehouse-hint-add">
              <Link to="/org/branches/new?type=warehouse">
                <Plus className="size-4" aria-hidden />
                {t("branches.mgmt.warehouseHintAdd")}
              </Link>
            </Button>
          ) : null}
        </div>
      ) : null}

      <div className="branch-mgmt-toolbar">
        <div className="branch-mgmt-filters">
          <div className="branch-mgmt-filter-group">
            <span className="branch-mgmt-filter-group__label">{t("branches.mgmt.filter.typeLabel")}</span>
            <ExitsChipBar
              ariaLabel={t("branches.mgmt.filter.typeLabel")}
              testId="branch-mgmt-type-filters"
              variant="filter"
              items={[
                {
                  key: "all",
                  label: t("branches.mgmt.filter.all"),
                  state: typeFilter === "all" ? "active" : "idle",
                  testId: "branch-mgmt-type-all",
                  onSelect: () => setTypeFilter("all"),
                },
                {
                  key: "retail",
                  label: t("branches.mgmt.filter.retail"),
                  state: typeFilter === "retail" ? "active" : "idle",
                  testId: "branch-mgmt-type-retail",
                  onSelect: () => setTypeFilter("retail"),
                },
                {
                  key: "warehouse",
                  label: t("branches.mgmt.filter.warehouse"),
                  state: typeFilter === "warehouse" ? "active" : "idle",
                  testId: "branch-mgmt-type-warehouse",
                  onSelect: () => setTypeFilter("warehouse"),
                },
              ]}
            />
          </div>
          <div className="branch-mgmt-filter-group">
            <span className="branch-mgmt-filter-group__label">
              {t("branches.mgmt.filter.statusLabel")}
            </span>
            <ExitsChipBar
              ariaLabel={t("branches.mgmt.filter.statusLabel")}
              testId="branch-mgmt-status-filters"
              variant="filter"
              items={[
                {
                  key: "all",
                  label: t("branches.mgmt.filter.all"),
                  state: statusFilter === "all" ? "active" : "idle",
                  testId: "branch-mgmt-filter-all",
                  onSelect: () => setStatusFilter("all"),
                },
                {
                  key: "active",
                  label: t("branches.mgmt.filter.active"),
                  state: statusFilter === "active" ? "active" : "idle",
                  testId: "branch-mgmt-filter-active",
                  onSelect: () => setStatusFilter("active"),
                },
                {
                  key: "suspended",
                  label: t("branches.mgmt.filter.suspended"),
                  state: statusFilter === "suspended" ? "active" : "idle",
                  testId: "branch-mgmt-filter-suspended",
                  onSelect: () => setStatusFilter("suspended"),
                },
                {
                  key: "archived",
                  label: t("branches.mgmt.filter.archived"),
                  state: statusFilter === "archived" ? "active" : "idle",
                  testId: "branch-mgmt-filter-archived",
                  onSelect: () => setStatusFilter("archived"),
                },
              ]}
            />
          </div>
        </div>
        {showAreasLink ? (
          <Button asChild variant="outline" data-testid="branch-mgmt-areas">
            <Link to="/org/areas">
              <MapPin className="size-4" aria-hidden />
              {t("areas.title")}
            </Link>
          </Button>
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
        <ul className="branch-mgmt-list m-0 grid list-none p-0" data-testid="branch-mgmt-items">
          {branches.map((branch) => {
            const location = [branch.city, branch.region].filter(Boolean).join(", ");
            const warehouse = isWarehouseBranch(branch.branchType);
            return (
              <li key={branch.id}>
                <article
                  className="exits-entity-card exits-entity-card--interactive branch-mgmt-card min-w-0"
                  data-testid={`branch-mgmt-card-${branch.id}`}
                >
                  <div className="exits-entity-card__header">
                    <div className="exits-entity-card__identity">
                      <div className="exits-entity-card__title-row">
                        <h2 className="exits-entity-card__title">{branch.name}</h2>
                        <span className="exits-entity-card__code-sep" aria-hidden>
                          ·
                        </span>
                        <p className="exits-entity-card__code">{branch.code}</p>
                      </div>
                      {location ? (
                        <p className="exits-entity-card__subtitle">{location}</p>
                      ) : null}
                    </div>
                    <div className="exits-entity-card__badges">
                      <span data-testid={`branch-mgmt-type-${branch.id}`}>
                        <StatusChip tone={warehouse ? "warning" : "info"}>
                          {warehouse ? t("branches.type.warehouse") : t("branches.type.retail")}
                        </StatusChip>
                      </span>
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

                  <dl className="exits-entity-card__meta">
                    <div className="exits-entity-card__meta-item">
                      <dt>{t("branches.mgmt.staffAccess")}</dt>
                      <dd data-testid={`branch-mgmt-staff-${branch.id}`}>
                        {branch.assignedStaffCount}
                      </dd>
                    </div>
                    <div className="exits-entity-card__meta-item">
                      <dt>{t("branches.mgmt.devices")}</dt>
                      <dd data-testid={`branch-mgmt-devices-${branch.id}`}>
                        {t("branches.mgmt.devicesActive").replace(
                          "{count}",
                          String(branch.activeDeviceCount),
                        )}
                      </dd>
                    </div>
                    {showAreasLink ? (
                      <div className="exits-entity-card__meta-item">
                        <dt>{t("areas.singular")}</dt>
                        <dd data-testid={`branch-mgmt-area-${branch.id}`}>
                          {branch.areaName ?? t("areas.unassigned")}
                        </dd>
                      </div>
                    ) : null}
                    {!warehouse ? (
                      <>
                        <div className="exits-entity-card__meta-item">
                          <dt>{t("branches.mgmt.pickup")}</dt>
                          <dd data-testid={`branch-mgmt-pickup-${branch.id}`}>
                            {branch.pickupEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")}
                          </dd>
                        </div>
                        <div className="exits-entity-card__meta-item">
                          <dt>{t("branches.mgmt.delivery")}</dt>
                          <dd data-testid={`branch-mgmt-delivery-${branch.id}`}>
                            {branch.deliveryEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")}
                          </dd>
                        </div>
                      </>
                    ) : null}
                  </dl>

                  <div className="exits-entity-card__actions">
                    <Button asChild variant="outline" data-testid={`branch-mgmt-open-${branch.id}`}>
                      <Link to={`/org/branches/${branch.id}`}>
                        {warehouse
                          ? t("branches.mgmt.openWarehouse")
                          : t("branches.mgmt.open")}
                      </Link>
                    </Button>
                    {!warehouse ? (
                      <Button
                        asChild
                        variant="outline"
                        data-testid={`branch-mgmt-view-qr-${branch.id}`}
                      >
                        <Link to={`/org/branches/${branch.id}?focus=qr#branch-storefront-qr`}>
                          {t("branches.mgmt.viewQr")}
                        </Link>
                      </Button>
                    ) : null}

                    {/* Mobile: open BottomSheet */}
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      className="md:hidden"
                      data-testid={`branch-mgmt-more-${branch.id}`}
                      aria-label={t("branches.mgmt.more")}
                      aria-haspopup="dialog"
                      onClick={() => setMobileMenuBranch(branch)}
                    >
                      <MoreHorizontal className="size-4" aria-hidden />
                    </Button>

                    {/* Desktop/tablet: anchored DropdownMenu */}
                    <DropdownMenu
                      open={desktopMenuId === branch.id}
                      onOpenChange={(open) => setDesktopMenuId(open ? branch.id : null)}
                      align="end"
                      menuLabel={t("branches.mgmt.more")}
                      className="branch-mgmt-more-menu hidden md:inline-flex"
                      trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
                        <Button
                          type="button"
                          id={id}
                          variant="ghost"
                          size="icon"
                          data-testid={`branch-mgmt-more-desktop-${branch.id}`}
                          aria-label={t("branches.mgmt.more")}
                          aria-haspopup="menu"
                          aria-expanded={expanded}
                          aria-controls={controls}
                          onClick={onClick}
                          onKeyDown={onKeyDown}
                        >
                          <MoreHorizontal className="size-4" aria-hidden />
                        </Button>
                      )}
                    >
                      {branchSecondaryActions(branch).map((action) => (
                        <MenuItem
                          key={action.to}
                          data-testid={action.testId}
                          onSelect={() => {
                            setDesktopMenuId(null);
                            navigate(action.to);
                          }}
                        >
                          {action.label}
                        </MenuItem>
                      ))}
                    </DropdownMenu>
                  </div>
                </article>
              </li>
            );
          })}
        </ul>
      ) : null}

      <BottomSheet
        open={mobileMenuBranch !== null}
        onClose={() => setMobileMenuBranch(null)}
        panelId="branch-mgmt-more-panel"
        testId="branch-mgmt-more-panel"
        title={mobileMenuBranch?.name ?? t("branches.mgmt.more")}
        closeLabel={t("branches.cancel")}
      >
        {mobileMenuBranch ? (
          <div className="flex flex-col gap-2" data-testid="branch-mgmt-more-actions">
            {branchSecondaryActions(mobileMenuBranch).map((action) => (
              <Button
                key={action.to}
                asChild
                variant="outline"
                className="justify-start"
                data-testid={action.testId}
              >
                <Link to={action.to} onClick={() => setMobileMenuBranch(null)}>
                  {action.label}
                </Link>
              </Button>
            ))}
          </div>
        ) : null}
      </BottomSheet>
    </div>
  );
}
