import { useQuery } from "@tanstack/react-query";
import { useEffect, useId, useMemo, useRef, useState } from "react";
import {
  Activity,
  Archive,
  Banknote,
  Check,
  Contact,
  HandCoins,
  History,
  Pencil,
  Scale,
  Wallet,
  type LucideIcon,
} from "lucide-react";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  getPersonalMe,
  isUtangConcurrencyConflict,
  isUtangSettlementStaleConflict,
  listPersonalUtangHistory,
  recordPersonalUtangEntry,
  settlePersonalDebtRelationship,
  type PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { ContactIdentityProfile } from "@/features/personal/ContactIdentityProfile";
import {
  ExitsStackedSheets,
  EXITS_STACKED_SHEETS_DURATION_MS,
  type ExitsStackedLowerPanel,
} from "@/components/exits/ExitsStackedSheets";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsCheckbox } from "@/components/exits/ExitsCheckbox";
import { ExitsOrgEhgButton } from "@/components/exits/ExitsOrgEhgButton";
import { ExitsOutlineInput, ExitsOutlineTextarea } from "@/components/exits/ExitsOutlineField";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { SoftGlassDateField } from "@/components/exits/SoftGlassDateField";
import { Button } from "@/components/ui/button";
import {
  useCreateUtangMutation,
  useInvalidatePersonalPeople,
  usePersonalUtangSummariesQuery,
  useUpdatePersonalContactMutation,
} from "@/features/personal/people-queries";
import type {
  PeopleClosedUtangSnapshot,
  PeopleRowModel,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { usePrefersReducedMotion } from "@/lib/motion";
import { createSecureMutationId } from "@/lib/secure-mutation-id";

const UTANG_NOTES_MAX_LENGTH = 512;

export type PersonalContactStackedAction =
  | "actions"
  | "edit"
  | "lent"
  | "owe"
  | "settle"
  | "details"
  | "activity"
  | "previousClosed"
  | "updateBalance";

type QaIdentity =
  | "edit"
  | "lent"
  | "owe"
  | "settle"
  | "details"
  | "activity"
  | "previous"
  | "balance";

function SavePenIcon({ className }: { className?: string }) {
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
      <path d="M13.33 13H8a1 1 0 0 0-1 1v7" />
      <path d="M14.363 17.634a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506l4.013-4.009a1 1 0 1 0-3.004-3.004z" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v.3" />
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
    >
      <path d="M12.5 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v4.35" />
      <path d="m16 19 2 2 4-4" />
      <path d="M17 15.13V14a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v7" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
    </svg>
  );
}

function canEditLocalContact(row: PeopleRowModel): boolean {
  return row.connectionStatus === "local";
}

export function personalContactShowSettle(row: PeopleRowModel): boolean {
  const utang = row.utang;
  return Boolean(utang?.relationshipId && utang.isLedgerOwner && (utang.balance ?? 0) > 0);
}

export function personalContactShowUpdateBalance(row: PeopleRowModel): boolean {
  const utang = row.utang;
  return Boolean(utang?.relationshipId && utang.isLedgerOwner);
}

export function personalContactCanViewActivity(row: PeopleRowModel): boolean {
  return Boolean(row.utang?.relationshipId);
}

export function personalContactShowPreviousClosed(row: PeopleRowModel): boolean {
  return row.previousClosed.length > 0;
}

function peopleActivityEntryTitle(
  entry: PersonalUtangEntryDto,
  isOpening: boolean,
  t: (key: MessageKey) => string,
): string {
  if (entry.isSettlement) {
    return t("personal.utang.settlementEntry");
  }
  if (entry.entryType === "Payment") {
    return t("personal.utang.recordPayment");
  }
  if (entry.entryType === "Adjustment") {
    return t("personal.utang.adjustBalance");
  }
  if (isOpening) {
    return t("personal.utang.startingBalance");
  }
  return t("personal.utang.addAmount");
}

function peopleActivityStatusLabel(entry: PersonalUtangEntryDto, t: (key: MessageKey) => string): string {
  const status = entry.status ?? "Confirmed";
  if (status === "Cancelled") return t("personal.utang.statusCancelled");
  if (status === "Disputed") return t("personal.utang.statusDisputed");
  if (status === "Pending") return t("personal.utang.statusPending");
  if (entry.wasAutoSynced) return t("personal.utang.statusAutoSynced");
  if (!entry.isSharedLedger) return t("personal.utang.statusPrivate");
  return t("personal.utang.statusConfirmed");
}

function formatPeopleActivityWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function ContactQaCard({
  label,
  ariaLabel,
  icon: Icon,
  identity,
  disabled,
  testId,
  onSelect,
}: {
  label: string;
  /** Longer accessible name when the visible QA label is abbreviated. */
  ariaLabel?: string;
  icon: LucideIcon;
  identity: QaIdentity;
  disabled?: boolean;
  testId: string;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      className={cn(
        "personal-contact-sheet__qa-card",
        `personal-contact-sheet__qa-card--${identity}`,
      )}
      disabled={disabled}
      aria-label={ariaLabel}
      data-testid={testId}
      onClick={onSelect}
    >
      <span className="personal-contact-sheet__qa-icon" aria-hidden>
        <Icon className="size-4 shrink-0" strokeWidth={2.1} />
      </span>
      <span className="personal-contact-sheet__qa-label">{label}</span>
    </button>
  );
}

