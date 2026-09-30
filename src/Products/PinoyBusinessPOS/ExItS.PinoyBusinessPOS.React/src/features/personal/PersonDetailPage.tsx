import { ArrowLeft, Check, ChevronDown, Users } from "lucide-react";
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

import { StatusChip } from "@/components/ui/badge";

import { Button } from "@/components/ui/button";

import { Card } from "@/components/ui/card";

import { LoadingState } from "@/components/ui/skeleton";

import { cn } from "@/lib/cn";

import {

  useBlockContactMutation,

  useCreateUtangMutation,

  useInvalidatePersonalPeople,

  usePersonalConnectionRequestsQuery,

  usePersonalContactsQuery,

  usePersonalSharedUtangPreferenceQuery,

  usePersonalUtangSummariesQuery,

  useRequestConnectionMutation,

  useRevokeConnectionMutation,

  useUnblockContactMutation,

  useUnlinkContactMutation,

  useUpdatePersonalSharedUtangPreferenceMutation,

} from "@/features/personal/people-queries";

import { deriveConnectionStatus, formatShortDate } from "@/features/personal/people-status";
import { isSharedRelationship } from "@/features/personal/utang/utang-workspace";

import { useI18n } from "@/i18n/I18nProvider";

import { formatPeso } from "@/lib/format-money";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";

