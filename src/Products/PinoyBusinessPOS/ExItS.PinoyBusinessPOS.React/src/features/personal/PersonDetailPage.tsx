import {
  ArrowLeft,
  ChevronDown,
  Contact,
  HandCoins,
  Pencil,
  ShieldCheck,
  SlidersHorizontal,
  Users,
  Wallet,
  type LucideIcon,
} from "lucide-react";
import { useMemo, useState } from "react";

import { Link, useParams } from "react-router-dom";

import { PlatformApiError } from "@/api/platform/platform-http";
import {
  getPersonalMe,
  recordPersonalUtangEntry,
} from "@/api/platform/personal-utang-client";

import { EmptyState } from "@/components/exits/EmptyState";

import { ErrorState } from "@/components/exits/ErrorState";

import { ExitsCheckbox } from "@/components/exits/ExitsCheckbox";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsOutlineInput, ExitsOutlineTextarea } from "@/components/exits/ExitsOutlineField";

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

import { INTENT_GLOBAL_SECONDARY } from "@/features/ui-standards/intent-color-global-default";
import { useI18n } from "@/i18n/I18nProvider";

import { formatPeso } from "@/lib/format-money";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";

const UTANG_NOTES_MAX_LENGTH = 512;

function SaveCheckIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v4.35" />
      <path d="m16 19 2 2 4-4" />
      <path d="M17 15.13V14a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v7" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function RibbonPennant({ icon: Icon }: { icon: LucideIcon }) {
  return (
    <span className="personal-tracker-card__pennant" aria-hidden>
      <Icon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
    </span>
  );
}

