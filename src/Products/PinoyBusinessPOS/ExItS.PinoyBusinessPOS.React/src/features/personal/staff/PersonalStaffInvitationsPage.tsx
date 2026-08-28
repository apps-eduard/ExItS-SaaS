import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import {
  acceptStaffInvitationById,
  declineStaffInvitationById,
  listMyPendingStaffInvitations,
  type AcceptInvitationResultWire,
  type OrganizationInvitationWire,
} from "@/api/platform/staff-invitation-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

export const PERSONAL_STAFF_INVITATIONS_QUERY_KEY = ["personal", "staff-invitations", "pending"] as const;

export function PersonalStaffInvitationsPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const [password, setPassword] = useState("");
  const [acceptForId, setAcceptForId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [success, setSuccess] = useState<AcceptInvitationResultWire | null>(null);

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
    setSuccess(result.result);
    setAcceptForId(null);
    setPassword("");
    await queryClient.invalidateQueries({ queryKey: PERSONAL_STAFF_INVITATIONS_QUERY_KEY });
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

  if (success) {
    return (
      <div
        className="exits-page mx-auto flex w-full max-w-lg flex-col gap-3"
        data-testid="personal-staff-invitations-success"
      >
        <PageHeader
          title={t("staffInvite.personalAcceptedTitle")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
        />
        <p className="m-0">
          {t("staffInvite.personalAcceptedDetail")
            .replace("{org}", success.organizationDisplayName)
            .replace("{login}", success.staffLogin)}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("staffInvite.personalPrivacyNote")}
        </p>
      </div>
    );
  }

  const rows = pendingQuery.data ?? [];

  return (
    <div
      className="exits-page mx-auto flex w-full max-w-lg flex-col gap-3"
      data-testid="personal-staff-invitations-page"
    >
      <PageHeader
        title={t("staffInvite.personalTitle")}
        description={t("staffInvite.personalLede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
      />

      {actionError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {actionError}
        </p>
      ) : null}

      {pendingQuery.isLoading ? <LoadingSkeleton count={2} /> : null}
      {pendingQuery.isError ? (
        <ErrorState title={t("error.title")} detail={t("staffInvite.personalLoadError")} />
      ) : null}

      {pendingQuery.isSuccess && rows.length === 0 ? (
        <EmptyState
          title={t("staffInvite.personalEmptyTitle")}
          detail={t("staffInvite.personalEmptyDetail")}
        />
      ) : null}

      {rows.map((invitation) => {
        const orgName = invitation.organizationDisplayName ?? t("staffInvite.thisBusiness");
        const posRole = invitation.productRoleDisplay ?? invitation.productRole ?? t("staffInvite.orgRoleStaff");
        return (
          <article
            key={invitation.id}
            className="exits-list__card flex flex-col gap-2"
            data-testid={`staff-invitation-card-${invitation.id}`}
          >
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {t("staffInvite.personalJoinTitle").replace("{org}", orgName)}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("staffInvite.personalJoinDetail").replace("{org}", orgName)}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {t("staffInvite.orgRoleStaff")} · {posRole}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("staffInvite.personalPrivacyNote")}
            </p>

            {acceptForId === invitation.id ? (
              <div className="flex flex-col gap-2" data-testid="staff-invitation-accept-confirm">
                <Input
                  label={t("staffInvite.personalPasswordLabel")}
                  name="staff-password"
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                />
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("staffInvite.personalPasswordHint")}
                </p>
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" onClick={() => setAcceptForId(null)}>
                    {t("staffInvite.back")}
                  </Button>
                  <Button
                    type="button"
                    disabled={!online || acceptMutation.isPending || !password.trim()}
                    data-testid="staff-invitation-accept-submit"
                    onClick={() => void onAccept(invitation)}
                  >
                    {acceptMutation.isPending ? (
                      <Loader2 className="size-4 animate-spin" aria-hidden />
                    ) : null}
                    {t("staffInvite.personalAccept")}
                  </Button>
                </div>
              </div>
            ) : (
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={!online || declineMutation.isPending}
                  data-testid={`staff-invitation-decline-${invitation.id}`}
                  onClick={() => void onDecline(invitation.id)}
                >
                  {t("staffInvite.personalDecline")}
                </Button>
                <Button
                  type="button"
                  disabled={!online}
                  data-testid={`staff-invitation-accept-${invitation.id}`}
                  onClick={() => {
                    setAcceptForId(invitation.id);
                    setPassword("");
                    setActionError(null);
                  }}
                >
                  {t("staffInvite.personalAccept")}
                </Button>
              </div>
            )}
          </article>
        );
      })}
    </div>
  );
}
