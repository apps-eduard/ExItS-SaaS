import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Search } from "lucide-react";
import { canInviteOrganizationStaff } from "@/access/pos-capabilities";
import {
  listMembershipBranchAssignments,
  setMembershipBranchAssignments,
} from "@/api/platform/membership-branch-assignments-client";
import {
  listBranchStaffAccess,
  type BranchStaffAccessItemDto,
} from "@/api/platform/organization-branches-client";
import {
  friendlyMembershipRoleLabel,
  listOrganizationMembers,
  type OrganizationMemberWire,
} from "@/api/platform/organization-members-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";

type BranchStaffAccessPanelProps = {
  organizationId: string;
  branchId: string;
  sessionGrant: PosSessionGrantFacts | null | undefined;
};

export function BranchStaffAccessPanel({
  organizationId,
  branchId,
  sessionGrant,
}: BranchStaffAccessPanelProps) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const canManageStaff = canInviteOrganizationStaff(sessionGrant);
  const [addOpen, setAddOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);

  const staffQuery = useQuery({
    queryKey: ["branch-staff-access", organizationId, branchId],
    enabled: Boolean(organizationId && branchId),
    queryFn: async ({ signal }) => {
      const result = await listBranchStaffAccess(organizationId, branchId, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  const membersQuery = useQuery({
    queryKey: ["org-members-for-branch-access", organizationId],
    enabled: Boolean(organizationId && addOpen && canManageStaff),
    queryFn: async () => {
      const result = await listOrganizationMembers(organizationId, "Active");
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.members;
    },
  });

  const assigned = useMemo(() => staffQuery.data ?? [], [staffQuery.data]);
  const assignedIds = useMemo(
    () => new Set(assigned.map((item) => item.membershipId)),
    [assigned],
  );

  const candidates = useMemo(() => {
    const members = membersQuery.data ?? [];
    const q = search.trim().toLowerCase();
    return members
      .filter((member) => {
        const role = friendlyMembershipRoleLabel(member.role);
        if (role === "Owner" || role === "Admin") {
          return false;
        }
        if (assignedIds.has(member.id)) {
          return false;
        }
        if (!q) {
          return true;
        }
        const haystack = [member.displayName, member.email, member.username, member.roleDisplay]
          .filter(Boolean)
          .join(" ")
          .toLowerCase();
        return haystack.includes(q);
      })
      .slice(0, 20);
  }, [membersQuery.data, search, assignedIds]);

  const addMutation = useMutation({
    mutationFn: async (member: OrganizationMemberWire) => {
      const current = await listMembershipBranchAssignments(organizationId, member.id);
      if (!current.ok) {
        throw new Error(current.body?.detail ?? t("branches.staff.addFailed"));
      }
      const nextIds = [...new Set([...current.value.map((a) => a.branchId), branchId])];
      const result = await setMembershipBranchAssignments(organizationId, member.id, nextIds);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.staff.addFailed"));
      }
      return result.value;
    },
    onSuccess: async () => {
      setActionError(null);
      setAddOpen(false);
      setSearch("");
      await queryClient.invalidateQueries({
        queryKey: ["branch-staff-access", organizationId, branchId],
      });
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("branches.staff.addFailed"));
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (item: BranchStaffAccessItemDto) => {
      const current = await listMembershipBranchAssignments(organizationId, item.membershipId);
      if (!current.ok) {
        throw new Error(current.body?.detail ?? t("branches.staff.removeFailed"));
      }
      const nextIds = current.value.map((a) => a.branchId).filter((id) => id !== branchId);
      if (nextIds.length === 0) {
        throw new Error(t("branches.staff.lastAssignment"));
      }
      const result = await setMembershipBranchAssignments(
        organizationId,
        item.membershipId,
        nextIds,
      );
      if (!result.ok) {
        const detail = (result.body?.detail ?? "").toLowerCase();
        if (detail.includes("at least one")) {
          throw new Error(t("branches.staff.lastAssignment"));
        }
        throw new Error(result.body?.detail ?? t("branches.staff.removeFailed"));
      }
      return result.value;
    },
    onSuccess: async () => {
      setActionError(null);
      await queryClient.invalidateQueries({
        queryKey: ["branch-staff-access", organizationId, branchId],
      });
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("branches.staff.removeFailed"));
    },
  });

  if (staffQuery.isLoading) {
    return <LoadingSkeleton count={3} label={t("loading.label")} />;
  }

  if (staffQuery.isError) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={
          staffQuery.error instanceof Error
            ? staffQuery.error.message
            : t("branches.mgmt.loadError")
        }
      />
    );
  }

  return (
    <div className="flex flex-col gap-3" data-testid="branch-staff-panel">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("branches.staff.lede")}</p>
      <p
        className="m-0 rounded-[var(--exits-radius-md)] border border-[var(--exits-border)] bg-[var(--exits-surface-muted)] p-3 text-[length:var(--exits-text-sm)]"
        data-testid="branch-staff-automatic-note"
      >
        {t("branches.staff.automaticAccess")}
      </p>

      {actionError ? (
        <div className="exits-alert exits-alert--error" role="alert" data-testid="branch-staff-error">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{actionError}</p>
        </div>
      ) : null}

      <div className="flex items-center justify-between gap-2">
        <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.staff.assigned")}
        </h3>
        {canManageStaff ? (
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            data-testid="branch-staff-add"
            onClick={() => {
              setActionError(null);
              setAddOpen(true);
            }}
          >
            <Plus className="size-4" aria-hidden />
            {t("branches.staff.add")}
          </Button>
        ) : null}
      </div>

      {assigned.length === 0 ? (
        <EmptyState title={t("branches.staff.empty")} detail="" />
      ) : (
        <ul className="m-0 grid list-none gap-2 p-0" data-testid="branch-staff-list">
          {assigned.map((item) => (
            <li key={item.membershipId}>
              <article
                className="exits-list__card staff-row min-w-0"
                data-testid={`branch-staff-row-${item.membershipId}`}
              >
                <div className="staff-row__main min-w-0">
                  <p className="exits-list__name m-0 truncate font-semibold">{item.displayName}</p>
                  <div className="staff-row__roles mt-1">
                    <StatusChip tone="info">
                      {item.posRoleDisplay ??
                        friendlyMembershipRoleLabel(item.membershipRole)}
                    </StatusChip>
                    {item.hasOrganizationWideAccess ? (
                      <StatusChip tone="info">{t("branches.mgmt.primary")}</StatusChip>
                    ) : null}
                    {item.membershipStatus.toLowerCase() === "suspended" ? (
                      <StatusChip tone="warning">{t("branches.mgmt.status.suspended")}</StatusChip>
                    ) : null}
                  </div>
                </div>
                {canManageStaff && item.hasExplicitAccess && !item.hasOrganizationWideAccess ? (
                  <div className="staff-row__actions">
                    <Button
                      type="button"
                      variant="ghost"
                      className="staff-row__action"
                      data-testid={`branch-staff-remove-${item.membershipId}`}
                      disabled={removeMutation.isPending}
                      onClick={() => removeMutation.mutate(item)}
                    >
                      {t("branches.staff.remove")}
                    </Button>
                  </div>
                ) : null}
              </article>
            </li>
          ))}
        </ul>
      )}

      <BottomSheet
        open={addOpen}
        onClose={() => setAddOpen(false)}
        panelId="branch-staff-add-panel"
        testId="branch-staff-add-panel"
        title={t("branches.staff.add")}
        closeLabel={t("branches.cancel")}
      >
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.staff.search")}
          <span className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted" />
            <input
              className="catalog-form-select w-full pl-9 font-normal"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              data-testid="branch-staff-search"
              placeholder={t("branches.staff.search")}
            />
          </span>
        </label>

        {membersQuery.isLoading ? <LoadingSkeleton count={3} label={t("loading.label")} /> : null}

        <ul className="m-0 mt-3 grid list-none gap-2 p-0" data-testid="branch-staff-candidates">
          {candidates.map((member) => (
            <li key={member.id}>
              <article className="exits-list__card staff-row min-w-0">
                <div className="staff-row__main min-w-0">
                  <p className="exits-list__name m-0 truncate font-semibold">
                    {member.displayName ?? member.email ?? member.username}
                  </p>
                  <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {member.roleDisplay ?? friendlyMembershipRoleLabel(member.role)}
                  </p>
                  <p className="m-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                    {t("branches.staff.noAccess")}
                  </p>
                </div>
                <div className="staff-row__actions">
                  <Button
                    type="button"
                    className="staff-row__action"
                    data-testid={`branch-staff-candidate-add-${member.id}`}
                    disabled={addMutation.isPending}
                    onClick={() => addMutation.mutate(member)}
                  >
                    {t("branches.staff.add")}
                  </Button>
                </div>
              </article>
            </li>
          ))}
        </ul>
      </BottomSheet>
    </div>
  );
}
