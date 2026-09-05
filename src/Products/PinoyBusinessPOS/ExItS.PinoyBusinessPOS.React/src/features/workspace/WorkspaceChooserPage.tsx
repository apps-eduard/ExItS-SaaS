import {
  BriefcaseBusiness,
  ChevronDown,
  ChevronRight,
  LayoutGrid,
  ShoppingCart,
  Warehouse,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  canUseAdminExperience,
  isOrganizationAdministratorMembership,
  isOrganizationOwnerMembership,
  resolveEffectivePosRoleCode,
  type PosSessionGrantFacts,
} from "@/access/pos-capabilities";
import { listBranchManagementSummaries } from "@/api/platform/organization-branches-client";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { resolveFriendlyPosRole } from "@/lib/user-display";
import { cn } from "@/lib/cn";
import { normalizePosError } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";
import { PlatformApiError } from "@/api/platform/platform-http";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import {
  groupWorkspaceBranchesByArea,
  resolveWorkspaceBranchGroupingMode,
  summarizeWorkspaceLocations,
} from "@/features/workspace/workspace-area-grouping";
import { useWorkspace, type WorkspaceGrantProbeFailure } from "@/workspace/WorkspaceProvider";
import { workspaceBindFailureTitleKey } from "@/workspace/workspace-bind-error";
import {
  buildOrganizationDestinations,
  type WorkspaceDestination,
} from "@/workspace/workspace-destinations";
import type {
  AccessibleOrganizationWorkspace,
  AccessibleWorkspaceBranch,
} from "@/workspace/types";

function destinationIcon(destination: WorkspaceDestination): LucideIcon {
  if (destination.experience === "manage_business") {
    return BriefcaseBusiness;
  }
  if (destination.experience === "start_selling") {
    return ShoppingCart;
  }
  if (destination.labelKey === "experience.warehouseOperations") {
    return Warehouse;
  }
  return LayoutGrid;
}

type OrganizationGrantState = {
  grant: ReturnType<typeof useWorkspace>["sessionGrant"];
  grantFailure: WorkspaceGrantProbeFailure | null;
  grantLoading: boolean;
};

type GrantFacts = PosSessionGrantFacts | SessionGrantResponse | null | undefined;

function resolveOrganizationGrantState(
  organizationId: string,
  grantByOrganizationId: ReadonlyMap<string, ReturnType<typeof useWorkspace>["sessionGrant"] | null>,
  grantProbeFailureByOrganizationId: ReadonlyMap<string, WorkspaceGrantProbeFailure>,
  grantLoadingOrgId: string | null,
  workspaceReady: boolean,
): OrganizationGrantState {
  const grant = grantByOrganizationId.get(organizationId) ?? null;
  const grantFailure = grantProbeFailureByOrganizationId.get(organizationId) ?? null;
  const grantLoading =
    grantLoadingOrgId === organizationId || (workspaceReady && !grant && !grantFailure);
  return { grant, grantFailure, grantLoading };
}

function staffCountLabel(count: number, t: (key: MessageKey) => string): string {
  if (count === 1) {
    return t("orgRoles.staffCountOne");
  }
  return t("orgRoles.staffCountMany").replace("{count}", String(count));
}

function locationCountLabel(count: number, t: (key: MessageKey) => string): string {
  if (count === 1) {
    return t("workspace.locationCountOne");
  }
  return t("workspace.locationCountMany").replace("{count}", String(count));
}

function locationBreakdownLabel(
  retail: number,
  warehouse: number,
  t: (key: MessageKey) => string,
): string {
  return t("workspace.locationTypeBreakdown")
    .replace("{retail}", String(retail))
    .replace("{warehouse}", String(warehouse));
}

