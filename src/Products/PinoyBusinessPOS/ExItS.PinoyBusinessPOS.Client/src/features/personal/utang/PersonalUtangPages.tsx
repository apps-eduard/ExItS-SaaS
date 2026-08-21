import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
  type PersonalContactDto,
  type PersonalDebtRelationshipSummaryDto,
} from "@/api/platform/personal-utang-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

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

export function PersonalContactsPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [displayName, setDisplayName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
  });

  const createMutation = useMutation({
    mutationFn: () =>
      createPersonalContact({
        displayName: displayName.trim(),
        phone: phone.trim() || null,
        email: email.trim() || null,
      }),
    onSuccess: async () => {
      setDisplayName("");
      setPhone("");
      setEmail("");
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "utang", "contacts"] });
      await queryClient.invalidateQueries({ queryKey: ["personal", "dashboard"] });
    },
    onError: (error) => {
      setFormError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
    },
  });

  if (contactsQuery.isPending) return <LoadingSkeleton />;
  if (contactsQuery.isError) {
    return (
      <ErrorState
        title={t("personal.utang.loadErrorTitle")}
        detail={t("personal.utang.loadErrorDetail")}
      />
    );
  }

  const contacts = contactsQuery.data;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-utang-people">
      <PageHeader title={t("personal.utang.people")} description={t("personal.utang.peopleLede")} />

      <form
        className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
        onSubmit={(event) => {
          event.preventDefault();
          if (!displayName.trim()) {
            setFormError(t("personal.utang.nameRequired"));
            return;
          }
          createMutation.mutate();
        }}
      >
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.name")}
          <input
            data-testid="utang-contact-name"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            required
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.phone")}
          <input
            data-testid="utang-contact-phone"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={phone}
            onChange={(e) => setPhone(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.email")}
          <input
            data-testid="utang-contact-email"
            type="email"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </label>
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
          disabled={createMutation.isPending}
          data-testid="utang-contact-submit"
        >
          {t("personal.utang.addPerson")}
        </Button>
      </form>

      {contacts.length === 0 ? (
        <EmptyState
          title={t("personal.utang.peopleEmptyTitle")}
          detail={t("personal.utang.peopleEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {contacts.map((contact) => (
            <li
              key={contact.id}
              className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
              data-testid={`utang-contact-${contact.id}`}
            >
              <p className="m-0 font-semibold">{contact.displayName}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {[contact.phone, contact.email].filter(Boolean).join(" · ") ||
                  t("personal.utang.unlinkedContact")}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function RelationshipListPage({ mode }: { mode: "lent" | "owe" }) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [contactId, setContactId] = useState("");
  const [amount, setAmount] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [notes, setNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
  });
  const meQuery = useQuery({
    queryKey: ["personal", "me"],
    queryFn: ({ signal }) => getPersonalMe(signal),
  });
  const listQuery = useQuery({
    queryKey: ["personal", "utang", mode],
    queryFn: ({ signal }) =>
      mode === "lent" ? listLentRelationships(signal) : listBorrowedRelationships(signal),
  });

  const createMutation = useMutation({
    mutationFn: async () => {
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
      setFormError(
        error instanceof PlatformApiError ? error.message : t("personal.utang.genericError"),
      );
    },
  });

  if (listQuery.isPending || contactsQuery.isPending || meQuery.isPending) {
    return <LoadingSkeleton />;
  }
  if (listQuery.isError || contactsQuery.isError || meQuery.isError) {
    return (
      <ErrorState
        title={t("personal.utang.loadErrorTitle")}
        detail={t("personal.utang.loadErrorDetail")}
      />
    );
  }

  const title = mode === "lent" ? t("personal.utang.lent") : t("personal.utang.owe");
  const lede = mode === "lent" ? t("personal.utang.lentLede") : t("personal.utang.oweLede");
  const contacts = contactsQuery.data;
  const rows = listQuery.data;

  return (
    <div
      className="flex min-w-0 flex-col gap-4"
      data-testid={mode === "lent" ? "personal-utang-lent" : "personal-utang-owe"}
    >
      <PageHeader title={title} description={lede} />

      <form
        className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
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
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.person")}
          <select
            data-testid="utang-rel-contact"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
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
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.amount")}
          <input
            data-testid="utang-rel-amount"
            inputMode="decimal"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            required
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.dueDate")}
          <input
            data-testid="utang-rel-due"
            type="date"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.utang.note")}
          <input
            data-testid="utang-rel-notes"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
          />
        </label>
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
          disabled={createMutation.isPending || contacts.length === 0}
          data-testid="utang-rel-submit"
        >
          {mode === "lent" ? t("personal.utang.recordLent") : t("personal.utang.recordOwe")}
        </Button>
        {contacts.length === 0 ? (
          <Button asChild variant="ghost" className="min-h-11">
            <Link to="/personal/utang/people">{t("personal.utang.addPersonFirst")}</Link>
          </Button>
        ) : null}
      </form>

      {rows.length === 0 ? (
        <EmptyState
          title={t("personal.utang.listEmptyTitle")}
          detail={t("personal.utang.listEmptyDetail")}
        />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {rows.map((row) => (
            <li key={row.id}>
              <Link
                to={`/personal/utang/relationships/${row.id}`}
                className="flex min-h-11 items-center justify-between gap-3 rounded-[var(--exits-radius-md)] border border-border px-3 py-3 text-foreground no-underline"
                data-testid={`utang-rel-row-${row.id}`}
              >
                <div className="min-w-0">
                  <p className="m-0 truncate font-semibold">{contactLabel(contacts, row)}</p>
                  <DueChip dueDateUtc={row.dueDateUtc} />
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
  const [entryType, setEntryType] = useState<"Payment" | "Loan" | "Adjustment">("Payment");
  const [amount, setAmount] = useState("");
  const [adjustmentDelta, setAdjustmentDelta] = useState("");
  const [notes, setNotes] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
  });
  const detailQuery = useQuery({
    queryKey: ["personal", "utang", "relationship", relationshipId],
    enabled: Boolean(relationshipId),
    queryFn: ({ signal }) => getPersonalDebtRelationship(relationshipId, signal),
  });
  const balanceQuery = useQuery({
    queryKey: ["personal", "utang", "balance", relationshipId],
    enabled: Boolean(relationshipId),
    queryFn: ({ signal }) => getPersonalUtangBalance(relationshipId, signal),
  });
  const historyQuery = useQuery({
    queryKey: ["personal", "utang", "history", relationshipId],
    enabled: Boolean(relationshipId),
    queryFn: ({ signal }) => listPersonalUtangHistory(relationshipId, signal),
  });

  const recordMutation = useMutation({
    mutationFn: async () => {
      const version = balanceQuery.data?.version ?? detailQuery.data?.version;
      const amt = Number(amount);
      if (!(amt > 0)) throw new Error("amount");
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
      return recordPersonalUtangEntry(relationshipId, body);
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

  const personName = useMemo(() => {
    if (!detailQuery.data || !contactsQuery.data) return "—";
    return contactLabel(contactsQuery.data, detailQuery.data);
  }, [contactsQuery.data, detailQuery.data]);

  if (detailQuery.isPending || balanceQuery.isPending || historyQuery.isPending) {
    return <LoadingSkeleton />;
  }
  if (detailQuery.isError || balanceQuery.isError || historyQuery.isError) {
    return (
      <ErrorState
        title={t("personal.utang.loadErrorTitle")}
        detail={t("personal.utang.loadErrorDetail")}
      />
    );
  }

  const detail = detailQuery.data;
  const balance = balanceQuery.data;
  const history = historyQuery.data;
  const balanceLabel =
    detail.perspective === "Borrowed" ? t("personal.home.iOwe") : t("personal.home.owedToMe");

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-utang-detail">
      <PageHeader title={personName} description={balanceLabel} />
      <div className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3">
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{balanceLabel}</p>
        <MoneyDisplay
          amount={balance.currentBalance}
          className="text-[length:var(--exits-text-xl)]"
        />
        <DueChip dueDateUtc={detail.dueDateUtc} />
      </div>

      <form
        className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
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
          disabled={recordMutation.isPending}
          data-testid="utang-entry-submit"
        >
          {t("personal.utang.saveEntry")}
        </Button>
      </form>

      <section aria-label={t("personal.utang.activity")}>
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
          {t("personal.utang.activity")}
        </h2>
        {history.length === 0 ? (
          <EmptyState
            title={t("personal.utang.historyEmptyTitle")}
            detail={t("personal.utang.historyEmptyDetail")}
          />
        ) : (
          <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="utang-history">
            {history.map((entry) => (
              <li
                key={entry.id}
                className="flex items-start justify-between gap-3 rounded-[var(--exits-radius-md)] border border-border px-3 py-2"
              >
                <div className="min-w-0">
                  <p className="m-0 font-medium">{entry.entryType}</p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {new Date(entry.createdAtUtc).toLocaleString()}
                    {entry.notes ? ` · ${entry.notes}` : ""}
                  </p>
                </div>
                <MoneyDisplay amount={entry.signedDelta} />
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
