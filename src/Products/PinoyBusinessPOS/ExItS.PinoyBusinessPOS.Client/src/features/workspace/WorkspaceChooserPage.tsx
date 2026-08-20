import { ChevronDown, ChevronRight } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  buildWorkspaceRoster,
  listOrganizationMembers,
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
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workspaceBindFailureTitleKey } from "@/workspace/workspace-bind-error";
import {
  buildOrganizationDestinations,
  type WorkspaceDestination,
} from "@/workspace/workspace-destinations";
import type { AccessibleOrganizationWorkspace } from "@/workspace/types";

function membershipRoleLabel(
  role: string | null | undefined,
  t: (key: MessageKey) => string,
): string | null {
  if (!role) {
    return null;
  }
  if (role.localeCompare("OrganizationOwner", undefined, { sensitivity: "accent" }) === 0) {
    return t("account.role.owner");
  }
  if (role.localeCompare("OrganizationAdministrator", undefined, { sensitivity: "accent" }) === 0) {
    return t("account.role.admin");
  }
  return null;
}

export function WorkspaceChooserPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const {
    status,
    workspaces,
    accessDeniedDetail,
    bindFailureKind,
    bindDestination,
    grantByOrganizationId,
    ensureOrganizationGrantHint,
  } = useWorkspace();
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
      const result = await listOrganizationMembers(expandedOrgId);
      if (cancelled || !result.ok) {
        return;
      }
      const roster = buildWorkspaceRoster(result.members);
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
    return <ErrorState title={t("error.title")} detail={t("workspace.loadError")} />;
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

  return (
    <div className="mx-auto flex w-full max-w-xl min-w-0 flex-col gap-4">
      <PageHeader title={t("workspace.title")} description={t("workspace.experienceLede")} />
      {failureDetailKey ? (
        <ErrorState title={t(failureTitleKey)} detail={t(failureDetailKey)} />
      ) : null}
      <div className="flex flex-col gap-3" role="list">
        {workspaces.map((organization) => (
          <OrganizationWorkspaceCard
            key={organization.organizationId}
            organization={organization}
            expanded={expandedOrgId === organization.organizationId}
            onToggle={() =>
              setExpandedOrgId(
                expandedOrgId === organization.organizationId ? null : organization.organizationId,
              )
            }
            grant={grantByOrganizationId.get(organization.organizationId) ?? null}
            grantLoading={grantLoadingOrgId === organization.organizationId}
            roster={rosterByOrg.get(organization.organizationId) ?? null}
            bindingKey={bindingKey}
            onSelectDestination={(destination) => void selectDestination(destination)}
            membershipLabel={membershipRoleLabel(organization.membershipRole, t)}
            t={t}
          />
        ))}
      </div>
    </div>
  );
}

function OrganizationWorkspaceCard({
  organization,
  expanded,
  onToggle,
  grant,
  grantLoading,
  roster,
  bindingKey,
  onSelectDestination,
  membershipLabel,
  t,
}: {
  organization: AccessibleOrganizationWorkspace;
  expanded: boolean;
  onToggle: () => void;
  grant: ReturnType<typeof useWorkspace>["sessionGrant"];
  grantLoading: boolean;
  roster: {
    managementTeam: WorkspaceRosterPerson[];
    branchStaff: WorkspaceRosterPerson[];
  } | null;
  bindingKey: string | null;
  onSelectDestination: (destination: WorkspaceDestination) => void;
  membershipLabel: string | null;
  t: (key: MessageKey) => string;
}) {
  const destinations = useMemo(
    () => buildOrganizationDestinations({ workspace: organization, grant }),
    [grant, organization],
  );
  const manageBusiness = destinations.find((d) => d.experience === "manage_business");
  const branchCountLabel =
    organization.branches.length === 1
      ? t("workspace.branchCountOne")
      : t("workspace.branchCountMany").replace("{count}", String(organization.branches.length));

  return (
    <Card className="overflow-hidden p-0" role="listitem">
      <button
        type="button"
        className="flex min-h-11 w-full items-center justify-between gap-3 border-0 bg-transparent px-4 py-3 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
        aria-expanded={expanded}
        onClick={onToggle}
      >
        <span className="min-w-0">
          <span className="block truncate text-[length:var(--exits-text-md)] font-semibold">
            {organization.displayName}
          </span>
          <span className="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[length:var(--exits-text-sm)] text-muted">
            <span>{branchCountLabel}</span>
            {membershipLabel ? (
              <>
                <span aria-hidden="true">·</span>
                <span>{membershipLabel}</span>
              </>
            ) : null}
          </span>
        </span>
        {expanded ? (
          <ChevronDown className="size-5 shrink-0 text-muted" aria-hidden="true" />
        ) : (
          <ChevronRight className="size-5 shrink-0 text-muted" aria-hidden="true" />
        )}
      </button>

      {expanded ? (
        <div className="border-t border-border px-4 py-3">
          {!grant && grantLoading ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("workspace.loadingDestinations")}
            </p>
          ) : (
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
                  <DestinationButton
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
                  >
                    {t("workspace.branches")}
                  </h3>
                  <ul className="m-0 flex list-none flex-col gap-3 p-0">
                    {organization.branches.map((branch) => {
                      const branchDestinations = destinations.filter(
                        (d) => d.branchId === branch.branchId,
                      );
                      const staffForBranch =
                        roster?.branchStaff.filter(
                          (person) =>
                            !person.branchName ||
                            person.branchName.localeCompare(branch.name, undefined, {
                              sensitivity: "base",
                            }) === 0,
                        ) ?? [];
                      return (
                        <li key={branch.branchId} className="min-w-0">
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
                            <div className="mt-2 grid grid-cols-1 gap-2 sm:grid-cols-2">
                              {branchDestinations.map((destination) => (
                                <DestinationButton
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
              ) : destinations.length === 0 ? (
                <EmptyState title={t("noLocation.title")} detail={t("noLocation.detail")} />
              ) : null}
            </div>
          )}
        </div>
      ) : null}
    </Card>
  );
}

function DestinationButton({
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
  return (
    <Button
      type="button"
      variant={primary ? "default" : "ghost"}
      className={cn("min-h-11 w-full justify-center")}
      disabled={busy || bindingKey != null}
      onClick={() => onSelect(destination)}
      data-testid={`workspace-destination-${destination.experience}`}
    >
      {busy ? t("workspace.opening") : t(destination.labelKey)}
    </Button>
  );
}