const UTANG_NOTES_MAX_LENGTH = 512;

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

  const unlinkContact = useUnlinkContactMutation();

  const blockContact = useBlockContactMutation();

  const unblockContact = useUnblockContactMutation();

  const createUtang = useCreateUtangMutation();

  const [amount, setAmount] = useState(() => formatMoneyAmountInput(0));
  const amountInputRef = useRef<HTMLInputElement>(null);
  const [notes, setNotes] = useState("");
  const [savingUtang, setSavingUtang] = useState(false);
  const [shareWithCounterparty, setShareWithCounterparty] = useState(false);

  const [mode, setMode] = useState<"lent" | "borrowed">("lent");

  const [connectionCardOpen, setConnectionCardOpen] = useState(false);

  const [confirmUnlink, setConfirmUnlink] = useState(false);

  const [confirmBlock, setConfirmBlock] = useState(false);

  const [actionError, setActionError] = useState<string | null>(null);



  const contact = contactsQuery.data?.find((item) => item.id === contactId);

  const connections = connectionsQuery.data ?? [];

  const connection = contact ? deriveConnectionStatus(contact, connections) : null;

  const publicUserId = contact?.resolvedPublicUserId ?? undefined;

  const linkedCounterpartyId = contact?.linkedUserIdentityId ?? null;
  const isConnected = connection?.status === "connected" && Boolean(linkedCounterpartyId);
  const sharedPrefsQuery = usePersonalSharedUtangPreferenceQuery(
    isConnected ? linkedCounterpartyId : null,
  );
  const updateSharedPrefs = useUpdatePersonalSharedUtangPreferenceMutation(
    linkedCounterpartyId ?? "",
  );



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



  return (

    <section className="personal-page exits-page flex w-full min-w-0 flex-col gap-4">

      <PageHeader
        title={contact.displayName}
        backTo="/personal/people"
        backLabel={t("people.backToFriends")}
        backTestId="person-detail-back"
      />

      <div className="flex items-start gap-3">
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



      {activeRelated.length > 0 ? (
        <Card data-testid="person-detail-utang-records">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold uppercase tracking-wide">
            {t("people.detail.utang")}
          </h2>
          <ul className="m-0 mt-2 flex list-none flex-col gap-2 p-0">
            {activeRelated.map((rel) => {
              const owned = rel.isLedgerOwner !== false;
              return (
                <li key={rel.id}>
                  <Link
                    to={`/personal/utang/relationships/${rel.id}`}
                    className="flex flex-col gap-0.5 text-foreground no-underline"
                    data-testid={`person-detail-utang-row-${rel.id}`}
                  >
                    <span className="text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
                      {owned
                        ? t("personal.utang.ownershipMine")
                        : t("personal.utang.ownershipSharedWithMe")}
                    </span>
                    <span className="font-medium">
                      {rel.perspective} · {formatMoney(rel.currentBalance, rel.currencyCode)}
                    </span>
                    <span className="text-[length:var(--exits-text-xs)] text-muted">
                      {owned
                        ? t("personal.utang.managedByMe")
                        : t("personal.utang.managedByOther").replace(
                            "{name}",
                            contact.displayName,
                          )}
                      {!owned ? ` · ${t("personal.utang.readOnly")}` : null}
                    </span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </Card>
      ) : null}



      {connection.status !== "request_sent" &&
      connection.status !== "request_received" &&
      connection.status !== "blocked" ? (
        <Card className="flex flex-col gap-3" data-testid="person-detail-utang-card">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("people.detail.utang")}
          </h2>

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
            <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
              {t("personal.utang.purposeHelp")}
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
        </Card>
      ) : null}

      {isConnected && linkedCounterpartyId ? (
        <Card className="flex flex-col gap-3" data-testid="person-detail-shared-utang-prefs">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("personal.utang.prefsTitle").replace(
              "{name}",
              contact?.displayName ?? t("personal.utang.person"),
            )}
          </h2>
          {sharedPrefsQuery.isLoading ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
          ) : sharedPrefsQuery.isError ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("personal.utang.prefsRequiresOnline")}
            </p>
          ) : (
            <>
              <label className="flex items-center justify-between gap-3 text-[length:var(--exits-text-sm)]">
                <span>{t("personal.utang.prefsReceive")}</span>
                <input
                  type="checkbox"
                  data-testid="person-detail-prefs-receive"
                  checked={sharedPrefsQuery.data?.receiveSharedUtang ?? true}
                  disabled={updateSharedPrefs.isPending}
                  onChange={(event) => {
                    const receive = event.target.checked;
                    void updateSharedPrefs.mutateAsync({
                      receiveSharedUtang: receive,
                      autoAcceptSharedUtang: false,
                      sharedUtangNotifications:
                        sharedPrefsQuery.data?.sharedUtangNotifications ?? true,
                      expectedVersion: sharedPrefsQuery.data?.version,
                    });
                  }}
                />
              </label>
              <label className="flex items-center justify-between gap-3 text-[length:var(--exits-text-sm)]">
                <span>{t("personal.utang.prefsNotifications")}</span>
                <input
                  type="checkbox"
                  data-testid="person-detail-prefs-notifications"
                  checked={sharedPrefsQuery.data?.sharedUtangNotifications ?? true}
                  disabled={updateSharedPrefs.isPending}
                  onChange={(event) => {
                    void updateSharedPrefs.mutateAsync({
                      receiveSharedUtang: sharedPrefsQuery.data?.receiveSharedUtang ?? true,
                      autoAcceptSharedUtang: false,
                      sharedUtangNotifications: event.target.checked,
                      expectedVersion: sharedPrefsQuery.data?.version,
                    });
                  }}
                />
              </label>
            </>
          )}
        </Card>
      ) : null}

      <Card className="flex flex-col gap-3" data-testid="person-detail-connection-card">
        <button
          type="button"
          className="flex w-full items-center justify-between gap-2 border-0 bg-transparent p-0 text-left"
          aria-expanded={connectionCardOpen}
          data-testid="person-detail-connection-toggle"
          onClick={() => setConnectionCardOpen((open) => !open)}
        >
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold uppercase tracking-wide">
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



        {connection.status === "connected" ? (
          <>
            <p className="m-0 text-muted">
              {t("people.detail.connectedSince").replace(
                "{date}",
                formatShortDate(contact.connectedAtUtc ?? contact.createdAtUtc),
              )}
            </p>

            {!confirmUnlink && !confirmBlock ? (
              <div className="flex min-w-0 flex-row flex-wrap gap-2">
                <Button
                  type="button"
                  intent="primary"
                  appearance="solid"
                  emphasis="soft"
                  className="min-w-0 flex-1"
                  data-testid="person-detail-unlink"
                  onClick={() => {
                    setConfirmBlock(false);
                    setConfirmUnlink(true);
                  }}
                >
                  {t("people.detail.unlink")}
                </Button>
                <Button
                  type="button"
                  intent="danger"
                  appearance="solid"
                  emphasis="soft"
                  className="min-w-0 flex-1"
                  data-testid="person-detail-block"
                  onClick={() => {
                    setConfirmUnlink(false);
                    setConfirmBlock(true);
                  }}
                >
                  {t("people.detail.block")}
                </Button>
              </div>
            ) : null}

            {confirmUnlink ? (
              <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3">
                <p className="m-0 font-semibold">
                  {t("people.detail.unlinkConfirmTitle").replace("{name}", contact.displayName)}
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("people.detail.unlinkConfirmBody")}
                </p>
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" onClick={() => setConfirmUnlink(false)}>
                    {t("people.add.cancel")}
                  </Button>
                  <Button
                    type="button"
                    intent="primary"
                    appearance="solid"
                    emphasis="soft"
                    disabled={unlinkContact.isPending}
                    onClick={() => {
                      setActionError(null);
                      void unlinkContact
                        .mutateAsync(contact.id)
                        .then(() => setConfirmUnlink(false))
                        .catch((err) => {
                          setActionError(err instanceof Error ? err.message : t("error.body"));
                        });
                    }}
                  >
                    {t("people.detail.unlinkConfirmAction")}
                  </Button>
                </div>
              </div>
            ) : null}

            {confirmBlock ? (
              <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3">
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("people.detail.blockConfirmBody")}
                </p>
                <div className="flex flex-wrap gap-2">
                  <Button type="button" variant="outline" onClick={() => setConfirmBlock(false)}>
                    {t("people.add.cancel")}
                  </Button>
                  <Button
                    type="button"
                    intent="danger"
                    appearance="solid"
                    emphasis="soft"
                    disabled={blockContact.isPending}
                    onClick={() => {
                      setActionError(null);
                      void blockContact
                        .mutateAsync(contact.id)
                        .then(() => setConfirmBlock(false))
                        .catch((err) => {
                          setActionError(err instanceof Error ? err.message : t("error.body"));
                        });
                    }}
                  >
                    {t("people.detail.block")}
                  </Button>
                </div>
              </div>
            ) : null}
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

