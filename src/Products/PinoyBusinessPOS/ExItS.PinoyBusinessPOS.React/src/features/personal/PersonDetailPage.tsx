import {
  ArrowLeft,
  Check,
  ChevronDown,
  HandCoins,
  ShieldCheck,
  SlidersHorizontal,
  Users,
  type LucideIcon,
} from "lucide-react";
import { useMemo, useRef, useState } from "react";

import { Link, useParams } from "react-router-dom";

import { PlatformApiError } from "@/api/platform/platform-http";
import {
  getPersonalMe,
  recordPersonalUtangEntry,
} from "@/api/platform/personal-utang-client";

import { EmptyState } from "@/components/exits/EmptyState";

import { ErrorState } from "@/components/exits/ErrorState";

import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";

import { Notice } from "@/components/exits/Notice";

import { PageHeader } from "@/components/exits/PageHeader";

import { PersonAvatar } from "@/components/exits/PersonAvatar";

import { StatusChip as ExitsStatusChip } from "@/components/exits/StatusChip";

import { StatusChip } from "@/components/ui/badge";

import { Button } from "@/components/ui/button";
import { EditLocalContactDialog } from "@/features/personal/EditLocalContactDialog";

import { Card } from "@/components/ui/card";

import { LoadingState } from "@/components/ui/skeleton";

import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";

import { cn } from "@/lib/cn";

import {

  useCreateUtangMutation,

  useInvalidatePersonalPeople,

  usePersonalConnectionRequestsQuery,

  usePersonalContactsQuery,

  usePersonalUtangSummariesQuery,

  useRequestConnectionMutation,

  useRevokeConnectionMutation,

  useUnblockContactMutation,

} from "@/features/personal/people-queries";

import { deriveConnectionStatus, formatShortDate } from "@/features/personal/people-status";
import { SharedUtangSettingPanel } from "@/features/personal/SharedUtangSettingPanel";
import { isSharedRelationship } from "@/features/personal/utang/utang-workspace";

import { useI18n } from "@/i18n/I18nProvider";

import { formatPeso } from "@/lib/format-money";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";

const UTANG_NOTES_MAX_LENGTH = 512;

function RibbonPennant({ icon: Icon }: { icon: LucideIcon }) {
  return (
    <span className="personal-tracker-card__pennant" aria-hidden>
      <Icon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
    </span>
  );
}

function formatMoney(amount: number, currencyCode: string): string {

  const code = (currencyCode || "PHP").trim().toUpperCase();

  if (code === "PHP") {

    return formatPeso(amount);

  }

  try {

    return new Intl.NumberFormat(undefined, {

      style: "currency",

      currency: code,

      maximumFractionDigits: 2,

    }).format(amount);

  } catch {

    return `${code} ${amount.toFixed(2)}`;

  }

}



