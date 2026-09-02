import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BriefcaseBusiness, Building2, Copy, Loader2 } from "lucide-react";
import {
  acceptStaffInvitationById,
  declineStaffInvitationById,
  listMyPendingStaffInvitations,
  type OrganizationInvitationWire,
} from "@/api/platform/staff-invitation-client";
import {
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
import { copyTextToClipboard } from "@/diagnostics/copy-text-to-clipboard";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { PERSONAL_STAFF_INVITATIONS_QUERY_KEY } from "@/features/personal/staff/PersonalStaffInvitationsPage";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { personalPageBackNav } from "@/navigation/page-back-nav";
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
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { session, signOut } = useSession();
  const personalEmail = session?.email?.trim() || null;

  const [password, setPassword] = useState("");
  const [acceptForId, setAcceptForId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
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

  async function copyLogin(staffLogin: string, membershipId: string) {
    const ok = await copyTextToClipboard(staffLogin);
    if (!ok) {
      setActionError(t("personal.workplaces.copyFailed"));
      return;
    }
    setCopiedId(membershipId);
    window.setTimeout(() => setCopiedId((current) => (current === membershipId ? null : current)), 1600);
  }

  async function openWorkplace(staffLogin: string) {
    await signOut();
    navigate("/sign-in", {
      replace: true,
      state: { staffLoginHint: staffLogin },
    });
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
        className="personal-page exits-page mx-auto flex w-full max-w-lg flex-col gap-3"
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
        <div className="flex flex-col gap-2 sm:flex-row">
          <Button
            type="button"
            className="min-h-11 w-full"
            data-testid="personal-workplaces-accepted-open"
            onClick={() => void openWorkplace(accepted.staffLogin)}
          >
            {t("personal.workplaces.openNamed").replace("{org}", accepted.organizationDisplayName)}
          </Button>
          <Button
            type="button"
            variant="outline"
            className="min-h-11 w-full"
            data-testid="personal-workplaces-accepted-view"
            onClick={() => setAccepted(null)}
          >
            {t("personal.workplaces.viewMine")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div
      className="personal-page exits-page mx-auto flex w-full max-w-lg flex-col gap-3"
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

      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {actionError}
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
                      className="min-h-11 flex-1"
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
                      className="min-h-11"
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
                    className="min-h-11 flex-1"
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
                    className="min-h-11"
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

            <div className="flex gap-2">
              <Button
                type="button"
                className="min-h-11 flex-1"
                disabled={!isActiveMembershipStatus(workplace.membershipStatus)}
                data-testid={`personal-workplace-open-${workplace.membershipId}`}
                onClick={() => void openWorkplace(workplace.staffLogin)}
              >
                {t("personal.workplaces.open")}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                data-testid={`personal-workplace-copy-${workplace.membershipId}`}
                onClick={() => void copyLogin(workplace.staffLogin, workplace.membershipId)}
              >
                <Copy className="size-4" aria-hidden />
                <span className="sr-only">{t("personal.workplaces.copyLogin")}</span>
                {copiedId === workplace.membershipId
                  ? t("personal.workplaces.copied")
                  : t("personal.workplaces.copyLogin")}
              </Button>
            </div>
          </article>
        ))}
      </section>
    </div>
  );
}
