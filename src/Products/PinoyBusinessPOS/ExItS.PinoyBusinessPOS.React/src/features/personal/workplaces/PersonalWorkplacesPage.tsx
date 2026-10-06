import { useMemo, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BriefcaseBusiness, Building2, Loader2, Users } from "lucide-react";
import {
  acceptStaffInvitationById,
  declineStaffInvitationById,
  listMyPendingStaffInvitations,
  type OrganizationInvitationWire,
} from "@/api/platform/staff-invitation-client";
import {
  completeWorkplacePasswordReset,
  listPersonalWorkplaces,
  type PersonalWorkplaceWire,
} from "@/api/platform/personal-workplaces-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { PERSONAL_STAFF_INVITATIONS_QUERY_KEY } from "@/features/personal/staff/PersonalStaffInvitationsPage";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { WorkplaceSignInDialog } from "@/features/personal/workplaces/WorkplaceSignInDialog";
import { useSession } from "@/session/SessionProvider";

export const PERSONAL_WORKPLACES_QUERY_KEY = ["personal", "workplaces"] as const;

function isActiveMembershipStatus(status: string): boolean {
  return status.trim().localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

function membershipStatusTone(status: string): "success" | "warning" | "danger" | "info" {
  const normalized = status.trim().toLowerCase();
  if (normalized === "active") {
    return "success";
  }
  if (normalized === "suspended") {
    return "warning";
  }
  return "info";
}

function membershipStatusLabel(status: string, t: (key: MessageKey) => string): string {
  if (isActiveMembershipStatus(status)) {
    return t("personal.workplaces.statusActive");
  }
  if (status.trim().localeCompare("Suspended", undefined, { sensitivity: "accent" }) === 0) {
    return t("personal.workplaces.statusSuspended");
  }
  return status;
}

function roleLabel(workplace: PersonalWorkplaceWire, t: (key: MessageKey) => string): string {
  return (
    workplace.productRoleDisplay?.trim() ||
    workplace.membershipRoleDisplay?.trim() ||
    t("personal.workplaces.roleUnknown")
  );
}

function branchLabel(workplace: PersonalWorkplaceWire, t: (key: MessageKey) => string): string {
  if (workplace.branches.length === 0) {
    return t("personal.workplaces.branchUnknown");
  }
  if (workplace.branches.length === 1) {
    return workplace.branches[0]!.name;
  }
  return workplace.branches.map((branch) => branch.name).join(", ");
}

export function PersonalWorkplacesPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { session } = useSession();
  const personalEmail = session?.email?.trim() || null;

  const [password, setPassword] = useState("");
  const [acceptForId, setAcceptForId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const routedSignInError =
    (location.state as { workplaceSignInError?: string } | null)?.workplaceSignInError ?? null;
  const visibleActionError = actionError ?? routedSignInError;
  const [notice, setNotice] = useState<string | null>(null);
  const [signInWorkplace, setSignInWorkplace] = useState<PersonalWorkplaceWire | null>(null);
  const [newPassword, setNewPassword] = useState<Record<string, string>>({});
  const [confirmPassword, setConfirmPassword] = useState<Record<string, string>>({});
  const [resettingId, setResettingId] = useState<string | null>(null);
  const [accepted, setAccepted] = useState<{
    organizationDisplayName: string;
    staffLogin: string;
    productRoleDisplay: string | null;
    organizationId: string;
  } | null>(null);

  const workplacesQuery = useQuery({
    queryKey: PERSONAL_WORKPLACES_QUERY_KEY,
    queryFn: async ({ signal }) => {
      const result = await listPersonalWorkplaces(signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("personal.workplaces.loadError"));
      }
      return result.workplaces;
    },
    meta: { suppressGlobalError: true, operation: "list personal workplaces" },
  });

  const pendingQuery = useQuery({
    queryKey: PERSONAL_STAFF_INVITATIONS_QUERY_KEY,
    queryFn: ({ signal }) => listMyPendingStaffInvitations(signal),
    meta: { suppressGlobalError: true, operation: "list personal staff invitations" },
  });

  const acceptMutation = useMutation({
    mutationFn: (invitation: OrganizationInvitationWire) =>
      acceptStaffInvitationById({ invitationId: invitation.id, password }),
  });

  const declineMutation = useMutation({
    mutationFn: (invitationId: string) => declineStaffInvitationById(invitationId),
  });

  const workplaces = workplacesQuery.data ?? [];
  const pending = pendingQuery.data ?? [];

  const acceptedWorkplace = useMemo(() => {
    if (!accepted) {
      return null;
    }
    return (
      workplaces.find((item) => item.organizationId === accepted.organizationId) ?? null
    );
  }, [accepted, workplaces]);

  async function onAccept(invitation: OrganizationInvitationWire) {
    if (!online || !password.trim()) {
      setActionError(t("staffInvite.personalPasswordRequired"));
      return;
    }
    setActionError(null);
    const result = await acceptMutation.mutateAsync(invitation);
    if (!result.ok) {
      setActionError(result.body?.detail ?? t("staffInvite.personalAcceptFailed"));
      return;
    }
    setAccepted({
      organizationDisplayName: result.result.organizationDisplayName,
      staffLogin: result.result.staffLogin,
      productRoleDisplay: invitation.productRoleDisplay ?? invitation.productRole ?? null,
      organizationId: result.result.organizationId,
    });
    setAcceptForId(null);
    setPassword("");
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: PERSONAL_STAFF_INVITATIONS_QUERY_KEY }),
      queryClient.invalidateQueries({ queryKey: PERSONAL_WORKPLACES_QUERY_KEY }),
    ]);
  }

  async function onDecline(invitationId: string) {
    if (!online) {
      setActionError(t("staffInvite.onlineRequired"));
      return;
    }
    setActionError(null);
    const result = await declineMutation.mutateAsync(invitationId);
    if (!result.ok) {
      setActionError(result.body?.detail ?? t("staffInvite.personalDeclineFailed"));
      return;
    }
    setAcceptForId(null);
    await queryClient.invalidateQueries({ queryKey: PERSONAL_STAFF_INVITATIONS_QUERY_KEY });
  }

  async function saveResetPassword(workplace: PersonalWorkplaceWire) {
    const requestId = workplace.passwordResetRequestId;
    const next = newPassword[workplace.membershipId] ?? "";
    const confirm = confirmPassword[workplace.membershipId] ?? "";
    if (!requestId || !online) {
      return;
    }
    if (next !== confirm) {
      setActionError(t("personal.workplaces.passwordMismatch"));
      return;
    }
    setActionError(null);
    setResettingId(workplace.membershipId);
    const result = await completeWorkplacePasswordReset(workplace.membershipId, requestId, next);
    setResettingId(null);
    if (!result.ok) {
      setActionError(result.body?.detail ?? t("personal.workplaces.resetApproved"));
      return;
    }
    setNewPassword((current) => ({ ...current, [workplace.membershipId]: "" }));
    setConfirmPassword((current) => ({ ...current, [workplace.membershipId]: "" }));
    setNotice(t("personal.workplaces.resetSaved"));
    await queryClient.invalidateQueries({ queryKey: PERSONAL_WORKPLACES_QUERY_KEY });
  }

  if (accepted) {
    const role =
      acceptedWorkplace?.productRoleDisplay?.trim() ||
      accepted.productRoleDisplay?.trim() ||
      t("personal.workplaces.roleUnknown");
    const branch =
      acceptedWorkplace && acceptedWorkplace.branches.length > 0
        ? branchLabel(acceptedWorkplace, t)
        : t("personal.workplaces.branchPending");

    return (
      <div
        className="personal-page exits-page flex w-full min-w-0 flex-col gap-3"
        data-testid="personal-workplaces-accept-success"
      >
        <PageHeader
          title={t("personal.workplaces.acceptedTitle")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
        />
        <p className="m-0 font-semibold" data-testid="personal-workplaces-accepted-lede">
          {t("personal.workplaces.acceptedLede").replace("{org}", accepted.organizationDisplayName)}
        </p>
        <div className="catalog-form-section flex flex-col gap-2">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.workplaces.workLogin")}
          </p>
          <p className="m-0 font-semibold" data-testid="personal-workplaces-accepted-login">
            {accepted.staffLogin}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("personal.workplaces.role")}: {role}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("personal.workplaces.branch")}: {branch}
          </p>
          {personalEmail ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("personal.workplaces.personalAccount")}: {personalEmail}
            </p>
          ) : null}
        </div>
        <div className="flex flex-col gap-2">
          <div className="flex flex-col gap-2 sm:flex-row">
            <Button
              type="button"
              className="w-full"
              disabled={!online}
              data-testid="personal-workplaces-accepted-open"
              onClick={() =>
                setSignInWorkplace({
                  organizationId: accepted.organizationId,
                  organizationDisplayName: accepted.organizationDisplayName,
                  publicOrganizationId: acceptedWorkplace?.publicOrganizationId ?? null,
                  staffUserId: acceptedWorkplace?.staffUserId ?? "",
                  staffLogin: accepted.staffLogin,
                  membershipId: acceptedWorkplace?.membershipId ?? accepted.organizationId,
                  membershipRole: acceptedWorkplace?.membershipRole ?? "OrganizationMember",
                  membershipRoleDisplay: role,
                  membershipStatus: "Active",
                  productRole: acceptedWorkplace?.productRole ?? null,
                  productRoleDisplay: accepted.productRoleDisplay,
                  branches: acceptedWorkplace?.branches ?? [],
                  passwordResetStatus: acceptedWorkplace?.passwordResetStatus ?? null,
                  passwordResetRequestId: acceptedWorkplace?.passwordResetRequestId ?? null,
                })
              }
            >
              {t("personal.workplaces.login")}
            </Button>
            <Button
              type="button"
              variant="outline"
              className="w-full"
              data-testid="personal-workplaces-accepted-view"
              onClick={() => setAccepted(null)}
            >
              {t("personal.workplaces.viewMine")}
            </Button>
          </div>
        </div>
      <WorkplaceSignInDialog
        workplace={signInWorkplace}
        open={signInWorkplace !== null}
        returnPath="/personal/workplaces"
        onClose={() => setSignInWorkplace(null)}
      />
    </div>
  );
  }

  return (
    <div
      className="personal-page exits-page flex w-full min-w-0 flex-col gap-3"
      data-testid="personal-workplaces-page"
    >
      <PageHeader
        title={t("personal.workplaces.title")}
        description={t("personal.workplaces.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
      />

      {personalEmail ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-[length:var(--exits-text-sm)]"
          data-testid="personal-workplaces-personal-email"
        >
          <span className="text-muted">{t("personal.workplaces.personalAccount")}: </span>
          <span className="font-semibold">{personalEmail}</span>
        </p>
      ) : null}

      {visibleActionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {visibleActionError}
        </p>
      ) : null}
      {notice ? (
        <p className="m-0 text-[length:var(--exits-text-sm)]" role="status">
          {notice}
        </p>
      ) : null}

      <section
        className="catalog-form-section exits-animate-panel flex flex-col gap-2"
        data-testid="personal-workplaces-pending"
        aria-labelledby="personal-workplaces-pending-heading"
      >
        <h2 id="personal-workplaces-pending-heading" className="catalog-form-section__title">
          {t("personal.workplaces.pendingSection")}
        </h2>
        {pendingQuery.isLoading ? <LoadingSkeleton count={1} label={t("loading.label")} /> : null}
        {pendingQuery.isError ? (
          <ErrorState
            title={t("error.title")}
            detail={
              pendingQuery.error instanceof Error
                ? pendingQuery.error.message
                : t("staffInvite.personalLoadError")
            }
          />
        ) : null}
        {pendingQuery.isSuccess && pending.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.workplaces.pendingEmpty")}
          </p>
        ) : null}
        {pending.map((invitation) => {
          const orgName =
            invitation.organizationDisplayName?.trim() || t("staffInvite.thisBusiness");
          const accepting = acceptForId === invitation.id;
          return (
            <article
              key={invitation.id}
              className="exits-list__card flex flex-col gap-2"
              data-testid={`personal-workplaces-pending-${invitation.id}`}
            >
              <div className="flex items-start gap-2">
                <Building2 className="mt-0.5 size-4 shrink-0 text-muted" aria-hidden />
                <div className="min-w-0">
                  <p className="m-0 font-semibold">{orgName}</p>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {invitation.productRoleDisplay ??
                      invitation.productRole ??
                      t("staffInvite.orgRoleStaff")}
                  </p>
                </div>
              </div>
              {accepting ? (
                <div className="flex flex-col gap-2">
                  <Input
                    label={t("staffInvite.personalPasswordLabel")}
                    type="password"
                    autoComplete="new-password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    placeholder={t("staffInvite.personalPasswordLabel")}
                    data-testid={`personal-workplaces-password-${invitation.id}`}
                  />
                  <div className="flex gap-2">
                    <Button
                      type="button"
                      className="flex-1"
                      disabled={acceptMutation.isPending || !online}
                      data-testid={`personal-workplaces-accept-${invitation.id}`}
                      onClick={() => void onAccept(invitation)}
                    >
                      {acceptMutation.isPending ? (
                        <Loader2 className="size-4 animate-spin" aria-hidden />
                      ) : null}
                      {t("staffInvite.personalAccept")}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      onClick={() => {
                        setAcceptForId(null);
                        setPassword("");
                      }}
                    >
                      {t("staffInvite.cancel")}
                    </Button>
                  </div>
                </div>
              ) : (
                <div className="flex gap-2">
                  <Button
                    type="button"
                    className="flex-1"
                    disabled={!online}
                    data-testid={`personal-workplaces-start-accept-${invitation.id}`}
                    onClick={() => {
                      setAcceptForId(invitation.id);
                      setActionError(null);
                    }}
                  >
                    {t("staffInvite.personalAccept")}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    disabled={declineMutation.isPending || !online}
                    data-testid={`personal-workplaces-decline-${invitation.id}`}
                    onClick={() => void onDecline(invitation.id)}
                  >
                    {t("staffInvite.personalDecline")}
                  </Button>
                </div>
              )}
            </article>
          );
        })}
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          <Link to="/personal/staff-invitations" className="font-semibold text-primary underline">
            {t("personal.workplaces.openInvitations")}
          </Link>
        </p>
      </section>

      <section
        className="catalog-form-section exits-animate-panel flex flex-col gap-2"
        data-testid="personal-workplaces-list"
        aria-labelledby="personal-workplaces-list-heading"
      >
        <h2 id="personal-workplaces-list-heading" className="catalog-form-section__title">
          {t("personal.workplaces.mineSection")}
        </h2>
        {workplacesQuery.isLoading ? <LoadingSkeleton count={2} label={t("loading.label")} /> : null}
        {workplacesQuery.isError ? (
          <ErrorState
            title={t("error.title")}
            detail={
              workplacesQuery.error instanceof Error
                ? workplacesQuery.error.message
                : t("personal.workplaces.loadError")
            }
          />
        ) : null}
        {workplacesQuery.isSuccess && workplaces.length === 0 ? (
          <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("personal.workplaces.emptyTitle")}
            detail={t("personal.workplaces.emptyDetail")}
          />
        ) : null}
        {workplaces.map((workplace) => (
          <article
            key={workplace.membershipId}
            className="exits-list__card flex flex-col gap-3"
            data-testid={`personal-workplace-${workplace.membershipId}`}
          >
            <div className="flex items-start gap-2">
              <BriefcaseBusiness className="mt-0.5 size-4 shrink-0 text-muted" aria-hidden />
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="m-0 font-semibold">{workplace.organizationDisplayName}</p>
                  <StatusChip tone={membershipStatusTone(workplace.membershipStatus)}>
                    {membershipStatusLabel(workplace.membershipStatus, t)}
                  </StatusChip>
                </div>
                <p className="m-0 mt-1 text-[length:var(--exits-text-sm)]">
                  {t("personal.workplaces.role")}: {roleLabel(workplace, t)}
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {t("personal.workplaces.branch")}: {branchLabel(workplace, t)}
                </p>
              </div>
            </div>

            <div
              className="rounded-[var(--exits-radius-md)] border border-border bg-[color-mix(in_srgb,var(--exits-surface-muted)_70%,transparent)] px-3 py-2"
              data-testid={`personal-workplace-login-${workplace.membershipId}`}
            >
              <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
                {t("personal.workplaces.workLogin")}
              </p>
              <p className="m-0 mt-1 font-semibold">{workplace.staffLogin}</p>
              {personalEmail ? (
                <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                  {t("personal.workplaces.personalAccount")}: {personalEmail}
                </p>
              ) : null}
            </div>

            {isActiveMembershipStatus(workplace.membershipStatus) ? (
              <WorkplaceAccess
                workplace={workplace}
                online={online}
                resetting={resettingId === workplace.membershipId}
                newPassword={newPassword[workplace.membershipId] ?? ""}
                confirmPassword={confirmPassword[workplace.membershipId] ?? ""}
                onNewPassword={(value) =>
                  setNewPassword((current) => ({ ...current, [workplace.membershipId]: value }))
                }
                onConfirmPassword={(value) =>
                  setConfirmPassword((current) => ({ ...current, [workplace.membershipId]: value }))
                }
                onLogin={() => setSignInWorkplace(workplace)}
                onSavePassword={() => void saveResetPassword(workplace)}
              />
            ) : null}
          </article>
        ))}
      </section>
      <WorkplaceSignInDialog
        workplace={signInWorkplace}
        open={signInWorkplace !== null}
        returnPath="/personal/workplaces"
        onClose={() => setSignInWorkplace(null)}
      />
    </div>
  );
}

