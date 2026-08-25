import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
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
  useCreateUtangWithOptionalInviteMutation,
  usePersonalContactsQuery,
  usePersonalInvitationsQuery,
  usePersonalUtangSummariesQuery,
  useResendInvitationMutation,
  useRevokeInvitationMutation,
} from "@/features/personal/people-queries";
import {
  deriveConnectionStatus,
  formatShortDate,
  readResolvedPublicIdCache,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { usePreferences } from "@/hooks/usePreferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

export function PersonDetailPage() {
  const { contactId = "" } = useParams();
  const { t } = useI18n();
  const { session } = useSession();
  const { preferences } = usePreferences();
  const contactsQuery = usePersonalContactsQuery();
  const invitationsQuery = usePersonalInvitationsQuery();
  const utangQuery = usePersonalUtangSummariesQuery();
  const createUtang = useCreateUtangWithOptionalInviteMutation();
  const revokeInvite = useRevokeInvitationMutation();
  const resendInvite = useResendInvitationMutation();
  const [amount, setAmount] = useState("1000");
  const [mode, setMode] = useState<"lent" | "borrowed" | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const contact = contactsQuery.data?.find((item) => item.id === contactId);
  const invitations = invitationsQuery.data ?? [];
  const connection = contact ? deriveConnectionStatus(contact, invitations) : null;
  const publicUserId = readResolvedPublicIdCache()[contactId];

  const related = useMemo(() => {
    if (!contact || !utangQuery.data) {
      return [];
    }
    return [...utangQuery.data.lent, ...utangQuery.data.borrowed].filter(
      (rel) =>
        rel.creditorContactId === contact.id ||
        rel.debtorContactId === contact.id ||
        (contact.linkedUserIdentityId &&
          (rel.creditorUserIdentityId === contact.linkedUserIdentityId ||
            rel.debtorUserIdentityId === contact.linkedUserIdentityId)),
    );
  }, [contact, utangQuery.data]);

  const activeRel = related.find((rel) => rel.status.toLowerCase() === "active");

  async function submitUtang(kind: "lent" | "borrowed") {
    if (!contact || !session) {
      return;
    }
    setActionError(null);
    const parsed = Number(amount);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      setActionError(t("people.detail.amountInvalid"));
      return;
    }

    const shouldInvite = !contact.linkedUserIdentityId;
    const relationship =
      kind === "lent"
        ? {
            creditorUserIdentityId: session.userId,
            debtorContactId: contact.id,
            currencyCode: "PHP",
            initialLoanAmount: parsed,
          }
        : {
            debtorUserIdentityId: session.userId,
            creditorContactId: contact.id,
            currencyCode: "PHP",
            initialLoanAmount: parsed,
          };

    try {
      await createUtang.mutateAsync({
        relationship,
        inviteeContactId: contact.id,
        shouldInvite,
      });
      setMode(null);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.body"));
    }
  }

  if (contactsQuery.isLoading || invitationsQuery.isLoading || utangQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const loadError = contactsQuery.error ?? invitationsQuery.error ?? utangQuery.error;
  if (loadError) {
    return (
      <ErrorState
        title={t("error.title")}
        body={loadError instanceof ApiClientError ? loadError.message : t("error.body")}
        record={normalizeDiagnosticError(loadError, {
          locale: preferences.locale,
          theme: preferences.theme,
          pathname: `/personal/people/${contactId}`,
        })}
      />
    );
  }

  if (!contact || !connection) {
    return (
      <EmptyState title={t("people.detail.notFoundTitle")} body={t("people.detail.notFoundBody")} />
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-lg flex-col gap-4">
      <PageHeader title={contact.displayName} />
      <div>
        <p className="m-0 text-muted">
          {publicUserId ? publicUserId : t("people.localContact")}
        </p>
        <div className="mt-2">
          <StatusChip
            tone={
              connection.status === "connected"
                ? "success"
                : connection.status === "request_pending"
                  ? "warning"
                  : "neutral"
            }
          >
            {connection.status === "connected"
              ? t("people.status.connected")
              : connection.status === "request_pending"
                ? t("people.status.requestPending")
                : t("people.status.notConnected")}
          </StatusChip>
        </div>
      </div>

      {connection.status === "not_connected" ? (
        <p className="m-0 text-muted">{t("people.detail.notConnectedHelp")}</p>
      ) : null}

      {connection.status === "request_pending" && connection.pendingInvitation ? (
        <Card className="flex flex-col gap-3">
          <p className="m-0 font-semibold">{t("people.detail.waitingTitle")}</p>
          <p className="m-0 text-muted">
            {t("people.detail.waitingBody").replace("{name}", contact.displayName.split(" ")[0] ?? contact.displayName)}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("people.detail.sentOn").replace(
              "{date}",
              formatShortDate(connection.pendingInvitation.createdAtUtc),
            )}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="secondary"
              disabled={revokeInvite.isPending}
              onClick={() => void revokeInvite.mutateAsync(connection.pendingInvitation!.id)}
            >
              {t("people.detail.cancelRequest")}
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={resendInvite.isPending}
              onClick={() => void resendInvite.mutateAsync(connection.pendingInvitation!.id)}
            >
              {t("people.detail.sendAgain")}
            </Button>
          </div>
        </Card>
      ) : null}

      {activeRel ? (
        <Card>
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t("people.detail.utang")}</h2>
          <p className="m-0 mt-2 text-muted">
            {activeRel.perspective} · {activeRel.currencyCode} {activeRel.currentBalance.toFixed(2)}
          </p>
        </Card>
      ) : null}

      {connection.status !== "request_pending" ? (
        <div className="flex flex-col gap-2">
          <Button type="button" onClick={() => setMode("lent")}>
            {t("people.detail.iLent")}
          </Button>
          <Button type="button" variant="secondary" onClick={() => setMode("borrowed")}>
            {t("people.detail.iBorrowed")}
          </Button>
        </div>
      ) : null}

      {mode ? (
        <Card className="flex flex-col gap-3">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {mode === "lent" ? t("people.detail.iLent") : t("people.detail.iBorrowed")}
          </h2>
          <label className="flex flex-col gap-1">
            <span className="font-semibold">{t("people.detail.amount")}</span>
            <input
              inputMode="decimal"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              className="h-[var(--exits-control-height)] rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 outline-none focus-visible:ring-2 focus-visible:ring-[var(--exits-focus-ring)]"
            />
          </label>
          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="secondary" onClick={() => setMode(null)}>
              {t("people.add.cancel")}
            </Button>
            <Button
              type="button"
              disabled={createUtang.isPending}
              onClick={() => void submitUtang(mode)}
            >
              {createUtang.isPending ? t("loading.label") : t("people.detail.confirmUtang")}
            </Button>
          </div>
        </Card>
      ) : null}

      {connection.status === "connected" ? (
        <Card>
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("people.detail.relationship")}
          </h2>
          <p className="m-0 mt-2 text-muted">
            {t("people.detail.connectedSince").replace(
              "{date}",
              formatShortDate(contact.createdAtUtc),
            )}
          </p>
        </Card>
      ) : null}

      {actionError ? (
        <p className="m-0 text-destructive" role="alert">
          {actionError}
        </p>
      ) : null}

      <Button asChild variant="ghost">
        <Link to="/personal/people">{t("shell.back")}</Link>
      </Button>
    </section>
  );
}
