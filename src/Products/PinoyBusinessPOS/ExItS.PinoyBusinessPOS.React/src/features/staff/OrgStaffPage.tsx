import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { MoreHorizontal, Plus, UserRound } from "lucide-react";
import {
  listOrganizationMembers,
  revokeOrganizationMembership,
  suspendOrganizationMembership,
  type OrganizationMemberWire,
} from "@/api/platform/organization-members-client";
import {
  friendlyPosRoleLabel,
  listProductLocalRoles,
  revokeProductLocalRole,
  type ProductLocalRoleGrantWire,
} from "@/api/platform/product-local-roles-client";
import {
  listOrganizationInvitations,
  revokeStaffInvitation,
  type OrganizationInvitationWire,
} from "@/api/platform/staff-invitation-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { StatusChip } from "@/components/exits/StatusChip";
import { DropdownMenu, MenuItem, useDismissibleOpen } from "@/components/ui/dropdown-menu";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type StaffGrant = {
  id: string;
  roleCode: string;
  mappedPosRoleCode: string;
  roleDisplay: string | null;
};

type StaffRow = {
  membershipId: string;
  userId: string;
  displayName: string;
  email: string | null;
  membershipRole: string;
  membershipStatus: string;
  posGrants: StaffGrant[];
};

type PendingAction =
  | { kind: "suspend"; membershipId: string; name: string }
  | { kind: "remove"; membershipId: string; name: string }
  | { kind: "removePosAccess"; grantId: string; name: string }
  | { kind: "cancelInvite"; invitationId: string; name: string };

function statusTone(status: string): "success" | "warning" | "danger" | "info" {
  const normalized = status.trim().toLowerCase();
  if (normalized === "active") {
    return "success";
  }
  if (normalized === "suspended") {
    return "warning";
  }
  if (normalized === "revoked" || normalized === "removed") {
    return "danger";
  }
  return "info";
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }
  if (parts.length === 1) {
    return parts[0]!.slice(0, 1).toUpperCase();
  }
  return `${parts[0]!.slice(0, 1)}${parts[1]!.slice(0, 1)}`.toUpperCase();
}

export function isOrganizationOwnerMembershipRole(role: string): boolean {
  return (
    role.trim().localeCompare("OrganizationOwner", undefined, { sensitivity: "accent" }) === 0
  );
}

function organizationAccessLabel(
  membershipRole: string,
  t: (key: MessageKey) => string,
): string {
  if (isOrganizationOwnerMembershipRole(membershipRole)) {
    return t("staffManage.organizationOwnerRole");
  }
  if (
    membershipRole.trim().localeCompare("OrganizationAdministrator", undefined, {
      sensitivity: "accent",
    }) === 0
  ) {
    return t("staffManage.organizationAdminRole");
  }
  return t("staffManage.staffMemberRole");
}

function primaryPosGrant(row: StaffRow): StaffGrant | null {
  return row.posGrants[0] ?? null;
}

function staffPosRoleLabel(grant: StaffGrant): string {
  const label = friendlyPosRoleLabel(
    grant.mappedPosRoleCode,
    grant.roleCode,
    grant.roleDisplay,
  );
  const code = (grant.mappedPosRoleCode || grant.roleCode || "").trim().toLowerCase();
  if (
    (code === "owner" || code === "posowner") &&
    label.localeCompare("Owner", undefined, { sensitivity: "accent" }) === 0
  ) {
    return "POS Owner";
  }
  return label;
}

function buildRows(
  members: OrganizationMemberWire[],
  grants: ProductLocalRoleGrantWire[],
): StaffRow[] {
  const grantsByUser = new Map<string, StaffGrant[]>();
  for (const grant of grants) {
    const list = grantsByUser.get(grant.userIdentityId) ?? [];
    list.push({
      id: grant.id,
      roleCode: grant.roleCode,
      mappedPosRoleCode: grant.mappedPosRoleCode,
      roleDisplay: grant.roleDisplay ?? null,
    });
    grantsByUser.set(grant.userIdentityId, list);
  }

  return members.map((member) => {
    const displayName =
      member.displayName?.trim() ||
      member.username?.trim() ||
      member.email?.trim() ||
      "Team member";
    return {
      membershipId: member.id,
      userId: member.userId,
      displayName,
      email: member.email?.trim() || null,
      membershipRole: member.role,
      membershipStatus: member.status,
      posGrants: grantsByUser.get(member.userId) ?? [],
    };
  });
}