function WorkplaceAccess({
  workplace,
  online,
  resetting,
  newPassword,
  confirmPassword,
  onNewPassword,
  onConfirmPassword,
  onLogin,
  onSavePassword,
}: {
  workplace: PersonalWorkplaceWire;
  online: boolean;
  resetting: boolean;
  newPassword: string;
  confirmPassword: string;
  onNewPassword: (value: string) => void;
  onConfirmPassword: (value: string) => void;
  onLogin: () => void;
  onSavePassword: () => void;
}) {
  const { t } = useI18n();
  const resetStatus = workplace.passwordResetStatus;
  const approved = resetStatus === "Approved";
  const pending = resetStatus === "Pending";

  return (
    <div className="flex flex-col gap-2">
      {pending ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.workplaces.resetRequested")}
        </p>
      ) : null}
      {approved ? (
        <>
          <p className="m-0 text-[length:var(--exits-text-sm)]">{t("personal.workplaces.resetApproved")}</p>
          <Input
            label={t("personal.workplaces.newPassword")}
            type="password"
            autoComplete="new-password"
            value={newPassword}
            onChange={(event) => onNewPassword(event.target.value)}
            data-testid={`personal-workplace-new-password-${workplace.membershipId}`}
          />
          <Input
            label={t("personal.workplaces.confirmPassword")}
            type="password"
            autoComplete="new-password"
            value={confirmPassword}
            onChange={(event) => onConfirmPassword(event.target.value)}
            data-testid={`personal-workplace-confirm-password-${workplace.membershipId}`}
          />
          <Button
            type="button"
            disabled={resetting || !online || !newPassword.trim()}
            data-testid={`personal-workplace-save-password-${workplace.membershipId}`}
            onClick={onSavePassword}
          >
            {t("personal.workplaces.savePassword")}
          </Button>
        </>
      ) : (
        <Button
          type="button"
          className="w-fit"
          disabled={!online}
          data-testid={`personal-workplace-sign-in-${workplace.membershipId}`}
          onClick={onLogin}
        >
          {t("personal.workplaces.login")}
        </Button>
      )}
    </div>
  );
}