export function PersonDetailPage() {

  const { contactId = "" } = useParams();

  const { t } = useI18n();

  const contactsQuery = usePersonalContactsQuery();

  const connectionsQuery = usePersonalConnectionRequestsQuery();

  const utangQuery = usePersonalUtangSummariesQuery();

  const invalidatePersonal = useInvalidatePersonalPeople();

  const requestConnection = useRequestConnectionMutation();

  const revokeConnection = useRevokeConnectionMutation();

  const unblockContact = useUnblockContactMutation();

  const createUtang = useCreateUtangMutation();

  const [amount, setAmount] = useState(() => formatMoneyAmountInput(0));
  const amountInputRef = useRef<HTMLInputElement>(null);
  const [notes, setNotes] = useState("");
  const [savingUtang, setSavingUtang] = useState(false);
  const [shareWithCounterparty, setShareWithCounterparty] = useState(false);

  const [mode, setMode] = useState<"lent" | "borrowed">("lent");

  const [connectionCardOpen, setConnectionCardOpen] = useState(false);
  const [editContactOpen, setEditContactOpen] = useState(false);

  const [actionError, setActionError] = useState<string | null>(null);



  const contact = contactsQuery.data?.find((item) => item.id === contactId);

  const connections = connectionsQuery.data ?? [];

  const connection = contact ? deriveConnectionStatus(contact, connections) : null;

  const publicUserId = contact?.resolvedPublicUserId ?? undefined;

  const linkedCounterpartyId = contact?.linkedUserIdentityId ?? null;
  const isConnected = connection?.status === "connected" && Boolean(linkedCounterpartyId);



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

  const activeRelated = useMemo(
    () => related.filter((rel) => rel.status.toLowerCase() === "active"),
    [related],
  );

  const ownedActiveForMode = useMemo(() => {
    const expectedPerspective = mode === "lent" ? "lent" : "borrowed";
    return activeRelated.find(
      (rel) =>
        rel.isLedgerOwner !== false &&
        rel.perspective.toLowerCase() === expectedPerspective,
    );
  }, [activeRelated, mode]);

  const sharedWithMeForMode = useMemo(() => {
    const expectedPerspective = mode === "lent" ? "lent" : "borrowed";
    return activeRelated.find(
      (rel) =>
        rel.isLedgerOwner === false &&
        isSharedRelationship(rel) &&
        rel.perspective.toLowerCase() === expectedPerspective,
    );
  }, [activeRelated, mode]);

  async function submitUtang(kind: "lent" | "borrowed") {
    if (!contact) {
      return;
    }

    setActionError(null);

    const parsed = parseMoneyAmountInput(amount);

    if (parsed === null || parsed <= 0) {
      setActionError(t("people.detail.amountInvalid"));
      return;
    }

    const purpose = notes.trim();
    if (!purpose) {
      setActionError(t("personal.utang.purposeRequired"));
      return;
    }

    const expectedPerspective = kind === "lent" ? "lent" : "borrowed";
    // Only append to a ledger this viewer owns — never reuse a shared-with-me record.
    const matchingOwnedRel = related.find(
      (rel) =>
        rel.status.toLowerCase() === "active" &&
        rel.isLedgerOwner !== false &&
        rel.perspective.toLowerCase() === expectedPerspective,
    );

    setSavingUtang(true);
    try {
      if (matchingOwnedRel) {
        await recordPersonalUtangEntry(matchingOwnedRel.id, {
          entryType: "Loan",
          amount: parsed,
          notes: purpose,
          expectedVersion: matchingOwnedRel.version,
        });
        await invalidatePersonal();
      } else {
        const me = await getPersonalMe();
        const share = Boolean(isConnected && shareWithCounterparty);
        const relationship =
          kind === "lent"
            ? {
                creditorUserIdentityId: me.userIdentityId,
                creditorContactId: null,
                debtorUserIdentityId: null,
                debtorContactId: contact.id,
                currencyCode: "PHP",
                initialLoanAmount: parsed,
                initialLoanNotes: purpose,
                shareWithCounterparty: share,
              }
            : {
                creditorUserIdentityId: null,
                creditorContactId: contact.id,
                debtorUserIdentityId: me.userIdentityId,
                debtorContactId: null,
                currencyCode: "PHP",
                initialLoanAmount: parsed,
                initialLoanNotes: purpose,
                shareWithCounterparty: share,
              };
        const created = await createUtang.mutateAsync(relationship);
        if (created.shareOutcome === "PrivateNotReceiving") {
          setActionError(t("personal.utang.savedPrivatelyNotReceiving"));
        }
      }
      setAmount(formatMoneyAmountInput(0));
      setNotes("");
      setShareWithCounterparty(false);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSavingUtang(false);
    }
  }



  function statusLabel(status: NonNullable<typeof connection>["status"]): string {

    switch (status) {

      case "connected":

        return t("people.status.connected");

      case "request_sent":
        return t("people.status.requestSent");
      case "request_received":
        return t("people.status.requestReceived");

      case "blocked":

        return t("people.status.blocked");

      case "local":

        return t("people.status.local");

      default:

        return t("people.status.notConnected");

    }

  }



  function statusTone(

    status: NonNullable<typeof connection>["status"],

  ): "neutral" | "success" | "warning" | "info" {

    if (status === "connected") {

      return "success";

    }

    if (status === "request_sent" || status === "request_received") {

      return "warning";

    }

    if (status === "blocked") {

      return "warning";

    }

    return "neutral";

  }



  if (contactsQuery.isLoading || connectionsQuery.isLoading || utangQuery.isLoading) {

    return <LoadingState label={t("loading.label")} />;

  }



  const loadError = contactsQuery.error ?? connectionsQuery.error ?? utangQuery.error;

  if (loadError) {

    return (

      <ErrorState
        title={t("error.title")}
        detail={loadError instanceof PlatformApiError ? loadError.message : t("error.body")}
        error={loadError}
      />

    );

  }



  if (!contact || !connection) {

    return (

      <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />} title={t("people.detail.notFoundTitle")} detail={t("people.detail.notFoundBody")} />

    );

  }



  const connected = connection.status === "connected";
  const showUtangForm =
    connection.status !== "request_sent" &&
    connection.status !== "request_received" &&
    connection.status !== "blocked";

  return (

    <section className="personal-page exits-page flex w-full min-w-0 flex-col gap-4">

      <PageHeader
        title={contact.displayName}
        backTo="/personal/people"
        backLabel={t("people.backToFriends")}
        backTestId="person-detail-back"
        circleContent={
          connected ? (
            <>
              <ArrowLeft className="size-7 shrink-0 lg:hidden rtl:rotate-180" aria-hidden />
              <span className="hidden size-full lg:contents">
                <PersonAvatar name={contact.displayName} size="lg" className="page-header-capsule__avatar" />
              </span>
            </>
          ) : undefined
        }
        subtitle={connected && publicUserId ? publicUserId : undefined}
        subtitleClassName="truncate max-lg:hidden"
        titleTrailing={
          connection.status === "local" ? (
            <Button
              type="button"
              intent="primary"
              appearance="outline"
              shape="standard"
              className="w-auto"
              data-testid="person-detail-edit"
              onClick={() => setEditContactOpen(true)}
            >
              {t("people.action.edit")}
            </Button>
          ) : connected ? (
            <StatusChip tone={statusTone(connection.status)} className="max-lg:hidden" data-testid="person-detail-header-status">
              {statusLabel(connection.status)}
            </StatusChip>
          ) : null
        }
      />

      <div className={cn("flex items-start gap-3", connected && "lg:hidden")}>
        <PersonAvatar name={contact.displayName} size="lg" />

        <div className="min-w-0 flex-1">
          <p className="m-0 text-muted">
            {publicUserId ? publicUserId : t("people.localContact")}
          </p>

          <div className="mt-2">
            <StatusChip tone={statusTone(connection.status)}>{statusLabel(connection.status)}</StatusChip>
          </div>
        </div>
      </div>

      {connection.status === "local" ? (
        <EditLocalContactDialog
          open={editContactOpen}
          contact={contact}
          onOpenChange={setEditContactOpen}
        />
      ) : null}

      <div className="person-detail-cards" data-testid="person-detail-cards">
      {activeRelated.length > 0 || showUtangForm ? (
        <Card className="person-ribbon-card flex flex-col gap-3" data-testid="person-detail-utang-card">
          <h2 className="personal-tracker-card__ribbon">
            <RibbonPennant icon={HandCoins} />
            {t("people.detail.utang")}
          </h2>
      {activeRelated.length > 0 ? (
          <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="person-detail-utang-records">
            {activeRelated.map((rel) => {
              const owned = rel.isLedgerOwner !== false;
              return (
                <li key={rel.id}>
                  <Link
                    to={`/personal/utang/relationships/${rel.id}`}
                    className={cn(
                      "flex items-center justify-between gap-3 rounded-[var(--exits-radius-md)] border border-transparent px-2 py-1.5 text-foreground no-underline",
                      !owned && "utang-account-card--shared",
                    )}
                    data-testid={`person-detail-utang-row-${rel.id}`}
                  >
                    <span className="flex min-w-0 flex-1 flex-col gap-0.5">
                      <span
                        className="truncate text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted"
                        data-utang-ownership=""
                      >
                        {owned
                          ? t("personal.utang.ownershipMine")
                          : t("personal.utang.ownershipSharedWithMe")}
                      </span>
                      <span className="font-medium">{rel.perspective}</span>
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {owned
                          ? t("personal.utang.managedByMe")
                          : t("personal.utang.managedByOther").replace(
                              "{name}",
                              contact.displayName,
                            )}
                      </span>
                    </span>
                    <span className="grid shrink-0 justify-items-start self-stretch font-medium">
                      {!owned ? (
                        <ExitsStatusChip
                          tone={UTANG_READ_ONLY_CHIP.tone}
                          appearance={UTANG_READ_ONLY_CHIP.appearance}
                          shape={UTANG_READ_ONLY_CHIP.shape}
                          className="col-start-1 row-start-1 self-start max-md:hidden"
                          data-testid={`person-detail-utang-readonly-${rel.id}`}
                        >
                          {t("personal.utang.readOnly")}
                        </ExitsStatusChip>
                      ) : null}
                      <span className="col-start-1 row-start-1 self-center">
                        {formatMoney(rel.currentBalance, rel.currencyCode)}
                      </span>
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
      ) : null}

      {activeRelated.length > 0 && showUtangForm ? (
          <div className="person-ribbon-card__rule" role="separator" />
      ) : null}

      {showUtangForm ? (
        <>
          <ExitsPillSelect
            appearance="tile"
            value={mode}
            options={[
              { value: "lent", label: t("people.detail.iLent") },
              { value: "borrowed", label: t("people.detail.iBorrowed") },
            ]}
            onChange={(next) => {
              setMode(next);
              requestAnimationFrame(() => {
                const input = amountInputRef.current;
                if (!input) {
                  return;
                }
                input.focus();
                input.select();
              });
            }}
            className="grid-cols-2"
            testId="person-detail-mode-select"
            aria-label={t("people.detail.relationship")}
          />

          <label className="flex min-w-0 flex-col gap-1">
            <span className="sr-only">{t("people.detail.amount")}</span>
            <div className="exits-currency-field exits-currency-field--emphasis">
              <span className="exits-currency-field__prefix" aria-hidden>
                ₱
              </span>
              <input
                ref={amountInputRef}
                type="text"
                inputMode="decimal"
                value={amount}
                onChange={(event) => setAmount(normalizeMoneyAmountTyping(event.target.value))}
                onBlur={() => {
                  const parsed = parseMoneyAmountInput(amount);
                  setAmount(formatMoneyAmountInput(parsed ?? 0));
                }}
                className="exits-currency-field__input"
                data-testid="person-detail-amount"
                aria-label={t("people.detail.amount")}
                autoComplete="off"
              />
            </div>
          </label>

          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="font-medium">
              {t("personal.utang.purpose")}
              <span className="text-destructive" aria-hidden>
                {" "}
                *
              </span>
            </span>
            <textarea
              data-testid="person-detail-utang-notes"
              className="min-h-20 w-full min-w-0 resize-y rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
              value={notes}
              maxLength={UTANG_NOTES_MAX_LENGTH}
              required
              aria-required="true"
              onChange={(event) => setNotes(event.target.value)}
            />
          </label>

          {sharedWithMeForMode && !ownedActiveForMode ? (
            <Notice
              tone="info"
              testId="person-detail-shared-exists-hint"
              className="text-[length:var(--exits-text-sm)]"
            >
              {t("personal.utang.counterpartyAlreadyShared").replace(
                "{name}",
                contact?.displayName ?? t("personal.utang.person"),
              )}{" "}
              <Link
                to={`/personal/utang/relationships/${sharedWithMeForMode.id}`}
                data-testid="person-detail-view-shared-record"
              >
                {t("personal.utang.viewSharedRecord")}
              </Link>
              {" · "}
              {t("personal.utang.createOwnRecordHint")}
            </Notice>
          ) : null}

          {isConnected && !ownedActiveForMode ? (
            <label
              className="flex min-w-0 cursor-pointer items-start gap-2 text-[length:var(--exits-text-sm)]"
              data-testid="person-detail-share-toggle"
            >
              <input
                type="checkbox"
                className="mt-0.5"
                checked={shareWithCounterparty}
                onChange={(event) => setShareWithCounterparty(event.target.checked)}
                data-testid="person-detail-share-checkbox"
              />
              <span>
                <span className="font-medium">
                  {t("personal.utang.shareWithPerson").replace(
                    "{name}",
                    contact?.displayName ?? t("personal.utang.person"),
                  )}
                </span>
                <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                  {shareWithCounterparty
                    ? t("personal.utang.shareOnHint").replace(
                        "{name}",
                        contact?.displayName ?? t("personal.utang.person"),
                      )
                    : t("personal.utang.shareOffHint")}
                </span>
              </span>
            </label>
          ) : null}

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              disabled={savingUtang || createUtang.isPending}
              onClick={() => void submitUtang(mode)}
              data-testid="person-detail-utang-save"
            >
              <Check className="size-4 shrink-0" aria-hidden />
              {savingUtang || createUtang.isPending
                ? t("loading.label")
                : isConnected && shareWithCounterparty && !ownedActiveForMode
                  ? t("personal.utang.shareWithPerson").replace(
                      "{name}",
                      contact?.displayName ?? t("personal.utang.person"),
                    )
                  : mode === "lent"
                    ? t("people.detail.saveILent")
                    : t("people.detail.saveIBorrowed")}
            </Button>
          </div>
        </>
      ) : null}
        </Card>
      ) : null}

      {isConnected && linkedCounterpartyId ? (
        <Card
          className="person-ribbon-card person-shared-card flex flex-col gap-3"
          data-testid="person-detail-shared-utang-prefs"
        >
          <h2 className="personal-tracker-card__ribbon">
            <RibbonPennant icon={SlidersHorizontal} />
            {t("personal.utang.prefsTitle").replace(
              "{name}",
              contact?.displayName ?? t("personal.utang.person"),
            )}
          </h2>
          <SharedUtangSettingPanel
            contactId={contact.id}
            displayName={contact.displayName}
            connectedAtUtc={contact.connectedAtUtc}
            createdAtUtc={contact.createdAtUtc}
            counterpartyUserIdentityId={linkedCounterpartyId}
          />
        </Card>
      ) : null}

      {isConnected ? null : (
      <Card className="person-ribbon-card flex flex-col gap-3" data-testid="person-detail-connection-card">
        <button
          type="button"
          className="person-ribbon-card__toggle flex w-full items-center justify-between gap-2 border-0 bg-transparent p-0 text-left"
          aria-expanded={connectionCardOpen}
          data-testid="person-detail-connection-toggle"
          onClick={() => setConnectionCardOpen((open) => !open)}
        >
          <h2 className="personal-tracker-card__ribbon">
            <RibbonPennant icon={ShieldCheck} />
            {t("people.detail.connectionAndSafety")}
          </h2>
          <ChevronDown
            className={cn(
              "size-4 shrink-0 text-muted transition-transform",
              connectionCardOpen && "rotate-180",
            )}
            aria-hidden
          />
        </button>

        {connectionCardOpen ? (
          <>
        {connection.status === "not_connected" ? (

          <>

            <p className="m-0 text-muted">{t("people.detail.connectionHelp")}</p>

            <Button

              type="button"

              disabled={requestConnection.isPending}

              onClick={() => {

                setActionError(null);

                void requestConnection.mutateAsync(contact.id).catch((err) => {

                  setActionError(err instanceof Error ? err.message : t("error.body"));

                });

              }}

            >

              {requestConnection.isPending ? t("loading.label") : t("people.detail.requestConnection")}

            </Button>

          </>

        ) : null}



        {connection.status === "request_sent" && connection.pendingConnectionRequest ? (

          <>

            <p className="m-0 text-muted">

              {t("people.detail.waitingBody").replace(

                "{name}",

                contact.displayName.split(" ")[0] ?? contact.displayName,

              )}

            </p>

            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">

              {t("people.detail.sentOn").replace(

                "{date}",

                formatShortDate(connection.pendingConnectionRequest.createdAtUtc),

              )}

            </p>

            <Button

              type="button"

              variant="outline"

              disabled={revokeConnection.isPending}

              onClick={() => {

                setActionError(null);

                void revokeConnection

                  .mutateAsync(connection.pendingConnectionRequest!.id)

                  .catch((err) => {

                    setActionError(err instanceof Error ? err.message : t("error.body"));

                  });

              }}

            >

              {t("people.detail.cancelRequest")}

            </Button>

          </>

        ) : null}




        {connection.status === "blocked" ? (
          <>
            <p className="m-0 text-muted">{t("people.detail.blockedHelp")}</p>
            <Button
              type="button"
              disabled={unblockContact.isPending}
              onClick={() => {
                setActionError(null);
                void unblockContact.mutateAsync(contact.id).catch((err) => {
                  setActionError(err instanceof Error ? err.message : t("error.body"));
                });
              }}
            >
              {t("people.detail.unblock")}
            </Button>
          </>
        ) : null}
          </>
        ) : null}

      </Card>
      )}
      </div>



      {actionError ? (
        <Notice tone="danger" testId="person-detail-action-error">
          {actionError}
        </Notice>
      ) : null}



      <Link

        to="/personal/people"

        className="inline-flex items-center justify-center gap-1.5 font-bold text-primary no-underline"

        data-testid="person-detail-back-footer"

      >

        <ArrowLeft className="size-4 shrink-0" aria-hidden />

        {t("people.backToFriends")}

      </Link>

    </section>

  );

}