function ContactActionsPanel({
  row,
  busy,
  onSelect,
}: {
  row: PeopleRowModel;
  busy: boolean;
  onSelect: (action: Exclude<PersonalContactStackedAction, "actions">) => void;
}) {
  const { t } = useI18n();
  const showSettle = personalContactShowSettle(row);
  const showUpdateBalance = personalContactShowUpdateBalance(row);
  const canViewActivity = personalContactCanViewActivity(row);
  const showPreviousClosed = personalContactShowPreviousClosed(row);

  function select(action: Exclude<PersonalContactStackedAction, "actions">) {
    if (busy) {
      return;
    }
    onSelect(action);
  }

  return (
    <div
      className="personal-contact-sheet__actions"
      data-testid={`personal-contact-sheet-actions-${row.contact.id}`}
    >
      <div
        className="personal-contact-sheet__qa-grid"
        role="group"
        aria-label={t("people.contactSheet.quickActions")}
      >
        <ContactQaCard
          label={t("people.action.edit")}
          icon={Pencil}
          identity="edit"
          testId={`personal-contact-sheet-qa-edit-${row.contact.id}`}
          onSelect={() => select("edit")}
        />
        <ContactQaCard
          label={t("people.detail.iLentAction")}
          icon={Wallet}
          identity="lent"
          testId={`personal-contact-sheet-qa-lent-${row.contact.id}`}
          onSelect={() => select("lent")}
        />
        <ContactQaCard
          label={t("people.detail.iOweAction")}
          icon={HandCoins}
          identity="owe"
          testId={`personal-contact-sheet-qa-owe-${row.contact.id}`}
          onSelect={() => select("owe")}
        />
        {showSettle ? (
          <ContactQaCard
            label={t("personal.utang.settle")}
            icon={Scale}
            identity="settle"
            testId={`personal-contact-sheet-qa-settle-${row.contact.id}`}
            onSelect={() => select("settle")}
          />
        ) : (
          <div className="personal-contact-sheet__qa-spacer" aria-hidden />
        )}
        <ContactQaCard
          label={t("people.contactSheet.qaDetails")}
          ariaLabel={t("people.contactSheet.qaDetailsAria")}
          icon={Contact}
          identity="details"
          testId={`personal-contact-sheet-qa-details-${row.contact.id}`}
          onSelect={() => select("details")}
        />
        <ContactQaCard
          label={t("personal.utang.activity")}
          icon={Activity}
          identity="activity"
          disabled={!canViewActivity}
          testId={`personal-contact-sheet-qa-activity-${row.contact.id}`}
          onSelect={() => select("activity")}
        />
        <ContactQaCard
          label={t("people.contactSheet.qaClose")}
          ariaLabel={t("people.contactSheet.qaCloseAria")}
          icon={Archive}
          identity="previous"
          disabled={!showPreviousClosed}
          testId={`personal-contact-sheet-qa-previous-${row.contact.id}`}
          onSelect={() => select("previousClosed")}
        />
        {showUpdateBalance ? (
          <ContactQaCard
            label={t("people.contactSheet.qaUpdate")}
            ariaLabel={t("people.contactSheet.qaUpdateAria")}
            icon={Banknote}
            identity="balance"
            testId={`personal-contact-sheet-qa-balance-${row.contact.id}`}
            onSelect={() => select("updateBalance")}
          />
        ) : (
          <div className="personal-contact-sheet__qa-spacer" aria-hidden />
        )}
      </div>
    </div>
  );
}

