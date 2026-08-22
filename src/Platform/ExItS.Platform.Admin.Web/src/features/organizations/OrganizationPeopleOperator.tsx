import { useState } from "react";
import { MailPlus, MoreHorizontal } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import {
  ORGANIZATION_MEMBER_ROLES,
  type OrganizationInvitation,
  type OrganizationMember,
} from "@/api/organizations/organization-types";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import {
  DropdownMenu,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  buildCreateInvitationBody,
  type InviteMemberFormValues,
} from "@/features/organizations/organization-admin-mapping";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import {
  useChangeMembershipRoleMutation,
  useCreateInvitationMutation,
  useReactivateMembershipMutation,
  useResendInvitationMutation,
  useRevokeInvitationMutation,
  useRevokeMembershipMutation,
  useSuspendMembershipMutation,
} from "@/features/organizations/use-organization-mutations";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

const EMPTY_INVITE: InviteMemberFormValues = {
  email: "",
  role: "OrganizationMember",
  firstName: "",
  lastName: "",
  displayName: "",
  phone: "",
  employeeCode: "",
  branch: "",
};

export function OrganizationPeopleInviteButton({ organizationId }: { organizationId: string }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageMemberships);
  const inviteMutation = useCreateInvitationMutation();
  const [open, setOpen] = useState(false);
  const [values, setValues] = useState<InviteMemberFormValues>(EMPTY_INVITE);
  const [error, setError] = useState<{ title: string; detail: string } | null>(null);

  if (!canManage) {
    return null;
  }

  async function submit() {
    if (inviteMutation.isPending) {
      return;
    }
    setError(null);
    try {
      await inviteMutation.mutateAsync({
        organizationId,
        body: buildCreateInvitationBody(values),
      });
      setOpen(false);
      setValues(EMPTY_INVITE);
    } catch (mutationError) {
      setError(organizationMutationFailureCopy(mutationError, t));
    }
  }

  return (
    <>
      <Button type="button" size="sm" onClick={() => setOpen(true)}>
        <MailPlus aria-hidden className="mr-2 size-4" />
        {t("organization.people.invite.action")}
      </Button>
      {open ? (
        <ConfirmActionDialog
          open
          title={t("organization.people.invite.title")}
          description={t("organization.people.invite.description")}
          confirmLabel={t("organization.people.invite.confirm")}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          pending={inviteMutation.isPending}
          confirmDisabled={values.email.trim().length === 0}
          error={
            error ? (
              <Alert title={error.title} tone="danger">
                {error.detail}
              </Alert>
            ) : null
          }
          onCancel={() => {
            inviteMutation.reset();
            setOpen(false);
            setError(null);
          }}
          onConfirm={() => void submit()}
        >
          <div className="grid gap-2">
            <div className="grid gap-1">
              <Label htmlFor="invite-email">{t("organization.people.column.contact")}</Label>
              <Input
                id="invite-email"
                type="email"
                value={values.email}
                onChange={(event) => setValues((current) => ({ ...current, email: event.target.value }))}
              />
            </div>
            <div className="grid gap-1">
              <Label htmlFor="invite-role">{t("organization.people.column.role")}</Label>
              <select
                id="invite-role"
                className={controlClass}
                value={values.role}
                onChange={(event) => setValues((current) => ({ ...current, role: event.target.value }))}
              >
                {ORGANIZATION_MEMBER_ROLES.map((role) => (
                  <option key={role} value={role}>
                    {t(
                      role === "OrganizationOwner"
                        ? "organization.people.role.owner"
                        : "organization.people.role.member",
                    )}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="grid gap-1">
                <Label htmlFor="invite-first">{t("organization.people.invite.firstName")}</Label>
                <Input
                  id="invite-first"
                  value={values.firstName}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, firstName: event.target.value }))
                  }
                />
              </div>
              <div className="grid gap-1">
                <Label htmlFor="invite-last">{t("organization.people.invite.lastName")}</Label>
                <Input
                  id="invite-last"
                  value={values.lastName}
                  onChange={(event) =>
                    setValues((current) => ({ ...current, lastName: event.target.value }))
                  }
                />
              </div>
            </div>
            <div className="grid gap-1">
              <Label htmlFor="invite-branch">{t("organization.people.invite.branch")}</Label>
              <Input
                id="invite-branch"
                value={values.branch}
                onChange={(event) => setValues((current) => ({ ...current, branch: event.target.value }))}
              />
            </div>
          </div>
        </ConfirmActionDialog>
      ) : null}
    </>
  );
}

function actorReference(sessionEmail: string | undefined): string {
  return sessionEmail && sessionEmail.length > 0 ? sessionEmail : "platform-admin";
}