function ContactDetailFields({
  displayName,
  phone,
  email,
  publicUserId,
}: {
  displayName: string;
  phone?: string | null;
  email?: string | null;
  publicUserId?: string | null;
}) {
  const { t } = useI18n();
  return (
    <dl className="m-0 grid min-w-0 grid-cols-2 gap-x-4 gap-y-3">
      <div data-testid="person-detail-contact-name">
        <dt className="m-0 text-[13px] font-medium text-[var(--exits-primary)]">{t("people.displayName")}</dt>
        <dd className="m-0 text-[length:var(--exits-text-sm)]">{displayName}</dd>
      </div>
      {publicUserId ? (
        <div data-testid="person-detail-contact-id">
          <dt className="m-0 text-[13px] font-medium text-[var(--exits-primary)]">{t("people.add.exitsId")}</dt>
          <dd className="m-0 text-[length:var(--exits-text-sm)]">{publicUserId}</dd>
        </div>
      ) : null}
      <div data-testid="person-detail-contact-phone">
        <dt className="m-0 text-[13px] font-medium text-[var(--exits-primary)]">{t("people.column.contact")}</dt>
        <dd className="m-0 text-[length:var(--exits-text-sm)]">{phone?.trim() || "—"}</dd>
      </div>
      <div data-testid="person-detail-contact-email">
        <dt className="m-0 text-[13px] font-medium text-[var(--exits-primary)]">{t("people.column.email")}</dt>
        <dd className="m-0 break-all text-[length:var(--exits-text-sm)]">{email?.trim() || "—"}</dd>
      </div>
    </dl>
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
  const [notes, setNotes] = useState("");
  const [savingUtang, setSavingUtang] = useState(false);
  const [shareWithCounterparty, setShareWithCounterparty] = useState(false);

  const [mode, setMode] = useState<"lent" | "borrowed">("lent");
  const [recordOpen, setRecordOpen] = useState(false);

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
      setRecordOpen(false);
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSavingUtang(false);
    }
  }

  function openRecord(kind: "lent" | "borrowed") {
    setMode(kind);
    setActionError(null);
    setRecordOpen(true);
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
          <PersonAvatar name={contact.displayName} size="lg" className="page-header-capsule__avatar" />
        }
        subtitle={connected && publicUserId ? publicUserId : undefined}
        subtitleClassName="truncate max-lg:hidden"
        titleTrailing={
          connected ? (
            <StatusChip tone={statusTone(connection.status)} className="max-lg:hidden" data-testid="person-detail-header-status">
              {statusLabel(connection.status)}
            </StatusChip>
          ) : null
        }
      />

      {connection.status === "local" ? null : (
      <div className={cn("flex items-start gap-3", connected && "lg:hidden")}>
        <div className="min-w-0 flex-1">
          <p className="m-0 text-muted">
            {publicUserId ? publicUserId : t("people.localContact")}
          </p>

          <div className="mt-2">
            <StatusChip tone={statusTone(connection.status)}>{statusLabel(connection.status)}</StatusChip>
          </div>
        </div>
      </div>
      )}

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
            {t("people.detail.transactionDetails")}
          </h2>
          {showUtangForm ? (
            <div className="person-detail-utang-actions">
              <Button
                type="button"
                intent="success"
                appearance="glossy"
                emphasis="strong"
                data-testid="person-detail-utang-lent"
                onClick={() => openRecord("lent")}
              >
                <HandCoins className="size-4 shrink-0" aria-hidden />
                {t("people.detail.iLentAction")}
              </Button>
              <Button
                type="button"
                intent="primary"
                appearance="glossy"
                style={{ backgroundColor: INTENT_GLOBAL_SECONDARY, color: "#ffffff" }}
                data-testid="person-detail-utang-owe"
                onClick={() => openRecord("borrowed")}
              >
                <Wallet className="size-4 shrink-0" aria-hidden />
                {t("people.detail.iOweAction")}
              </Button>
            </div>
          ) : null}
      {activeRelated.length > 0 ? (
          <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="person-detail-utang-records">
            {activeRelated.map((rel) => {
              const owned = rel.isLedgerOwner !== false;
              return (
                <li key={rel.id}>
                  <Card
                    padding="compact"
                    className={cn("min-w-0", !owned && "utang-account-card--shared")}
                  >
                  <Link
                    to={`/personal/utang/relationships/${rel.id}`}
                    className="flex items-center justify-between gap-3 text-foreground no-underline"
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
                      <span className="font-medium">
                        {rel.perspective.toLowerCase() === "borrowed"
                          ? t("people.detail.iOweAction")
                          : t("people.detail.iLentAction")}
                      </span>
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {owned
                          ? t("personal.utang.managedByMe")
                          : t("personal.utang.managedByOther").replace(
                              "{name}",
                              contact.displayName,
                            )}
                      </span>
                    </span>
                    <span className="flex shrink-0 flex-col items-end gap-0.5 self-center text-end">
                      {!owned ? (
                        <ExitsStatusChip
                          tone={UTANG_READ_ONLY_CHIP.tone}
                          appearance={UTANG_READ_ONLY_CHIP.appearance}
                          shape={UTANG_READ_ONLY_CHIP.shape}
                          className="max-md:hidden"
                          data-testid={`person-detail-utang-readonly-${rel.id}`}
                        >
                          {t("personal.utang.readOnly")}
                        </ExitsStatusChip>
                      ) : null}
                      <span
                        className="text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`person-detail-utang-date-${rel.id}`}
                      >
                        {formatShortDate(rel.updatedAtUtc)}
                      </span>
                      <span className="font-[550]">
                        {formatMoney(rel.currentBalance, rel.currencyCode)}
                      </span>
                    </span>
                  </Link>
                  </Card>
                </li>
              );
            })}
          </ul>
      ) : null}

      {showUtangForm ? (
        <ExitsModal
          open={recordOpen}
          onOpenChange={setRecordOpen}
          title={
            <>
              {mode === "lent" ? t("people.detail.iLentAction") : t("people.detail.iOweAction")}
              {" - "}
              <span className="person-detail-record-name">{contact.displayName}</span>
            </>
          }
          testId="person-detail-record-dialog"
          closeLabel={t("people.add.cancel")}
          size="sm"
          closeOnOutsideClick={false}
          busy={savingUtang || createUtang.isPending}
          ribbon
          ribbonIcon={mode === "lent" ? HandCoins : Wallet}
          footer={
            <>
              <Button
                type="button"
                intent="danger"
                appearance="glossy"
                emphasis="strong"
                disabled={savingUtang || createUtang.isPending}
                data-testid="person-detail-record-cancel"
                onClick={() => setRecordOpen(false)}
              >
                {t("people.add.cancel")}
              </Button>
              <Button
                type="button"
                intent="primary"
                appearance="glossy"
                className="min-w-[8rem]"
                disabled={savingUtang || createUtang.isPending}
                onClick={() => void submitUtang(mode)}
                data-testid="person-detail-utang-save"
              >
                <SaveCheckIcon className="size-4 shrink-0" />
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
            </>
          }
        >
          <div className="flex min-w-0 flex-col gap-3">
          {actionError ? (
            <Notice tone="danger" testId="person-detail-record-error">
              {actionError}
            </Notice>
          ) : null}
          <ExitsOutlineInput
            id="person-detail-amount"
            label={t("personal.utang.amount")}
            value={amount}
            prefix="₱"
            onChange={(next) => setAmount(normalizeMoneyAmountTyping(next))}
            onFocus={() => {
              if ((parseMoneyAmountInput(amount) ?? 0) === 0) {
                setAmount("");
              }
            }}
            onBlur={() => {
              setAmount(formatMoneyAmountInput(parseMoneyAmountInput(amount) ?? 0));
            }}
            testId="person-detail-amount"
            inputMode="decimal"
            autoComplete="off"
            required
            data-exits-modal-autofocus="true"
          />
          <ExitsOutlineTextarea
            id="person-detail-utang-notes"
            label={t("personal.utang.purpose")}
            value={notes}
            onChange={setNotes}
            maxLength={UTANG_NOTES_MAX_LENGTH}
            required
            testId="person-detail-utang-notes"
          />

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
            <div className="flex min-w-0 flex-col gap-1" data-testid="person-detail-share-toggle">
              <ExitsCheckbox
                label={t("personal.utang.shareWithPerson").replace(
                  "{name}",
                  contact?.displayName ?? t("personal.utang.person"),
                )}
                checked={shareWithCounterparty}
                onCheckedChange={setShareWithCounterparty}
                testId="person-detail-share-checkbox"
              />
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                {shareWithCounterparty
                  ? t("personal.utang.shareOnHint").replace(
                      "{name}",
                      contact?.displayName ?? t("personal.utang.person"),
                    )
                  : t("personal.utang.shareOffHint")}
              </p>
            </div>
          ) : null}

          </div>
        </ExitsModal>
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
          <ContactDetailFields
            displayName={contact.displayName}
            phone={contact.phone}
            email={contact.email}
            publicUserId={publicUserId}
          />
          <div className="person-ribbon-card__rule" role="separator" />
          <SharedUtangSettingPanel
            contactId={contact.id}
            displayName={contact.displayName}
            connectedAtUtc={contact.connectedAtUtc}
            createdAtUtc={contact.createdAtUtc}
            counterpartyUserIdentityId={linkedCounterpartyId}
          />
        </Card>
      ) : null}

      {connection.status === "local" ? (
        <Card className="person-ribbon-card flex flex-col gap-3" data-testid="person-detail-contact-card">
          <h2 className="personal-tracker-card__ribbon">
            <RibbonPennant icon={Contact} />
            {t("people.detail.contactDetail")}
          </h2>
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            size="icon"
            shape="round"
            className="person-detail-contact-edit"
            aria-label={t("people.action.edit")}
            data-testid="person-detail-edit"
            onClick={() => setEditContactOpen(true)}
          >
            <Pencil className="size-4" aria-hidden />
          </Button>
          <ContactDetailFields
            displayName={contact.displayName}
            phone={contact.phone}
            email={contact.email}
          />
        </Card>
      ) : isConnected ? null : (
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