function resolveOwnWorkspaceRoleLabel(
  grant: GrantFacts,
  t: (key: MessageKey) => string,
): string | null {
  if (isOrganizationOwnerMembership(grant)) {
    return t("account.role.owner");
  }
  if (isOrganizationAdministratorMembership(grant)) {
    return t("account.role.admin");
  }
  const friendlyRole = resolveFriendlyPosRole(resolveEffectivePosRoleCode(grant));
  if (friendlyRole === "owner") {
    return t("account.role.owner");
  }
  if (friendlyRole === "manager") {
    return t("account.role.manager");
  }
  if (friendlyRole === "cashier") {
    return t("account.role.cashier");
  }
  return null;
}

function branchCardMetaLine(input: {
  grant: GrantFacts;
  staffCount: number | undefined;
  t: (key: MessageKey) => string;
}): string {
  if (canUseAdminExperience(input.grant)) {
    if (typeof input.staffCount === "number" && Number.isFinite(input.staffCount)) {
      return staffCountLabel(input.staffCount, input.t);
    }
    return input.t("workspace.branchActive");
  }
  const role = resolveOwnWorkspaceRoleLabel(input.grant, input.t);
  if (role) {
    return input.t("workspace.yourRole").replace("{role}", role);
  }
  return input.t("workspace.branchActive");
}

