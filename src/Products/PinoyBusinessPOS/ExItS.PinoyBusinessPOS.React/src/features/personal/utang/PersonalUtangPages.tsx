import { useEffect, useMemo, useState } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  HandCoins,
  Loader2,
  UserPlus,
  Wallet,
} from "lucide-react";
import {
  cancelPersonalUtangEntry,
  confirmPersonalUtangEntry,
  createPersonalDebtRelationship,
  disputePersonalUtangEntry,
  formatDueLabel,
  getPersonalDebtRelationship,
  getPersonalMe,
  getPersonalUtangBalance,
  isUtangConcurrencyConflict,
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
  listPersonalUtangHistory,
  recordPersonalUtangEntry,
  type PersonalContactDto,
  type PersonalDebtRelationshipSummaryDto,
  type PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { RelationshipInviteReminderPanel } from "@/features/personal/social/PersonalSocialPages";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
import { ONLINE_REQUIRED_CODES, onlineRequiredDetailKey } from "@/offline/online-required";
import { usePersonalOfflineContext } from "@/offline/personal-offline-context";
import {
  cachePersonalContacts,
  cachePersonalEntries,
  cachePersonalRelationship,
  cachePersonalRelationships,
  cachePersonalUserIdentityId,
  getCachedPersonalRelationship,
  getCachedPersonalUserIdentityId,
  listCachedPersonalContacts,
  listCachedPersonalEntries,
  listCachedPersonalRelationships,
  type CachedPersonalContact,
  type CachedPersonalEntry,
  type CachedPersonalRelationship,
} from "@/offline/personal-utang-cache";
import {
  enqueuePersonalRelationshipCreate,
  enqueuePersonalUtangEntry,
} from "@/offline/personal-utang-offline";

const UTANG_NOTES_MAX_LENGTH = 512;
const EM_DASH = "\u2014";

function contactLabel(
  contacts: PersonalContactDto[],
  relationship: PersonalDebtRelationshipSummaryDto,
): string {
  const contactId =
    relationship.perspective === "Borrowed"
      ? relationship.creditorContactId
      : relationship.debtorContactId;
  if (contactId) {
    return contacts.find((c) => c.id === contactId)?.displayName ?? EM_DASH;
  }
  return EM_DASH;
}

function loanActivityLabel(
  perspective: string,
  personName: string,
  pendingIncoming: boolean,
  t: (key: MessageKey) => string,
): string {
  if (pendingIncoming) {
    return perspective === "Borrowed"
      ? t("personal.utang.activityTheyLentYou").replace("{name}", personName)
      : t("personal.utang.activityTheyBorrowed").replace("{name}", personName);
  }
  return perspective === "Borrowed"
    ? t("personal.utang.activityYouBorrowed").replace("{name}", personName)
    : t("personal.utang.activityYouLent").replace("{name}", personName);
}

function entryTypeLabelKey(entryType: string): MessageKey {
  if (entryType === "Payment") return "personal.utang.entryTypePayment";
  if (entryType === "Adjustment") return "personal.utang.entryTypeAdjustment";
  return "personal.utang.entryTypeLoan";
}

function entryStatusLabelKey(status: string | undefined): MessageKey {
  switch (status) {
    case "Pending":
      return "personal.utang.statusPending";
    case "Disputed":
      return "personal.utang.statusDisputed";
    case "Cancelled":
      return "personal.utang.statusCancelled";
    default:
      return "personal.utang.statusConfirmed";
  }
}

function isSharedRelationship(
  row: Pick<PersonalDebtRelationshipSummaryDto, "isSharedLedger" | "isPrivate">,
): boolean {
  if (row.isSharedLedger) return true;
  if (row.isPrivate) return false;
  return false;
}

function contactLooksLinked(
  contacts: ReadonlyArray<PersonalContactDto | CachedPersonalContact>,
  contactId: string,
): boolean {
  const contact = contacts.find((c) => c.id === contactId);
  return Boolean(contact?.linkedUserIdentityId);
}

/** Backend shared-loan proposal pending cap (sender → counterparty). */
export const PERSONAL_UTANG_MAX_PENDING_OUTGOING = 3;

export const PERSONAL_UTANG_PROPOSAL_ERROR_CODES = {
  pendingLimit: "application.personal.utang.pending_limit_reached",
  dailyLimit: "application.personal.utang.daily_limit_reached",
  duplicate: "application.personal.utang.duplicate_submission",
} as const;

type PendingOutgoingEntryLike = {
  status?: string;
  entryType?: string;
  canCancel?: boolean;
  canConfirm?: boolean;
};

/** Outgoing Loan proposals waiting for the counterparty (matches anti-spam gate). */
export function countPendingOutgoingLoanProposals(
  history: ReadonlyArray<PendingOutgoingEntryLike>,
): number {
  return history.reduce((count, entry) => {
    const pendingOutgoing =
      entry.status === "Pending" &&
      entry.entryType === "Loan" &&
      Boolean(entry.canCancel) &&
      !entry.canConfirm;
    return count + (pendingOutgoing ? 1 : 0);
  }, 0);
}

/** Map create/record API errors by errorCode (not English detail strings). */
export function mapPersonalUtangMutationError(
  error: unknown,
  counterpartyName: string,
  t: (key: MessageKey) => string,
): string {
  if (!(error instanceof PlatformApiError)) {
    return t("personal.utang.genericError");
  }
  const code = error.errorCode ?? "";
  const name = counterpartyName.trim() || t("personal.utang.person");
  if (code === PERSONAL_UTANG_PROPOSAL_ERROR_CODES.pendingLimit) {
    return t("personal.utang.pendingLimitReached").replace("{name}", name);
  }
  if (code === PERSONAL_UTANG_PROPOSAL_ERROR_CODES.dailyLimit) {
    return t("personal.utang.dailyLimitReached").replace("{name}", name);
  }
  if (code === PERSONAL_UTANG_PROPOSAL_ERROR_CODES.duplicate) {
    return t("personal.utang.duplicateSubmission");
  }
  return error.message || t("personal.utang.genericError");
}

function findSharedRelationshipForContact(
  rows: ReadonlyArray<
    Pick<
      PersonalDebtRelationshipSummaryDto,
      | "id"
      | "isSharedLedger"
      | "isPrivate"
      | "debtorContactId"
      | "creditorContactId"
      | "debtorUserIdentityId"
      | "creditorUserIdentityId"
    >
  >,
  contacts: ReadonlyArray<PersonalContactDto | CachedPersonalContact>,
  contactId: string,
  mode: "lent" | "owe",
): { id: string } | null {
  if (!contactId) {
    return null;
  }
  const linkedId =
    contacts.find((c) => c.id === contactId)?.linkedUserIdentityId?.trim() || null;
  const match = rows.find((row) => {
    if (!isSharedRelationship(row)) {
      return false;
    }
    if (mode === "lent") {
      return (
        row.debtorContactId === contactId ||
        (linkedId != null && row.debtorUserIdentityId === linkedId)
      );
    }
    return (
      row.creditorContactId === contactId ||
      (linkedId != null && row.creditorUserIdentityId === linkedId)
    );
  });
  return match ? { id: match.id } : null;
}

function PendingOutgoingHint({
  count,
  name,
  atLimit,
  viewPendingTo,
}: {
  count: number;
  name: string;
  atLimit: boolean;
  viewPendingTo: string;
}) {
  const { t } = useI18n();
  if (count < 1) {
    return null;
  }
  if (atLimit) {
    return (
      <div className="flex min-w-0 flex-col gap-2" data-testid="utang-pending-limit-hint">
        <p
          role="alert"
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {t("personal.utang.pendingLimitReached").replace("{name}", name)}
        </p>
        <Button asChild variant="ghost" className="min-h-11 w-fit px-0">
          <Link to={viewPendingTo} data-testid="utang-view-pending">
            {t("personal.utang.viewPending")}
          </Link>
        </Button>
      </div>
    );
  }
  return (
    <p
      className="m-0 text-[length:var(--exits-text-sm)] text-muted"
      data-testid="utang-pending-waiting-hint"
    >
      {t("personal.utang.pendingWaitingCount")
        .replace("{count}", String(count))
        .replace("{name}", name)}
    </p>
  );
}

function DueChip({ dueDateUtc }: { dueDateUtc: string | null | undefined }) {
  const { t } = useI18n();
  const due = formatDueLabel(dueDateUtc);
  if (due.kind === "none" || !due.iso) return null;
  const label =
    due.kind === "overdue"
      ? t("personal.utang.dueOverdue")
      : due.kind === "dueSoon"
        ? t("personal.utang.dueSoon")
        : t("personal.utang.dueUpcoming");
  return (
    <span className="text-[length:var(--exits-text-xs)] text-muted">
      {label}: {new Date(due.iso).toLocaleDateString()}
    </span>
  );
}

/** A server row and a cached row render the same way, so the origin is read defensively. */
function rowOrigin(row: object): "Server" | "Local" {
  return (row as { origin?: unknown }).origin === "Local" ? "Local" : "Server";
}

/** Marks a row that exists only on this device until the outbox drains. */
function WaitingChip({ origin }: { origin: "Server" | "Local" }) {
  const { t } = useI18n();
  if (origin !== "Local") return null;
  return (
    <span
      className="text-[length:var(--exits-text-xs)] text-muted"
      data-testid="utang-waiting-chip"
    >
      {t("offline.personalWaitingBadge")}
    </span>
  );
}

function OfflineNotice({ message }: { message: string }) {
  return (
    <p
      className="m-0 text-[length:var(--exits-text-sm)] text-muted"
      data-testid="utang-offline-notice"
    >
      {message}
    </p>
  );
}

/**
 * Cached Personal Utang read state.
 *
 * Offline, the network queries are switched off entirely rather than left to fail, so the page
 * renders the encrypted Personal cache instead of an error. The cache is also the fallback when an
 * online read fails, because a person who just lost signal should still see who owes them money.
 */
function usePersonalUtangCache<T>(
  load: (context: { db: NonNullable<ReturnType<typeof usePersonalOfflineContext>> }) => Promise<T>,
  deps: ReadonlyArray<unknown>,
  fallback: T,
): T {
  const offline = usePersonalOfflineContext();
  const [value, setValue] = useState<T>(fallback);

  useEffect(() => {
    if (!offline) {
      return;
    }
    let cancelled = false;
    void load({ db: offline }).then((loaded) => {
      if (!cancelled) {
        setValue(loaded);
      }
    });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [offline, ...deps]);

  return value;
}

export function PersonalContactsPage() {
  return <Navigate to="/personal/people" replace />;
}

function RelationshipListPage({ mode }: { mode: "lent" | "owe" }) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const { refreshCounts: refreshOfflineSync } = useOfflineSync();
  const perspective = mode === "lent" ? "Lent" : "Borrowed";
  const [contactId, setContactId] = useState("");
  const [amount, setAmount] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [notes, setNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [cacheEpoch, setCacheEpoch] = useState(0);

  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
    enabled: online,
  });
  const meQuery = useQuery({
    queryKey: ["personal", "me"],
    queryFn: ({ signal }) => getPersonalMe(signal),
    enabled: online,
  });
  const listQuery = useQuery({
    queryKey: ["personal", "utang", mode],
    queryFn: ({ signal }) =>
      mode === "lent" ? listLentRelationships(signal) : listBorrowedRelationships(signal),
    enabled: online,
  });

  useEffect(() => {
    if (!offline) {
      return;
    }
    if (contactsQuery.data) {
      void cachePersonalContacts(offline.db, offline.scopeBinding, contactsQuery.data);
    }
    if (listQuery.data) {
      void cachePersonalRelationships(
        offline.db,
        offline.scopeBinding,
        perspective,
        listQuery.data,
      );
    }
    if (meQuery.data) {
      void cachePersonalUserIdentityId(offline.db, meQuery.data.userIdentityId);
    }
  }, [contactsQuery.data, listQuery.data, meQuery.data, offline, perspective]);

  const cachedContacts = usePersonalUtangCache<CachedPersonalContact[]>(
    ({ db }) => listCachedPersonalContacts(db.db, db.scopeBinding),
    [cacheEpoch, contactsQuery.dataUpdatedAt],
    [],
  );
  const cachedRows = usePersonalUtangCache<CachedPersonalRelationship[]>(
    ({ db }) => listCachedPersonalRelationships(db.db, db.scopeBinding, perspective),
    [cacheEpoch, listQuery.dataUpdatedAt, perspective],
    [],
  );
  const cachedOwnerId = usePersonalUtangCache<string | null>(
    ({ db }) => getCachedPersonalUserIdentityId(db.db),
    [cacheEpoch, meQuery.dataUpdatedAt],
    null,
  );

  const usingCache = !online || listQuery.isError || contactsQuery.isError;
  const ownerUserIdentityId = meQuery.data?.userIdentityId ?? cachedOwnerId;

  const contacts: CachedPersonalContact[] | PersonalContactDto[] = usingCache
    ? cachedContacts
    : (contactsQuery.data ?? []);
  const rows: CachedPersonalRelationship[] | PersonalDebtRelationshipSummaryDto[] = usingCache
    ? cachedRows
    : (listQuery.data ?? []);
  const selectedLinked = contactId ? contactLooksLinked(contacts, contactId) : false;
  const existingSharedForContact =
    selectedLinked && contactId
      ? findSharedRelationshipForContact(rows, contacts, contactId, mode)
      : null;

  const sharedHistoryQuery = useQuery({
    queryKey: ["personal", "utang", "history", existingSharedForContact?.id ?? ""],
    enabled: Boolean(existingSharedForContact?.id) && online && selectedLinked,
    queryFn: ({ signal }) => listPersonalUtangHistory(existingSharedForContact!.id, signal),
  });

  const pendingOutgoingCount = useMemo(
    () =>
      selectedLinked && existingSharedForContact
        ? countPendingOutgoingLoanProposals(sharedHistoryQuery.data ?? [])
        : 0,
    [existingSharedForContact, selectedLinked, sharedHistoryQuery.data],
  );
  const pendingAtLimit = pendingOutgoingCount >= PERSONAL_UTANG_MAX_PENDING_OUTGOING;

  const saveOffline = async () => {
    if (!offline) {
      throw new Error("offline-unavailable");
    }
    if (!ownerUserIdentityId) {
      throw new Error("owner-unknown");
    }
    const purpose = notes.trim();
    if (!purpose) {
      throw new Error("purpose");
    }
    const id = createSecureMutationId();
    if (!id.ok) {
      throw new Error("id-unavailable");
    }
    // A contact added offline has no server id yet, so the queued debt waits for it.
    const contact = cachedContacts.find((row) => row.id === contactId);
    const contactIsLocal = contact?.origin === "Local";
    const { relationship } = await enqueuePersonalRelationshipCreate({
      db: offline.db,
      scopeBinding: offline.scopeBinding,
      userId: offline.userId,
      relationshipId: id.id,
      perspective,
      contactId,
      contactIsLocal,
      dependsOnContactOperationId: contactIsLocal ? contactId : null,
      ownerUserIdentityId,
      dueDateUtc: dueDate ? new Date(dueDate).toISOString() : null,
      initialLoanAmount: Number(amount),
      initialLoanNotes: purpose,
    });
    await refreshOfflineSync();
    setCacheEpoch((epoch) => epoch + 1);
    return relationship;
  };

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!online) {
        return saveOffline();
      }
      const me = meQuery.data?.userIdentityId;
      if (!me) throw new Error("missing me");
      if (!contactId) throw new Error("missing contact");
      const initial = Number(amount);
      if (!(initial > 0)) throw new Error("amount");
      const purpose = notes.trim();
      if (!purpose) throw new Error("purpose");
      const body =
        mode === "lent"
          ? {
              creditorUserIdentityId: me,
              creditorContactId: null,
              debtorUserIdentityId: null,
              debtorContactId: contactId,
              currencyCode: "PHP",
              dueDateUtc: dueDate ? new Date(dueDate).toISOString() : null,
              initialLoanAmount: initial,
              initialLoanNotes: purpose,
            }
          : {
              creditorUserIdentityId: null,
              creditorContactId: contactId,
              debtorUserIdentityId: me,
              debtorContactId: null,
              currencyCode: "PHP",
              dueDateUtc: dueDate ? new Date(dueDate).toISOString() : null,
              initialLoanAmount: initial,
              initialLoanNotes: purpose,
            };
      return createPersonalDebtRelationship(body);
    },
    onSuccess: async (created) => {
      setContactId("");
      setAmount("");
      setDueDate("");
      setNotes("");
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
      await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
      navigate(`/personal/utang/relationships/${created.id}`);
    },
    onError: (error) => {
      if (!online) {
        if (error instanceof Error && error.message === "purpose") {
          setFormError(t("personal.utang.purposeRequired"));
          return;
        }
        setFormError(
          error instanceof Error && error.message === "owner-unknown"
            ? t("offline.personalOwnerUnknown")
            : t("offline.personalEnqueueFailed"),
        );
        return;
      }
      if (error instanceof Error && error.message === "purpose") {
        setFormError(t("personal.utang.purposeRequired"));
        return;
      }
      const name =
        contacts.find((c) => c.id === contactId)?.displayName ?? t("personal.utang.person");
      setFormError(mapPersonalUtangMutationError(error, name, t));
    },
  });

  if (online && (listQuery.isPending || contactsQuery.isPending || meQuery.isPending)) {
    return <LoadingSkeleton />;
  }
  if (online && (listQuery.isError || contactsQuery.isError) && cachedRows.length === 0) {
    return (
      <ErrorState
        title={t("personal.utang.loadErrorTitle")}
        detail={t("personal.utang.loadErrorDetail")}
      />
    );
  }

  const title = mode === "lent" ? t("personal.utang.lent") : t("personal.utang.owe");
  const lede = mode === "lent" ? t("personal.utang.lentLede") : t("personal.utang.oweLede");
  const submitLabel = selectedLinked
    ? t("personal.utang.sendForConfirmation")
    : t("personal.utang.saveUtang");
  const selectedContactName =
    contactId
      ? (contacts.find((c) => c.id === contactId)?.displayName ?? t("personal.utang.person"))
      : "";
  const viewPendingTo = existingSharedForContact
    ? `/personal/utang/relationships/${existingSharedForContact.id}`
    : "/personal/utang";

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid={mode === "lent" ? "personal-utang-lent" : "personal-utang-owe"}
    >
      <PageHeader
        title={title}
        description={lede}
        backTo={personalPageBackNav.utang.to}
        backLabel={t("personal.utang.back")}
        backTestId={mode === "lent" ? "page-header-back-utang-lent" : "page-header-back-utang-owe"}
      />

      {usingCache ? <OfflineNotice message={t("offline.personalCachedNotice")} /> : null}

      <form
        className="catalog-form-section exits-animate-panel personal-section flex min-w-0 flex-col gap-2 overflow-hidden"
        onSubmit={(event) => {
          event.preventDefault();
          if (!contactId) {
            setFormError(t("personal.utang.personRequired"));
            return;
          }
          if (!(Number(amount) > 0)) {
            setFormError(t("personal.utang.amountRequired"));
            return;
          }
          if (!notes.trim()) {
            setFormError(t("personal.utang.purposeRequired"));
            return;
          }
          if (pendingAtLimit) {
            setFormError(
              t("personal.utang.pendingLimitReached").replace(
                "{name}",
                selectedContactName || t("personal.utang.person"),
              ),
            );
            return;
          }
          createMutation.mutate();
        }}
      >
        <h2 className="catalog-form-section__title">{t("personal.utang.recordUtang")}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {mode === "lent" ? t("personal.utang.whatHappenedLent") : t("personal.utang.whatHappenedBorrowed")}
        </p>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.person")}
          <select
            data-testid="utang-rel-contact"
            className="min-h-11 w-full min-w-0 max-w-full rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={contactId}
            onChange={(e) => setContactId(e.target.value)}
            required
          >
            <option value="">{t("personal.utang.choosePerson")}</option>
            {contacts.map((c) => (
              <option key={c.id} value={c.id}>
                {c.displayName}
              </option>
            ))}
          </select>
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.amount")}
          <input
            data-testid="utang-rel-amount"
            inputMode="decimal"
            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.purpose")}
          <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
            {t("personal.utang.purposeHelp")}
          </span>
          <textarea
            data-testid="utang-rel-notes"
            className="min-h-20 w-full min-w-0 resize-y rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
            value={notes}
            maxLength={UTANG_NOTES_MAX_LENGTH}
            aria-required="true"
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.dueDate")}
          <input
            data-testid="utang-rel-due"
            type="date"
            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </label>
        {contactId && Number(amount) > 0 && notes.trim() ? (
          <div
            className="rounded-[var(--exits-radius-md)] border border-border bg-[color-mix(in_srgb,var(--exits-surface)_92%,var(--exits-muted)_8%)] p-3 text-[length:var(--exits-text-sm)]"
            data-testid="utang-rel-review"
          >
            <p className="m-0 font-semibold">
              {mode === "lent"
                ? t("personal.utang.reviewLent")
                    .replace("{name}", selectedContactName)
                    .replace("{amount}", amount)
                : t("personal.utang.reviewBorrowed")
                    .replace("{name}", selectedContactName)
                    .replace("{amount}", amount)}
            </p>
            <p className="m-0 mt-1 text-muted">
              {t("personal.utang.purpose")}: {notes.trim()}
            </p>
          </div>
        ) : null}
        {!online ? <OfflineNotice message={t("offline.personalUtangWillQueue")} /> : null}
        {selectedLinked ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="utang-rel-confirm-hint"
          >
            {t("personal.utang.sendForConfirmationHint").replace("{name}", selectedContactName)}
          </p>
        ) : contactId ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="utang-rel-private-hint">
            {t("personal.utang.privateSaveHint")}
          </p>
        ) : null}
        {selectedLinked && existingSharedForContact && pendingOutgoingCount > 0 ? (
          <PendingOutgoingHint
            count={pendingOutgoingCount}
            name={selectedContactName}
            atLimit={pendingAtLimit}
            viewPendingTo={viewPendingTo}
          />
        ) : null}
        {formError ? (
          <p
            role="alert"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {formError}
          </p>
        ) : null}
        <Button
          type="submit"
          className="min-h-11"
          disabled={
            createMutation.isPending ||
            contacts.length === 0 ||
            pendingAtLimit ||
            (!online && (!offline || !ownerUserIdentityId))
          }
          data-testid="utang-rel-submit"
        >
          {createMutation.isPending ? (
            <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          ) : mode === "lent" ? (
            <HandCoins className="size-4 shrink-0" aria-hidden />
          ) : (
            <Wallet className="size-4 shrink-0" aria-hidden />
          )}
          {submitLabel}
        </Button>
        {contacts.length === 0 ? (
          <Button asChild variant="ghost" className="min-h-11">
            <Link to="/personal/people">
              <UserPlus className="size-4 shrink-0" aria-hidden />
              {t("personal.utang.addPersonFirst")}
            </Link>
          </Button>
        ) : null}
      </form>

      {rows.length === 0 ? (
        <EmptyState
          title={t("personal.utang.listEmptyTitle")}
          detail={t("personal.utang.listEmptyDetail")}
        />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0">
          {rows.map((row) => {
            const name = contactLabel(contacts, row);
            const shared = isSharedRelationship(row);
            const ledgerLabel = shared
              ? t("personal.utang.linked")
              : t("personal.utang.notLinkedToExits");
            const perspectiveLabel =
              mode === "lent" ? t("personal.utang.owesYou") : t("personal.utang.youOwe");
            return (
              <li key={row.id}>
                <Link
                  to={`/personal/utang/relationships/${row.id}`}
                  className="exits-list__card flex min-h-11 items-center justify-between gap-3 text-foreground no-underline"
                  data-testid={`utang-rel-row-${row.id}`}
                >
                  <div className="min-w-0">
                    <p className="exits-list__name m-0 truncate font-semibold">{name}</p>
                    <p className="m-0 truncate text-[length:var(--exits-text-sm)] text-muted">
                      {perspectiveLabel}
                      {" · "}
                      <span data-testid={`utang-rel-ledger-${row.id}`}>{ledgerLabel}</span>
                    </p>
                    <DueChip dueDateUtc={row.dueDateUtc} />
                    <WaitingChip origin={rowOrigin(row)} />
                  </div>
                  <MoneyDisplay amount={row.currentBalance} />
                </Link>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}

export function PersonalLentPage() {
  return <RelationshipListPage mode="lent" />;
}

export function PersonalOwePage() {
  return <RelationshipListPage mode="owe" />;
}

export function PersonalRelationshipDetailPage() {
  const { t } = useI18n();
  const { relationshipId = "" } = useParams();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const { refreshCounts: refreshOfflineSync } = useOfflineSync();
  const [entryType, setEntryType] = useState<"Payment" | "Loan" | "Adjustment">("Payment");
  const [amount, setAmount] = useState("");
  const [adjustmentDelta, setAdjustmentDelta] = useState("");
  const [notes, setNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [cacheEpoch, setCacheEpoch] = useState(0);
  const [disputeEntryId, setDisputeEntryId] = useState<string | null>(null);
  const [disputeReasonKey, setDisputeReasonKey] = useState<
    "amount" | "notReceived" | "other" | ""
  >("");
  const [actionError, setActionError] = useState<string | null>(null);

  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
    enabled: online,
  });
  const detailQuery = useQuery({
    queryKey: ["personal", "utang", "relationship", relationshipId],
    enabled: Boolean(relationshipId) && online,
    queryFn: ({ signal }) => getPersonalDebtRelationship(relationshipId, signal),
  });
  const balanceQuery = useQuery({
    queryKey: ["personal", "utang", "balance", relationshipId],
    enabled: Boolean(relationshipId) && online,
    queryFn: ({ signal }) => getPersonalUtangBalance(relationshipId, signal),
  });
  const historyQuery = useQuery({
    queryKey: ["personal", "utang", "history", relationshipId],
    enabled: Boolean(relationshipId) && online,
    queryFn: ({ signal }) => listPersonalUtangHistory(relationshipId, signal),
  });

  useEffect(() => {
    if (!offline) {
      return;
    }
    if (contactsQuery.data) {
      void cachePersonalContacts(offline.db, offline.scopeBinding, contactsQuery.data);
    }
    if (detailQuery.data) {
      void cachePersonalRelationship(
        offline.db,
        offline.scopeBinding,
        detailQuery.data.perspective === "Borrowed" ? "Borrowed" : "Lent",
        detailQuery.data,
      );
    }
    if (historyQuery.data) {
      void cachePersonalEntries(offline.db, offline.scopeBinding, historyQuery.data);
    }
  }, [contactsQuery.data, detailQuery.data, historyQuery.data, offline]);

  const cachedContacts = usePersonalUtangCache<CachedPersonalContact[]>(
    ({ db }) => listCachedPersonalContacts(db.db, db.scopeBinding),
    [cacheEpoch, contactsQuery.dataUpdatedAt],
    [],
  );
  const cachedDetail = usePersonalUtangCache<CachedPersonalRelationship | null>(
    ({ db }) => getCachedPersonalRelationship(db.db, db.scopeBinding, relationshipId),
    [cacheEpoch, detailQuery.dataUpdatedAt, relationshipId],
    null,
  );
  const cachedHistory = usePersonalUtangCache<CachedPersonalEntry[]>(
    ({ db }) => listCachedPersonalEntries(db.db, db.scopeBinding, relationshipId),
    [cacheEpoch, historyQuery.dataUpdatedAt, relationshipId],
    [],
  );

  const usingCache = !online || detailQuery.isError || balanceQuery.isError;
  const detail = usingCache ? cachedDetail : (detailQuery.data ?? null);
  // No live balance offline: the cached relationship's own balance is the last agreed figure.
  const currentBalance = usingCache
    ? (cachedDetail?.currentBalance ?? 0)
    : (balanceQuery.data?.currentBalance ?? 0);
  const history: CachedPersonalEntry[] | PersonalUtangEntryDto[] = usingCache
    ? cachedHistory
    : (historyQuery.data ?? []);
  const relationshipIsLocal = cachedDetail?.origin === "Local";
  const pendingOutgoingCount = useMemo(
    () =>
      detail && isSharedRelationship(detail)
        ? countPendingOutgoingLoanProposals(history)
        : 0,
    [detail, history],
  );
  const pendingAtLimit = pendingOutgoingCount >= PERSONAL_UTANG_MAX_PENDING_OUTGOING;

  const invalidateUtang = async () => {
    await queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
    await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
  };

  const queueEntryOffline = async () => {
    if (!offline || !detail) {
      throw new Error("offline-unavailable");
    }
    if (entryType === "Adjustment") {
      throw new Error("adjustment-online-only");
    }
    const id = createSecureMutationId();
    if (!id.ok) {
      throw new Error("id-unavailable");
    }
    const owner =
      detail.perspective === "Borrowed"
        ? detail.debtorUserIdentityId
        : detail.creditorUserIdentityId;
    await enqueuePersonalUtangEntry({
      db: offline.db,
      scopeBinding: offline.scopeBinding,
      userId: offline.userId,
      entryId: id.id,
      relationshipId,
      relationshipIsLocal,
      dependsOnRelationshipOperationId: relationshipIsLocal ? relationshipId : null,
      entryType,
      amount: Number(amount),
      notes: notes.trim() || null,
      ownerUserIdentityId: owner ?? offline.userId,
      localBalanceBefore: currentBalance,
    });
    await refreshOfflineSync();
    setCacheEpoch((epoch) => epoch + 1);
  };

  const recordMutation = useMutation({
    mutationFn: async () => {
      const amt = Number(amount);
      if (!(amt > 0)) throw new Error("amount");
      const purpose = notes.trim();
      if ((entryType === "Loan" || entryType === "Adjustment") && !purpose) {
        throw new Error("purpose");
      }
      if (!online) {
        await queueEntryOffline();
        return;
      }
      const version = balanceQuery.data?.version ?? detailQuery.data?.version;
      const body =
        entryType === "Adjustment"
          ? {
              entryType: "Adjustment" as const,
              amount: amt,
              adjustmentDelta: Number(adjustmentDelta),
              expectedVersion: version ?? null,
              notes: purpose,
            }
          : {
              entryType,
              amount: amt,
              expectedVersion: version ?? null,
              notes: entryType === "Loan" ? purpose : purpose || null,
            };
      await recordPersonalUtangEntry(relationshipId, body);
    },
    onSuccess: async () => {
      setAmount("");
      setAdjustmentDelta("");
      setNotes("");
      setFormError(null);
      await invalidateUtang();
    },
    onError: (error) => {
      if (!online) {
        setFormError(
          error instanceof Error && error.message === "adjustment-online-only"
            ? t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangAdjustment))
            : error instanceof Error && error.message === "purpose"
              ? t("personal.utang.purposeRequired")
              : t("offline.personalEnqueueFailed"),
        );
        return;
      }
      if (error instanceof Error && error.message === "purpose") {
        setFormError(
          entryType === "Adjustment"
            ? t("personal.utang.adjustmentReasonRequired")
            : t("personal.utang.purposeRequired"),
        );
        return;
      }
      if (isUtangConcurrencyConflict(error)) {
        setFormError(t("personal.utang.concurrencyConflict"));
        void balanceQuery.refetch();
        void detailQuery.refetch();
        void historyQuery.refetch();
        return;
      }
      const labelContacts = usingCache ? cachedContacts : (contactsQuery.data ?? []);
      const name =
        detail && labelContacts.length > 0
          ? contactLabel(labelContacts, detail)
          : t("personal.utang.person");
      setFormError(mapPersonalUtangMutationError(error, name === EM_DASH ? "" : name, t));
    },
  });

  const resolveMutation = useMutation({
    mutationFn: async (input: {
      action: "confirm" | "dispute" | "cancel";
      entryId: string;
      reason?: string | null;
    }) => {
      const version = balanceQuery.data?.version ?? detailQuery.data?.version ?? null;
      if (input.action === "confirm") {
        return confirmPersonalUtangEntry(relationshipId, input.entryId, {
          expectedVersion: version,
        });
      }
      if (input.action === "dispute") {
        return disputePersonalUtangEntry(relationshipId, input.entryId, {
          expectedVersion: version,
          reason: input.reason ?? null,
        });
      }
      return cancelPersonalUtangEntry(relationshipId, input.entryId, {
        expectedVersion: version,
      });
    },
    onSuccess: async () => {
      setActionError(null);
      setDisputeEntryId(null);
      setDisputeReasonKey("");
      await invalidateUtang();
    },
    onError: (error) => {
      if (isUtangConcurrencyConflict(error)) {
        setActionError(t("personal.utang.concurrencyConflict"));
        void balanceQuery.refetch();
        void detailQuery.refetch();
        void historyQuery.refetch();
        return;
      }
      setActionError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
    },
  });

  const contactsForLabel = useMemo<CachedPersonalContact[] | PersonalContactDto[]>(
    () => (usingCache ? cachedContacts : (contactsQuery.data ?? [])),
    [cachedContacts, contactsQuery.data, usingCache],
  );

  const personName = useMemo(() => {
    if (!detail || contactsForLabel.length === 0) return EM_DASH;
    return contactLabel(contactsForLabel, detail);
  }, [contactsForLabel, detail]);

  if (online && (detailQuery.isPending || balanceQuery.isPending || historyQuery.isPending)) {
    return <LoadingSkeleton />;
  }
  if (!detail) {
    return (
      <ErrorState
        title={t("personal.utang.loadErrorTitle")}
        detail={t("personal.utang.loadErrorDetail")}
      />
    );
  }

  const shared = isSharedRelationship(detail);
  const perspectiveLabel =
    detail.perspective === "Borrowed"
      ? t("personal.utang.perspectiveDebtor")
      : t("personal.utang.perspectiveCreditor");
  const ledgerLabel = shared
    ? t("personal.utang.sharedLedger")
    : t("personal.utang.privateRecord");
  const listBack =
    detail.perspective === "Borrowed" ? personalPageBackNav.utangOwe : personalPageBackNav.utangLent;
  // An Adjustment rewrites a balance against a version this device may no longer be showing.
  const adjustmentBlocked = !online && entryType === "Adjustment";
  const loanBlockedByPendingLimit = shared && pendingAtLimit && entryType === "Loan";
  const submitLabel = shared
    ? t("personal.utang.sendForConfirmation")
    : t("personal.utang.saveEntry");
  const viewPendingTo = `/personal/utang/relationships/${relationshipId}`;

  const disputeReasonText = (): string | null => {
    if (disputeReasonKey === "amount") return t("personal.utang.disputeReasonAmount");
    if (disputeReasonKey === "notReceived") return t("personal.utang.disputeReasonNotReceived");
    if (disputeReasonKey === "other") return t("personal.utang.disputeReasonOther");
    return null;
  };

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-utang-detail"
    >
      <PageHeader
        title={personName}
        description={perspectiveLabel}
        backTo={listBack.to}
        backLabel={t(listBack.labelKey)}
        backTestId="page-header-back-utang-detail"
      />
      {usingCache ? <OfflineNotice message={t("offline.personalCachedNotice")} /> : null}
      <div className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3">
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{perspectiveLabel}</p>
        <MoneyDisplay amount={currentBalance} className="text-[length:var(--exits-text-xl)]" />
        <p
          className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="utang-detail-ledger"
        >
          {ledgerLabel}
        </p>
        <DueChip dueDateUtc={detail.dueDateUtc} />
        <WaitingChip origin={relationshipIsLocal ? "Local" : "Server"} />
      </div>

      <form
        className="catalog-form-section exits-animate-panel personal-section flex min-w-0 flex-col gap-2 overflow-hidden"
        onSubmit={(event) => {
          event.preventDefault();
          if (!(Number(amount) > 0)) {
            setFormError(t("personal.utang.amountRequired"));
            return;
          }
          if ((entryType === "Loan" || entryType === "Adjustment") && !notes.trim()) {
            setFormError(
              entryType === "Adjustment"
                ? t("personal.utang.adjustmentReasonRequired")
                : t("personal.utang.purposeRequired"),
            );
            return;
          }
          if (loanBlockedByPendingLimit) {
            setFormError(
              t("personal.utang.pendingLimitReached").replace(
                "{name}",
                personName === EM_DASH ? t("personal.utang.person") : personName,
              ),
            );
            return;
          }
          recordMutation.mutate();
        }}
      >
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.entryType")}
          <select
            data-testid="utang-entry-type"
            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={entryType}
            onChange={(e) => setEntryType(e.target.value as typeof entryType)}
          >
            <option value="Payment">{t("personal.utang.recordPayment")}</option>
            <option value="Loan">{t("personal.utang.addAmount")}</option>
            <option value="Adjustment">{t("personal.utang.adjustBalance")}</option>
          </select>
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.amount")}
          <input
            data-testid="utang-entry-amount"
            inputMode="decimal"
            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>
        {entryType === "Adjustment" ? (
          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.utang.adjustmentDelta")}
            <input
              data-testid="utang-entry-delta"
              inputMode="decimal"
              className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={adjustmentDelta}
              onChange={(e) => setAdjustmentDelta(e.target.value)}
              required
            />
          </label>
        ) : null}
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {entryType === "Payment"
            ? t("personal.utang.noteOptional")
            : entryType === "Adjustment"
              ? t("personal.utang.adjustmentReason")
              : t("personal.utang.purpose")}
          {entryType !== "Payment" ? (
            <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
              {entryType === "Adjustment"
                ? t("personal.utang.adjustmentReasonHelp")
                : t("personal.utang.purposeHelp")}
            </span>
          ) : null}
          <textarea
            data-testid="utang-entry-notes"
            className="min-h-16 w-full min-w-0 resize-y rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
            value={notes}
            maxLength={UTANG_NOTES_MAX_LENGTH}
            aria-required={entryType !== "Payment"}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
        {adjustmentBlocked ? (
          <OfflineNotice
            message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangAdjustment))}
          />
        ) : null}
        {!online && !adjustmentBlocked ? (
          <OfflineNotice message={t("offline.personalEntryWillQueue")} />
        ) : null}
        {shared && online ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="utang-entry-confirm-hint"
          >
            {t("personal.utang.sendForConfirmationHint").replace("{name}", personName)}
          </p>
        ) : null}
        {shared && pendingOutgoingCount > 0 ? (
          <PendingOutgoingHint
            count={pendingOutgoingCount}
            name={personName === EM_DASH ? t("personal.utang.person") : personName}
            atLimit={pendingAtLimit}
            viewPendingTo={viewPendingTo}
          />
        ) : null}
        {formError ? (
          <p
            role="alert"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {formError}
          </p>
        ) : null}
        <Button
          type="submit"
          className="min-h-11"
          disabled={
            recordMutation.isPending ||
            adjustmentBlocked ||
            loanBlockedByPendingLimit ||
            (!online && !offline)
          }
          data-testid="utang-entry-submit"
        >
          {submitLabel}
        </Button>
      </form>

      {online ? (
        <RelationshipInviteReminderPanel
          relationshipId={relationshipId}
          inviteeContactId={
            detail.perspective === "Borrowed" ? detail.creditorContactId : detail.debtorContactId
          }
        />
      ) : (
        <OfflineNotice
          message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangInvite))}
        />
      )}

      <section
        className="catalog-form-section exits-animate-panel personal-section min-w-0 gap-2 overflow-hidden"
        aria-label={t("personal.utang.activity")}
      >
        <h2 className="catalog-form-section__title">{t("personal.utang.activity")}</h2>
        {actionError ? (
          <p
            role="alert"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {actionError}
          </p>
        ) : null}
        {history.length === 0 ? (
          <EmptyState
            title={t("personal.utang.historyEmptyTitle")}
            detail={t("personal.utang.historyEmptyDetail")}
          />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="utang-history">
            {history.map((entry) => {
              const status = "status" in entry ? entry.status : "Confirmed";
              const canConfirm = "canConfirm" in entry ? Boolean(entry.canConfirm) : false;
              const canDispute = "canDispute" in entry ? Boolean(entry.canDispute) : false;
              const canCancel = "canCancel" in entry ? Boolean(entry.canCancel) : false;
              const affectsBalance =
                "affectsBalance" in entry
                  ? Boolean(entry.affectsBalance)
                  : status === "Confirmed";
              const disputeReason =
                "disputeReason" in entry ? (entry.disputeReason ?? null) : null;
              const pendingIncoming = status === "Pending" && (canConfirm || canDispute);
              const pendingOutgoing = status === "Pending" && canCancel && !canConfirm;
              const confirmLabel =
                entry.entryType === "Payment"
                  ? t("personal.utang.confirmReceived")
                  : t("personal.utang.confirm");
              const isDisputing = disputeEntryId === entry.id;

              return (
                <li key={entry.id}>
                  <div
                    className="exits-list__card flex min-w-0 flex-col gap-2"
                    data-testid={`utang-history-entry-${entry.id}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="exits-list__name m-0 font-medium">
                          {entry.entryType === "Loan"
                            ? loanActivityLabel(
                                detail.perspective,
                                personName,
                                pendingIncoming,
                                t,
                              )
                            : t(entryTypeLabelKey(entry.entryType))}
                        </p>
                        {pendingIncoming ? (
                          <p
                            className="m-0 text-[length:var(--exits-text-sm)] font-medium"
                            data-testid={`utang-waiting-you-${entry.id}`}
                          >
                            {t("personal.utang.recordedForReview").replace("{name}", personName)}
                          </p>
                        ) : null}
                        {pendingIncoming ? (
                          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                            {t("personal.utang.waitingForYou")}
                          </p>
                        ) : null}
                        {entry.notes ? (
                          <p
                            className="m-0 text-[length:var(--exits-text-sm)]"
                            data-testid={`utang-entry-purpose-${entry.id}`}
                          >
                            <span className="text-muted">{t("personal.utang.purpose")}: </span>
                            {entry.notes}
                          </p>
                        ) : null}
                        {"dueDateUtc" in entry && entry.dueDateUtc ? (
                          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                            {t("personal.utang.dueDate")}:{" "}
                            {new Date(String(entry.dueDateUtc)).toLocaleDateString()}
                          </p>
                        ) : null}
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)]"
                          data-testid={`utang-entry-status-${entry.id}`}
                        >
                          {t(entryStatusLabelKey(status))}
                        </p>
                        {pendingOutgoing ? (
                          <p
                            className="m-0 text-[length:var(--exits-text-sm)] font-medium"
                            data-testid={`utang-waiting-other-${entry.id}`}
                          >
                            {t("personal.utang.waitingForName").replace("{name}", personName)}
                          </p>
                        ) : null}
                        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                          {new Date(entry.createdAtUtc).toLocaleString()}
                        </p>
                        {disputeReason ? (
                          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                            {disputeReason}
                          </p>
                        ) : null}
                        <WaitingChip origin={rowOrigin(entry)} />
                      </div>
                      <div className="shrink-0 text-right">
                        <MoneyDisplay amount={entry.signedDelta} />
                        {affectsBalance ? (
                          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                            {t("personal.utang.balanceAfter")}:{" "}
                            <MoneyDisplay amount={entry.balanceAfter} />
                          </p>
                        ) : (
                          <p
                            className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                            data-testid={`utang-no-balance-${entry.id}`}
                          >
                            {t("personal.utang.noBalanceChange")}
                          </p>
                        )}
                      </div>
                    </div>

                    {pendingIncoming && online ? (
                      <div className="flex min-w-0 flex-wrap gap-2">
                        {canConfirm ? (
                          <Button
                            type="button"
                            className="min-h-11"
                            disabled={resolveMutation.isPending}
                            data-testid={`utang-confirm-${entry.id}`}
                            onClick={() =>
                              resolveMutation.mutate({ action: "confirm", entryId: entry.id })
                            }
                          >
                            {confirmLabel}
                          </Button>
                        ) : null}
                        {canDispute ? (
                          <Button
                            type="button"
                            variant="ghost"
                            className="min-h-11"
                            disabled={resolveMutation.isPending}
                            data-testid={`utang-dispute-${entry.id}`}
                            onClick={() => {
                              setDisputeEntryId(entry.id);
                              setDisputeReasonKey("");
                            }}
                          >
                            {t("personal.utang.dispute")}
                          </Button>
                        ) : null}
                      </div>
                    ) : null}

                    {pendingOutgoing && online && canCancel ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11 w-fit"
                        disabled={resolveMutation.isPending}
                        data-testid={`utang-cancel-${entry.id}`}
                        onClick={() =>
                          resolveMutation.mutate({ action: "cancel", entryId: entry.id })
                        }
                      >
                        {t("personal.utang.cancelPending")}
                      </Button>
                    ) : null}

                    {isDisputing ? (
                      <div
                        className="flex min-w-0 flex-col gap-2"
                        data-testid={`utang-dispute-form-${entry.id}`}
                      >
                        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                          {t("personal.utang.disputeReason")}
                          <select
                            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                            value={disputeReasonKey}
                            data-testid={`utang-dispute-reason-${entry.id}`}
                            onChange={(e) =>
                              setDisputeReasonKey(
                                e.target.value as typeof disputeReasonKey,
                              )
                            }
                          >
                            <option value="">{EM_DASH}</option>
                            <option value="amount">
                              {t("personal.utang.disputeReasonAmount")}
                            </option>
                            <option value="notReceived">
                              {t("personal.utang.disputeReasonNotReceived")}
                            </option>
                            <option value="other">
                              {t("personal.utang.disputeReasonOther")}
                            </option>
                          </select>
                        </label>
                        <div className="flex min-w-0 flex-wrap gap-2">
                          <Button
                            type="button"
                            className="min-h-11"
                            disabled={resolveMutation.isPending}
                            data-testid={`utang-dispute-submit-${entry.id}`}
                            onClick={() =>
                              resolveMutation.mutate({
                                action: "dispute",
                                entryId: entry.id,
                                reason: disputeReasonText(),
                              })
                            }
                          >
                            {t("personal.utang.disputeSubmit")}
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            className="min-h-11"
                            disabled={resolveMutation.isPending}
                            onClick={() => {
                              setDisputeEntryId(null);
                              setDisputeReasonKey("");
                            }}
                          >
                            {t("personal.utang.disputeKeep")}
                          </Button>
                        </div>
                      </div>
                    ) : null}
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/utang">{t("personal.utang.back")}</Link>
      </Button>
    </div>
  );
}

