import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ApiClientError } from "@/api/http";
import { useSession } from "@/auth/SessionProvider";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/ui/skeleton";
import {
  useAcceptInvitationMutation,
  useDeclineInvitationMutation,
  usePersonalContactsQuery,
  usePersonalInvitationsQuery,
  useResendInvitationMutation,
  useRevokeInvitationMutation,
} from "@/features/personal/people-queries";
import { formatShortDate, isPendingInvitation } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

export function InvitationsPage() {
  const { t } = useI18n();
  const { session } = useSession();
  const { preferences } = usePreferences();
  const [searchParams] = useSearchParams();
  const tokenFromLink = searchParams.get("token")?.trim() ?? "";
  const [token, setToken] = useState(tokenFromLink);
  const [actionError, setActionError] = useState<string | null>(null);
  const invitationsQuery = usePersonalInvitationsQuery();
  const contactsQuery = usePersonalContactsQuery();
  const acceptMutation = useAcceptInvitationMutation();
  const declineMutation = useDeclineInvitationMutation();
  const revokeMutation = useRevokeInvitationMutation();
  const resendMutation = useResendInvitationMutation();

  const { received, sent } = useMemo(() => {
    const list = invitationsQuery.data ?? [];
    const myId = session?.userId ?? "";
    const pending = list.filter(isPendingInvitation);
    return {
      received: pending.filter((item) => item.invitedByUserIdentityId !== myId),
      sent: pending.filter((item) => item.invitedByUserIdentityId === myId),
    };
  }, [invitationsQuery.data, session?.userId]);

  const contactName = (contactId: string) =>
    contactsQuery.data?.find((c) => c.id === contactId)?.displayName ?? t("invitations.someone");

  async function onAccept() {
    setActionError(null);
    if (!token.trim()) {
      setActionError(t("invitations.tokenRequired"));
      return;
    }
    try {
      await acceptMutation.mutateAsync(token.trim());
      setToken("");
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  async function onDecline() {
    setActionError(null);
    if (!token.trim()) {
      setActionError(t("invitations.tokenRequired"));
      return;
    }
    try {
      await declineMutation.mutateAsync(token.trim());
      setToken("");
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  if (invitationsQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (invitationsQuery.error) {
    const err = invitationsQuery.error;
    return (
      <ErrorState
        title={t("error.title")}
        body={err instanceof ApiClientError ? err.message : t("error.body")}
        record={normalizeDiagnosticError(err, {
          locale: preferences.locale,
          theme: preferences.theme,
          pathname: "/personal/invitations",
        })}
      />
    );
  }

  return (
    <section className="flex flex-col gap-4">
      <PageHeader title={t("invitations.title")} />

      <div className="flex flex-col gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">{t("invitations.received")}</h2>
        {received.length === 0 ? (
          <EmptyState title={t("invitations.emptyTitle")} body={t("invitations.emptyBody")} />
        ) : (
          received.map((invite) => (
            <Card key={invite.id} className="flex flex-col gap-2">
              <p className="m-0 font-semibold">{contactName(invite.inviteeContactId)}</p>
              <p className="m-0 text-muted">{t("invitations.personalUtangRequest")}</p>
              <StatusChip tone="warning">{t("people.status.requestPending")}</StatusChip>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {formatShortDate(invite.createdAtUtc)}
              </p>
            </Card>
          ))
        )}
      </div>

      <Card className="flex flex-col gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("invitations.respondTitle")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("invitations.respondHelp")}</p>
        <label className="flex flex-col gap-1">
          <span className="font-semibold">{t("invitations.tokenLabel")}</span>
          <input
            value={token}
            onChange={(event) => setToken(event.target.value)}
            className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
            autoComplete="off"
            spellCheck={false}
          />
        </label>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="secondary"
            disabled={declineMutation.isPending || !token.trim()}
            onClick={() => void onDecline()}
          >
            {t("invitations.decline")}
          </Button>
          <Button
            type="button"
            disabled={acceptMutation.isPending || !token.trim()}
            onClick={() => void onAccept()}
          >
            {t("invitations.accept")}
          </Button>
        </div>
        {actionError ? (
          <p className="m-0 text-destructive" role="alert">
            {actionError}
          </p>
        ) : null}
      </Card>

      <div className="flex flex-col gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">{t("invitations.sent")}</h2>
        {sent.length === 0 ? (
          <p className="m-0 text-muted">{t("invitations.sentEmpty")}</p>
        ) : (
          sent.map((invite) => (
            <Card key={invite.id} className="flex flex-col gap-2">
              <p className="m-0 font-semibold">{contactName(invite.inviteeContactId)}</p>
              <p className="m-0 text-muted">{t("invitations.waitingResponse")}</p>
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={revokeMutation.isPending}
                  onClick={() => void revokeMutation.mutateAsync(invite.id)}
                >
                  {t("people.detail.cancelRequest")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  disabled={resendMutation.isPending}
                  onClick={() => void resendMutation.mutateAsync(invite.id)}
                >
                  {t("people.detail.sendAgain")}
                </Button>
              </div>
            </Card>
          ))
        )}
      </div>

      <Button asChild variant="ghost">
        <Link to="/personal/people">{t("shell.back")}</Link>
      </Button>
    </section>
  );
}