export function WorkspaceChooserPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const {
    status,
    workspaces,
    accessDeniedDetail,
    bindFailureKind,
    failureDiagnostic,
    bindDestination,
    grantByOrganizationId,
    grantProbeFailureByOrganizationId,
    ensureOrganizationGrantHint,
    retryOrganizationGrantHint,
  } = useWorkspace();
  const canCollapseOrgs = workspaces.length > 1;
  const [expandedOrgId, setExpandedOrgId] = useState<string | null>(() =>
    workspaces.length === 1 ? (workspaces[0]?.organizationId ?? null) : null,
  );
  const [bindingKey, setBindingKey] = useState<string | null>(null);
  const [localErrorKey, setLocalErrorKey] = useState<MessageKey | null>(null);
  const [staffCountByOrg, setStaffCountByOrg] = useState<Map<string, Map<string, number>>>(
    () => new Map(),
  );
  const [grantLoadingOrgId, setGrantLoadingOrgId] = useState<string | null>(null);
  const staffCountFetchAttempted = useRef<Set<string>>(new Set());

  useEffect(() => {
    if (workspaces.length === 1 && !expandedOrgId) {
      setExpandedOrgId(workspaces[0].organizationId);
    }
  }, [expandedOrgId, workspaces]);

  useEffect(() => {
    if (!expandedOrgId) {
      return;
    }
    let cancelled = false;
    void (async () => {
      setGrantLoadingOrgId(expandedOrgId);
      const grant = await ensureOrganizationGrantHint(expandedOrgId);
      if (cancelled) {
        return;
      }
      setGrantLoadingOrgId(null);
      if (!canUseAdminExperience(grant)) {
        return;
      }
      if (staffCountFetchAttempted.current.has(expandedOrgId)) {
        return;
      }
      staffCountFetchAttempted.current.add(expandedOrgId);
      const summary = await listBranchManagementSummaries(expandedOrgId);
      if (cancelled || !summary.ok) {
        return;
      }
      const byBranch = new Map<string, number>();
      for (const item of summary.value) {
        if (item.id) {
          byBranch.set(item.id, item.assignedStaffCount);
        }
      }
      setStaffCountByOrg((prev) => {
        const next = new Map(prev);
        next.set(expandedOrgId, byBranch);
        return next;
      });
    })();
    return () => {
      cancelled = true;
    };
  }, [ensureOrganizationGrantHint, expandedOrgId]);

  if (status === "loading" || status === "binding") {
    return <LoadingState label={t("workspace.loading")} />;
  }

  if (status === "error") {
    return (
      <ErrorState
        title={t("diagnostics.loadFailedTitle")}
        detail={t("workspace.loadError")}
        diagnostic={failureDiagnostic ?? undefined}
      />
    );
  }

  if (workspaces.length === 0) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
        <PageHeader title={t("workspace.title")} description={t("workspace.lede")} />
        <EmptyState title={t("noLocation.title")} detail={t("noLocation.detail")} />
      </div>
    );
  }

  async function selectDestination(destination: WorkspaceDestination) {
    const key = `${destination.organizationId}:${destination.experience}:${destination.branchId ?? "org"}`;
    setBindingKey(key);
    setLocalErrorKey(null);
    const ok = await bindDestination(destination);
    setBindingKey(null);
    if (!ok) {
      setLocalErrorKey("accessDenied.generic");
      return;
    }
    navigate(destination.route, { replace: true });
  }

  const failureTitleKey = bindFailureKind
    ? workspaceBindFailureTitleKey(bindFailureKind)
    : "accessDenied.title";
  const failureDetailKey = (accessDeniedDetail as MessageKey | null) ?? localErrorKey;

  if (workspaces.length === 1) {
    const organization = workspaces[0];
    const grantState = resolveOrganizationGrantState(
      organization.organizationId,
      grantByOrganizationId,
      grantProbeFailureByOrganizationId,
      grantLoadingOrgId,
      status === "ready",
    );

    if (grantState.grantLoading) {
      return (
        <div
          className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4"
          data-testid="workspace-grant-loading"
        >
          <PageHeader title={t("workspace.title")} description={t("workspace.experienceLede")} />
          <LoadingState label={t("workspace.preparingPermissions")} />
        </div>
      );
    }

    if (grantState.grantFailure && !grantState.grant) {
      return (
        <div className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4">
          <PageHeader title={t("workspace.title")} description={t("workspace.experienceLede")} />
          <div className="flex flex-col gap-3" data-testid="workspace-grant-probe-error">
            <ErrorState
              title={t("workspace.grantProbeFailedTitle")}
              detail={t("workspace.grantProbeFailedDetail")}
              diagnostic={workspaceGrantFailureDiagnostic(
                organization.displayName,
                grantState.grantFailure,
              )}
            />
            <Button
              type="button"
              className="w-full sm:w-auto"
              onClick={() => void retryOrganizationGrantHint(organization.organizationId)}
            >
              {t("workspace.grantProbeRetry")}
            </Button>
          </div>
        </div>
      );
    }

    const destinations = buildOrganizationDestinations({
      workspace: organization,
      grant: grantState.grant,
    });

    if (destinations.length === 0) {
      return (
        <div
          className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4"
          data-testid="workspace-no-authorized-destinations"
        >
          <PageHeader title={t("workspace.title")} description={t("workspace.experienceLede")} />
          <EmptyState
            title={t("workspace.noAuthorizedDestinationsTitle")}
            detail={t("workspace.noAuthorizedDestinationsDetail")}
          />
        </div>
      );
    }

    return (
      <div className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4">
        <PageHeader title={t("workspace.title")} description={t("workspace.experienceLede")} />
        {failureDetailKey ? (
          <ErrorState
            title={t(failureTitleKey)}
            detail={t(failureDetailKey)}
            diagnostic={failureDiagnostic ?? undefined}
          />
        ) : null}
        <OrganizationWorkspaceCard
          organization={organization}
          expanded
          canCollapse={false}
          onToggle={() => undefined}
          grant={grantState.grant}
          grantResolved
          staffCountByBranch={staffCountByOrg.get(organization.organizationId) ?? null}
          bindingKey={bindingKey}
          onSelectDestination={(destination) => void selectDestination(destination)}
          t={t}
        />
      </div>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-4">
      <PageHeader title={t("workspace.title")} description={t("workspace.experienceLede")} />
      {failureDetailKey ? (
        <ErrorState
          title={t(failureTitleKey)}
          detail={t(failureDetailKey)}
          diagnostic={failureDiagnostic ?? undefined}
        />
      ) : null}
      <div className="flex flex-col gap-3" role="list">
        {workspaces.map((organization) => {
          const expanded = canCollapseOrgs
            ? expandedOrgId === organization.organizationId
            : true;
          const grantState = resolveOrganizationGrantState(
            organization.organizationId,
            grantByOrganizationId,
            grantProbeFailureByOrganizationId,
            grantLoadingOrgId,
            status === "ready",
          );

          if (expanded && grantState.grantLoading) {
            return (
              <div key={organization.organizationId} data-testid="workspace-grant-loading">
                <LoadingState label={t("workspace.preparingPermissions")} />
              </div>
            );
          }

          if (expanded && grantState.grantFailure && !grantState.grant) {
            return (
              <div
                key={organization.organizationId}
                className="flex flex-col gap-3"
                data-testid="workspace-grant-probe-error"
              >
                <ErrorState
                  title={t("workspace.grantProbeFailedTitle")}
                  detail={t("workspace.grantProbeFailedDetail")}
                  diagnostic={workspaceGrantFailureDiagnostic(
                    organization.displayName,
                    grantState.grantFailure,
                  )}
                />
                <Button
                  type="button"
                  className="w-full sm:w-auto"
                  onClick={() => void retryOrganizationGrantHint(organization.organizationId)}
                >
                  {t("workspace.grantProbeRetry")}
                </Button>
              </div>
            );
          }

          if (
            expanded &&
            grantState.grant &&
            buildOrganizationDestinations({ workspace: organization, grant: grantState.grant })
              .length === 0
          ) {
            return (
              <div
                key={organization.organizationId}
                data-testid="workspace-no-authorized-destinations"
              >
                <EmptyState
                  title={t("workspace.noAuthorizedDestinationsTitle")}
                  detail={t("workspace.noAuthorizedDestinationsDetail")}
                />
              </div>
            );
          }

          return (
            <OrganizationWorkspaceCard
              key={organization.organizationId}
              organization={organization}
              expanded={expanded}
              canCollapse={canCollapseOrgs}
              onToggle={() =>
                setExpandedOrgId(
                  expandedOrgId === organization.organizationId
                    ? null
                    : organization.organizationId,
                )
              }
              grant={grantState.grant}
              grantResolved={Boolean(grantState.grant)}
              staffCountByBranch={staffCountByOrg.get(organization.organizationId) ?? null}
              bindingKey={bindingKey}
              onSelectDestination={(destination) => void selectDestination(destination)}
              t={t}
            />
          );
        })}
      </div>
    </div>
  );
}