export function OrganizationMemberActions({
  organizationId,
  member,
}: {
  organizationId: string;
  member: OrganizationMember;
}) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const { session } = useSession();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageMemberships);
  const changeRole = useChangeMembershipRoleMutation();
  const suspend = useSuspendMembershipMutation();
  const reactivate = useReactivateMembershipMutation();
  const revoke = useRevokeMembershipMutation();
  const [confirm, setConfirm] = useState<"suspend" | "revoke" | null>(null);
  const actor = actorReference(session?.email);

  if (!canManage || member.status === "Removed") {
    return null;
  }

  const pending =
    changeRole.isPending || suspend.isPending || reactivate.isPending || revoke.isPending;

  async function runConfirm() {
    if (!confirm || pending) {
      return;
    }
    const body = { actorReference: actor };
    if (confirm === "suspend") {
      await suspend.mutateAsync({ organizationId, membershipId: member.id, body });
    } else {
      await revoke.mutateAsync({ organizationId, membershipId: member.id, body });
    }
    setConfirm(null);
  }

  return (
    <>
      <DropdownMenu
        label={t("organization.people.actions.member")}
        trigger={
          <Button type="button" size="sm" variant="ghost" aria-label={t("organization.people.actions.member")}>
            <MoreHorizontal aria-hidden className="size-4" />
          </Button>
        }
      >
        {member.status === "Active" ? (
          <>
            <DropdownMenuItem
              onSelect={() =>
                void changeRole.mutateAsync({
                  organizationId,
                  membershipId: member.id,
                  body: {
                    role:
                      member.role === "OrganizationOwner"
                        ? "OrganizationMember"
                        : "OrganizationOwner",
                    actorReference: actor,
                  },
                })
              }
            >
              {t("organization.people.actions.changeRole")}
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setConfirm("suspend")}>
              {t("organization.people.actions.suspend")}
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setConfirm("revoke")}>
              {t("organization.people.actions.revoke")}
            </DropdownMenuItem>
          </>
        ) : null}
        {member.status === "Suspended" ? (
          <>
            <DropdownMenuItem
              onSelect={() =>
                void reactivate.mutateAsync({
                  organizationId,
                  membershipId: member.id,
                  body: { actorReference: actor },
                })
              }
            >
              {t("organization.people.actions.reactivate")}
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => setConfirm("revoke")}>
              {t("organization.people.actions.revoke")}
            </DropdownMenuItem>
          </>
        ) : null}
      </DropdownMenu>
      {confirm ? (
        <ConfirmActionDialog
          open
          title={t(
            confirm === "suspend"
              ? "organization.people.suspend.title"
              : "organization.people.revoke.title",
          )}
          description={t(
            confirm === "suspend"
              ? "organization.people.suspend.description"
              : "organization.people.revoke.description",
          )}
          confirmLabel={t(
            confirm === "suspend"
              ? "organization.people.suspend.confirm"
              : "organization.people.revoke.confirm",
          )}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          destructive
          pending={pending}
          onCancel={() => setConfirm(null)}
          onConfirm={() => void runConfirm()}
        />
      ) : null}
    </>
  );
}

export function OrganizationInvitationActions({
  organizationId,
  invitation,
}: {
  organizationId: string;
  invitation: OrganizationInvitation;
}) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const canManage = authorization.hasPermission(PLATFORM_PERMISSIONS.manageMemberships);
  const resend = useResendInvitationMutation();
  const revokeInvite = useRevokeInvitationMutation();
  const [confirmRevoke, setConfirmRevoke] = useState(false);
  const display = invitation.invitationStatus || invitation.status;
  const actionable = display === "Pending" || display === "Sent" || display === "Expired";

  if (!canManage || !actionable) {
    return null;
  }

  return (
    <>
      <DropdownMenu
        label={t("organization.people.actions.invitation")}
        trigger={
          <Button
            type="button"
            size="sm"
            variant="ghost"
            aria-label={t("organization.people.actions.invitation")}
          >
            <MoreHorizontal aria-hidden className="size-4" />
          </Button>
        }
      >
        <DropdownMenuItem
          onSelect={() =>
            void resend.mutateAsync({ organizationId, invitationId: invitation.id })
          }
        >
          {t("organization.people.actions.resend")}
        </DropdownMenuItem>
        <DropdownMenuItem onSelect={() => setConfirmRevoke(true)}>
          {t("organization.people.actions.revokeInvite")}
        </DropdownMenuItem>
      </DropdownMenu>
      {confirmRevoke ? (
        <ConfirmActionDialog
          open
          title={t("organization.people.revokeInvite.title")}
          description={t("organization.people.revokeInvite.description")}
          confirmLabel={t("organization.people.revokeInvite.confirm")}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          destructive
          pending={revokeInvite.isPending}
          onCancel={() => setConfirmRevoke(false)}
          onConfirm={() => {
            void revokeInvite
              .mutateAsync({ organizationId, invitationId: invitation.id })
              .finally(() => setConfirmRevoke(false));
          }}
        />
      ) : null}
    </>
  );
}
