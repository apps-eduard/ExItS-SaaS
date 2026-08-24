import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ChevronRight, HandCoins, Loader2, UserPlus, Wallet } from "lucide-react";
import {
  createPersonalContact,
  createPersonalDebtRelationship,
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
  updatePersonalContact,
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
import { cn } from "@/lib/cn";
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
  enqueuePersonalContactCreate,
  enqueuePersonalRelationshipCreate,
  enqueuePersonalUtangEntry,
} from "@/offline/personal-utang-offline";

function contactLabel(
  contacts: PersonalContactDto[],
  relationship: PersonalDebtRelationshipSummaryDto,
): string {
  const contactId =
    relationship.perspective === "Borrowed"
      ? relationship.creditorContactId
      : relationship.debtorContactId;
  if (contactId) {
    return contacts.find((c) => c.id === contactId)?.displayName ?? "—";
  }
  return "—";
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
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const { refreshCounts: refreshOfflineSync } = useOfflineSync();
  const formRef = useRef<HTMLFormElement>(null);
  const nameInputRef = useRef<HTMLInputElement>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [cacheEpoch, setCacheEpoch] = useState(0);

  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
    enabled: online,
  });

  useEffect(() => {
    if (!offline || !contactsQuery.data) {
      return;
    }
    void cachePersonalContacts(offline.db, offline.scopeBinding, contactsQuery.data);
  }, [contactsQuery.data, offline]);

  const cachedContacts = usePersonalUtangCache<CachedPersonalContact[]>(
    ({ db }) => listCachedPersonalContacts(db.db, db.scopeBinding),
    [cacheEpoch, contactsQuery.dataUpdatedAt],
    [],
  );

  const usingCache = !online || contactsQuery.isError;

  function resetForm() {
    setEditingId(null);
    setDisplayName("");
    setPhone("");
    setEmail("");
    setFormError(null);
  }

  function startEdit(contact: PersonalContactDto) {
    setEditingId(contact.id);
    setDisplayName(contact.displayName);
    setPhone(contact.phone ?? "");
    setEmail(contact.email ?? "");
    setFormError(null);
  }

  useEffect(() => {
    if (!editingId) {
      return;
    }
    formRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
    const focusTimer = window.setTimeout(() => {
      nameInputRef.current?.focus();
      nameInputRef.current?.select();
    }, 180);
    return () => window.clearTimeout(focusTimer);
  }, [editingId]);

  const saveOffline = async () => {
    if (!offline) {
      throw new Error("offline-unavailable");
    }
    if (editingId) {
      throw new Error("offline-edit-unsupported");
    }
    const id = createSecureMutationId();
    if (!id.ok) {
      throw new Error("id-unavailable");
    }
    await enqueuePersonalContactCreate({
      db: offline.db,
      scopeBinding: offline.scopeBinding,
      userId: offline.userId,
      contactId: id.id,
      contact: {
        displayName: displayName.trim(),
        phone: phone.trim() || null,
        email: email.trim() || null,
      },
    });
    await refreshOfflineSync();
    setCacheEpoch((epoch) => epoch + 1);
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!online) {
        await saveOffline();
        return;
      }
      const body = {
        displayName: displayName.trim(),
        phone: phone.trim() || null,
        email: email.trim() || null,
      };
      if (editingId) {
        await updatePersonalContact(editingId, body);
        return;
      }
      await createPersonalContact(body);
    },
    onSuccess: async () => {
      resetForm();
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang", "contacts"] });
      await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
    },
    onError: (error) => {
      if (!online) {
        setFormError(
          error instanceof Error && error.message === "offline-edit-unsupported"
            ? t("personal.utang.editRequiresOnline")
            : t("offline.personalEnqueueFailed"),
        );
        return;
      }
      setFormError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
    },
  });

  if (online && contactsQuery.isPending) return <LoadingSkeleton />;
  if (online && contactsQuery.isError && cachedContacts.length === 0) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("personal.utang.people")}
          description={t("personal.utang.peopleLede")}
          backTo={personalPageBackNav.utang.to}
          backLabel={t("personal.utang.back")}
          backTestId="page-header-back-utang-people"
        />
        <ErrorState
          title={t("personal.utang.loadErrorTitle")}
          detail={t("personal.utang.loadErrorDetail")}
        />
      </div>
    );
  }

  const contacts: CachedPersonalContact[] | PersonalContactDto[] = usingCache
    ? cachedContacts
    : (contactsQuery.data ?? []);

  const isEditing = editingId != null;
  const editingContact = isEditing
    ? contacts.find((contact) => contact.id === editingId)
    : undefined;

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-utang-people">
      <PageHeader
        title={t("personal.utang.people")}
        subtitle={editingContact?.displayName}
        description={t("personal.utang.peopleLede")}
        backTo={personalPageBackNav.utang.to}
        backLabel={t("personal.utang.back")}
        backTestId="page-header-back-utang-people"
      />

      {usingCache ? <OfflineNotice message={t("offline.personalCachedNotice")} /> : null}

      <form
        ref={formRef}
        className={cn(
          "catalog-form-section exits-animate-panel personal-section flex flex-col gap-2",
          isEditing && "catalog-form-section--editing",
        )}
        data-testid="utang-contact-form"
        onSubmit={(event) => {
          event.preventDefault();
          if (!displayName.trim()) {
            setFormError(t("personal.utang.nameRequired"));
            return;
          }
          saveMutation.mutate();
        }}
      >
        <h2 className="catalog-form-section__title">
          {isEditing ? t("personal.utang.editPerson") : t("personal.utang.addPerson")}
        </h2>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]" htmlFor="utang-contact-name">
          {t("personal.utang.name")}
          <input
            id="utang-contact-name"
            ref={nameInputRef}
            data-testid="utang-contact-name"
            autoComplete="name"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            required
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]" htmlFor="utang-contact-phone">
          {t("personal.utang.phone")}
          <input
            id="utang-contact-phone"
            data-testid="utang-contact-phone"
            autoComplete="tel"
            inputMode="tel"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]" htmlFor="utang-contact-email">
          {t("personal.utang.email")}
          <input
            id="utang-contact-email"
            data-testid="utang-contact-email"
            type="email"
            autoComplete="email"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
        {!online && !isEditing ? (
          <OfflineNotice message={t("offline.personalContactWillQueue")} />
        ) : null}
        {isEditing && !online ? (
          <OfflineNotice message={t("personal.utang.editRequiresOnline")} />
        ) : null}
        {formError ? (
          <p
            role="alert"
            className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {formError}
          </p>
        ) : null}
        <div className="flex flex-wrap gap-2">
          <Button
            type="submit"
            className="min-h-11"
            disabled={saveMutation.isPending || (!online && !offline) || (isEditing && !online)}
            data-testid="utang-contact-submit"
          >
            {isEditing ? t("personal.utang.savePerson") : t("personal.utang.addPerson")}
          </Button>
          {isEditing ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              disabled={saveMutation.isPending}
              data-testid="utang-contact-cancel-edit"
              onClick={() => resetForm()}
            >
              {t("personal.utang.cancelEdit")}
            </Button>
          ) : null}
        </div>
      </form>

      {contacts.length === 0 ? (
        <EmptyState
          title={t("personal.utang.peopleEmptyTitle")}
          detail={t("personal.utang.peopleEmptyDetail")}
        />
      ) : (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          aria-label={t("personal.utang.people")}
        >
          <h2 className="catalog-form-section__title text-muted">{t("personal.utang.people")}</h2>
          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {contacts.map((contact) => {
              const isLocal = rowOrigin(contact) === "Local";
              const isActive = editingId === contact.id;
              const meta =
                [contact.phone, contact.email].filter(Boolean).join(" · ") ||
                t("personal.utang.unlinkedContact");

              if (isLocal) {
                return (
                  <li key={contact.id}>
                    <div
                      className="exits-list__card flex flex-col gap-1"
                      data-testid={`utang-contact-${contact.id}`}
                    >
                      <p className="exits-list__name m-0 font-semibold">{contact.displayName}</p>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{meta}</p>
                      <WaitingChip origin="Local" />
                    </div>
                  </li>
                );
              }

              return (
                <li key={contact.id}>
                  <button
                    type="button"
                    className={cn(
                      "exits-list__card flex min-h-11 w-full items-center justify-between gap-3",
                      isActive && "exits-list__card--editing",
                    )}
                    data-testid={`utang-contact-${contact.id}`}
                    aria-pressed={isActive}
                    aria-label={`${t("personal.utang.editPerson")}: ${contact.displayName}`}
                    disabled={saveMutation.isPending}
                    onClick={() => startEdit(contact)}
                  >
                    <div className="min-w-0 flex-1">
                      <p className="exits-list__name m-0 truncate font-semibold">
                        {contact.displayName}
                      </p>
                      <p className="m-0 truncate text-[length:var(--exits-text-sm)] text-muted">
                        {meta}
                      </p>
                      <WaitingChip origin={rowOrigin(contact)} />
                    </div>
                    <ChevronRight className="size-4 shrink-0 text-muted" aria-hidden />
                  </button>
                </li>
              );
            })}
          </ul>
        </section>
      )}
    </div>
  );
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

  const saveOffline = async () => {
    if (!offline) {
      throw new Error("offline-unavailable");
    }
    if (!ownerUserIdentityId) {
      throw new Error("owner-unknown");
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
      initialLoanNotes: notes.trim() || null,
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
              initialLoanNotes: notes.trim() || null,
            }
          : {
              creditorUserIdentityId: null,
              creditorContactId: contactId,
              debtorUserIdentityId: me,
              debtorContactId: null,
              currencyCode: "PHP",
              dueDateUtc: dueDate ? new Date(dueDate).toISOString() : null,
              initialLoanAmount: initial,
              initialLoanNotes: notes.trim() || null,
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
        setFormError(
          error instanceof Error && error.message === "owner-unknown"
            ? t("offline.personalOwnerUnknown")
            : t("offline.personalEnqueueFailed"),
        );
        return;
      }
      setFormError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
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
  const contacts: CachedPersonalContact[] | PersonalContactDto[] = usingCache
    ? cachedContacts
    : (contactsQuery.data ?? []);
  const rows: CachedPersonalRelationship[] | PersonalDebtRelationshipSummaryDto[] = usingCache
    ? cachedRows
    : (listQuery.data ?? []);

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
          createMutation.mutate();
        }}
      >
        <h2 className="catalog-form-section__title">
          {mode === "lent" ? t("personal.utang.recordLent") : t("personal.utang.recordOwe")}
        </h2>
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
          {t("personal.utang.dueDate")}
          <input
            data-testid="utang-rel-due"
            type="date"
            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </label>
        <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.note")}
          <input
            data-testid="utang-rel-notes"
            className="min-h-11 w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
        {!online ? <OfflineNotice message={t("offline.personalUtangWillQueue")} /> : null}
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
          {mode === "lent" ? t("personal.utang.recordLent") : t("personal.utang.recordOwe")}
        </Button>
        {contacts.length === 0 ? (
          <Button asChild variant="ghost" className="min-h-11">
            <Link to="/personal/utang/people">
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
          {rows.map((row) => (
            <li key={row.id}>
              <Link
                to={`/personal/utang/relationships/${row.id}`}
                className="exits-list__card flex min-h-11 items-center justify-between gap-3 text-foreground no-underline"
                data-testid={`utang-rel-row-${row.id}`}
              >
                <div className="min-w-0">
                  <p className="exits-list__name m-0 truncate font-semibold">
                    {contactLabel(contacts, row)}
                  </p>
                  <DueChip dueDateUtc={row.dueDateUtc} />
                  <WaitingChip origin={rowOrigin(row)} />
                </div>
                <MoneyDisplay amount={row.currentBalance} />
              </Link>
            </li>
          ))}
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
              notes: notes.trim() || null,
            }
          : {
              entryType,
              amount: amt,
              expectedVersion: version ?? null,
              notes: notes.trim() || null,
            };
      await recordPersonalUtangEntry(relationshipId, body);
    },
    onSuccess: async () => {
      setAmount("");
      setAdjustmentDelta("");
      setNotes("");
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang"] });
      await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
    },
    onError: (error) => {
      if (!online) {
        setFormError(
          error instanceof Error && error.message === "adjustment-online-only"
            ? t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalUtangAdjustment))
            : t("offline.personalEnqueueFailed"),
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
      setFormError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
    },
  });

  const contactsForLabel = useMemo<CachedPersonalContact[] | PersonalContactDto[]>(
    () => (usingCache ? cachedContacts : (contactsQuery.data ?? [])),
    [cachedContacts, contactsQuery.data, usingCache],
  );

  const personName = useMemo(() => {
    if (!detail || contactsForLabel.length === 0) return "—";
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

  const balanceLabel =
    detail.perspective === "Borrowed" ? t("personal.home.iOwe") : t("personal.home.owedToMe");
  const listBack =
    detail.perspective === "Borrowed" ? personalPageBackNav.utangOwe : personalPageBackNav.utangLent;
  // An Adjustment rewrites a balance against a version this device may no longer be showing.
  const adjustmentBlocked = !online && entryType === "Adjustment";

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-utang-detail">
      <PageHeader
        title={personName}
        description={balanceLabel}
        backTo={listBack.to}
        backLabel={t(listBack.labelKey)}
        backTestId="page-header-back-utang-detail"
      />
      {usingCache ? <OfflineNotice message={t("offline.personalCachedNotice")} /> : null}
      <div className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3">
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{balanceLabel}</p>
        <MoneyDisplay amount={currentBalance} className="text-[length:var(--exits-text-xl)]" />
        <DueChip dueDateUtc={detail.dueDateUtc} />
        <WaitingChip origin={relationshipIsLocal ? "Local" : "Server"} />
      </div>

      <form
        className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-2"
        onSubmit={(event) => {
          event.preventDefault();
          recordMutation.mutate();
        }}
      >
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.entryType")}
          <select
            data-testid="utang-entry-type"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={entryType}
            onChange={(e) => setEntryType(e.target.value as typeof entryType)}
          >
            <option value="Payment">{t("personal.utang.recordPayment")}</option>
            <option value="Loan">{t("personal.utang.addAmount")}</option>
            <option value="Adjustment">{t("personal.utang.adjustBalance")}</option>
          </select>
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.amount")}
          <input
            data-testid="utang-entry-amount"
            inputMode="decimal"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>
        {entryType === "Adjustment" ? (
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.utang.adjustmentDelta")}
            <input
              data-testid="utang-entry-delta"
              inputMode="decimal"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={adjustmentDelta}
              onChange={(e) => setAdjustmentDelta(e.target.value)}
              required
            />
          </label>
        ) : null}
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.note")}
          <input
            data-testid="utang-entry-notes"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={notes}
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
          disabled={recordMutation.isPending || adjustmentBlocked || (!online && !offline)}
          data-testid="utang-entry-submit"
        >
          {t("personal.utang.saveEntry")}
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
        className="catalog-form-section exits-animate-panel personal-section gap-2"
        aria-label={t("personal.utang.activity")}
      >
        <h2 className="catalog-form-section__title">{t("personal.utang.activity")}</h2>
        {history.length === 0 ? (
          <EmptyState
            title={t("personal.utang.historyEmptyTitle")}
            detail={t("personal.utang.historyEmptyDetail")}
          />
        ) : (
          <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="utang-history">
            {history.map((entry) => (
              <li key={entry.id}>
                <div className="exits-list__card flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="exits-list__name m-0 font-medium">{entry.entryType}</p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {new Date(entry.createdAtUtc).toLocaleString()}
                    {entry.notes ? ` · ${entry.notes}` : ""}
                  </p>
                  <WaitingChip origin={rowOrigin(entry)} />
                </div>
                <MoneyDisplay amount={entry.signedDelta} />
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/utang">{t("personal.utang.back")}</Link>
      </Button>
    </div>
  );
}