function workspaceGrantFailureDiagnostic(
  organizationName: string,
  failure: WorkspaceGrantProbeFailure,
): PosErrorReportInput {
  const detail = failure.detail ?? "";
  const isAntiforgeryBootstrap =
    failure.errorCode === "application.auth.account_scope_denied" &&
    detail.includes("/api/v1/platform/antiforgery/token");

  return normalizePosError({
    source: "workspace",
    error: new PlatformApiError(failure.status, {
      errorCode: failure.errorCode,
      detail: failure.detail,
    }),
    operation: isAntiforgeryBootstrap ? "antiforgery bootstrap" : "workspace session grant probe",
    httpMethod: isAntiforgeryBootstrap ? "GET" : "POST",
    path: isAntiforgeryBootstrap
      ? "/api/v1/platform/antiforgery/token"
      : "/api/v1/platform/auth/token",
    screen: "/workspace",
    organizationName,
    status: failure.status,
    errorCode: failure.errorCode,
  });
}

function OrganizationWorkspaceCard({
  organization,
  expanded,
  canCollapse,
  onToggle,
  grant,
  grantResolved,
  staffCountByBranch,
  bindingKey,
  onSelectDestination,
  t,
}: {
  organization: AccessibleOrganizationWorkspace;
  expanded: boolean;
  canCollapse: boolean;
  onToggle: () => void;
  grant: ReturnType<typeof useWorkspace>["sessionGrant"];
  grantResolved: boolean;
  staffCountByBranch: Map<string, number> | null;
  bindingKey: string | null;
  onSelectDestination: (destination: WorkspaceDestination) => void;
  t: (key: MessageKey) => string;
}) {
  const destinations = useMemo(
    () => buildOrganizationDestinations({ workspace: organization, grant }),
    [grant, organization],
  );
  const manageBusiness = destinations.find((d) => d.experience === "manage_business");
  const locationCount = organization.branches.length;
  const locationsHeading =
    locationCount === 0
      ? t("workspace.locations")
      : t("workspace.locationsWithCount").replace("{count}", String(locationCount));
  const groupingMode = useMemo(
    () => resolveWorkspaceBranchGroupingMode(organization.branches),
    [organization.branches],
  );
  const areaGroups = useMemo(
    () =>
      groupingMode === "grouped" ? groupWorkspaceBranchesByArea(organization.branches) : [],
    [groupingMode, organization.branches],
  );

  function renderBranchTile(branch: AccessibleWorkspaceBranch) {
    const warehouse = isWarehouseBranch(branch.branchType);
    const branchDestinations = destinations.filter((d) => d.branchId === branch.branchId);
    const meta = branchCardMetaLine({
      grant,
      staffCount: staffCountByBranch?.get(branch.branchId),
      t,
    });
    return (
      <li
        key={branch.branchId}
        className="min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3"
        data-testid={`workspace-branch-${branch.branchId}`}
        data-branch-type={warehouse ? "Warehouse" : "Retail"}
      >
        <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
          <p className="m-0 min-w-0 flex-1 truncate font-semibold">{branch.name}</p>
          <div className="flex flex-wrap gap-1">
            <StatusChip tone={warehouse ? "warning" : "info"}>
              {warehouse ? t("branches.type.warehouse") : t("branches.type.retail")}
            </StatusChip>
            {!warehouse && branch.isPrimary ? (
              <StatusChip tone="info">{t("branches.mgmt.primary")}</StatusChip>
            ) : null}
          </div>
        </div>
        <p
          className="m-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted"
          data-testid={`workspace-branch-meta-${branch.branchId}`}
        >
          {meta}
        </p>
        {branchDestinations.length > 0 ? (
          <div
            className={cn(
              "mt-3 grid gap-2",
              branchDestinations.length === 1 ? "grid-cols-1" : "grid-cols-2",
            )}
          >
            {branchDestinations.map((destination) => (
              <DestinationTile
                key={`${destination.experience}:${destination.branchId}:${destination.labelKey}`}
                destination={destination}
                bindingKey={bindingKey}
                onSelect={onSelectDestination}
                t={t}
              />
            ))}
          </div>
        ) : null}
      </li>
    );
  }

  function renderAreaGroupMeta(branches: AccessibleWorkspaceBranch[]) {
    const breakdown = summarizeWorkspaceLocations(branches);
    return (
      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
        {locationCountLabel(breakdown.total, t)}
        <span aria-hidden> · </span>
        {locationBreakdownLabel(breakdown.retail, breakdown.warehouse, t)}
      </p>
    );
  }

  const headerContent = (
    <span className="min-w-0">
      <span className="block truncate text-[length:var(--exits-text-lg)] font-semibold text-foreground">
        {organization.displayName}
      </span>
    </span>
  );

  return (
    <Card className="overflow-hidden p-0" role="listitem">
      {canCollapse ? (
        <button
          type="button"
          className="flex w-full items-center justify-between gap-3 border-0 bg-transparent px-4 py-3 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
          aria-expanded={expanded}
          onClick={onToggle}
        >
          {headerContent}
          {expanded ? (
            <ChevronDown className="size-5 shrink-0 text-muted" aria-hidden="true" />
          ) : (
            <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden="true" />
          )}
        </button>
      ) : (
        <div className="px-4 py-3">{headerContent}</div>
      )}

      {expanded && grantResolved ? (
        <div className={cn("px-4 py-3", canCollapse && "border-t border-border")}>
          <div className="flex flex-col gap-4">
            {manageBusiness ? (
              <section aria-labelledby={`mgmt-${organization.organizationId}`}>
                <h3
                  id={`mgmt-${organization.organizationId}`}
                  className="m-0 mb-2 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted"
                >
                  {t("workspace.management")}
                </h3>
                <DestinationTile
                  destination={manageBusiness}
                  bindingKey={bindingKey}
                  onSelect={onSelectDestination}
                  t={t}
                  primary
                />
              </section>
            ) : null}

            {organization.branches.length > 0 ? (
              <section aria-labelledby={`locations-${organization.organizationId}`}>
                <h3
                  id={`locations-${organization.organizationId}`}
                  className="m-0 mb-2 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted"
                  data-testid="workspace-locations-heading"
                >
                  {locationsHeading}
                </h3>
                {groupingMode === "grouped" ? (
                  <div className="flex flex-col gap-4" data-testid="workspace-area-groups">
                    {areaGroups.map((group) => (
                      <section
                        key={group.key}
                        aria-labelledby={`area-${organization.organizationId}-${group.key}`}
                        data-testid={`workspace-area-group-${group.key}`}
                      >
                        <div className="mb-3 border-b border-border pb-2">
                          <h4
                            id={`area-${organization.organizationId}-${group.key}`}
                            className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground"
                          >
                            {group.isUnassigned
                              ? t("areas.unassigned")
                              : (group.areaName ?? t("areas.singular"))}
                          </h4>
                          <div data-testid={`workspace-area-meta-${group.key}`}>
                            {renderAreaGroupMeta(group.branches)}
                          </div>
                        </div>
                        <ul className="m-0 grid list-none grid-cols-1 gap-3 p-0 sm:grid-cols-2">
                          {group.branches.map(renderBranchTile)}
                        </ul>
                      </section>
                    ))}
                  </div>
                ) : (
                  <ul className="m-0 grid list-none grid-cols-1 gap-3 p-0 sm:grid-cols-2">
                    {organization.branches.map(renderBranchTile)}
                  </ul>
                )}
              </section>
            ) : manageBusiness ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("workspace.noActiveBranches")}
              </p>
            ) : null}
          </div>
        </div>
      ) : null}
    </Card>
  );
}

