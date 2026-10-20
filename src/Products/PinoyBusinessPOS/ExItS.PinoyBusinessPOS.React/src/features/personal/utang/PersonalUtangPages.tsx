import { useEffect, useMemo, useRef, useState, type CSSProperties, type ReactNode } from "react";
import { Link, Navigate, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, BellRing, Check, CheckCircle2, ChevronRight, CircleAlert, Contact, HandCoins, Handshake, History, List, Loader2, Minus, Pencil, PenLine, Plus, Share2, SlidersHorizontal, Users, Wallet, Zap, type LucideIcon } from "lucide-react";
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
import { INTENT_GLOBAL_SECONDARY } from "@/features/ui-standards/intent-color-global-default";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsOutlineInput, ExitsOutlineSelect, ExitsOutlineTextarea } from "@/components/exits/ExitsOutlineField";
import { Switch } from "@/components/ui/switch";
import { Notice } from "@/components/exits/Notice";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { UtangDueCaption } from "@/features/personal/utang/UtangListMeta";
import { UTANG_READ_ONLY_CHIP } from "@/features/personal/utang/utang-ownership-ui";
import {
  isSharedRelationship as workspaceIsSharedRelationship,
  resolveRelationshipContactName,
} from "@/features/personal/utang/utang-workspace";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { formatShortDate } from "@/features/personal/people-status";
import { EditLocalContactDialog } from "@/features/personal/EditLocalContactDialog";
import { SharedUtangSettingPanel } from "@/features/personal/SharedUtangSettingPanel";
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

