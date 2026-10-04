import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Link, Navigate, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Check, CheckCircle2, ChevronDown, CircleAlert, HandCoins, Loader2, PenLine, Plus, UserPlus, Users, Wallet } from "lucide-react";
import {
  cancelPersonalUtangEntry,
  closePersonalDebtRelationship,
  confirmPersonalUtangEntry,
  createPersonalDebtRelationship,
  disputePersonalUtangEntry,
  formatDueLabel,
  getPersonalDebtRelationship,
  getPersonalMe,
  getPersonalUtangBalance,
  getPersonalUtangEntry,
  isUtangConcurrencyConflict,
  isUtangSettlementStaleConflict,
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
  listPersonalUtangHistory,
  recordPersonalUtangEntry,
  settlePersonalDebtRelationship,
  type PersonalContactDto,
  type PersonalDebtRelationshipSummaryDto,
  type PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { StatusChip } from "@/components/exits/StatusChip";
import { UtangDueCaption, UtangDirectionTags } from "@/features/personal/utang/UtangListMeta";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import {
  isSharedRelationship as workspaceIsSharedRelationship,
  resolveRelationshipContactName,
} from "@/features/personal/utang/utang-workspace";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { RelationshipInviteReminderPanel } from "@/features/personal/social/PersonalSocialPages";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { cn } from "@/lib/cn";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { ONLINE_REQUIRED_CODES, onlineRequiredDetailKey } from "@/offline/online-required";
import { usePersonalOfflineContext } from "@/offline/personal-offline-context";
import {
  cachePersonalContacts,
  cachePersonalEntries,
  cachePersonalRelationship,
  cachePersonalRelationships,
  cachePersonalUserIdentityId,
  getCachedPersonalRelationship,
  listCachedPersonalContacts,
  listCachedPersonalEntries,
  listCachedPersonalRelationships,
  type CachedPersonalContact,
  type CachedPersonalEntry,
  type CachedPersonalRelationship,
} from "@/offline/personal-utang-cache";
import {
  isNotFoundStatus,
  resolveAmbiguousMutationOutcome,
} from "@/runtime/ambiguous-mutation-outcome";

const UTANG_NOTES_MAX_LENGTH = 512;
const EM_DASH = "\u2014";

function UtangRequiredMark() {
  const { t } = useI18n();
  return (
    <>
      <span
        className="text-[length:var(--exits-text-xs)] font-bold text-[var(--exits-danger)]"
        aria-hidden="true"
      >
        *
      </span>
      <span className="sr-only">{t("checkout.fieldRequired")}</span>
    </>
  );
}

function UtangFieldLabel({
  children,
  required = false,
}: {
  children: ReactNode;
  required?: boolean;
}) {
  return (
    <span className="inline-flex items-center gap-1">
      {children}
      {required ? <UtangRequiredMark /> : null}
    </span>
  );
}

function contactLabel(
  contacts: ReadonlyArray<
    Pick<PersonalContactDto, "id" | "displayName" | "linkedUserIdentityId">
  >,
  relationship: PersonalDebtRelationshipSummaryDto,
): string {
  return resolveRelationshipContactName(contacts, relationship);
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

function entryActionLabel(
  entryType: string,
  options: { isSettlement?: boolean },
  t: (key: MessageKey) => string,
): string {
  if (options.isSettlement) {
    return t("personal.utang.settlementEntry");
  }
  if (entryType === "Payment") {
    return t("personal.utang.recordPayment");
  }
  if (entryType === "Adjustment") {
    return t("personal.utang.adjustBalance");
  }
  return t("personal.utang.addAmount");
}

function entryStatusLabel(
  entry: {
    status?: string;
    isSharedLedger?: boolean;
    wasAutoSynced?: boolean;
  },
  options: { pendingIncoming?: boolean; reporterName?: string },
  t: (key: MessageKey) => string,
): string {
  const status = entry.status ?? "Confirmed";
  if (status === "Cancelled") return t("personal.utang.statusCancelled");
  if (status === "Disputed") return t("personal.utang.statusDisputed");
  if (status === "Pending") {
    if (options.pendingIncoming && options.reporterName) {
      return t("personal.utang.statusSharedReportedBy").replace(
        "{name}",
        options.reporterName,
      );
    }
    return t("personal.utang.statusPending");
  }
  if (entry.wasAutoSynced) return t("personal.utang.statusAutoSynced");
  if (!entry.isSharedLedger) return t("personal.utang.statusPrivate");
  return t("personal.utang.statusConfirmed");
}

function isSharedRelationship(
  row: Pick<PersonalDebtRelationshipSummaryDto, "isSharedLedger" | "isPrivate">,
): boolean {
  return workspaceIsSharedRelationship(row);
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
      | "isLedgerOwner"
      | "debtorContactId"
      | "creditorContactId"
      | "debtorUserIdentityId"
      | "creditorUserIdentityId"
    >
  >,
  contacts: ReadonlyArray<PersonalContactDto | CachedPersonalContact>,
  contactId: string,
  mode: "lent" | "owe",
  options?: { ownedOnly?: boolean; sharedWithMeOnly?: boolean },
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
    if (options?.ownedOnly && row.isLedgerOwner === false) {
      return false;
    }
    if (options?.sharedWithMeOnly && row.isLedgerOwner !== false) {
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
        <Button asChild variant="ghost" className="w-fit px-0">
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
  const perspective = mode === "lent" ? "Lent" : "Borrowed";
  const [contactId, setContactId] = useState("");
  const [amount, setAmount] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [notes, setNotes] = useState("");
  const [shareWithCounterparty, setShareWithCounterparty] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [recordFormOpen, setRecordFormOpen] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const pendingRelationshipIdRef = useRef<string | null>(null);
  const pendingInitialLoanEntryIdRef = useRef<string | null>(null);

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
    [contactsQuery.dataUpdatedAt],
    [],
  );
  const cachedRows = usePersonalUtangCache<CachedPersonalRelationship[]>(
    ({ db }) => listCachedPersonalRelationships(db.db, db.scopeBinding, perspective),
    [listQuery.dataUpdatedAt, perspective],
    [],
  );

  const usingCache = !online || listQuery.isError || contactsQuery.isError;

  const contacts: CachedPersonalContact[] | PersonalContactDto[] = usingCache
    ? (cachedContacts.length > 0 ? cachedContacts : (contactsQuery.data ?? []))
    : (contactsQuery.data ?? []);
  const rows: CachedPersonalRelationship[] | PersonalDebtRelationshipSummaryDto[] = usingCache
    ? (cachedRows.length > 0 ? cachedRows : (listQuery.data ?? []))
    : (listQuery.data ?? []);
  const selectedLinked = contactId ? contactLooksLinked(contacts, contactId) : false;
  const existingSharedOwnedByMe =
    selectedLinked && contactId
      ? findSharedRelationshipForContact(rows, contacts, contactId, mode, {
          ownedOnly: true,
        })
      : null;
  const existingSharedWithMe =
    selectedLinked && contactId
      ? findSharedRelationshipForContact(rows, contacts, contactId, mode, {
          sharedWithMeOnly: true,
        })
      : null;

  const sharedHistoryQuery = useQuery({
    queryKey: ["personal", "utang", "history", existingSharedOwnedByMe?.id ?? ""],
    enabled: Boolean(existingSharedOwnedByMe?.id) && online && selectedLinked,
    queryFn: ({ signal }) => listPersonalUtangHistory(existingSharedOwnedByMe!.id, signal),
  });

  const pendingOutgoingCount = useMemo(
    () =>
      selectedLinked && existingSharedOwnedByMe
        ? countPendingOutgoingLoanProposals(sharedHistoryQuery.data ?? [])
        : 0,
    [existingSharedOwnedByMe, selectedLinked, sharedHistoryQuery.data],
  );
  const pendingAtLimit = pendingOutgoingCount >= PERSONAL_UTANG_MAX_PENDING_OUTGOING;

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!online) {
        throw new Error("online-required");
      }
      if (statusLocked) {
        throw new Error("status-locked");
      }
      const me = meQuery.data?.userIdentityId;
      if (!me) throw new Error("missing me");
      if (!contactId) throw new Error("missing contact");
      const initial = parseMoneyAmountInput(amount);
      if (initial == null || !(initial > 0)) throw new Error("amount");
      const purpose = notes.trim();
      if (!purpose) throw new Error("purpose");

      if (!pendingRelationshipIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) throw new Error("id-unavailable");
        pendingRelationshipIdRef.current = generated.id;
      }
      if (!pendingInitialLoanEntryIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) throw new Error("id-unavailable");
        pendingInitialLoanEntryIdRef.current = generated.id;
      }
      const relationshipId = pendingRelationshipIdRef.current;
      const initialLoanEntryId = pendingInitialLoanEntryIdRef.current;

      const share = Boolean(selectedLinked && shareWithCounterparty);
      const body =
        mode === "lent"
          ? {
              relationshipId,
              initialLoanEntryId,
              creditorUserIdentityId: me,
              creditorContactId: null,
              debtorUserIdentityId: null,
              debtorContactId: contactId,
              currencyCode: "PHP",
              dueDateUtc: dueDate ? new Date(dueDate).toISOString() : null,
              initialLoanAmount: initial,
              initialLoanNotes: purpose,
              shareWithCounterparty: share,
            }
          : {
              relationshipId,
              initialLoanEntryId,
              creditorUserIdentityId: null,
              creditorContactId: contactId,
              debtorUserIdentityId: me,
              debtorContactId: null,
              currencyCode: "PHP",
              dueDateUtc: dueDate ? new Date(dueDate).toISOString() : null,
              initialLoanAmount: initial,
              initialLoanNotes: purpose,
              shareWithCounterparty: share,
            };
      try {
        return await createPersonalDebtRelationship(body);
      } catch (error) {
        setFormError(t("checkout.confirmingTransaction"));
        const outcome = await resolveAmbiguousMutationOutcome({
          error,
          lookup: () => getPersonalDebtRelationship(relationshipId),
        });
        if (outcome.kind === "confirmed") {
          return outcome.value;
        }
        if (outcome.kind === "still_unknown") {
          setStatusLocked(true);
          throw new Error("status-unknown");
        }
        if (outcome.kind === "not_found" && isNotFoundStatus(outcome.lookupError)) {
          // Confirmed not created — allow resubmission with the same stable ids.
          throw error;
        }
        throw error;
      }
    },
    onSuccess: async (created) => {
      pendingRelationshipIdRef.current = null;
      pendingInitialLoanEntryIdRef.current = null;
      setStatusLocked(false);
      setContactId("");
      setAmount("");
      setDueDate("");
      setNotes("");
      setShareWithCounterparty(false);
      setFormError(null);
      if (created.shareOutcome === "PrivateNotReceiving") {
        // Soft notice — still navigate to the saved private record.
        setFormError(t("personal.utang.savedPrivatelyNotReceiving"));
      }
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
      await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
      navigate(`/personal/utang/relationships/${created.id}`, {
        state:
          created.shareOutcome === "PrivateNotReceiving"
            ? { shareNotice: "PrivateNotReceiving" }
            : undefined,
      });
    },
    onError: (error) => {
      setRecordFormOpen(true);
      if (error instanceof Error && error.message === "online-required") {
        setFormError(t("offline.requiredPersonalUtangRecord"));
        return;
      }
      if (!online) {
        setFormError(t("offline.requiredPersonalUtangRecord"));
        return;
      }
      if (error instanceof Error && error.message === "purpose") {
        setFormError(t("personal.utang.purposeRequired"));
        return;
      }
      if (error instanceof Error && error.message === "status-unknown") {
        setFormError(t("checkout.transactionStatusUnknown"));
        return;
      }
      if (error instanceof Error && error.message === "status-locked") {
        setFormError(t("checkout.transactionStatusUnknown"));
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
  const selectedContactName =
    contactId
      ? (contacts.find((c) => c.id === contactId)?.displayName ?? t("personal.utang.person"))
      : "";
  const willShare = Boolean(selectedLinked && shareWithCounterparty);
  const submitLabel = willShare
    ? t("personal.utang.shareWithPerson").replace("{name}", selectedContactName)
    : t("personal.utang.savePrivately");
  const recordFormLabel =
    mode === "lent" ? t("personal.utang.recordLent") : t("personal.utang.recordOwe");
  const viewPendingTo = existingSharedOwnedByMe
    ? `/personal/utang/relationships/${existingSharedOwnedByMe.id}`
    : "/personal/utang";
  // Pending limit only applies when sharing into the viewer's own existing shared ledger.
  const sharePendingBlocked =
    willShare && Boolean(existingSharedOwnedByMe) && pendingAtLimit;

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

      <section
        className="catalog-form-section exits-animate-panel personal-section utang-record-card flex min-w-0 flex-col gap-2 overflow-hidden"
        data-testid="utang-record-card"
      >
        <button
          type="button"
          className="catalog-form-section__title m-0 flex w-full items-center gap-2 border-0 bg-transparent p-0 text-left text-muted"
          aria-expanded={recordFormOpen}
          aria-controls="utang-record-panel"
          data-testid="utang-record-toggle"
          onClick={() => setRecordFormOpen((open) => !open)}
        >
          <span
            className={cn(
              "inline-flex size-10 shrink-0 items-center justify-center rounded-full bg-[var(--exits-surface-muted)] text-primary transition-transform",
              recordFormOpen && "rotate-45",
            )}
            aria-hidden="true"
          >
            <Plus className="size-5" />
          </span>
          <span className="min-w-0 flex-1 truncate">{recordFormLabel}</span>
          <ChevronDown
            className={cn(
              "size-4 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)]",
              recordFormOpen && "rotate-180",
            )}
            aria-hidden="true"
          />
        </button>
      {recordFormOpen ? (
      <form
        id="utang-record-panel"
        className={cn(
          "flex min-w-0 flex-col gap-2",
          mode === "lent" && "utang-record-form",
        )}
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (!contactId) {
            setFormError(t("personal.utang.personRequired"));
            return;
          }
          if (!((parseMoneyAmountInput(amount) ?? 0) > 0)) {
            setFormError(t("personal.utang.amountRequired"));
            return;
          }
          if (!notes.trim()) {
            setFormError(t("personal.utang.purposeRequired"));
            return;
          }
          // New independent ledgers are never blocked by pending-count on another record.
          if (willShare && existingSharedOwnedByMe && pendingAtLimit) {
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
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {mode === "lent" ? t("personal.utang.whatHappenedLent") : t("personal.utang.whatHappenedBorrowed")}
            </p>
            {existingSharedWithMe ? (
              <p
                className="m-0 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="utang-rel-shared-exists-hint"
                role="status"
              >
                {t("personal.utang.counterpartyAlreadyShared").replace(
                  "{name}",
                  selectedContactName || t("personal.utang.person"),
                )}{" "}
                <Link
                  to={`/personal/utang/relationships/${existingSharedWithMe.id}`}
                  data-testid="utang-rel-view-shared-record"
                >
                  {t("personal.utang.viewSharedRecord")}
                </Link>
                {" · "}
                {t("personal.utang.createOwnRecordHint")}
              </p>
            ) : null}
        <div
          className="utang-record-fields grid min-w-0 grid-cols-1 gap-2 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_minmax(0,1fr)] lg:items-end"
          data-testid="utang-rel-primary-fields"
        >
          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.utang.person")}
            <select
              data-testid="utang-rel-contact"
              className="exits-select"
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
            <div className="exits-currency-field">
              <span className="exits-currency-field__prefix" aria-hidden>
                ₱
              </span>
              <input
                data-testid="utang-rel-amount"
                type="text"
                inputMode="decimal"
                autoComplete="off"
                className="exits-currency-field__input"
                value={amount}
                onChange={(e) => setAmount(normalizeMoneyAmountTyping(e.target.value))}
                onBlur={() => {
                  const parsed = parseMoneyAmountInput(amount);
                  if (parsed != null) {
                    setAmount(formatMoneyAmountInput(parsed));
                  }
                }}
                required
                aria-label={t("personal.utang.amount")}
              />
            </div>
          </label>
          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.utang.dueDate")}
            <input
              data-testid="utang-rel-due"
              type="date"
              className="w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
            />
          </label>
        </div>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <UtangFieldLabel required>
            {t("personal.utang.purpose")}
          </UtangFieldLabel>
          <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
            {t("personal.utang.purposeHelp")}
          </span>
          <textarea
            data-testid="utang-rel-notes"
            className="min-h-20 w-full min-w-0 resize-y rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
            value={notes}
            maxLength={UTANG_NOTES_MAX_LENGTH}
            required
            aria-required="true"
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
        {contactId && (parseMoneyAmountInput(amount) ?? 0) > 0 && notes.trim() ? (
          <div
            className="rounded-[var(--exits-radius-md)] border border-border bg-[color-mix(in_srgb,var(--exits-surface)_92%,var(--exits-muted)_8%)] p-3 text-[length:var(--exits-text-sm)]"
            data-testid="utang-rel-review"
          >
            <p className="m-0 font-semibold">
              {mode === "lent"
                ? t("personal.utang.reviewLent")
                    .replace("{name}", selectedContactName)
                    .replace("{amount}", formatMoneyAmountInput(parseMoneyAmountInput(amount) ?? 0))
                : t("personal.utang.reviewBorrowed")
                    .replace("{name}", selectedContactName)
                    .replace("{amount}", formatMoneyAmountInput(parseMoneyAmountInput(amount) ?? 0))}
            </p>
            <p className="m-0 mt-1 text-muted">
              {t("personal.utang.purpose")}: {notes.trim()}
            </p>
          </div>
        ) : null}
        {selectedLinked ? (
          <label
            className="flex min-w-0 cursor-pointer items-start gap-2 text-[length:var(--exits-text-sm)]"
            data-testid="utang-rel-share-toggle"
          >
            <input
              type="checkbox"
              className="mt-0.5"
              checked={shareWithCounterparty}
              onChange={(e) => setShareWithCounterparty(e.target.checked)}
              data-testid="utang-rel-share-checkbox"
            />
            <span>
              <span className="font-medium">
                {t("personal.utang.shareWithPerson").replace("{name}", selectedContactName)}
              </span>
              <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
                {shareWithCounterparty
                  ? t("personal.utang.shareOnHint").replace("{name}", selectedContactName)
                  : t("personal.utang.shareOffHint")}
              </span>
            </span>
          </label>
        ) : contactId ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="utang-rel-private-hint">
            {t("personal.utang.privateSaveHint")}
          </p>
        ) : null}
        {!online ? (
          <OfflineNotice
            message={
              willShare
                ? t("personal.utang.shareRequiresOnline")
                : t("offline.requiredPersonalUtangRecord")
            }
          />
        ) : null}
        {willShare && existingSharedOwnedByMe && pendingOutgoingCount > 0 ? (
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
        <div
          className="flex w-full min-w-0 flex-col gap-2 lg:flex-row lg:items-center lg:justify-end"
          data-testid="utang-rel-actions"
        >
          <Button
            type="submit"
            className="order-1 lg:order-2"
            disabled={
              createMutation.isPending ||
              statusLocked ||
              contacts.length === 0 ||
              sharePendingBlocked ||
              !online
            }
            data-testid="utang-rel-submit"
          >
            {createMutation.isPending ? (
              <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
            ) : willShare ? (
              mode === "lent" ? (
                <HandCoins className="size-4 shrink-0" aria-hidden />
              ) : (
                <Wallet className="size-4 shrink-0" aria-hidden />
              )
            ) : (
              <Check className="size-4 shrink-0" aria-hidden />
            )}
            {submitLabel}
          </Button>
          <Button asChild intent="primary" appearance="ghost" className="order-2 lg:order-1">
            <Link to="/personal/people" data-testid="utang-rel-add-person">
              <UserPlus className="size-4 shrink-0 text-primary" aria-hidden />
              {contacts.length === 0
                ? t("personal.utang.addPersonFirst")
                : t("personal.utang.addPerson")}
            </Link>
          </Button>
        </div>
      </form>
      ) : null}
      </section>

      {rows.length === 0 ? (
        <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
          title={t("personal.utang.listEmptyTitle")}
          detail={
            mode === "lent"
              ? t("personal.utang.listEmptyDetailLent")
              : t("personal.utang.listEmptyDetailOwe")
          }
        />
      ) : (
        <ul className="exits-list m-0 grid list-none gap-2 p-0">
          {rows.map((row) => {
            const name = contactLabel(contacts, row);
            const shared = isSharedRelationship(row);
            const owned = row.isLedgerOwner !== false;
            const ownershipLabel = owned
              ? t("personal.utang.ownershipMine")
              : t("personal.utang.ownershipSharedWithMe");
            return (
              <li key={row.id}>
                <Link
                  to={`/personal/utang/relationships/${row.id}`}
                  className={cn(
                    "exits-list__card utang-account-card flex items-center justify-between gap-3 text-foreground no-underline",
                    !owned && "utang-account-card--shared",
                  )}
                  data-testid={`utang-rel-row-${row.id}`}
                >
                  <PersonAvatar name={name} size="sm" />
                  <div className="min-w-0 flex-1">
                    <p
                      className="m-0 truncate text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted"
                      data-utang-ownership=""
                      data-testid={`utang-rel-ownership-${row.id}`}
                    >
                      {ownershipLabel}
                    </p>
                    <p className="exits-list__name m-0 truncate font-semibold">{name}</p>
                    <p className="m-0 flex min-w-0 flex-wrap items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
                      <UtangDirectionTags
                        direction={mode === "lent" ? "lent" : "owe"}
                        shared={shared}
                        linkTestId={`utang-rel-ledger-${row.id}`}
                      />
                      {!owned ? (
                        <span className="truncate">
                          {t("personal.utang.managedByOther").replace("{name}", name)}
                        </span>
                      ) : null}
                    </p>
                    <WaitingChip origin={rowOrigin(row)} />
                    <UtangDueCaption dueDateUtc={row.dueDateUtc} />
                  </div>
                  <div className="grid shrink-0 justify-items-start self-stretch">
                    {!owned ? (
                      <StatusChip
                        tone={UTANG_READ_ONLY_CHIP.tone}
                        appearance={UTANG_READ_ONLY_CHIP.appearance}
                        shape={UTANG_READ_ONLY_CHIP.shape}
                        className="col-start-1 row-start-1 self-start"
                        data-testid={`utang-rel-readonly-${row.id}`}
                      >
                        {t("personal.utang.readOnly")}
                      </StatusChip>
                    ) : null}
                    <span className="col-start-1 row-start-1 self-center">
                      <MoneyDisplay amount={row.currentBalance} />
                    </span>
                  </div>
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
  const [entryType, setEntryType] = useState<"Payment" | "Loan" | "Adjustment">("Payment");
  const [amount, setAmount] = useState(() => formatMoneyAmountInput(0));
  const [adjustmentDelta, setAdjustmentDelta] = useState("");
  const [notes, setNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [statusLocked, setStatusLocked] = useState(false);
  const pendingEntryIdRef = useRef<string | null>(null);
  const [disputeEntryId, setDisputeEntryId] = useState<string | null>(null);
  const [disputeReasonKey, setDisputeReasonKey] = useState<
    "amount" | "notReceived" | "other" | ""
  >("");
  const [actionError, setActionError] = useState<string | null>(null);
  const [settleOpen, setSettleOpen] = useState(false);
  const [balanceOpen, setBalanceOpen] = useState(true);
  const [settleError, setSettleError] = useState<string | null>(null);
  const pendingSettlementEntryIdRef = useRef<string | null>(null);

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
    [contactsQuery.dataUpdatedAt],
    [],
  );
  const cachedDetail = usePersonalUtangCache<CachedPersonalRelationship | null>(
    ({ db }) => getCachedPersonalRelationship(db.db, db.scopeBinding, relationshipId),
    [detailQuery.dataUpdatedAt, relationshipId],
    null,
  );
  const cachedHistory = usePersonalUtangCache<CachedPersonalEntry[]>(
    ({ db }) => listCachedPersonalEntries(db.db, db.scopeBinding, relationshipId),
    [historyQuery.dataUpdatedAt, relationshipId],
    [],
  );

  const usingCache = !online || detailQuery.isError || balanceQuery.isError;
  const detail = usingCache
    ? (cachedDetail ?? detailQuery.data ?? null)
    : (detailQuery.data ?? null);
  // No live balance offline: prefer the last server balance, then the cached relationship.
  const currentBalance = usingCache
    ? (balanceQuery.data?.currentBalance ??
      cachedDetail?.currentBalance ??
      detailQuery.data?.currentBalance ??
      0)
    : (balanceQuery.data?.currentBalance ?? 0);
  const history: CachedPersonalEntry[] | PersonalUtangEntryDto[] = useMemo(() => {
    const rows = usingCache
      ? (historyQuery.data ?? cachedHistory)
      : (historyQuery.data ?? []);
    return [...rows].sort((a, b) => {
      const byTime = b.createdAtUtc.localeCompare(a.createdAtUtc);
      if (byTime !== 0) {
        return byTime;
      }
      return b.id.localeCompare(a.id);
    });
  }, [usingCache, historyQuery.data, cachedHistory]);
  const relationshipIsLocal = cachedDetail?.origin === "Local";
  const pendingOutgoingCount = useMemo(
    () =>
      detail && isSharedRelationship(detail)
        ? countPendingOutgoingLoanProposals(history)
        : 0,
    [detail, history],
  );
  const unresolvedPendingCount = useMemo(
    () => history.filter((entry) => entry.status === "Pending").length,
    [history],
  );
  const pendingAtLimit = pendingOutgoingCount >= PERSONAL_UTANG_MAX_PENDING_OUTGOING;

  const invalidateUtang = async () => {
    await queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
    await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
  };

  const recordMutation = useMutation({
    mutationFn: async () => {
      const amt = parseMoneyAmountInput(amount);
      if (amt == null || !(amt > 0)) throw new Error("amount");
      const purpose = notes.trim();
      if (!purpose) throw new Error("purpose");
      if (!online) {
        throw new Error("online-required");
      }
      if (statusLocked) {
        throw new Error("status-locked");
      }
      if (!pendingEntryIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) throw new Error("id-unavailable");
        pendingEntryIdRef.current = generated.id;
      }
      const entryId = pendingEntryIdRef.current;
      const version = balanceQuery.data?.version ?? detailQuery.data?.version;
      const body =
        entryType === "Adjustment"
          ? {
              entryId,
              entryType: "Adjustment" as const,
              amount: amt,
              adjustmentDelta: Number(adjustmentDelta),
              expectedVersion: version ?? null,
              notes: purpose,
            }
          : {
              entryId,
              entryType,
              amount: amt,
              expectedVersion: version ?? null,
              notes: purpose,
            };
      try {
        await recordPersonalUtangEntry(relationshipId, body);
      } catch (error) {
        setFormError(t("checkout.confirmingTransaction"));
        const outcome = await resolveAmbiguousMutationOutcome({
          error,
          lookup: () => getPersonalUtangEntry(entryId),
        });
        if (outcome.kind === "confirmed") {
          return;
        }
        if (outcome.kind === "still_unknown") {
          setStatusLocked(true);
          throw new Error("status-unknown");
        }
        if (outcome.kind === "not_found" && isNotFoundStatus(outcome.lookupError)) {
          throw error;
        }
        throw error;
      }
    },
    onSuccess: async () => {
      pendingEntryIdRef.current = null;
      setStatusLocked(false);
      setAmount(formatMoneyAmountInput(0));
      setAdjustmentDelta("");
      setNotes("");
      setFormError(null);
      await invalidateUtang();
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setFormError(t("offline.requiredPersonalUtangRecord"));
        return;
      }
      if (!online) {
        setFormError(t("offline.requiredPersonalUtangRecord"));
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
      if (error instanceof Error && (error.message === "status-unknown" || error.message === "status-locked")) {
        setFormError(t("checkout.transactionStatusUnknown"));
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

  const settleMutation = useMutation({
    mutationFn: async () => {
      if (!online) throw new Error("online-required");
      if (!pendingSettlementEntryIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) throw new Error("id-unavailable");
        pendingSettlementEntryIdRef.current = generated.id;
      }
      const settlementEntryId = pendingSettlementEntryIdRef.current;
      const version = balanceQuery.data?.version ?? detailQuery.data?.version ?? null;
      return settlePersonalDebtRelationship(relationshipId, {
        expectedVersion: version,
        settlementEntryId,
      });
    },
    onSuccess: async () => {
      pendingSettlementEntryIdRef.current = null;
      setSettleOpen(false);
      setSettleError(null);
      await invalidateUtang();
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setSettleError(t("personal.utang.settleRequiresOnline"));
        return;
      }
      if (isUtangSettlementStaleConflict(error)) {
        setSettleError(t("personal.utang.settleStaleConflict"));
        void balanceQuery.refetch();
        void detailQuery.refetch();
        void historyQuery.refetch();
        return;
      }
      if (isUtangConcurrencyConflict(error)) {
        setSettleError(t("personal.utang.concurrencyConflict"));
        void balanceQuery.refetch();
        void detailQuery.refetch();
        void historyQuery.refetch();
        return;
      }
      if (
        error instanceof PlatformApiError &&
        (error.errorCode ?? "").toLowerCase().includes("settlement.pending")
      ) {
        setSettleError(t("personal.utang.settlePendingBlocked"));
        return;
      }
      setSettleError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
    },
  });

  const closeMutation = useMutation({
    mutationFn: async () => {
      if (!online) throw new Error("online-required");
      const version = balanceQuery.data?.version ?? detailQuery.data?.version ?? null;
      return closePersonalDebtRelationship(relationshipId, {
        expectedVersion: version,
      });
    },
    onSuccess: async () => {
      setSettleError(null);
      await invalidateUtang();
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setSettleError(t("personal.utang.settleRequiresOnline"));
        return;
      }
      if (isUtangSettlementStaleConflict(error) || isUtangConcurrencyConflict(error)) {
        setSettleError(
          isUtangSettlementStaleConflict(error)
            ? t("personal.utang.settleStaleConflict")
            : t("personal.utang.concurrencyConflict"),
        );
        void balanceQuery.refetch();
        void detailQuery.refetch();
        void historyQuery.refetch();
        return;
      }
      setSettleError(
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
  const isLedgerOwner = detail.isLedgerOwner !== false;
  const relationshipClosed = detail.status.toLowerCase() === "closed";
  const relationshipActive = detail.status.toLowerCase() === "active";
  const awaitingSettlement = history.some((entry) => {
    const isSettlement =
      "isSettlement" in entry ? Boolean(entry.isSettlement) : false;
    const status = "status" in entry ? entry.status : "Confirmed";
    return isSettlement && status === "Pending";
  });
  const perspectiveLabel =
    detail.perspective === "Borrowed"
      ? t("personal.utang.perspectiveDebtor")
      : t("personal.utang.perspectiveCreditor");
  const ownerDisplayName =
    personName === EM_DASH ? t("personal.utang.person") : personName;
  const ledgerLabel = !shared
    ? t("personal.utang.privateRecord")
    : isLedgerOwner
      ? t("personal.utang.sharedLedger")
      : t("personal.utang.sharedBy").replace("{name}", ownerDisplayName);
  const statusLabel = relationshipClosed
    ? t("personal.utang.statusSettled")
    : t("personal.utang.statusActive");
  const listBack =
    detail.perspective === "Borrowed" ? personalPageBackNav.utangOwe : personalPageBackNav.utangLent;
  // An Adjustment rewrites a balance against a version this device may no longer be showing.
  const adjustmentBlocked = !online && entryType === "Adjustment";
  // Owner-model: unresolved-pending limit does not apply to new Confirmed writes.
  const loanBlockedByPendingLimit = false;
  const submitLabel = t("personal.utang.saveEntry");
  const viewPendingTo = `/personal/utang/relationships/${relationshipId}`;
  const settleBlockedOffline = !online;
  const canMutateFinances = isLedgerOwner;

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
      <div
        className="flex min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
        data-testid="utang-balance-card"
      >
        <h2 className="m-0">
          <button
            type="button"
            className="flex w-full items-center gap-2 border-0 bg-transparent p-0 text-left text-inherit"
            aria-expanded={balanceOpen}
            aria-controls="utang-balance-panel"
            data-testid="utang-balance-toggle"
            onClick={() => setBalanceOpen((open) => !open)}
          >
            {detail.perspective === "Borrowed" ? (
              <Wallet className="size-5 shrink-0 text-primary" aria-hidden="true" />
            ) : (
              <HandCoins className="size-5 shrink-0 text-primary" aria-hidden="true" />
            )}
            <span className="min-w-0 flex-1 text-[length:var(--exits-text-base)] font-semibold">
              {perspectiveLabel}
            </span>
            <ChevronDown
              className={cn(
                "size-4 shrink-0 text-muted transition-transform duration-[var(--exits-motion-fast)]",
                balanceOpen && "rotate-180",
              )}
              aria-hidden="true"
            />
          </button>
        </h2>
        {balanceOpen ? (
        <div id="utang-balance-panel" className="flex min-w-0 flex-col gap-2">
        <MoneyDisplay amount={currentBalance} className="text-[length:var(--exits-text-xl)]" />
        <p
          className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="utang-detail-ledger"
        >
          {ledgerLabel}
        </p>
        <p
          className="m-0 mt-1 text-[length:var(--exits-text-sm)] font-medium"
          data-testid="utang-detail-status"
        >
          {statusLabel}
        </p>
        <DueChip dueDateUtc={detail.dueDateUtc} />
        <WaitingChip origin={relationshipIsLocal ? "Local" : "Server"} />
        {shared && !isLedgerOwner ? (
          <p
            className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="utang-detail-owner-manages"
          >
            {t("personal.utang.ownerManagesRecord").replace("{name}", ownerDisplayName)}
          </p>
        ) : null}

        {canMutateFinances &&
        relationshipActive &&
        currentBalance > 0 &&
        unresolvedPendingCount === 0 ? (
          <div className="flex min-w-0 flex-col gap-2">
            {settleBlockedOffline ? (
              <OfflineNotice
                message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangSettle))}
              />
            ) : null}
            <Button
              type="button"
              className="w-auto self-start"
              disabled={settleBlockedOffline || settleMutation.isPending}
              data-testid="utang-settle"
              onClick={() => {
                setSettleError(null);
                setSettleOpen(true);
              }}
            >
              <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
              {t("personal.utang.settle")}
            </Button>
            {settleOpen ? (
              <div
                className="flex min-w-0 flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
                data-testid="utang-settle-panel"
              >
                <h2 className="m-0 text-[length:var(--exits-text-base)] font-medium">
                  {t("personal.utang.settleTitle")}
                </h2>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("personal.utang.settleAmount")}: <MoneyDisplay amount={currentBalance} />
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("personal.utang.settleAfter")}: <MoneyDisplay amount={0} />
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {shared
                    ? t("personal.utang.settleSharedHint")
                    : t("personal.utang.settlePrivateHint")}
                </p>
                <div className="flex min-w-0 flex-wrap gap-2">
                  <Button
                    type="button"
                    className="w-auto self-start"
                    disabled={settleBlockedOffline || settleMutation.isPending}
                    data-testid="utang-settle-confirm"
                    onClick={() => settleMutation.mutate()}
                  >
                    <Check className="size-4 shrink-0" aria-hidden="true" />
                    {t("personal.utang.settleConfirm")}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    disabled={settleMutation.isPending}
                    onClick={() => setSettleOpen(false)}
                  >
                    {t("personal.utang.cancelEdit")}
                  </Button>
                </div>
              </div>
            ) : null}
          </div>
        ) : null}
        </div>
        ) : null}
      </div>

      {awaitingSettlement ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)]"
          data-testid="utang-settle-awaiting"
          role="status"
        >
          {t("personal.utang.settleAwaiting")}
        </p>
      ) : null}

      {settleError ? (
        <p
          role="alert"
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
        >
          {settleError}
        </p>
      ) : null}

      {canMutateFinances &&
      relationshipActive &&
      currentBalance === 0 &&
      unresolvedPendingCount === 0 ? (
        <div className="flex min-w-0 flex-col gap-2">
          {settleBlockedOffline ? (
            <OfflineNotice
              message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangClose))}
            />
          ) : null}
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.utang.markSettledHint")}
          </p>
          <Button
            type="button"
            className="w-auto self-start"
            disabled={settleBlockedOffline || closeMutation.isPending}
            data-testid="utang-mark-settled"
            onClick={() => closeMutation.mutate()}
          >
            <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
            {t("personal.utang.markSettled")}
          </Button>
        </div>
      ) : null}

      {canMutateFinances && !relationshipClosed ? (
      <form
        className="catalog-form-section exits-animate-panel personal-section flex min-w-0 flex-col gap-2 overflow-hidden"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          const parsedAmount = parseMoneyAmountInput(amount);
          if (parsedAmount == null || !(parsedAmount > 0)) {
            setFormError(t("personal.utang.amountRequired"));
            return;
          }
          if (!notes.trim()) {
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
        <h2 className="catalog-form-section__title m-0 flex items-center gap-2">
          <PenLine className="size-4 shrink-0" aria-hidden="true" />
          {t("personal.utang.entryType")}
        </h2>
        <div className="grid min-w-0 gap-2 lg:grid-cols-2 lg:items-end">
          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="font-medium">{t("personal.utang.entryType")}</span>
            <select
              data-testid="utang-entry-type"
              className="exits-select"
              value={entryType}
              onChange={(e) => setEntryType(e.target.value as typeof entryType)}
            >
              <option value="Payment">{t("personal.utang.recordPayment")}</option>
              <option value="Loan">{t("personal.utang.addAmount")}</option>
              <option value="Adjustment">{t("personal.utang.adjustBalance")}</option>
            </select>
          </label>
          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span className="font-medium">{t("personal.utang.amount")}</span>
            <div className="exits-currency-field">
              <span className="exits-currency-field__prefix" aria-hidden>
                ₱
              </span>
              <input
                data-testid="utang-entry-amount"
                type="text"
                inputMode="decimal"
                autoComplete="off"
                className="exits-currency-field__input"
                value={amount}
                onChange={(e) => setAmount(normalizeMoneyAmountTyping(e.target.value))}
                onBlur={() => {
                  const parsed = parseMoneyAmountInput(amount);
                  setAmount(formatMoneyAmountInput(parsed ?? 0));
                }}
                required
                aria-label={t("personal.utang.amount")}
              />
            </div>
          </label>
        </div>
        {entryType === "Adjustment" ? (
          <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.utang.adjustmentDelta")}
            <input
              data-testid="utang-entry-delta"
              inputMode="decimal"
              className="w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={adjustmentDelta}
              onChange={(e) => setAdjustmentDelta(e.target.value)}
              required
            />
          </label>
        ) : null}
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <UtangFieldLabel required>
            {entryType === "Adjustment"
              ? t("personal.utang.adjustmentReason")
              : t("personal.utang.purpose")}
          </UtangFieldLabel>
          <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
            {entryType === "Adjustment"
              ? t("personal.utang.adjustmentReasonHelp")
              : t("personal.utang.purposeHelp")}
          </span>
          <textarea
            data-testid="utang-entry-notes"
            className="min-h-16 w-full min-w-0 resize-y rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
            value={notes}
            maxLength={UTANG_NOTES_MAX_LENGTH}
            required
            aria-required="true"
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
        {adjustmentBlocked ? (
          <OfflineNotice
            message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangAdjustment))}
          />
        ) : null}
        {!online && !adjustmentBlocked ? (
          <OfflineNotice message={t("offline.requiredPersonalUtangRecord")} />
        ) : null}
        {shared && online && isLedgerOwner ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="utang-entry-confirm-hint"
          >
            {t("personal.utang.sharedEntryHint").replace("{name}", personName)}
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
          className="w-auto self-start"
          disabled={
            recordMutation.isPending ||
            statusLocked ||
            adjustmentBlocked ||
            loanBlockedByPendingLimit ||
            !online
          }
          data-testid="utang-entry-submit"
        >
          <Check className="size-4 shrink-0" aria-hidden="true" />
          {submitLabel}
        </Button>
      </form>
      ) : null}

      {!relationshipClosed && online ? (
        <RelationshipInviteReminderPanel
          relationshipId={relationshipId}
          inviteeContactId={
            detail.perspective === "Borrowed" ? detail.creditorContactId : detail.debtorContactId
          }
        />
      ) : null}
      {!relationshipClosed && !online ? (
        <OfflineNotice
          message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangInvite))}
        />
      ) : null}

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
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
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
              const isSettlement =
                "isSettlement" in entry ? Boolean(entry.isSettlement) : false;
              const confirmLabel =
                entry.entryType === "Payment"
                  ? t("personal.utang.confirmReceived")
                  : t("personal.utang.confirm");
              const isDisputing = disputeEntryId === entry.id;
              const entryTitle = entryActionLabel(
                entry.entryType,
                { isSettlement },
                t,
              );
              const loanPerspectiveHint =
                !isSettlement && entry.entryType === "Loan"
                  ? loanActivityLabel(
                      detail.perspective,
                      personName,
                      pendingIncoming,
                      t,
                    )
                  : null;

              return (
                <li key={entry.id}>
                  <div
                    className="exits-list__card flex min-w-0 flex-col gap-2"
                    data-testid={`utang-history-entry-${entry.id}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p
                          className="exits-list__name m-0 font-medium"
                          data-testid={`utang-entry-action-${entry.id}`}
                        >
                          {entryTitle}
                        </p>
                        {loanPerspectiveHint ? (
                          <p
                            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                            data-testid={`utang-entry-perspective-${entry.id}`}
                          >
                            {loanPerspectiveHint}
                          </p>
                        ) : null}
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
                          {entryStatusLabel(
                            {
                              status,
                              isSharedLedger: shared,
                              wasAutoSynced:
                                "wasAutoSynced" in entry
                                  ? Boolean(entry.wasAutoSynced)
                                  : false,
                            },
                            {
                              pendingIncoming,
                              reporterName: personName === EM_DASH ? undefined : personName,
                            },
                            t,
                          )}
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
                      <div className="flex min-w-0 flex-col gap-2 sm:flex-row sm:flex-wrap">
                        {canConfirm ? (
                          <Button
                            type="button"
                            className="flex-1 sm:flex-none"
                            disabled={resolveMutation.isPending}
                            data-testid={`utang-confirm-${entry.id}`}
                            onClick={() =>
                              resolveMutation.mutate({ action: "confirm", entryId: entry.id })
                            }
                          >
                            <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
                            {confirmLabel}
                          </Button>
                        ) : null}
                        {canDispute ? (
                          <Button
                            type="button"
                            variant="ghost"
                            className="flex-1 sm:flex-none"
                            disabled={resolveMutation.isPending}
                            data-testid={`utang-dispute-${entry.id}`}
                            onClick={() => {
                              setDisputeEntryId(entry.id);
                              setDisputeReasonKey("");
                            }}
                          >
                            <CircleAlert className="size-4 shrink-0" aria-hidden="true" />
                            {t("personal.utang.dispute")}
                          </Button>
                        ) : null}
                      </div>
                    ) : null}

                    {pendingOutgoing && online && canCancel ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="w-fit"
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
                            className="exits-select"
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

      <Button asChild intent="primary" appearance="ghost" className="utang-back-link w-full font-bold sm:w-fit">
        <Link to="/personal/utang">
          <ArrowLeft className="size-4 shrink-0" aria-hidden="true" />
          {t("personal.utang.back")}
        </Link>
      </Button>
    </div>
  );
}