function DestinationTile({
  destination,
  bindingKey,
  onSelect,
  t,
  primary = false,
}: {
  destination: WorkspaceDestination;
  bindingKey: string | null;
  onSelect: (destination: WorkspaceDestination) => void;
  t: (key: MessageKey) => string;
  primary?: boolean;
}) {
  const key = `${destination.organizationId}:${destination.experience}:${destination.branchId ?? "org"}`;
  const busy = bindingKey === key;
  const Icon = destinationIcon(destination);
  const label = busy ? t("workspace.opening") : t(destination.labelKey);

  if (primary) {
    return (
      <Button
        type="button"
        variant="default"
        className="w-full justify-center gap-2"
        disabled={busy || bindingKey != null}
        onClick={() => onSelect(destination)}
        data-testid={`workspace-destination-${destination.experience}`}
      >
        <Icon className="size-4 shrink-0" aria-hidden />
        {label}
      </Button>
    );
  }

  return (
    <button
      type="button"
      disabled={busy || bindingKey != null}
      onClick={() => onSelect(destination)}
      data-testid={`workspace-destination-${destination.experience}`}
      data-label-key={destination.labelKey}
      aria-label={label}
      className={cn(
        "inline-flex w-full items-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5 text-left text-foreground transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-60",
      )}
    >
      <Icon className="size-5 shrink-0 text-primary" aria-hidden />
      <span className="min-w-0 text-[length:var(--exits-text-sm)] font-semibold wrap-break-word">
        {label}
      </span>
    </button>
  );
}