function entryActionLabel(
  entryType: string,
  options: { isSettlement?: boolean; isOpening?: boolean },
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
  if (options.isOpening) {
    return t("personal.utang.startingBalance");
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
  const [searchParams] = useSearchParams();
  const requestedContactId = searchParams.get("contact") ?? "";
  const [contactId, setContactId] = useState(requestedContactId);
  const [recordFormOpen, setRecordFormOpen] = useState(Boolean(requestedContactId));
  const [recordMode, setRecordMode] = useState<"lent" | "owe">(mode);
  const [ledgerFilter, setLedgerFilter] = useState<"all" | "mine" | "shared">("all");
  const [amount, setAmount] = useState("0.00");
  const [dueDate, setDueDate] = useState("");
  const [notes, setNotes] = useState("");
  const [shareWithCounterparty, setShareWithCounterparty] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
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
  const switchedListQuery = useQuery({
    queryKey: ["personal", "utang", recordMode],
    queryFn: ({ signal }) =>
      recordMode === "lent" ? listLentRelationships(signal) : listBorrowedRelationships(signal),
    enabled: online && recordMode !== mode,
  });
  const oppositeMode = mode === "lent" ? "owe" : "lent";
  const oppositeListQuery = useQuery({
    queryKey: ["personal", "utang", oppositeMode],
    queryFn: ({ signal }) =>
      oppositeMode === "lent" ? listLentRelationships(signal) : listBorrowedRelationships(signal),
    enabled: online && recordFormOpen,
  });

  useEffect(() => {
    if (!requestedContactId) {
      return;
    }
    setContactId(requestedContactId);
    setRecordFormOpen(true);
  }, [requestedContactId]);

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
  const mineCount = rows.filter((row) => row.isLedgerOwner !== false).length;
  const sharedCount = rows.length - mineCount;
  const visibleRows =
    ledgerFilter === "all"
      ? rows
      : rows.filter((row) => (ledgerFilter === "mine" ? row.isLedgerOwner !== false : row.isLedgerOwner === false));
  const selectedLinked = contactId ? contactLooksLinked(contacts, contactId) : false;
  const recordRows = recordMode === mode ? rows : (switchedListQuery.data ?? []);
  const existingSharedOwnedByMe =
    selectedLinked && contactId
      ? findSharedRelationshipForContact(recordRows, contacts, contactId, recordMode, {
          ownedOnly: true,
        })
      : null;
  const existingSharedWithMe =
    selectedLinked && contactId
      ? findSharedRelationshipForContact(recordRows, contacts, contactId, recordMode, {
          sharedWithMeOnly: true,
        }) ??
        findSharedRelationshipForContact(
          oppositeListQuery.data ?? [],
          contacts,
          contactId,
          oppositeMode,
          { sharedWithMeOnly: true },
        )
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
        recordMode === "lent"
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
      setAmount("0.00");
      setDueDate("");
      setRecordMode(mode);
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
    recordMode === "lent" ? t("personal.utang.recordLent") : t("personal.utang.recordOwe");
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

      <div className="personal-home-metrics utang-summary-cards" data-testid="utang-summary-cards">
        <button
          type="button"
          className="personal-home-index-card personal-home-index-card--secondary utang-summary-cards__action"
          aria-controls="utang-record-panel"
          aria-expanded={recordFormOpen}
          data-testid="utang-summary-record"
          onClick={() => {
            setRecordMode(mode);
            setRecordFormOpen(true);
          }}
        >
          <span className="personal-home-index-card__ribbon" aria-hidden="true">
            <span className="personal-home-index-card__fold" />
            <Plus className="personal-home-index-card__icon" />
          </span>
          <span className="personal-home-index-card__body">
            <span className="personal-home-index-card__title">{recordFormLabel}</span>
          </span>
          <strong
            className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--ghost"
            aria-hidden="true"
          >
            +
          </strong>
        </button>
        {(
          [
            { id: "all", tone: "primary", label: t("personal.utang.filterAll"), Icon: List, count: rows.length },
            { id: "mine", tone: "success", label: t("personal.home.activeMine"), Icon: Handshake, count: mineCount },
            { id: "shared", tone: "info", label: t("personal.home.sharedLedgerStat"), Icon: Share2, count: sharedCount },
          ] as const
        ).map((filter) => {
          const FilterIcon = filter.Icon;
          return (
            <button
              key={filter.id}
              type="button"
              className={`personal-home-index-card personal-home-index-card--${filter.tone} utang-summary-cards__action utang-summary-cards__filter`}
              aria-pressed={ledgerFilter === filter.id}
              data-testid={`utang-summary-${filter.id}`}
              onClick={() => setLedgerFilter(filter.id)}
            >
              <span className="personal-home-index-card__ribbon" aria-hidden="true">
                <span className="personal-home-index-card__fold" />
                <FilterIcon className="personal-home-index-card__icon" />
              </span>
              <span className="personal-home-index-card__body">
                <span className="personal-home-index-card__title">{filter.label}</span>
              </span>
              <strong className="personal-home-index-card__badge personal-home-index-card__badge--ghost tabular-nums">
                {filter.count}
              </strong>
            </button>
          );
        })}
      </div>

      <ExitsModal
        open={recordFormOpen}
        onOpenChange={setRecordFormOpen}
        title={recordFormLabel}
        size="lg"
        closeOnEscape={false}
        busy={createMutation.isPending}
        testId="utang-record-dialog"
        closeLabel={t("personal.utang.closeRecordDialog")}
        ribbon
        ribbonTone={recordMode === "lent" ? "primary" : "secondary"}
        ribbonIcon={recordMode === "lent" ? HandCoins : Wallet}
        noValidate
        onSubmit={() => {
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
        footer={
          <>
            <Button
              type="button"
              intent="danger"
              appearance="glossy"
              emphasis="strong"
              disabled={createMutation.isPending}
              data-testid="utang-rel-cancel"
              onClick={() => setRecordFormOpen(false)}
            >
              {t("personal.utang.cancelEdit")}
            </Button>
            <Button
              type="submit"
              intent="primary"
              appearance="glossy"
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
              ) : (
                <SavePlusIcon className="size-4 shrink-0" />
              )}
              {submitLabel}
            </Button>
          </>
        }
      >
      <div id="utang-record-panel" className="flex min-w-0 flex-col gap-3">
            {existingSharedWithMe ? (
              <Notice tone="info" testId="utang-rel-shared-exists-hint">
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
              </Notice>
            ) : null}
        <div
          className="utang-record-fields grid min-w-0 grid-cols-1 gap-3 lg:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_minmax(0,1fr)] lg:items-end"
          data-testid="utang-rel-primary-fields"
        >
          <ExitsOutlineSelect
            label={t("personal.utang.person")}
            value={contactId}
            options={[
              { value: "", label: t("personal.utang.choosePerson") },
              ...contacts.map((contact) => ({ value: contact.id, label: contact.displayName })),
            ]}
            onChange={(next) => {
              setContactId(next);
              if (!next) {
                return;
              }
              window.setTimeout(() => {
                document
                  .getElementById("utang-record-panel")
                  ?.querySelector<HTMLInputElement>("[data-testid='utang-rel-amount']")
                  ?.focus();
              }, 0);
            }}
            testId="utang-rel-contact"
          />
          <ExitsOutlineInput
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
            testId="utang-rel-amount"
            inputMode="decimal"
            autoComplete="off"
            required
            {...(contactId ? { "data-exits-modal-autofocus": "true" } : {})}
          />
          <ExitsOutlineInput
            label={t("personal.utang.dueDate")}
            type="date"
            value={dueDate}
            onChange={setDueDate}
            testId="utang-rel-due"
          />
        </div>
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          {contactId ? (
            <Notice tone="info" testId="utang-rel-contact-kind" className="min-w-0 flex-1">
              {selectedLinked ? t("personal.utang.connectedContact") : t("personal.utang.privateRecord")}
            </Notice>
          ) : null}
          <Button
            type="button"
            intent={recordMode === "lent" ? "info" : "primary"}
            appearance="gradient"
            emphasis={recordMode === "lent" ? "strong" : undefined}
            shape="standard"
            className={cn(
              "shrink-0",
              recordMode === "owe" &&
                "border-transparent bg-gradient-to-b from-[var(--utang-record-accent)] to-[color-mix(in_srgb,var(--utang-record-accent)_78%,#000)] text-white hover:brightness-[1.02]",
            )}
            style={
              recordMode === "owe"
                ? ({ "--utang-record-accent": INTENT_GLOBAL_SECONDARY } as CSSProperties)
                : undefined
            }
            data-testid="utang-record-mode"
            onClick={() => setRecordMode(recordMode === "lent" ? "owe" : "lent")}
          >
            {recordMode === "lent"
              ? t("personal.utang.switchToRecordOwe")
              : t("personal.utang.switchToRecordLent")}
          </Button>
        </div>
        <ExitsOutlineTextarea
          label={t("personal.utang.purpose")}
          value={notes}
          onChange={setNotes}
          maxLength={UTANG_NOTES_MAX_LENGTH}
          required
          testId="utang-rel-notes"
        />
        {contactId && (parseMoneyAmountInput(amount) ?? 0) > 0 && notes.trim() ? (
          <div
            className="rounded-[var(--exits-radius-md)] border border-border bg-[color-mix(in_srgb,var(--exits-surface)_92%,var(--exits-muted)_8%)] p-3 text-[length:var(--exits-text-sm)]"
            data-testid="utang-rel-review"
          >
            <p className="m-0 font-semibold">
              {recordMode === "lent"
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
          <div
            className="flex min-w-0 items-start gap-3 text-[length:var(--exits-text-sm)]"
            data-testid="utang-rel-share-toggle"
          >
            <Switch
              checked={shareWithCounterparty}
              onCheckedChange={setShareWithCounterparty}
              showStateLabel={false}
              data-testid="utang-rel-share-checkbox"
              aria-label={t("personal.utang.shareWithPerson").replace("{name}", selectedContactName)}
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
          </div>
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
        {formError ? <Notice tone="danger">{formError}</Notice> : null}
      </div>
      </ExitsModal>

      {visibleRows.length === 0 ? (
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
        <ul className="utang-settings-list utang-relationship-list m-0 grid list-none gap-4 p-0">
          {visibleRows.map((row) => {
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
                    "ui-standards-qcard ui-standards-qcard--settings-card utang-account-card utang-account-card--standard utang-settings-card text-foreground no-underline",
                    mode === "lent" ? "utang-settings-card--lent" : "utang-settings-card--owe",
                    owned ? "utang-account-card--mine" : "utang-account-card--shared",
                  )}
                  data-testid={`utang-rel-row-${row.id}`}
                >
                  <div className="ui-standards-qcard__settings-ribbon">
                    <div
                      className="ui-standards-qcard__settings-tab utang-settings-card__tab"
                      data-utang-ownership=""
                      data-testid={`utang-rel-ownership-${row.id}`}
                    >
                      <span className="truncate">{ownershipLabel}</span>
                      {formatDueLabel(row.dueDateUtc).kind !== "none" ? (
                        <>
                          <span className="utang-settings-card__tab-divider" aria-hidden />
                          <UtangDueCaption
                            dueDateUtc={row.dueDateUtc}
                            className={cn(
                              "utang-settings-card__tab-text",
                              formatDueLabel(row.dueDateUtc).kind === "overdue" && "utang-settings-card__tab-text--danger",
                            )}
                          />
                        </>
                      ) : null}
                      <span className="ui-standards-qcard__settings-fold" aria-hidden />
                    </div>
                    <div className="utang-settings-card__content">
                      <div className="utang-settings-card__meta min-w-0 flex-1">
                        <div className="utang-settings-card__headline">
                          <p className="utang-settings-card__name m-0 truncate">{name}</p>
                          <span className="utang-settings-card__amount">
                            <MoneyDisplay amount={row.currentBalance} />
                          </span>
                        </div>
                        <span className="utang-settings-card__tags">
                          <StatusChip
                            tone={mode === "lent" ? "success" : "warning"}
                            appearance="emphasis"
                            shape="square"
                          >
                            {mode === "lent" ? t("personal.utang.owesYou") : t("personal.utang.youOwe")}
                          </StatusChip>
                          <StatusChip
                            tone={shared ? "success" : "info"}
                            appearance="emphasis"
                            shape="square"
                            data-testid={`utang-rel-ledger-${row.id}`}
                          >
                            {shared ? t("people.status.connected") : t("people.localContact")}
                          </StatusChip>
                          {!owned ? (
                            <StatusChip
                              tone={UTANG_READ_ONLY_CHIP.tone}
                              appearance={UTANG_READ_ONLY_CHIP.appearance}
                              shape={UTANG_READ_ONLY_CHIP.shape}
                              className="max-md:hidden"
                              data-testid={`utang-rel-readonly-${row.id}`}
                            >
                              {t("personal.utang.readOnly")}
                            </StatusChip>
                          ) : null}
                        </span>
                        {!owned ? (
                          <p className="m-0 truncate text-[length:var(--exits-text-sm)]">
                            {t("personal.utang.managedByOther").replace("{name}", name)}
                          </p>
                        ) : null}
                        <WaitingChip origin={rowOrigin(row)} />
                      </div>
                    </div>
                    <span className="utang-account-card__caret" aria-hidden>
                      <ChevronRight className="size-5" />
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

/** Balance after the entry being typed, using the same signs as the server (payment −, loan +, adjustment ±). */
export function projectUtangBalance(
  currentBalance: number,
  entryType: "Payment" | "Loan" | "Adjustment",
  amount: string,
  adjustmentDelta: string,
): number {
  const change =
    entryType === "Adjustment"
      ? (parseAdjustmentDelta(adjustmentDelta) ?? 0)
      : (parseMoneyAmountInput(amount) ?? 0) * (entryType === "Payment" ? -1 : 1);
  return Math.round((currentBalance + change) * 100) / 100;
}

/** Signed balance correction such as "+500", "-1,250.50"; null when empty, zero, or not a number. */
export function parseAdjustmentDelta(value: string): number | null {
  const cleaned = value.replace(/[,\s₱]/g, "");
  if (!/^[+-]?(\d+(\.\d{0,2})?|\.\d{1,2})$/.test(cleaned)) return null;
  const parsed = Number(cleaned);
  return Number.isFinite(parsed) && parsed !== 0 ? parsed : null;
}

function SavePlusIcon({ className }: { className?: string }) {
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
      data-testid="utang-record-save-icon"
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4V12" />
      <path d="M16 13H8a1 1 0 0 0-1 1v7" />
      <path d="M19 22v-6" />
      <path d="M22 19h-6" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

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
      data-testid="utang-entry-save-icon"
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v4.35" />
      <path d="m16 19 2 2 4-4" />
      <path d="M17 15.13V14a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v7" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function QuickActionIndexContent({ icon: Icon, label }: { icon: LucideIcon; label: string }) {
  return (
    <>
      <span className="personal-home-index-card__ribbon" aria-hidden="true">
        <span className="personal-home-index-card__fold" />
        <Icon className="personal-home-index-card__icon" />
      </span>
      <span className="personal-home-index-card__body">
        <span className="personal-home-index-card__title">{label}</span>
      </span>
      <strong
        className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--ghost"
        aria-hidden="true"
      >
        <ChevronRight className="personal-home-index-card__icon" />
      </strong>
    </>
  );
}

function RibbonTitle({ icon: Icon, children }: { icon: LucideIcon; children: ReactNode }) {
  return (
    <h2 className="personal-tracker-card__ribbon">
      <span className="personal-tracker-card__pennant" aria-hidden>
        <Icon className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
      </span>
      {children}
    </h2>
  );
}

function ViewContactDetailsDialog({
  open,
  contact,
  connected,
  onOpenChange,
}: {
  open: boolean;
  connected: boolean;
  contact: {
    displayName: string;
    phone?: string | null;
    email?: string | null;
    publicUserId?: string | null;
    linkedMaskedEmail?: string | null;
    linkedMaskedPhone?: string | null;
    createdAtUtc: string;
  };
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = useI18n();
  const phone = connected ? contact.linkedMaskedPhone || contact.phone : contact.phone;
  const email = connected ? contact.linkedMaskedEmail || contact.email : contact.email;
  const rows = [
    { id: "name", label: t("people.displayName"), value: contact.displayName },
    ...(connected
      ? [{ id: "id", label: t("people.add.exitsId"), value: contact.publicUserId || EM_DASH }]
      : []),
    { id: "phone", label: t("people.column.contact"), value: phone || EM_DASH },
    { id: "email", label: t("people.column.email"), value: email || EM_DASH },
    ...(connected
      ? [
          {
            id: "since",
            label: t("people.detail.connection"),
            value: t("people.detail.connectedSince").replace("{date}", formatShortDate(contact.createdAtUtc)),
          },
        ]
      : []),
  ];

  return (
    <ExitsModal
      open={open}
      onOpenChange={onOpenChange}
      title={t("personal.utang.qaViewContact")}
      testId="utang-view-contact-dialog"
      closeLabel={t("personal.utang.closeRecordDialog")}
      ribbon
      ribbonIcon={Contact}
    >
      <div className="flex min-w-0 flex-col gap-3" data-testid="utang-view-contact-body">
        <StatusChip
          tone={connected ? "success" : "info"}
          appearance="emphasis"
          shape="square"
          data-testid="utang-view-contact-status"
        >
          {connected ? t("people.status.connected") : t("people.localContact")}
        </StatusChip>
        <dl className="m-0 flex min-w-0 flex-col gap-3">
          {rows.map((row) => (
            <div key={row.id} data-testid={`utang-view-contact-${row.id}`}>
              <dt className="m-0 text-[13px] font-medium text-[var(--exits-primary)]">{row.label}</dt>
              <dd className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-text)]">{row.value}</dd>
            </div>
          ))}
        </dl>
      </div>
    </ExitsModal>
  );
}

export function PersonalRelationshipDetailPage() {
  const { t } = useI18n();
  const { relationshipId = "" } = useParams();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const [entryType, setEntryType] = useState<"Payment" | "Loan" | "Adjustment">("Payment");
  const [amount, setAmount] = useState("");
  const [adjustmentDelta, setAdjustmentDelta] = useState("");
  const adjustmentInputRef = useRef<HTMLInputElement | null>(null);
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
  const [settleNote, setSettleNote] = useState("");
  const [settleError, setSettleError] = useState<string | null>(null);
  const pendingSettlementEntryIdRef = useRef<string | null>(null);
  const [entryOpen, setEntryOpen] = useState(false);
  const [sharedSettingOpen, setSharedSettingOpen] = useState(false);
  const [remindersOpen, setRemindersOpen] = useState(false);
  const [editContactOpen, setEditContactOpen] = useState(false);
  const [viewContactOpen, setViewContactOpen] = useState(false);

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
  const openingLoanEntryId = useMemo(() => {
    let oldest: { id: string; createdAtUtc: string } | null = null;
    for (const entry of history) {
      const settlement = "isSettlement" in entry && Boolean(entry.isSettlement);
      if (entry.entryType !== "Loan" || settlement) {
        continue;
      }
      if (
        !oldest ||
        entry.createdAtUtc < oldest.createdAtUtc ||
        (entry.createdAtUtc === oldest.createdAtUtc && entry.id < oldest.id)
      ) {
        oldest = entry;
      }
    }
    return oldest?.id ?? null;
  }, [history]);
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
      const delta = entryType === "Adjustment" ? parseAdjustmentDelta(adjustmentDelta) : null;
      if (entryType === "Adjustment" && delta == null) throw new Error("adjustment");
      // The server stores a positive amount on every entry; a correction carries its size.
      const amt = delta != null ? Math.abs(delta) : parseMoneyAmountInput(amount);
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
              adjustmentDelta: delta as number,
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
      setEntryType("Payment");
      setAmount("");
      setAdjustmentDelta("");
      setNotes("");
      setFormError(null);
      setEntryOpen(false);
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
      if (error instanceof Error && error.message === "adjustment") {
        setFormError(t("personal.utang.adjustmentDeltaRequired"));
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
    mutationFn: async (notes: string) => {
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
        notes,
      });
    },
    onSuccess: async () => {
      pendingSettlementEntryIdRef.current = null;
      setSettleNote("");
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
      ? t("personal.utang.ownershipMine")
      : t("personal.utang.ownershipSharedWithMe");
  const statusLabel = relationshipClosed
    ? t("personal.utang.statusSettled")
    : t("personal.utang.statusActive");
  const listBack =
    detail.perspective === "Borrowed" ? personalPageBackNav.utangOwe : personalPageBackNav.utangLent;
  const localCounterpartyContactId =
    detail.perspective === "Borrowed" ? detail.creditorContactId : detail.debtorContactId;
  const counterpartyUserIdentityId =
    detail.perspective === "Borrowed" ? detail.creditorUserIdentityId : detail.debtorUserIdentityId;
  const counterpartyContactId =
    localCounterpartyContactId ??
    (counterpartyUserIdentityId
      ? contactsForLabel.find((contact) => contact.linkedUserIdentityId === counterpartyUserIdentityId)?.id
      : undefined);
  const sharedContact = counterpartyContactId
    ? contactsForLabel.find((contact) => contact.id === counterpartyContactId)
    : undefined;
  const showSharedSettingAction = Boolean(sharedContact && counterpartyUserIdentityId);
  const showRemindersAction = !relationshipClosed && online;
  const showUpdateBalanceAction = isLedgerOwner && !relationshipClosed;
  const newRecordHref = counterpartyContactId
    ? `/personal/utang/${detail.perspective === "Borrowed" ? "owe" : "lent"}?contact=${encodeURIComponent(counterpartyContactId)}`
    : null;
  const localContact = localCounterpartyContactId
    ? contactsForLabel.find((contact) => contact.id === localCounterpartyContactId)
    : undefined;
  const showEditContactAction = !shared && Boolean(localContact);
  const viewContact = (shared ? sharedContact : localContact) ?? sharedContact ?? localContact;
  const showViewContactAction = Boolean(viewContact);
  // An Adjustment rewrites a balance against a version this device may no longer be showing.
  const adjustmentBlocked = !online && entryType === "Adjustment";
  const projectedBalance = projectUtangBalance(currentBalance, entryType, amount, adjustmentDelta);
  const resetEntryForm = () => {
    setEntryType("Payment");
    setAmount("");
    setAdjustmentDelta("");
    setNotes("");
    setFormError(null);
  };
  const adjustmentSign: "+" | "-" | null =
    adjustmentDelta.startsWith("+") ? "+" : adjustmentDelta.startsWith("-") ? "-" : null;
  // Owner-model: unresolved-pending limit does not apply to new Confirmed writes.
  const loanBlockedByPendingLimit = false;
  const submitLabel = t("personal.utang.saveEntry");
  const viewPendingTo = `/personal/utang/relationships/${relationshipId}`;
  const settleBlockedOffline = !online;
  const canMutateFinances = isLedgerOwner;
  const showSettleAction =
    canMutateFinances &&
    relationshipActive &&
    currentBalance > 0 &&
    unresolvedPendingCount === 0;

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
        titleTrailing={
          showEditContactAction ? (
            <button
              type="button"
              className="page-header-round-action"
              aria-label={t("personal.utang.editPerson")}
              title={t("personal.utang.editPerson")}
              data-testid="utang-detail-edit"
              onClick={() => setEditContactOpen(true)}
            >
              <PenLine className="size-4" aria-hidden />
            </button>
          ) : null
        }
      />
      {usingCache ? <OfflineNotice message={t("offline.personalCachedNotice")} /> : null}
      <div className="utang-detail-top">
      <div
        className="utang-balance-card utang-ribbon-card flex min-w-0 flex-col gap-2"
        data-testid="utang-balance-card"
      >
        <RibbonTitle icon={detail.perspective === "Borrowed" ? Wallet : HandCoins}>
          {detail.perspective === "Borrowed"
            ? t("personal.utang.youOweName").replace("{name}", ownerDisplayName)
            : t("personal.utang.oweYou").replace("{name}", personName)}
        </RibbonTitle>
        <div id="utang-balance-panel" className="flex min-w-0 flex-col gap-2">
        <MoneyDisplay amount={currentBalance} className="utang-balance-card__amount" />
        <span className="utang-detail-tags">
          <StatusChip
            tone={isLedgerOwner ? "primary" : "info"}
            appearance="emphasis"
            shape="square"
            data-testid="utang-detail-ledger"
          >
            {ledgerLabel}
          </StatusChip>
          <StatusChip
            tone="info"
            appearance="emphasis"
            shape="square"
            data-testid="utang-detail-contact"
          >
            {shared ? t("personal.utang.sharedContact") : t("personal.utang.privateRecord")}
          </StatusChip>
          <StatusChip
            tone={relationshipActive ? "success" : "neutral"}
            appearance="emphasis"
            shape="square"
            data-testid="utang-detail-status"
          >
            {statusLabel}
          </StatusChip>
        </span>
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

        {showSettleAction || showUpdateBalanceAction ? (
          <div className="flex min-w-0 flex-col gap-2">
            {showSettleAction && settleBlockedOffline ? (
              <OfflineNotice
                message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangSettle))}
              />
            ) : null}
            <div className="flex w-full items-stretch gap-2">
              {showSettleAction ? (
                <Button
                  type="button"
                  intent="primary"
                  appearance="glossy"
                  className="min-w-0 flex-1"
                  disabled={settleBlockedOffline || settleMutation.isPending}
                  data-testid="utang-settle"
                  onClick={() => {
                    setSettleNote("");
                    setSettleError(null);
                    setSettleOpen(true);
                  }}
                >
                  <CheckCircle2 className="size-4 shrink-0" aria-hidden="true" />
                  {t("personal.utang.settle")}
                </Button>
              ) : null}
              {showUpdateBalanceAction ? (
                <Button
                  type="button"
                  intent="info"
                  appearance="glossy"
                  className="min-w-0 flex-1"
                  data-testid="utang-qa-update-balance"
                  onClick={() => {
                    resetEntryForm();
                    setEntryOpen(true);
                  }}
                >
                  <PenLine className="size-4 shrink-0" aria-hidden="true" />
                  {t("personal.utang.qaUpdateBalance")}
                </Button>
              ) : null}
            </div>
            {showSettleAction ? (
            <ExitsModal
              open={settleOpen}
              onOpenChange={(next) => {
                if (settleMutation.isPending) return;
                setSettleOpen(next);
                if (!next) setSettleError(null);
              }}
              title={t("personal.utang.settleTitle")}
              testId="utang-settle-dialog"
              className="utang-settle-dialog"
              size="sm"
              closeLabel={t("personal.utang.closeRecordDialog")}
              busy={settleMutation.isPending}
              ribbon
              ribbonIcon={CheckCircle2}
              footer={
                <>
                  <Button
                    type="button"
                    intent="danger"
                    appearance="glossy"
                    emphasis="strong"
                    disabled={settleMutation.isPending}
                    data-testid="utang-settle-cancel"
                    onClick={() => {
                      setSettleOpen(false);
                      setSettleError(null);
                    }}
                  >
                    {t("personal.utang.cancelEdit")}
                  </Button>
                  <Button
                    type="button"
                    intent="primary"
                    appearance="glossy"
                    disabled={settleBlockedOffline || settleMutation.isPending}
                    data-testid="utang-settle-confirm"
                    onClick={() => {
                      const notes = settleNote.trim();
                      if (!notes) {
                        setSettleError(t("personal.utang.settleNoteRequired"));
                        return;
                      }
                      setSettleError(null);
                      settleMutation.mutate(notes);
                    }}
                  >
                    <Check className="size-4 shrink-0" aria-hidden="true" />
                    {t("personal.utang.settleConfirm")}
                  </Button>
                </>
              }
            >
              <div className="utang-settle-dialog__body" data-testid="utang-settle-panel">
                <div className="utang-settle-dialog__summary">
                  <span className="utang-settle-dialog__summary-item">
                    <span className="utang-settle-dialog__label">{t("personal.utang.settleAmount")}</span>
                    <MoneyDisplay amount={currentBalance} className="utang-settle-dialog__amount" />
                  </span>
                  <span className="utang-settle-dialog__arrow" aria-hidden>
                    →
                  </span>
                  <span className="utang-settle-dialog__summary-item">
                    <span className="utang-settle-dialog__label">{t("personal.utang.settleAfter")}</span>
                    <MoneyDisplay amount={0} className="utang-settle-dialog__amount" />
                  </span>
                </div>
                <p className="utang-settle-dialog__hint">
                  {shared
                    ? t("personal.utang.settleSharedHint")
                    : t("personal.utang.settlePrivateHint")}
                </p>
                <ExitsOutlineInput
                  label={t("personal.utang.settleNote")}
                  value={settleNote}
                  onChange={setSettleNote}
                  maxLength={UTANG_NOTES_MAX_LENGTH}
                  required
                  testId="utang-settle-note"
                />
                {settleError ? <Notice tone="danger">{settleError}</Notice> : null}
              </div>
            </ExitsModal>
            ) : null}
          </div>
        ) : null}
        </div>
      </div>

      {showViewContactAction || showSharedSettingAction || showEditContactAction || showRemindersAction || newRecordHref ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section personal-quick-actions utang-quick-actions gap-3"
          aria-label={t("personal.home.quickActions")}
          data-testid="utang-detail-quick-actions"
        >
          <h2 className="personal-quick-actions__ribbon">
            <span className="personal-quick-actions__pennant" aria-hidden>
              <Zap className="personal-quick-actions__pennant-icon size-[0.95rem] shrink-0" />
            </span>
            {t("personal.home.quickActions")}
          </h2>
          <div className="personal-quick-actions__cards">
            {showViewContactAction ? (
              <button
                type="button"
                className="personal-home-index-card personal-home-index-card--info personal-home-index-card--solid"
                data-testid="utang-qa-view-contact"
                aria-haspopup="dialog"
                onClick={() => setViewContactOpen(true)}
              >
                <QuickActionIndexContent icon={Contact} label={t("personal.utang.qaViewContact")} />
              </button>
            ) : null}
            {showEditContactAction ? (
              <button
                type="button"
                className="personal-home-index-card personal-home-index-card--primary personal-home-index-card--solid"
                data-testid="utang-qa-edit-contact"
                aria-haspopup="dialog"
                onClick={() => setEditContactOpen(true)}
              >
                <QuickActionIndexContent icon={Pencil} label={t("personal.utang.qaEditContact")} />
              </button>
            ) : null}
            {showSharedSettingAction ? (
              <button
                type="button"
                className="personal-home-index-card personal-home-index-card--primary personal-home-index-card--solid"
                data-testid="utang-qa-shared-setting"
                aria-haspopup="dialog"
                onClick={() => setSharedSettingOpen(true)}
              >
                <QuickActionIndexContent icon={SlidersHorizontal} label={t("personal.utang.qaSharedSetting")} />
              </button>
            ) : null}
            {showRemindersAction ? (
              <button
                type="button"
                className="personal-home-index-card personal-home-index-card--warn personal-home-index-card--solid"
                data-testid="utang-qa-reminders"
                aria-haspopup="dialog"
                onClick={() => setRemindersOpen(true)}
              >
                <QuickActionIndexContent icon={BellRing} label={t("personal.utang.qaCreateReminders")} />
              </button>
            ) : null}
            {newRecordHref ? (
              <Link
                to={newRecordHref}
                className="personal-home-index-card personal-home-index-card--success personal-home-index-card--solid"
                data-testid="utang-qa-new-record"
              >
                <QuickActionIndexContent icon={Plus} label={t("personal.utang.qaNewRecord")} />
              </Link>
            ) : null}
          </div>
        </section>
      ) : null}

      {showViewContactAction && viewContact ? (
        <ViewContactDetailsDialog
          open={viewContactOpen}
          contact={viewContact}
          connected={shared}
          onOpenChange={setViewContactOpen}
        />
      ) : null}

      {showEditContactAction && localContact ? (
        <EditLocalContactDialog
          open={editContactOpen}
          contact={localContact}
          onOpenChange={setEditContactOpen}
        />
      ) : null}

      {showRemindersAction ? (
        <ExitsModal
          open={remindersOpen}
          onOpenChange={setRemindersOpen}
          title={t("personal.utang.qaReminders")}
          description={
            personName === EM_DASH ? undefined : (
              <span className="utang-reminders-dialog__name">{personName}</span>
            )
          }
          testId="utang-reminders-dialog"
          closeLabel={t("personal.utang.closeRecordDialog")}
          size="lg"
          ribbon
          ribbonIcon={BellRing}
        >
          <RelationshipInviteReminderPanel
            relationshipId={relationshipId}
            counterpartyName={ownerDisplayName}
            canRemindThem={
              shared &&
              detail.creditorUserIdentityId != null &&
              detail.debtorUserIdentityId != null
            }
          />
        </ExitsModal>
      ) : null}
      {!relationshipClosed && !online ? (
        <OfflineNotice
          message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangReminder))}
        />
      ) : null}

      {shared && pendingOutgoingCount > 0 ? (
        <PendingOutgoingHint
          count={pendingOutgoingCount}
          name={personName === EM_DASH ? t("personal.utang.person") : personName}
          atLimit={pendingAtLimit}
          viewPendingTo={viewPendingTo}
        />
      ) : null}

      {awaitingSettlement ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)]"
          data-testid="utang-settle-awaiting"
          role="status"
        >
          {t("personal.utang.settleAwaiting")}
        </p>
      ) : null}

      {settleError && !settleOpen ? (
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

      {showSharedSettingAction && counterpartyUserIdentityId && sharedContact ? (
        <ExitsModal
          open={sharedSettingOpen}
          onOpenChange={setSharedSettingOpen}
          title={t("personal.utang.prefsTitle").replace("{name}", sharedContact.displayName)}
          testId="utang-shared-setting-dialog"
          closeLabel={t("personal.utang.closeRecordDialog")}
          size="lg"
          ribbon
          ribbonIcon={SlidersHorizontal}
        >
          <SharedUtangSettingPanel
            contactId={sharedContact.id}
            displayName={sharedContact.displayName}
            createdAtUtc={sharedContact.createdAtUtc}
            counterpartyUserIdentityId={counterpartyUserIdentityId}
          />
        </ExitsModal>
      ) : null}

      {canMutateFinances && !relationshipClosed ? (
      <ExitsModal
        open={entryOpen}
        onOpenChange={(next) => {
          setEntryOpen(next);
          if (!next) resetEntryForm();
        }}
        title={t("personal.utang.qaUpdateBalance")}
        description={
          <span className="utang-entry-dialog__head" data-testid="utang-entry-dialog-head">
            {personName === EM_DASH ? null : (
              <>
                <span className="utang-entry-dialog__party">
                  <span className="utang-entry-dialog__label">{t("personal.utang.nameLabel")}</span>
                  <span className="utang-entry-dialog__name">{personName}</span>
                </span>
                <span className="utang-entry-dialog__divider" aria-hidden />
              </>
            )}
            <span className="utang-entry-dialog__balance">
              <span className="utang-entry-dialog__label">
                {detail.perspective === "Borrowed"
                  ? t("personal.utang.youOwe")
                  : t("personal.utang.youLent")}
              </span>
              <MoneyDisplay
                amount={projectedBalance}
                className={cn(
                  "utang-entry-dialog__amount",
                  projectedBalance !== currentBalance && "utang-entry-dialog__amount--changed",
                )}
                testId="utang-entry-dialog-balance"
              />
            </span>
          </span>
        }
        busy={recordMutation.isPending}
        closeLabel={t("personal.utang.closeRecordDialog")}
        testId="utang-entry-dialog"
        className="utang-entry-dialog"
        ribbon
        ribbonIcon={PenLine}
      >
      <form
        className="utang-entry-form flex min-w-0 flex-col gap-2"
        noValidate
        onSubmit={(event) => {
          event.preventDefault();
          if (entryType === "Adjustment") {
            if (parseAdjustmentDelta(adjustmentDelta) == null) {
              setFormError(t("personal.utang.adjustmentDeltaRequired"));
              return;
            }
          } else {
            const parsedAmount = parseMoneyAmountInput(amount);
            if (parsedAmount == null || !(parsedAmount > 0)) {
              setFormError(t("personal.utang.amountRequired"));
              return;
            }
            if (parsedAmount > currentBalance) {
              setFormError(t("personal.utang.amountExceedsBalance"));
              return;
            }
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
        <div className="grid min-w-0 gap-3 sm:grid-cols-2 sm:items-end">
          <ExitsOutlineSelect
            label={t("personal.utang.entryType")}
            value={entryType}
            options={[
              { value: "Payment", label: t("personal.utang.recordPayment") },
              { value: "Loan", label: t("personal.utang.addAmount") },
              { value: "Adjustment", label: t("personal.utang.adjustBalance") },
            ]}
            onChange={(next) => {
              setEntryType(next);
              setAdjustmentDelta("");
            }}
            testId="utang-entry-type"
          />
          {entryType === "Adjustment" ? (
            <div className="flex min-w-0 flex-col gap-1.5">
              {adjustmentSign ? (
                <>
                  <label htmlFor="utang-entry-delta" className="exits-type-label">
                    {t("personal.utang.adjustmentDelta")}
                  </label>
                  <div className="exits-currency-field utang-adjust-field" data-sign={adjustmentSign}>
                    <button
                      type="button"
                      className="exits-currency-field__prefix utang-adjust-field__sign"
                      aria-label={t("personal.utang.adjustmentChangeDirection")}
                      title={t("personal.utang.adjustmentChangeDirection")}
                      data-testid="utang-entry-delta-sign"
                      onClick={() => setAdjustmentDelta("")}
                    >
                      {adjustmentSign === "+" ? "+" : "−"}
                    </button>
                    <input
                      ref={adjustmentInputRef}
                      id="utang-entry-delta"
                      data-testid="utang-entry-delta"
                      className="exits-currency-field__input"
                      inputMode="decimal"
                      autoComplete="off"
                      placeholder="0.00"
                      value={adjustmentDelta.slice(1)}
                      onChange={(e) =>
                        setAdjustmentDelta(
                          adjustmentSign + normalizeMoneyAmountTyping(e.target.value.replace(/[+-]/g, "")),
                        )
                      }
                      onBlur={() => {
                        const parsed = parseMoneyAmountInput(adjustmentDelta.slice(1));
                        if (parsed != null && parsed > 0) {
                          setAdjustmentDelta(adjustmentSign + formatMoneyAmountInput(parsed));
                        }
                      }}
                      required
                    />
                  </div>
                </>
              ) : (
                <>
                  <span id="utang-entry-delta-label" className="exits-type-label">
                    {t("personal.utang.adjustmentDelta")}
                  </span>
                  <div
                    className="grid grid-cols-2 gap-2"
                    role="group"
                    aria-labelledby="utang-entry-delta-label"
                  >
                    {(["+", "-"] as const).map((sign) => (
                      <Button
                        key={sign}
                        type="button"
                        intent={sign === "+" ? "success" : "danger"}
                        appearance="outline"
                        className="w-full"
                        data-exits-modal-autofocus={sign === "+" ? "true" : undefined}
                        data-testid={sign === "+" ? "utang-entry-delta-add" : "utang-entry-delta-minus"}
                        onClick={() => {
                          setAdjustmentDelta(sign);
                          requestAnimationFrame(() => adjustmentInputRef.current?.focus());
                        }}
                      >
                        {sign === "+" ? (
                          <Plus className="size-4 shrink-0" aria-hidden />
                        ) : (
                          <Minus className="size-4 shrink-0" aria-hidden />
                        )}
                        {t(sign === "+" ? "personal.utang.adjustmentAdd" : "personal.utang.adjustmentMinus")}
                      </Button>
                    ))}
                  </div>
                </>
              )}
            </div>
          ) : (
            <ExitsOutlineInput
              id="utang-entry-amount"
              label={t("personal.utang.amount")}
              data-exits-modal-autofocus="true"
              testId="utang-entry-amount"
              autoComplete="off"
              inputMode="decimal"
              value={amount}
              clearable
              onChange={(next) => {
                const normalized = normalizeMoneyAmountTyping(next);
                const parsed = parseMoneyAmountInput(normalized);
                if (parsed != null && parsed > currentBalance) {
                  setFormError(t("personal.utang.amountExceedsBalance"));
                  return;
                }
                setAmount(normalized);
                setFormError((error) =>
                  error === t("personal.utang.amountExceedsBalance") ? null : error,
                );
              }}
              onFocus={() => {
                if ((parseMoneyAmountInput(amount) ?? 0) === 0) {
                  setAmount("");
                }
              }}
              onBlur={() => {
                const parsed = parseMoneyAmountInput(amount);
                setAmount(formatMoneyAmountInput(parsed && parsed > 0 ? parsed : 0));
              }}
              required
            />
          )}
        </div>
        <div className="flex min-w-0 flex-col gap-1.5">
          <label htmlFor="utang-entry-notes" className="exits-type-label">
            <UtangFieldLabel required>
              {entryType === "Adjustment"
                ? t("personal.utang.adjustmentReason")
                : t("personal.utang.purpose")}
            </UtangFieldLabel>
          </label>
          {entryType === "Adjustment" ? (
            <span className="text-[length:var(--exits-text-xs)] font-normal text-muted">
              {t("personal.utang.adjustmentReasonHelp")}
            </span>
          ) : null}
          <textarea
            id="utang-entry-notes"
            data-testid="utang-entry-notes"
            className="exits-input min-h-20 w-full min-w-0 resize-y"
            value={notes}
            maxLength={UTANG_NOTES_MAX_LENGTH}
            required
            aria-required="true"
            onChange={(e) => setNotes(e.target.value)}
          />
        </div>
        {adjustmentBlocked ? (
          <OfflineNotice
            message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangAdjustment))}
          />
        ) : null}
        {!online && !adjustmentBlocked ? (
          <OfflineNotice message={t("offline.requiredPersonalUtangRecord")} />
        ) : null}
        {shared && online && isLedgerOwner ? (
          <Notice tone="info" testId="utang-entry-confirm-hint">
            {t("personal.utang.sharedEntryHint").replace("{name}", personName)}
          </Notice>
        ) : null}
        {formError ? (
          <Notice tone="danger" testId="utang-entry-error">
            {formError}
          </Notice>
        ) : null}
        <div className="flex min-w-0 flex-wrap justify-end gap-2 border-t border-border pt-3">
          <Button
            type="button"
            intent="danger"
            appearance="glossy"
            emphasis="strong"
            disabled={recordMutation.isPending}
            onClick={() => {
              setEntryOpen(false);
              resetEntryForm();
            }}
            data-testid="utang-entry-cancel"
          >
            {t("personal.utang.cancelEdit")}
          </Button>
          <Button
            type="submit"
            intent="primary"
            appearance="glossy"
            emphasis="strong"
            disabled={
              recordMutation.isPending ||
              statusLocked ||
              adjustmentBlocked ||
              loanBlockedByPendingLimit ||
              !online
            }
            data-testid="utang-entry-submit"
          >
            <SaveCheckIcon className="size-4 shrink-0" />
            {submitLabel}
          </Button>
        </div>
      </form>
      </ExitsModal>
      ) : null}
      </div>

      <section
        className="utang-activity-card utang-ribbon-card catalog-form-section exits-animate-panel personal-section min-w-0 gap-2"
        aria-label={t("personal.utang.activity")}
      >
        <RibbonTitle icon={History}>{t("personal.utang.activity")}</RibbonTitle>
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
          <ul className="utang-relationship-list utang-activity-list m-0 list-none p-0" data-testid="utang-history">
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
              const lent = detail.perspective !== "Borrowed";
              const isOpening = entry.id === openingLoanEntryId;
              const entryTitle = entryActionLabel(
                entry.entryType,
                { isSettlement, isOpening },
                t,
              );
              const statusText = entryStatusLabel(
                {
                  status,
                  isSharedLedger: shared,
                  wasAutoSynced: "wasAutoSynced" in entry ? Boolean(entry.wasAutoSynced) : false,
                },
                {
                  pendingIncoming,
                  reporterName: personName === EM_DASH ? undefined : personName,
                },
                t,
              );
              const entryDue =
                "dueDateUtc" in entry && entry.dueDateUtc ? String(entry.dueDateUtc) : null;

              return (
                <li key={entry.id}>
                  <div
                    className={cn(
                      "utang-activity-entry ui-standards-qcard ui-standards-qcard--settings-card utang-account-card utang-account-card--standard utang-settings-card min-w-0 text-foreground",
                      lent ? "utang-settings-card--lent" : "utang-settings-card--owe",
                      isLedgerOwner ? "utang-account-card--mine" : "utang-account-card--shared",
                    )}
                    data-testid={`utang-history-entry-${entry.id}`}
                  >
                    <div className="ui-standards-qcard__settings-ribbon">
                      <div
                        className="ui-standards-qcard__settings-tab utang-settings-card__tab"
                        data-utang-ownership=""
                      >
                        <span className="truncate">
                          {isLedgerOwner
                            ? t("personal.utang.ownershipMine")
                            : t("personal.utang.ownershipSharedWithMe")}
                        </span>
                        {entryDue && formatDueLabel(entryDue).kind !== "none" ? (
                          <>
                            <span className="utang-settings-card__tab-divider" aria-hidden />
                            <UtangDueCaption
                              dueDateUtc={entryDue}
                              className={cn(
                                "utang-settings-card__tab-text",
                                formatDueLabel(entryDue).kind === "overdue" &&
                                  "utang-settings-card__tab-text--danger",
                              )}
                            />
                          </>
                        ) : null}
                        <span className="ui-standards-qcard__settings-fold" aria-hidden />
                      </div>
                      <div className="utang-settings-card__content">
                        <div className="utang-settings-card__meta flex min-w-0 flex-1 flex-col gap-2">
                          <div className="utang-settings-card__headline">
                            <p
                              className="utang-settings-card__name m-0 truncate"
                              data-testid={`utang-entry-action-${entry.id}`}
                            >
                              {entryTitle}
                            </p>
                            <span className="utang-activity-amount">
                              <span className="utang-settings-card__amount">
                                <MoneyDisplay amount={entry.signedDelta} />
                              </span>
                              {!isOpening && affectsBalance ? (
                                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                                  {t("personal.utang.balanceAfter")}:{" "}
                                  <MoneyDisplay amount={entry.balanceAfter} />
                                </p>
                              ) : null}
                              {!isOpening && !affectsBalance ? (
                                <p
                                  className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                                  data-testid={`utang-no-balance-${entry.id}`}
                                >
                                  {t("personal.utang.noBalanceChange")}
                                </p>
                              ) : null}
                            </span>
                          </div>
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
                        <div className="utang-activity-facts">
                          <span className="utang-activity-facts__chips">
                            <StatusChip
                              tone={lent ? "success" : "warning"}
                              appearance="emphasis"
                              shape="square"
                            >
                              {lent ? t("personal.utang.owesYou") : t("personal.utang.youOwe")}
                            </StatusChip>
                            <StatusChip
                              tone={shared ? "success" : "info"}
                              appearance="emphasis"
                              shape="square"
                            >
                              {shared ? t("people.status.connected") : t("people.localContact")}
                            </StatusChip>
                            {!isLedgerOwner ? (
                              <StatusChip
                                tone={UTANG_READ_ONLY_CHIP.tone}
                                appearance={UTANG_READ_ONLY_CHIP.appearance}
                                shape={UTANG_READ_ONLY_CHIP.shape}
                                className="max-md:hidden"
                              >
                                {t("personal.utang.readOnly")}
                              </StatusChip>
                            ) : null}
                          </span>
                          {entry.notes ? (
                            <p
                              className="text-[length:var(--exits-text-sm)]"
                              data-testid={`utang-entry-purpose-${entry.id}`}
                            >
                              <span className="text-muted">{t("personal.utang.purpose")}: </span>
                              {entry.notes}
                            </p>
                          ) : null}
                          <p className="utang-activity-facts__created">
                            <span className="text-muted">{t("personal.utang.created")}: </span>
                            {new Date(entry.createdAtUtc).toLocaleString()}
                          </p>
                        </div>
                        {statusText !== t("personal.utang.statusPrivate") ? (
                          <p
                            className="m-0 text-[length:var(--exits-text-sm)]"
                            data-testid={`utang-entry-status-${entry.id}`}
                          >
                            {statusText}
                          </p>
                        ) : null}
                        {pendingOutgoing ? (
                          <p
                            className="m-0 text-[length:var(--exits-text-sm)] font-medium"
                            data-testid={`utang-waiting-other-${entry.id}`}
                          >
                            {t("personal.utang.waitingForName").replace("{name}", personName)}
                          </p>
                        ) : null}
                        {disputeReason ? (
                          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                            {disputeReason}
                          </p>
                        ) : null}
                        <WaitingChip origin={rowOrigin(entry)} />

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
                      </div>
                    </div>
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

