import {
  BriefcaseBusiness,
  ChevronDown,
  ChevronRight,
  LayoutGrid,
  ShoppingCart,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  listMembershipBranchAssignments,
} from "@/api/platform/membership-branch-assignments-client";
import {
  buildWorkspaceRoster,
  listOrganizationMembers,
  personAppearsOnBranch,
  type WorkspaceRosterPerson,
} from "@/api/platform/organization-members-client";
import { canUseAdminExperience } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { normalizePosError } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";
import { PlatformApiError } from "@/api/platform/platform-http";
import { useWorkspace, type WorkspaceGrantProbeFailure } from "@/workspace/WorkspaceProvider";
import { workspaceBindFailureTitleKey } from "@/workspace/workspace-bind-error";
import {
  buildOrganizationDestinations,
  type WorkspaceDestination,
} from "@/workspace/workspace-destinations";
import type { AccessibleOrganizationWorkspace } from "@/workspace/types";
import type { WorkingExperience } from "@/workspace/working-experience";

function destinationIcon(experience: WorkingExperience): LucideIcon {
  if (experience === "manage_business") {
    return BriefcaseBusiness;
  }
  if (experience === "start_selling") {
    return ShoppingCart;
  }
  return LayoutGrid;
}

type OrganizationGrantState = {
  grant: ReturnType<typeof useWorkspace>["sessionGrant"];
  grantFailure: WorkspaceGrantProbeFailure | null;
  grantLoading: boolean;
};

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

async function loadWorkspaceRosterWithBranchAccess(
  organizationId: string,
): Promise<{ managementTeam: WorkspaceRosterPerson[]; branchStaff: WorkspaceRosterPerson[] } | null> {
  const result = await listOrganizationMembers(organizationId);
  if (!result.ok) {
    return null;
  }
  const roster = buildWorkspaceRoster(result.members);
  const enrichedStaff = await Promise.all(
    roster.branchStaff.map(async (person) => {
      const access = await listMembershipBranchAssignments(organizationId, person.membershipId);
      if (!access.ok) {
        return person;
      }
      return {
        ...person,
        allActiveBranches: access.value.scope === "AllActive",
        branchIds:
          access.value.scope === "AllActive"
            ? []
            : access.value.branches.map((branch) => branch.branchId).filter(Boolean),
      };
    }),
  );
  return { managementTeam: roster.managementTeam, branchStaff: enrichedStaff };
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
  const [rosterByOrg, setRosterByOrg] = useState<
    Map<string, { managementTeam: WorkspaceRosterPerson[]; branchStaff: WorkspaceRosterPerson[] }>
  >(() => new Map());
  const [grantLoadingOrgId, setGrantLoadingOrgId] = useState<string | null>(null);
  const rosterFetchAttempted = useRef<Set<string>>(new Set());

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
      if (rosterFetchAttempted.current.has(expandedOrgId)) {
        return;
      }
      rosterFetchAttempted.current.add(expandedOrgId);
      const roster = await loadWorkspaceRosterWithBranchAccess(expandedOrgId);
      if (cancelled || !roster) {
        return;
      }
      setRosterByOrg((prev) => {
        const next = new Map(prev);
        next.set(expandedOrgId, roster);
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
              className="min-h-11 w-full sm:w-auto"
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
          roster={rosterByOrg.get(organization.organizationId) ?? null}
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
                  className="min-h-11 w-full sm:w-auto"
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
              roster={rosterByOrg.get(organization.organizationId) ?? null}
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
  roster,
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
  roster: {
    managementTeam: WorkspaceRosterPerson[];
    branchStaff: WorkspaceRosterPerson[];
  } | null;
  bindingKey: string | null;
  onSelectDestination: (destination: WorkspaceDestination) => void;
  t: (key: MessageKey) => string;
}) {
  const destinations = useMemo(
    () => buildOrganizationDestinations({ workspace: organization, grant }),
    [grant, organization],
  );
  const manageBusiness = destinations.find((d) => d.experience === "manage_business");
  const branchCount = organization.branches.length;
  const branchesHeading =
    branchCount === 0
      ? t("workspace.branches")
      : t("workspace.branchesWithCount").replace("{count}", String(branchCount));

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
          className="flex min-h-11 w-full items-center justify-between gap-3 border-0 bg-transparent px-4 py-3 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
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
                {roster && roster.managementTeam.length > 0 ? (
                  <ul
                    className="mb-2 list-none space-y-1 p-0"
                    aria-label={t("workspace.managementTeam")}
                  >
                    {roster.managementTeam.map((person) => (
                      <li
                        key={person.membershipId}
                        className="truncate text-[length:var(--exits-text-sm)] text-muted"
                      >
                        {person.displayName} — {person.roleLabel}
                      </li>
                    ))}
                  </ul>
                ) : null}
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
              <section aria-labelledby={`branches-${organization.organizationId}`}>
                <h3
                  id={`branches-${organization.organizationId}`}
                  className="m-0 mb-2 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted"
                  data-testid="workspace-branches-heading"
                >
                  {branchesHeading}
                </h3>
                <ul className="m-0 grid list-none grid-cols-1 gap-3 p-0 sm:grid-cols-2">
                  {organization.branches.map((branch) => {
                    const branchDestinations = destinations.filter(
                      (d) => d.branchId === branch.branchId,
                    );
                    const staffForBranch =
                      roster?.branchStaff.filter((person) =>
                        personAppearsOnBranch(person, branch),
                      ) ?? [];
                    return (
                      <li
                        key={branch.branchId}
                        className="min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-3"
                        data-testid={`workspace-branch-${branch.branchId}`}
                      >
                        <p className="m-0 truncate font-semibold">{branch.name}</p>
                        {staffForBranch.length > 0 ? (
                          <ul className="mb-2 mt-1 list-none space-y-0.5 p-0">
                            {staffForBranch.map((person) => (
                              <li
                                key={person.membershipId}
                                className="truncate text-[length:var(--exits-text-sm)] text-muted"
                              >
                                {person.displayName} — {person.roleLabel}
                              </li>
                            ))}
                          </ul>
                        ) : null}
                        {branchDestinations.length > 0 ? (
                          <div
                            className={cn(
                              "mt-3 grid gap-2",
                              branchDestinations.length === 1 ? "grid-cols-1" : "grid-cols-2",
                            )}
                          >
                            {branchDestinations.map((destination) => (
                              <DestinationTile
                                key={`${destination.experience}:${destination.branchId}`}
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
                  })}
                </ul>
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
  const Icon = destinationIcon(destination.experience);
  const label = busy ? t("workspace.opening") : t(destination.labelKey);

  if (primary) {
    return (
      <Button
        type="button"
        variant="default"
        className="min-h-11 w-full justify-center gap-2"
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
      aria-label={label}
      className={cn(
        "inline-flex min-h-11 w-full items-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2.5 text-left text-foreground transition-colors hover:bg-[var(--exits-surface-muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-60",
      )}
    >
      <Icon className="size-5 shrink-0 text-primary" aria-hidden />
      <span className="min-w-0 text-[length:var(--exits-text-sm)] font-semibold wrap-break-word">
        {label}
      </span>
    </button>
  );
}