function ContactEditPanel({
  row,
  onBack,
  onSaved,
}: {
  row: PeopleRowModel;
  onBack: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const updateContact = useUpdatePersonalContactMutation();
  const editable = canEditLocalContact(row);
  const [name, setName] = useState(row.contact.displayName);
  const [phone, setPhone] = useState(row.contact.phone ?? "");
  const [email, setEmail] = useState(row.contact.email ?? "");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setName(row.contact.displayName);
    setPhone(row.contact.phone ?? "");
    setEmail(row.contact.email ?? "");
    setError(null);
  }, [row.contact.id, row.contact.displayName, row.contact.phone, row.contact.email]);

  function save() {
    if (!editable) {
      onBack();
      return;
    }
    if (!name.trim()) {
      setError(t("people.localAdd.nameRequired"));
      return;
    }
    setError(null);
    updateContact.mutate(
      {
        contactId: row.contact.id,
        body: {
          displayName: name.trim(),
          phone: phone.trim() || null,
          email: email.trim() || null,
          linkedUserIdentityId: row.contact.linkedUserIdentityId ?? null,
          publicUserId: row.publicUserId ?? row.contact.resolvedPublicUserId ?? null,
        },
      },
      {
        onSuccess: () => onSaved(),
        onError: (err) =>
          setError(err instanceof PlatformApiError ? err.message : t("people.loadError")),
      },
    );
  }

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-edit-${row.contact.id}`}
    >
      <h3 className="personal-contact-sheet__form-title">{t("people.edit.title")}</h3>
      {error ? (
        <Notice tone="danger" testId={`personal-contact-sheet-edit-error-${row.contact.id}`}>
          {error}
        </Notice>
      ) : null}
      {editable ? (
        <>
          <ExitsOutlineInput
            id={`personal-contact-sheet-edit-name-${row.contact.id}`}
            label={t("people.displayName")}
            value={name}
            onChange={setName}
            testId={`personal-contact-sheet-edit-name-${row.contact.id}`}
            required
            autoComplete="name"
          />
          <ExitsOutlineInput
            id={`personal-contact-sheet-edit-phone-${row.contact.id}`}
            label={t("people.column.contact")}
            value={phone}
            onChange={setPhone}
            testId={`personal-contact-sheet-edit-phone-${row.contact.id}`}
            autoComplete="tel"
          />
          <ExitsOutlineInput
            id={`personal-contact-sheet-edit-email-${row.contact.id}`}
            label={t("people.column.email")}
            value={email}
            onChange={setEmail}
            testId={`personal-contact-sheet-edit-email-${row.contact.id}`}
            type="email"
            autoComplete="email"
          />
        </>
      ) : (
        <dl className="personal-contact-sheet__facts m-0 grid gap-2">
          <div>
            <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
              {t("people.displayName")}
            </dt>
            <dd className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {row.contact.displayName}
            </dd>
          </div>
          <div>
            <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
              {t("people.column.contact")}
            </dt>
            <dd className="m-0 text-[length:var(--exits-text-sm)]">
              {row.contact.phone?.trim() || "—"}
            </dd>
          </div>
          <div>
            <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
              {t("people.column.email")}
            </dt>
            <dd className="m-0 text-[length:var(--exits-text-sm)]">
              {row.contact.email?.trim() || "—"}
            </dd>
          </div>
          {row.publicUserId || row.contact.resolvedPublicUserId ? (
            <div>
              <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
                {t("people.add.exitsId")}
              </dt>
              <dd className="m-0 text-[length:var(--exits-text-sm)]">
                {row.publicUserId ?? row.contact.resolvedPublicUserId}
              </dd>
            </div>
          ) : null}
        </dl>
      )}
      <div className="personal-contact-sheet__form-actions">
        <Button
          type="button"
          intent="danger"
          appearance="glossy"
          emphasis="strong"
          disabled={updateContact.isPending}
          data-testid={`personal-contact-sheet-edit-cancel-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </Button>
        {editable ? (
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            className="min-w-[8rem]"
            disabled={updateContact.isPending}
            data-testid={`personal-contact-sheet-edit-save-${row.contact.id}`}
            onClick={save}
          >
            <SavePenIcon className="size-4 shrink-0" />
            {updateContact.isPending ? t("loading.label") : t("people.edit.save")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}

function ContactDetailsPanel({
  row,
  onBack,
}: {
  row: PeopleRowModel;
  onBack: () => void;
}) {
  const { t } = useI18n();
  const title =
    row.connectionStatus === "local"
      ? t("people.detail.contactDetailLocal")
      : row.connectionStatus === "connected"
        ? t("people.detail.contactDetailConnected")
        : t("people.contactSheet.detailsTitle");

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-details-${row.contact.id}`}
    >
      <h3 className="personal-contact-sheet__form-title">{title}</h3>
      <dl className="personal-contact-sheet__facts m-0 grid gap-2">
        <div>
          <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {t("people.displayName")}
          </dt>
          <dd className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {row.contact.displayName}
          </dd>
        </div>
        <div>
          <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {t("people.column.contact")}
          </dt>
          <dd className="m-0 text-[length:var(--exits-text-sm)]">
            {row.contact.phone?.trim() || "—"}
          </dd>
        </div>
        <div>
          <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {t("people.column.email")}
          </dt>
          <dd className="m-0 text-[length:var(--exits-text-sm)]">
            {row.contact.email?.trim() || "—"}
          </dd>
        </div>
        {row.publicUserId || row.contact.resolvedPublicUserId ? (
          <div>
            <dt className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
              {t("people.add.exitsId")}
            </dt>
            <dd className="m-0 text-[length:var(--exits-text-sm)]">
              {row.publicUserId ?? row.contact.resolvedPublicUserId}
            </dd>
          </div>
        ) : null}
      </dl>
      <div className="personal-contact-sheet__form-actions">
        <Button
          type="button"
          intent="danger"
          appearance="glossy"
          emphasis="strong"
          data-testid={`personal-contact-sheet-details-back-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </Button>
      </div>
    </div>
  );
}

function ContactUtangPanel({
  row,
  mode,
  onBack,
  onSaved,
}: {
  row: PeopleRowModel;
  mode: "lent" | "borrowed";
  onBack: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const createUtang = useCreateUtangMutation();
  const invalidatePersonal = useInvalidatePersonalPeople();
  const utangQuery = usePersonalUtangSummariesQuery();
  const [amount, setAmount] = useState(() => formatMoneyAmountInput(0));
  const [notes, setNotes] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [shareWithCounterparty, setShareWithCounterparty] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isConnected =
    row.connectionStatus === "connected" && Boolean(row.contact.linkedUserIdentityId);

  useEffect(() => {
    setAmount(formatMoneyAmountInput(0));
    setNotes("");
    setDueDate("");
    setShareWithCounterparty(false);
    setError(null);
  }, [mode, row.contact.id]);

  async function submit() {
    setError(null);
    const parsed = parseMoneyAmountInput(amount);
    if (parsed === null || parsed <= 0) {
      setError(t("people.detail.amountInvalid"));
      return;
    }
    const purpose = notes.trim();
    if (!purpose) {
      setError(t("personal.utang.purposeRequired"));
      return;
    }
    const dueDateUtc = /^\d{4}-\d{2}-\d{2}$/.test(dueDate.trim())
      ? new Date(dueDate.trim()).toISOString()
      : null;

    const related = [...(utangQuery.data?.lent ?? []), ...(utangQuery.data?.borrowed ?? [])].filter(
      (rel) =>
        rel.creditorContactId === row.contact.id ||
        rel.debtorContactId === row.contact.id ||
        (row.contact.linkedUserIdentityId &&
          (rel.creditorUserIdentityId === row.contact.linkedUserIdentityId ||
            rel.debtorUserIdentityId === row.contact.linkedUserIdentityId)),
    );
    const expectedPerspective = mode === "lent" ? "lent" : "borrowed";
    const matchingOwnedRel = related.find(
      (rel) =>
        rel.status.toLowerCase() === "active" &&
        rel.isLedgerOwner !== false &&
        rel.perspective.toLowerCase() === expectedPerspective,
    );

    setSaving(true);
    try {
      if (matchingOwnedRel) {
        await recordPersonalUtangEntry(matchingOwnedRel.id, {
          entryType: "Loan",
          amount: parsed,
          notes: purpose,
          dueDateUtc,
          expectedVersion: matchingOwnedRel.version,
        });
        await invalidatePersonal();
      } else {
        const me = await getPersonalMe();
        const share = Boolean(isConnected && shareWithCounterparty);
        const relationship =
          mode === "lent"
            ? {
                creditorUserIdentityId: me.userIdentityId,
                creditorContactId: null,
                debtorUserIdentityId: null,
                debtorContactId: row.contact.id,
                currencyCode: "PHP",
                dueDateUtc,
                initialLoanAmount: parsed,
                initialLoanNotes: purpose,
                shareWithCounterparty: share,
              }
            : {
                creditorUserIdentityId: null,
                creditorContactId: row.contact.id,
                debtorUserIdentityId: me.userIdentityId,
                debtorContactId: null,
                currencyCode: "PHP",
                dueDateUtc,
                initialLoanAmount: parsed,
                initialLoanNotes: purpose,
                shareWithCounterparty: share,
              };
        const created = await createUtang.mutateAsync(relationship);
        if (created.shareOutcome === "PrivateNotReceiving") {
          setError(t("personal.utang.savedPrivatelyNotReceiving"));
        }
      }
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-utang-${row.contact.id}-${mode}`}
    >
      <h3 className="personal-contact-sheet__form-title">
        {mode === "lent" ? t("people.detail.iLentAction") : t("people.detail.iOweAction")}
      </h3>
      {error ? (
        <Notice tone="danger" testId={`personal-contact-sheet-utang-error-${row.contact.id}`}>
          {error}
        </Notice>
      ) : null}
      <ExitsOutlineInput
        id={`personal-contact-sheet-utang-amount-${row.contact.id}`}
        label={t("personal.utang.amount")}
        value={amount}
        onChange={(next) => setAmount(normalizeMoneyAmountTyping(next))}
        inputMode="decimal"
        testId={`personal-contact-sheet-utang-amount-${row.contact.id}`}
      />
      <SoftGlassDateField
        label={t("personal.utang.dueDate")}
        value={dueDate}
        onChange={setDueDate}
        clearable
        testId={`personal-contact-sheet-utang-due-${row.contact.id}`}
      />
      <ExitsOutlineTextarea
        id={`personal-contact-sheet-utang-notes-${row.contact.id}`}
        label={t("personal.utang.purpose")}
        value={notes}
        onChange={setNotes}
        testId={`personal-contact-sheet-utang-notes-${row.contact.id}`}
      />
      {isConnected ? (
        <ExitsCheckbox
          checked={shareWithCounterparty}
          onCheckedChange={setShareWithCounterparty}
          label={t("personal.utang.shareWithPerson").replace("{name}", row.contact.displayName)}
          testId={`personal-contact-sheet-utang-share-${row.contact.id}`}
        />
      ) : null}
      <div className="personal-contact-sheet__form-actions">
        <ExitsOrgEhgButton
          type="button"
          intent="danger"
          appearance="standard"
          size="md"
          disabled={saving || createUtang.isPending}
          testId={`personal-contact-sheet-utang-cancel-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </ExitsOrgEhgButton>
        <ExitsOrgEhgButton
          type="button"
          intent="primary"
          appearance="standard"
          size="md"
          className="min-w-[8rem]"
          disabled={saving || createUtang.isPending}
          testId={`personal-contact-sheet-utang-save-${row.contact.id}`}
          onClick={() => void submit()}
        >
          <SaveCheckIcon className="size-4 shrink-0" />
          {saving || createUtang.isPending
            ? t("loading.label")
            : mode === "lent"
              ? t("people.detail.saveILent")
              : t("people.detail.saveIBorrowed")}
        </ExitsOrgEhgButton>
      </div>
    </div>
  );
}

function ContactSettlePanel({
  row,
  onBack,
  onSaved,
}: {
  row: PeopleRowModel;
  onBack: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const invalidatePersonal = useInvalidatePersonalPeople();
  const utangQuery = usePersonalUtangSummariesQuery();
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const settlementEntryIdRef = useRef<string | null>(null);

  const relationshipId = row.utang?.relationshipId ?? null;
  const currentBalance = row.utang?.balance ?? 0;
  const related = [...(utangQuery.data?.lent ?? []), ...(utangQuery.data?.borrowed ?? [])].find(
    (rel) => rel.id === relationshipId,
  );
  const version = related?.version ?? null;
  const shared = Boolean(related?.isSharedLedger);
  const online = typeof navigator === "undefined" ? true : navigator.onLine;

  useEffect(() => {
    setNote("");
    setError(null);
    settlementEntryIdRef.current = null;
  }, [relationshipId]);

  async function submit() {
    if (!relationshipId) {
      setError(t("error.body"));
      return;
    }
    if (!online) {
      setError(t("personal.utang.settleRequiresOnline"));
      return;
    }
    const notes = note.trim();
    if (!notes) {
      setError(t("personal.utang.settleNoteRequired"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      if (!settlementEntryIdRef.current) {
        const generated = createSecureMutationId();
        if (!generated.ok) {
          throw new Error("id-unavailable");
        }
        settlementEntryIdRef.current = generated.id;
      }
      await settlePersonalDebtRelationship(relationshipId, {
        expectedVersion: version,
        settlementEntryId: settlementEntryIdRef.current,
        notes,
      });
      settlementEntryIdRef.current = null;
      await invalidatePersonal();
      onSaved();
    } catch (err) {
      if (isUtangSettlementStaleConflict(err)) {
        setError(t("personal.utang.settleStaleConflict"));
        void utangQuery.refetch();
      } else if (isUtangConcurrencyConflict(err)) {
        setError(t("personal.utang.concurrencyConflict"));
        void utangQuery.refetch();
      } else if (
        err instanceof PlatformApiError &&
        (err.errorCode ?? "").toLowerCase().includes("settlement.pending")
      ) {
        setError(t("personal.utang.settlePendingBlocked"));
      } else {
        setError(err instanceof Error ? err.message : t("error.body"));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-settle-${row.contact.id}`}
    >
      <h3 className="personal-contact-sheet__form-title">{t("personal.utang.settleTitle")}</h3>
      <div className="people-mobile-sheet__settle-summary">
        <span className="people-mobile-sheet__settle-summary-item">
          <span className="text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {t("personal.utang.settleAmount")}
          </span>
          <MoneyDisplay
            amount={currentBalance}
            className="text-[length:var(--exits-text-lg)] font-bold"
            testId={`personal-contact-sheet-settle-amount-${row.contact.id}`}
          />
        </span>
        <span className="people-mobile-sheet__settle-arrow" aria-hidden>
          →
        </span>
        <span className="people-mobile-sheet__settle-summary-item">
          <span className="text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {t("personal.utang.settleAfter")}
          </span>
          <MoneyDisplay
            amount={0}
            className="text-[length:var(--exits-text-lg)] font-bold"
            testId={`personal-contact-sheet-settle-after-${row.contact.id}`}
          />
        </span>
      </div>
      <p className="people-mobile-sheet__settle-hint">
        {shared ? t("personal.utang.settleSharedHint") : t("personal.utang.settlePrivateHint")}
      </p>
      {error ? (
        <Notice tone="danger" testId={`personal-contact-sheet-settle-error-${row.contact.id}`}>
          {error}
        </Notice>
      ) : null}
      <ExitsOutlineInput
        id={`personal-contact-sheet-settle-note-${row.contact.id}`}
        label={t("personal.utang.settleNote")}
        value={note}
        onChange={setNote}
        maxLength={UTANG_NOTES_MAX_LENGTH}
        testId={`personal-contact-sheet-settle-note-${row.contact.id}`}
      />
      <div className="personal-contact-sheet__form-actions">
        <Button
          type="button"
          intent="danger"
          appearance="glossy"
          emphasis="strong"
          disabled={saving}
          data-testid={`personal-contact-sheet-settle-cancel-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </Button>
        <Button
          type="button"
          intent="primary"
          appearance="glossy"
          className="min-w-[8rem]"
          disabled={saving || !online}
          data-testid={`personal-contact-sheet-settle-confirm-${row.contact.id}`}
          onClick={() => void submit()}
        >
          <Check className="size-4 shrink-0" aria-hidden />
          {saving ? t("loading.label") : t("personal.utang.settleConfirm")}
        </Button>
      </div>
    </div>
  );
}

function ContactUpdateBalancePanel({
  row,
  onBack,
  onSaved,
}: {
  row: PeopleRowModel;
  onBack: () => void;
  onSaved: () => void;
}) {
  const { t } = useI18n();
  const invalidatePersonal = useInvalidatePersonalPeople();
  const utangQuery = usePersonalUtangSummariesQuery();
  const [entryType, setEntryType] = useState<"Payment" | "Loan">("Payment");
  const [amount, setAmount] = useState("");
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const relationshipId = row.utang?.relationshipId ?? null;
  const currentBalance = row.utang?.balance ?? 0;
  const related = [...(utangQuery.data?.lent ?? []), ...(utangQuery.data?.borrowed ?? [])].find(
    (rel) => rel.id === relationshipId,
  );
  const version = related?.version ?? null;
  const projected =
    entryType === "Payment"
      ? Math.max(0, currentBalance - (parseMoneyAmountInput(amount) ?? 0))
      : currentBalance + (parseMoneyAmountInput(amount) ?? 0);

  useEffect(() => {
    setEntryType("Payment");
    setAmount("");
    setNotes("");
    setError(null);
  }, [relationshipId]);

  async function submit() {
    if (!relationshipId) {
      setError(t("error.body"));
      return;
    }
    setError(null);
    const parsed = parseMoneyAmountInput(amount);
    if (parsed === null || parsed <= 0) {
      setError(t("personal.utang.amountRequired"));
      return;
    }
    if (entryType === "Payment" && parsed > currentBalance) {
      setError(t("personal.utang.amountExceedsBalance"));
      return;
    }
    const purpose = notes.trim();
    if (!purpose) {
      setError(t("personal.utang.purposeRequired"));
      return;
    }
    setSaving(true);
    try {
      await recordPersonalUtangEntry(relationshipId, {
        entryType,
        amount: parsed,
        notes: purpose,
        expectedVersion: version,
      });
      await invalidatePersonal();
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-balance-${row.contact.id}`}
    >
      <h3 className="personal-contact-sheet__form-title">{t("personal.utang.qaUpdateBalance")}</h3>
      <div className="personal-contact-sheet__balance-card">
        <span className="text-[length:var(--exits-text-xs)] font-semibold text-muted">
          {row.utang?.direction === "owe"
            ? t("personal.utang.youOwe")
            : t("personal.utang.youLent")}
        </span>
        <MoneyDisplay
          amount={projected}
          className="text-[length:var(--exits-text-lg)] font-bold"
          testId={`personal-contact-sheet-balance-projected-${row.contact.id}`}
        />
        <span className="text-[length:var(--exits-text-xs)] text-muted">
          {row.contact.displayName}
        </span>
      </div>
      {error ? (
        <Notice tone="danger" testId={`personal-contact-sheet-balance-error-${row.contact.id}`}>
          {error}
        </Notice>
      ) : null}
      <div className="personal-contact-sheet__entry-types">
        <Button
          type="button"
          intent={entryType === "Payment" ? "primary" : "neutral"}
          appearance={entryType === "Payment" ? "glossy" : "outline"}
          size="sm"
          data-testid={`personal-contact-sheet-balance-type-payment-${row.contact.id}`}
          onClick={() => setEntryType("Payment")}
        >
          {t("personal.utang.recordPayment")}
        </Button>
        <Button
          type="button"
          intent={entryType === "Loan" ? "primary" : "neutral"}
          appearance={entryType === "Loan" ? "glossy" : "outline"}
          size="sm"
          data-testid={`personal-contact-sheet-balance-type-loan-${row.contact.id}`}
          onClick={() => setEntryType("Loan")}
        >
          {t("personal.utang.addAmount")}
        </Button>
      </div>
      <ExitsOutlineInput
        id={`personal-contact-sheet-balance-amount-${row.contact.id}`}
        label={t("personal.utang.amount")}
        value={amount}
        onChange={(next) => setAmount(normalizeMoneyAmountTyping(next))}
        inputMode="decimal"
        testId={`personal-contact-sheet-balance-amount-${row.contact.id}`}
      />
      <ExitsOutlineTextarea
        id={`personal-contact-sheet-balance-notes-${row.contact.id}`}
        label={t("personal.utang.purpose")}
        value={notes}
        onChange={setNotes}
        testId={`personal-contact-sheet-balance-notes-${row.contact.id}`}
      />
      <div className="personal-contact-sheet__form-actions">
        <Button
          type="button"
          intent="danger"
          appearance="glossy"
          emphasis="strong"
          disabled={saving}
          data-testid={`personal-contact-sheet-balance-cancel-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </Button>
        <Button
          type="button"
          intent="primary"
          appearance="glossy"
          className="min-w-[8rem]"
          disabled={saving}
          data-testid={`personal-contact-sheet-balance-save-${row.contact.id}`}
          onClick={() => void submit()}
        >
          <SaveCheckIcon className="size-4 shrink-0" />
          {saving ? t("loading.label") : t("people.edit.save")}
        </Button>
      </div>
    </div>
  );
}

function ContactPreviousClosedPanel({
  row,
  onBack,
  onViewActivity,
}: {
  row: PeopleRowModel;
  onBack: () => void;
  onViewActivity: (closed: PeopleClosedUtangSnapshot) => void;
}) {
  const { t } = useI18n();
  const items = row.previousClosed;

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-previous-${row.contact.id}`}
    >
      <h3 className="personal-contact-sheet__form-title">{t("people.previousClosed.title")}</h3>
      <p className="personal-contact-sheet__subtitle">{row.contact.displayName}</p>
      {items.length === 0 ? (
        <EmptyState
          align="center"
          icon={<Archive className="size-5" strokeWidth={1.75} />}
          title={t("people.previousClosed.emptyTitle")}
          detail={t("people.previousClosed.emptyDetail")}
          testId={`personal-contact-sheet-previous-empty-${row.contact.id}`}
        />
      ) : (
        <ul
          className="personal-contact-sheet__list"
          data-testid={`personal-contact-sheet-previous-list-${row.contact.id}`}
        >
          {items.map((item) => (
            <li
              key={item.relationshipId}
              className="personal-contact-sheet__list-item"
              data-testid={`personal-contact-sheet-previous-item-${item.relationshipId}`}
            >
              <div className="personal-contact-sheet__list-head">
                <span
                  className={cn(
                    "personal-contact-sheet__list-title truncate",
                    item.direction === "lent"
                      ? "people-mobile-row__direction--lent"
                      : "people-mobile-row__direction--owe",
                  )}
                >
                  {item.direction === "lent"
                    ? t("people.previousClosed.lent")
                    : t("people.previousClosed.owe")}
                </span>
              </div>
              <div className="personal-contact-sheet__list-meta">
                <span>{formatPeopleActivityWhen(item.updatedAtUtc)}</span>
                <span>{t("personal.utang.statusClosed")}</span>
              </div>
              <Button
                type="button"
                intent="neutral"
                appearance="outline"
                size="sm"
                className="self-start"
                data-testid={`personal-contact-sheet-previous-activity-${item.relationshipId}`}
                onClick={() => onViewActivity(item)}
              >
                <Activity className="size-3.5 shrink-0" aria-hidden />
                {t("people.previousClosed.viewActivity")}
              </Button>
            </li>
          ))}
        </ul>
      )}
      <div className="personal-contact-sheet__form-actions">
        <Button
          type="button"
          intent="danger"
          appearance="glossy"
          emphasis="strong"
          data-testid={`personal-contact-sheet-previous-back-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </Button>
      </div>
    </div>
  );
}

function ContactActivityPanel({
  row,
  relationshipId: relationshipIdProp,
  onBack,
}: {
  row: PeopleRowModel;
  relationshipId?: string | null;
  onBack: () => void;
}) {
  const { t } = useI18n();
  const relationshipId = relationshipIdProp ?? row.utang?.relationshipId ?? null;
  const [search, setSearch] = useState("");

  const historyQuery = useQuery({
    queryKey: ["personal", "utang", "history", relationshipId ?? ""],
    enabled: Boolean(relationshipId),
    queryFn: ({ signal }) => listPersonalUtangHistory(relationshipId!, signal),
  });

  useEffect(() => {
    setSearch("");
  }, [relationshipId]);

  const history = historyQuery.data ?? [];
  const openingLoanEntryId = useMemo(() => {
    let oldest: { id: string; createdAtUtc: string } | null = null;
    for (const entry of history) {
      if (entry.entryType !== "Loan" || entry.isSettlement) {
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

  const needle = search.trim().toLowerCase();
  const rows = useMemo(() => {
    const mapped = history.map((entry) => {
      const isOpening = entry.id === openingLoanEntryId;
      const title = peopleActivityEntryTitle(entry, isOpening, t);
      const statusText = peopleActivityStatusLabel(entry, t);
      const showStatus = statusText !== t("personal.utang.statusPrivate");
      return { entry, title, statusText, showStatus, isOpening };
    });
    if (!needle) {
      return mapped;
    }
    return mapped.filter((mappedRow) =>
      [mappedRow.title, mappedRow.statusText, mappedRow.entry.notes ?? "", mappedRow.entry.createdAtUtc]
        .join(" ")
        .toLowerCase()
        .includes(needle),
    );
  }, [history, needle, openingLoanEntryId, t]);

  return (
    <div
      className="personal-contact-sheet__form-card"
      data-testid={`personal-contact-sheet-activity-${row.contact.id}`}
    >
      <h3 className="personal-contact-sheet__form-title">{t("personal.utang.activity")}</h3>
      <p className="personal-contact-sheet__subtitle">
        {row.contact.displayName}
        {row.utang ? (
          <>
            {" · "}
            <MoneyDisplay amount={row.utang.balance} />
          </>
        ) : null}
      </p>
      {history.length > 0 ? (
        <ExitsOutlineInput
          id={`personal-contact-sheet-activity-search-${row.contact.id}`}
          label={t("personal.utang.activitySearchLabel")}
          value={search}
          onChange={setSearch}
          placeholder={t("personal.utang.activitySearchPlaceholder")}
          placeholderOnly
          clearable
          testId={`personal-contact-sheet-activity-search-${row.contact.id}`}
        />
      ) : null}
      {historyQuery.isPending ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
      ) : historyQuery.isError ? (
        <Notice tone="danger" testId={`personal-contact-sheet-activity-error-${row.contact.id}`}>
          {historyQuery.error instanceof Error
            ? historyQuery.error.message
            : t("error.body")}
        </Notice>
      ) : history.length === 0 ? (
        <EmptyState
          align="center"
          icon={<History className="size-5" strokeWidth={1.75} />}
          title={t("personal.utang.historyEmptyTitle")}
          detail={t("personal.utang.historyEmptyDetail")}
          testId={`personal-contact-sheet-activity-empty-${row.contact.id}`}
        />
      ) : rows.length === 0 ? (
        <EmptyState
          variant="filtered"
          align="center"
          icon={<History className="size-5" strokeWidth={1.75} />}
          title={t("personal.utang.activityFilterEmptyTitle")}
          detail={t("personal.utang.activityFilterEmptyDetail")}
          testId={`personal-contact-sheet-activity-filtered-empty-${row.contact.id}`}
        />
      ) : (
        <ul
          className="personal-contact-sheet__list"
          data-testid={`personal-contact-sheet-activity-list-${row.contact.id}`}
        >
          {rows.map(({ entry, title, statusText, showStatus }) => (
            <li
              key={entry.id}
              className="personal-contact-sheet__list-item"
              data-testid={`personal-contact-sheet-activity-entry-${entry.id}`}
            >
              <div className="personal-contact-sheet__list-head">
                <span className="personal-contact-sheet__list-title truncate">{title}</span>
                <MoneyDisplay
                  amount={entry.signedDelta}
                  className="shrink-0 font-semibold"
                />
              </div>
              <div className="personal-contact-sheet__list-meta">
                <span>{formatPeopleActivityWhen(entry.createdAtUtc)}</span>
                {showStatus ? <span>{statusText}</span> : null}
              </div>
              {entry.notes?.trim() ? (
                <p className="personal-contact-sheet__list-notes">{entry.notes.trim()}</p>
              ) : null}
              {entry.affectsBalance !== false ? (
                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("personal.utang.balanceAfter")}: <MoneyDisplay amount={entry.balanceAfter} />
                </p>
              ) : null}
            </li>
          ))}
        </ul>
      )}
      <div className="personal-contact-sheet__form-actions">
        <Button
          type="button"
          intent="danger"
          appearance="glossy"
          emphasis="strong"
          data-testid={`personal-contact-sheet-activity-back-${row.contact.id}`}
          onClick={onBack}
        >
          {t("people.add.cancel")}
        </Button>
      </div>
    </div>
  );
}

export function PersonalContactStackedSheet({
  open,
  row,
  statusLabel,
  onOpenChange,
}: {
  open: boolean;
  row: PeopleRowModel | null;
  /** Connection status chip label for non-local / non-shared contacts. */
  statusLabel: string;
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = useI18n();
  const titleId = useId();
  const statusId = useId();
  const reducedMotion = usePrefersReducedMotion();
  const [action, setAction] = useState<PersonalContactStackedAction>("actions");
  const [activityRelationshipId, setActivityRelationshipId] = useState<string | null>(null);
  const swapBusyRef = useRef(false);
  const actionRef = useRef<PersonalContactStackedAction>("actions");
  const swapTimerRef = useRef<number | null>(null);
  const contactId = row?.contact.id;
  actionRef.current = action;

  useEffect(() => {
    if (!open) {
      setAction("actions");
      actionRef.current = "actions";
      setActivityRelationshipId(null);
      swapBusyRef.current = false;
      if (swapTimerRef.current != null) {
        window.clearTimeout(swapTimerRef.current);
        swapTimerRef.current = null;
      }
      return;
    }
    setAction("actions");
    actionRef.current = "actions";
    setActivityRelationshipId(null);
  }, [open, contactId]);

  function markSwapBusy() {
    swapBusyRef.current = true;
    const ms = reducedMotion ? 0 : EXITS_STACKED_SHEETS_DURATION_MS * 2;
    if (swapTimerRef.current != null) {
      window.clearTimeout(swapTimerRef.current);
    }
    swapTimerRef.current = window.setTimeout(() => {
      swapTimerRef.current = null;
      swapBusyRef.current = false;
    }, ms || 1);
  }

  function requestAction(next: PersonalContactStackedAction, activityRelId?: string | null) {
    if (!row || next === actionRef.current) {
      return;
    }
    // Block only forward QA picks while a swap is in flight (not Cancel / Escape back).
    if (next !== "actions" && swapBusyRef.current) {
      return;
    }
    if (next === "settle" && !personalContactShowSettle(row)) {
      return;
    }
    if (next === "updateBalance" && !personalContactShowUpdateBalance(row)) {
      return;
    }
    if (next === "previousClosed" && !personalContactShowPreviousClosed(row)) {
      return;
    }
    if (next === "activity") {
      const relId = activityRelId ?? row.utang?.relationshipId ?? null;
      if (!relId) {
        return;
      }
      setActivityRelationshipId(relId);
    } else if (next === "actions") {
      setActivityRelationshipId(null);
    }
    markSwapBusy();
    actionRef.current = next;
    setAction(next);
  }

  function goBackToActions() {
    requestAction("actions");
  }

  function handleEscape() {
    if (actionRef.current !== "actions") {
      goBackToActions();
      return;
    }
    if (swapBusyRef.current) {
      return;
    }
    onOpenChange(false);
  }

  function handleDismiss() {
    onOpenChange(false);
  }

  const lower: ExitsStackedLowerPanel = useMemo(() => {
    if (!row) {
      return { id: "actions", content: null, surface: "tinted" };
    }
    if (action === "edit") {
      return {
        id: "edit",
        surface: "solid",
        content: (
          <ContactEditPanel
            row={row}
            onBack={goBackToActions}
            onSaved={() => {
              goBackToActions();
            }}
          />
        ),
      };
    }
    if (action === "details") {
      return {
        id: "details",
        surface: "solid",
        content: <ContactDetailsPanel row={row} onBack={goBackToActions} />,
      };
    }
    if (action === "lent" || action === "owe") {
      return {
        id: action,
        surface: "solid",
        content: (
          <ContactUtangPanel
            row={row}
            mode={action === "lent" ? "lent" : "borrowed"}
            onBack={goBackToActions}
            onSaved={() => onOpenChange(false)}
          />
        ),
      };
    }
    if (action === "settle") {
      return {
        id: "settle",
        surface: "solid",
        content: (
          <ContactSettlePanel
            row={row}
            onBack={goBackToActions}
            onSaved={() => onOpenChange(false)}
          />
        ),
      };
    }
    if (action === "updateBalance") {
      return {
        id: "updateBalance",
        surface: "solid",
        content: (
          <ContactUpdateBalancePanel
            row={row}
            onBack={goBackToActions}
            onSaved={() => onOpenChange(false)}
          />
        ),
      };
    }
    if (action === "previousClosed") {
      return {
        id: "previousClosed",
        surface: "solid",
        content: (
          <ContactPreviousClosedPanel
            row={row}
            onBack={goBackToActions}
            onViewActivity={(closed) => {
              requestAction("activity", closed.relationshipId);
            }}
          />
        ),
      };
    }
    if (action === "activity") {
      return {
        id: "activity",
        surface: "solid",
        content: (
          <ContactActivityPanel
            row={row}
            relationshipId={activityRelationshipId}
            onBack={goBackToActions}
          />
        ),
      };
    }
    return {
      id: "actions",
      surface: "tinted",
      content: (
        <ContactActionsPanel
          row={row}
          busy={false}
          onSelect={(next) => requestAction(next)}
        />
      ),
    };
    // Panel content is keyed by action; handlers close over latest requestAction via render.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- panel identity from action + row
  }, [action, activityRelationshipId, row, t]);

  if (!row) {
    return null;
  }

  return (
    <ExitsStackedSheets
      open={open}
      onClose={handleDismiss}
      onEscape={handleEscape}
      testId={`personal-contact-sheet-${row.contact.id}`}
      closeLabel={t("people.contactSheet.close")}
      labelledBy={titleId}
      describedBy={statusId}
      forceLight
      className="personal-contact-sheet"
      upper={
        <div className="personal-contact-sheet__identity">
          <PersonAvatar
            name={row.contact.displayName}
            size="lg"
            className="personal-contact-sheet__avatar"
          />
          <h2
            id={titleId}
            className="personal-contact-sheet__name"
            data-testid={`personal-contact-sheet-name-${row.contact.id}`}
          >
            {row.contact.displayName}
          </h2>
          <ContactIdentityProfile
            row={row}
            statusFallback={statusLabel}
            statusId={statusId}
            testId={
              row.utang
                ? `personal-contact-sheet-identity-summary-${row.contact.id}`
                : `personal-contact-sheet-status-${row.contact.id}`
            }
          />
        </div>
      }
      lower={lower}
    />
  );
}