function pendingInviteLabel(invitation: OrganizationInvitationWire, fallback: string): string {
  return (
    invitation.inviteeDisplayName?.trim() ||
    invitation.targetPublicUserId?.trim() ||
    invitation.email?.trim() ||
    fallback
  );
}

export function OrgStaffPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { session } = useSession();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const currentUserId = session?.userId ?? null;
  const [actionError, setActionError] = useState<string | null>(null);
  const [pending, setPending] = useState<PendingAction | null>(null);

  const staffQuery = useQuery({
    queryKey: ["org-staff", organizationId],
    enabled: Boolean(organizationId),
    queryFn: async () => {
      if (!organizationId) {
        throw new Error("missing organization");
      }
      const [membersResult, grantsResult] = await Promise.all([
        listOrganizationMembers(organizationId, undefined),
        listProductLocalRoles(organizationId, "Active"),
      ]);
      if (!membersResult.ok) {
        throw new Error(membersResult.body?.detail ?? t("staffManage.loadError"));
      }
      if (!grantsResult.ok) {
        throw new Error(grantsResult.body?.detail ?? t("staffManage.loadError"));
      }
      return buildRows(membersResult.members, grantsResult.grants);
    },
  });

  const pendingInvitesQuery = useQuery({
    queryKey: ["org-staff-pending-invites", organizationId],
    enabled: Boolean(organizationId),
    queryFn: async ({ signal }) => {
      if (!organizationId) {
        throw new Error("missing organization");
      }
      return listOrganizationInvitations({
        organizationId,
        status: "Pending",
        signal,
      });
    },
    meta: { suppressGlobalError: true, operation: "list pending staff invitations" },
  });

  const busyMutation = useMutation({
    mutationFn: async (action: PendingAction) => {
      if (!organizationId) {
        throw new Error(t("staffInvite.noWorkspace"));
      }
      if (action.kind === "suspend") {
        const result = await suspendOrganizationMembership({
          membershipId: action.membershipId,
          reason: "Suspended from POS client",
        });
        if (!result.ok) {
          throw new Error(result.body?.detail ?? t("staffManage.actionError"));
        }
        return;
      }
      if (action.kind === "remove") {
        const result = await revokeOrganizationMembership({
          membershipId: action.membershipId,
          reason: "Removed from POS client",
        });
        if (!result.ok) {
          throw new Error(result.body?.detail ?? t("staffManage.actionError"));
        }
        return;
      }
      if (action.kind === "cancelInvite") {
        if (!online) {
          throw new Error(t("staffInvite.onlineRequired"));
        }
        const result = await revokeStaffInvitation(action.invitationId);
        if (!result.ok) {
          throw new Error(result.body?.detail ?? t("staffManage.actionError"));
        }
        return;
      }
      const result = await revokeProductLocalRole({
        organizationId,
        grantId: action.grantId,
        reason: "Removed POS access from POS client",
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("staffManage.actionError"));
      }
    },
    onSuccess: async (_data, action) => {
      setPending(null);
      setActionError(null);
      await queryClient.invalidateQueries({ queryKey: ["org-staff", organizationId] });
      if (action.kind === "cancelInvite") {
        await queryClient.invalidateQueries({
          queryKey: ["org-staff-pending-invites", organizationId],
        });
      }
    },
    onError: (error: Error) => {
      setActionError(error.message);
      setPending(null);
    },
  });

  const rows = staffQuery.data ?? [];
  const pendingInvites = pendingInvitesQuery.data ?? [];
  const confirmCopy = useMemo(() => {
    if (!pending) {
      return null;
    }
    if (pending.kind === "suspend") {
      return {
        title: t("staffManage.suspendConfirmTitle"),
        detail: t("staffManage.suspendConfirmDetail").replace("{name}", pending.name),
        confirmLabel: t("staffManage.suspend"),
      };
    }
    if (pending.kind === "remove") {
      return {
        title: t("staffManage.removeConfirmTitle"),
        detail: t("staffManage.removeConfirmDetail").replace("{name}", pending.name),
        confirmLabel: t("staffManage.remove"),
      };
    }
    if (pending.kind === "cancelInvite") {
      return {
        title: t("staffManage.cancelInvite"),
        detail: t("staffManage.cancelInviteConfirm").replace("{name}", pending.name),
        confirmLabel: t("staffManage.cancelInvite"),
      };
    }
    if (pending.kind === "removePosAccess") {
      return {
        title: t("staffManage.removePosAccessConfirmTitle"),
        detail: t("staffManage.removePosAccessConfirmDetail").replace("{name}", pending.name),
        confirmLabel: t("staffManage.removePosAccess"),
      };
    }
    return null;
  }, [pending, t]);

  if (!organizationId) {
    return (
      <div
        className="staff-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="org-staff-page"
      >
        <PageHeader
          title={t("staffManage.title")}
          description={t("staffManage.lede")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
        <ErrorState title={t("error.title")} detail={t("staffInvite.noWorkspace")} />
      </div>
    );
  }

  return (
    <div className="staff-page exits-page flex min-w-0 flex-col gap-3" data-testid="org-staff-page">
      <PageHeader
        title={t("staffManage.title")}
        description={t("staffManage.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("staffManage.title")}
        testId="staff-toolbar"
        className="exits-animate-toolbar"
        items={[
          {
            key: "invite",
            label: t("staffInvite.title"),
            icon: <Plus />,
            href: "/org/staff/invite",
            testId: "open-staff-invite",
            emphasis: "primary",
          },
        ]}
      />

      {actionError ? (
        <div className="exits-alert exits-alert--error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{actionError}</p>
        </div>
      ) : null}

      {staffQuery.isLoading ? <LoadingSkeleton count={4} label={t("loading.label")} /> : null}

      {staffQuery.isError ? (
        <section className="catalog-form-section exits-animate-panel gap-3">
          <ErrorState
            title={t("error.title")}
            detail={
              staffQuery.error instanceof Error
                ? staffQuery.error.message
                : t("staffManage.loadError")
            }
          />
          <Button
            type="button"
            variant="outline"
            className="min-h-11 w-full sm:w-auto"
            onClick={() => void staffQuery.refetch()}
          >
            {t("staffManage.retry")}
          </Button>
        </section>
      ) : null}

      {staffQuery.isSuccess && rows.length === 0 ? (
        <section className="catalog-form-section exits-animate-panel" data-testid="org-staff-empty">
          <EmptyState title={t("staffManage.emptyTitle")} detail={t("staffManage.emptyMessage")} />
        </section>
      ) : null}

      {staffQuery.isSuccess && rows.length > 0 ? (
        <ul
          className="exits-list m-0 grid list-none gap-2 p-0"
          data-testid="org-staff-list"
          aria-label={t("staffManage.title")}
        >
          {rows.map((row) => (
            <StaffMemberRow
              key={row.membershipId}
              row={row}
              currentUserId={currentUserId}
              busy={busyMutation.isPending}
              t={t}
              onPending={setPending}
            />
          ))}
        </ul>
      ) : null}

      {pendingInvitesQuery.isSuccess && pendingInvites.length > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel flex flex-col gap-2"
          data-testid="org-staff-pending-invites"
          aria-label={t("staffManage.pendingInvites")}
        >
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("staffManage.pendingInvites")}
          </h2>
          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {pendingInvites.map((invitation) => {
              const name = pendingInviteLabel(invitation, t("staffInvite.thisBusiness"));
              const posRole =
                invitation.productRoleDisplay ??
                invitation.productRole ??
                t("staffInvite.orgRoleStaff");
              return (
                <li key={invitation.id}>
                  <article
                    className="exits-list__card staff-row min-w-0"
                    data-testid={`org-staff-pending-invite-${invitation.id}`}
                  >
                    <span className="staff-row__avatar" aria-hidden>
                      {initials(name)}
                    </span>
                    <div className="staff-row__main min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="exits-list__name m-0 truncate font-semibold">{name}</p>
                        <StatusChip tone="warning">{t("staffManage.invitationPending")}</StatusChip>
                      </div>
                      <p className="mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                        {posRole}
                        {invitation.targetPublicUserId
                          ? ` · ${invitation.targetPublicUserId}`
                          : null}
                      </p>
                      <div className="staff-row__actions">
                        <Button
                          type="button"
                          variant="outline"
                          className="staff-row__action"
                          disabled={busyMutation.isPending || !online}
                          data-testid={`org-staff-cancel-invite-${invitation.id}`}
                          onClick={() =>
                            setPending({
                              kind: "cancelInvite",
                              invitationId: invitation.id,
                              name,
                            })
                          }
                        >
                          {t("staffManage.cancelInvite")}
                        </Button>
                      </div>
                    </div>
                  </article>
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}

      <p className="staff-page__footnote exits-animate-panel m-0 text-[length:var(--exits-text-sm)] text-muted">
        <UserRound className="mr-1.5 inline size-3.5 align-[-0.125rem]" aria-hidden />
        {t("staffManage.footnote")}
      </p>

      <ConfirmationDialog
        open={Boolean(pending && confirmCopy)}
        title={confirmCopy?.title ?? ""}
        detail={confirmCopy?.detail ?? ""}
        confirmLabel={confirmCopy?.confirmLabel ?? t("staffManage.confirm")}
        cancelLabel={t("staffManage.cancel")}
        onCancel={() => setPending(null)}
        onConfirm={() => {
          if (pending) {
            busyMutation.mutate(pending);
          }
        }}
        testId="org-staff-confirm"
      />
    </div>
  );
}

type StaffMemberRowProps = {
  row: StaffRow;
  currentUserId: string | null;
  busy: boolean;
  t: (key: MessageKey) => string;
  onPending: (action: PendingAction) => void;
};

function StaffMemberRow({ row, currentUserId, busy, t, onPending }: StaffMemberRowProps) {
  const menu = useDismissibleOpen(false);
  const isOwner = isOrganizationOwnerMembershipRole(row.membershipRole);
  const isSelf = Boolean(currentUserId && row.userId === currentUserId);
  const isActive =
    row.membershipStatus.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
  const posGrant = primaryPosGrant(row);
  const posRoleLabel = posGrant ? staffPosRoleLabel(posGrant) : null;
  const canManagePosRole = !isOwner && !isSelf && isActive;
  const canMutateMembership = !isOwner && !isSelf && isActive;
  const showPosRoleAction = canManagePosRole;
  const showMoreMenu =
    canManagePosRole && (posGrant !== null || canMutateMembership);

  return (
    <li>
      <article
        className={
          isOwner
            ? "exits-list__card staff-row staff-row--owner min-w-0"
            : "exits-list__card staff-row min-w-0"
        }
        data-testid={`org-staff-row-${row.membershipId}`}
        data-owner-protected={isOwner ? "true" : undefined}
      >
        <span className="staff-row__avatar" aria-hidden>
          {initials(row.displayName)}
        </span>
        <div className="staff-row__main min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <p className="exits-list__name m-0 truncate font-semibold">{row.displayName}</p>
            <StatusChip tone={statusTone(row.membershipStatus)}>{row.membershipStatus}</StatusChip>
          </div>
          {row.email ? (
            <p className="mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
              {row.email}
            </p>
          ) : null}

          <div className="staff-access-grid" data-testid={`org-staff-access-${row.membershipId}`}>
            <div className="staff-access-block">
              <p className="staff-access-block__label m-0">{t("staffManage.organizationAccess")}</p>
              <p className="staff-access-block__value m-0">
                {organizationAccessLabel(row.membershipRole, t)}
              </p>
            </div>
            <div className="staff-access-block">
              <p className="staff-access-block__label m-0">
                {isOwner ? t("staffManage.posAccess") : t("staffManage.posRole")}
              </p>
              <p
                className="staff-access-block__value m-0"
                data-testid={`org-staff-pos-role-${row.membershipId}`}
              >
                {isOwner
                  ? posRoleLabel ?? t("staffManage.posOwnerEquivalent")
                  : posRoleLabel ?? t("staffManage.noPosRoles")}
              </p>
            </div>
          </div>

          {!isActive && !isOwner ? (
            <p className="staff-row__owner-note m-0">{t("staffManage.suspendedRoleHint")}</p>
          ) : null}

          {isOwner ? (
            <p
              className="staff-row__owner-note m-0"
              data-testid={`org-staff-owner-note-${row.membershipId}`}
            >
              {t("staffManage.protectedAccount")}
            </p>
          ) : (
            <div className="staff-row__actions">
              {showPosRoleAction ? (
                <Button asChild variant="outline" className="staff-row__action">
                  <Link
                    to={`/org/staff/assign?userId=${encodeURIComponent(row.userId)}`}
                    data-testid={`org-staff-assign-${row.membershipId}`}
                  >
                    {posGrant ? t("staffManage.changePosRole") : t("staffManage.assignRole")}
                  </Link>
                </Button>
              ) : null}
              {showMoreMenu ? (
                <DropdownMenu
                  open={menu.open}
                  onOpenChange={menu.setOpen}
                  align="end"
                  menuLabel={t("staffManage.moreActions")}
                  trigger={({ id, expanded, controls, onClick, onKeyDown }) => (
                    <Button
                      id={id}
                      type="button"
                      variant="outline"
                      className="staff-row__action min-w-11 px-3"
                      aria-expanded={expanded}
                      aria-controls={controls}
                      aria-label={t("staffManage.moreActions")}
                      disabled={busy}
                      data-testid={`org-staff-more-${row.membershipId}`}
                      onClick={onClick}
                      onKeyDown={onKeyDown}
                    >
                      <MoreHorizontal className="size-4" aria-hidden />
                      <span className="sr-only">{t("staffManage.moreActions")}</span>
                    </Button>
                  )}
                >
                  {posGrant && canManagePosRole ? (
                    <MenuItem
                      data-testid={`org-staff-remove-pos-${row.membershipId}`}
                      onSelect={() => {
                        menu.close();
                        onPending({
                          kind: "removePosAccess",
                          grantId: posGrant.id,
                          name: row.displayName,
                        });
                      }}
                    >
                      {t("staffManage.removePosAccess")}
                    </MenuItem>
                  ) : null}
                  {canMutateMembership ? (
                    <MenuItem
                      data-testid={`org-staff-suspend-${row.membershipId}`}
                      onSelect={() => {
                        menu.close();
                        onPending({
                          kind: "suspend",
                          membershipId: row.membershipId,
                          name: row.displayName,
                        });
                      }}
                    >
                      {t("staffManage.suspend")}
                    </MenuItem>
                  ) : null}
                  {canMutateMembership ? (
                    <MenuItem
                      destructive
                      data-testid={`org-staff-remove-${row.membershipId}`}
                      onSelect={() => {
                        menu.close();
                        onPending({
                          kind: "remove",
                          membershipId: row.membershipId,
                          name: row.displayName,
                        });
                      }}
                    >
                      {t("staffManage.remove")}
                    </MenuItem>
                  ) : null}
                </DropdownMenu>
              ) : null}
            </div>
          )}
        </div>
      </article>
    </li>
  );
}
