import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  Archive,
  Check,
  ChevronDown,
  Contact,
  EllipsisVertical,
  HandCoins,
  History,
  Pencil,
  Scale,
  Wallet,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  formatDueLabel,
  getPersonalMe,
  isUtangConcurrencyConflict,
  isUtangSettlementStaleConflict,
  listPersonalUtangHistory,
  recordPersonalUtangEntry,
  settlePersonalDebtRelationship,
  type PersonalUtangEntryDto,
} from "@/api/platform/personal-utang-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsTooltip } from "@/components/exits/ExitsTooltip";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { SoftGlassDateField } from "@/components/exits/SoftGlassDateField";
import { ExitsOrgEhgButton } from "@/components/exits/ExitsOrgEhgButton";
import { ExitsOutlineInput, ExitsOutlineTextarea } from "@/components/exits/ExitsOutlineField";
import { ExitsCheckbox } from "@/components/exits/ExitsCheckbox";
import { Notice } from "@/components/exits/Notice";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  MenuItem,
  MenuSeparator,
  useDismissibleOpen,
} from "@/components/ui/dropdown-menu";
import {
  useCreateUtangMutation,
  useInvalidatePersonalPeople,
  usePersonalUtangSummariesQuery,
  useUpdatePersonalContactMutation,
} from "@/features/personal/people-queries";
import {
  type PeopleClosedUtangSnapshot,
  type PeopleRowModel,
} from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { createSecureMutationId } from "@/lib/secure-mutation-id";

const UTANG_NOTES_MAX_LENGTH = 512;
const ACTIVITY_FULL_SHEET_AT = 6;

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

function PeopleContactDetailsSheet({
  open,
  row,
  onClose,
}: {
  open: boolean;
  row: PeopleRowModel;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const updateContact = useUpdatePersonalContactMutation();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(row.contact.displayName);
  const [phone, setPhone] = useState(row.contact.phone ?? "");
  const [email, setEmail] = useState(row.contact.email ?? "");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }
    setEditing(false);
    setName(row.contact.displayName);
    setPhone(row.contact.phone ?? "");
    setEmail(row.contact.email ?? "");
    setError(null);
  }, [open, row.contact.id, row.contact.displayName, row.contact.email, row.contact.phone]);

  function cancelEdit() {
    setName(row.contact.displayName);
    setPhone(row.contact.phone ?? "");
    setEmail(row.contact.email ?? "");
    setError(null);
    setEditing(false);
  }

  function saveEdit() {
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
        onSuccess: () => setEditing(false),
        onError: (err) =>
          setError(err instanceof PlatformApiError ? err.message : t("people.loadError")),
      },
    );
  }

  const sheetTitle =
    row.connectionStatus === "local"
      ? t("people.detail.contactDetailLocal")
      : row.connectionStatus === "connected"
        ? t("people.detail.contactDetailConnected")
        : row.connectionStatus === "request_sent"
          ? t("people.detail.contactDetailRequestSent")
          : row.connectionStatus === "request_received"
            ? t("people.detail.contactDetailRequestReceived")
            : row.connectionStatus === "blocked"
              ? t("people.detail.contactDetailBlocked")
              : row.connectionStatus === "not_connected"
                ? t("people.detail.contactDetailNotConnected")
                : t("people.detail.contactDetail");

  return (
    <BottomSheet
      open={open}
      onClose={() => {
        if (updateContact.isPending) {
          return;
        }
        onClose();
      }}
      title={sheetTitle}
      panelId={`people-contact-sheet-${row.contact.id}`}
      testId={`people-contact-sheet-${row.contact.id}`}
      closeLabel={t("people.add.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      panelClassName="people-mobile-sheet"
    >
      <div className="people-mobile-sheet__body flex min-w-0 flex-col gap-3">
        {error ? (
          <Notice tone="danger" testId={`people-contact-sheet-error-${row.contact.id}`}>
            {error}
          </Notice>
        ) : null}
        {editing ? (
          <>
            <ExitsOutlineInput
              id={`people-contact-edit-name-${row.contact.id}`}
              label={t("people.displayName")}
              value={name}
              onChange={setName}
              testId={`people-contact-edit-name-${row.contact.id}`}
              required
            />
            <ExitsOutlineInput
              id={`people-contact-edit-phone-${row.contact.id}`}
              label={t("people.localAdd.phone")}
              value={phone}
              onChange={setPhone}
              testId={`people-contact-edit-phone-${row.contact.id}`}
            />
            <ExitsOutlineInput
              id={`people-contact-edit-email-${row.contact.id}`}
              label={t("people.column.email")}
              value={email}
              onChange={setEmail}
              testId={`people-contact-edit-email-${row.contact.id}`}
            />
            {row.publicUserId || row.contact.resolvedPublicUserId ? (
              <div className="people-mobile-sheet__readonly-fact">
                <span className="text-[length:var(--exits-text-xs)] font-semibold text-muted">
                  ExItS ID
                </span>
                <span className="text-[length:var(--exits-text-sm)]">
                  {row.publicUserId ?? row.contact.resolvedPublicUserId}
                </span>
              </div>
            ) : null}
            <div className="people-mobile-sheet__actions flex flex-wrap gap-2">
              <Button
                type="button"
                intent="danger"
                appearance="glossy"
                emphasis="strong"
                disabled={updateContact.isPending}
                data-testid={`people-contact-edit-cancel-${row.contact.id}`}
                onClick={cancelEdit}
              >
                {t("people.add.cancel")}
              </Button>
              <Button
                type="button"
                intent="primary"
                appearance="glossy"
                className="min-w-[8rem]"
                disabled={updateContact.isPending}
                data-testid={`people-contact-edit-save-${row.contact.id}`}
                onClick={saveEdit}
              >
                <SavePenIcon className="size-4 shrink-0" />
                {updateContact.isPending ? t("loading.label") : t("people.edit.save")}
              </Button>
            </div>
          </>
        ) : (
          <>
            <dl className="people-mobile-sheet__facts m-0 grid gap-2">
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
                    ExItS ID
                  </dt>
                  <dd className="m-0 text-[length:var(--exits-text-sm)]">
                    {row.publicUserId ?? row.contact.resolvedPublicUserId}
                  </dd>
                </div>
              ) : null}
            </dl>
            <div className="people-mobile-sheet__actions flex flex-wrap gap-2">
              <Button
                type="button"
                intent="primary"
                appearance="glossy"
                data-testid={`people-contact-edit-${row.contact.id}`}
                onClick={() => {
                  setName(row.contact.displayName);
                  setPhone(row.contact.phone ?? "");
                  setEmail(row.contact.email ?? "");
                  setError(null);
                  setEditing(true);
                }}
              >
                <Pencil className="size-4 shrink-0" aria-hidden />
                {t("people.action.edit")}
              </Button>
            </div>
          </>
        )}
      </div>
    </BottomSheet>
  );
}

function PeopleUpdateBalanceSheet({
  open,
  row,
  onClose,
}: {
  open: boolean;
  row: PeopleRowModel;
  onClose: () => void;
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
    if (!open) {
      return;
    }
    setEntryType("Payment");
    setAmount("");
    setNotes("");
    setError(null);
  }, [open, relationshipId]);

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
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={t("personal.utang.qaUpdateBalance")}
      panelId={`people-update-balance-sheet-${row.contact.id}`}
      testId={`people-update-balance-sheet-${row.contact.id}`}
      closeLabel={t("people.add.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      panelClassName="people-mobile-sheet"
    >
      <div className="people-mobile-sheet__body flex min-w-0 flex-col gap-3">
        <div className="people-mobile-sheet__balance-card">
          <span className="text-[length:var(--exits-text-xs)] font-semibold text-muted">
            {row.utang?.direction === "owe"
              ? t("personal.utang.youOwe")
              : t("personal.utang.youLent")}
          </span>
          <MoneyDisplay
            amount={projected}
            className="text-[length:var(--exits-text-lg)] font-bold"
            testId={`people-update-balance-projected-${row.contact.id}`}
          />
          <span className="text-[length:var(--exits-text-xs)] text-muted">
            {row.contact.displayName}
          </span>
        </div>
        {error ? (
          <Notice tone="danger" testId={`people-update-balance-error-${row.contact.id}`}>
            {error}
          </Notice>
        ) : null}
        <div className="people-mobile-sheet__entry-types flex flex-wrap gap-2">
          <Button
            type="button"
            intent={entryType === "Payment" ? "primary" : "neutral"}
            appearance={entryType === "Payment" ? "glossy" : "outline"}
            size="sm"
            data-testid={`people-update-balance-type-payment-${row.contact.id}`}
            onClick={() => setEntryType("Payment")}
          >
            {t("personal.utang.recordPayment")}
          </Button>
          <Button
            type="button"
            intent={entryType === "Loan" ? "primary" : "neutral"}
            appearance={entryType === "Loan" ? "glossy" : "outline"}
            size="sm"
            data-testid={`people-update-balance-type-loan-${row.contact.id}`}
            onClick={() => setEntryType("Loan")}
          >
            {t("personal.utang.addAmount")}
          </Button>
        </div>
        <ExitsOutlineInput
          id={`people-update-balance-amount-${row.contact.id}`}
          label={t("personal.utang.amount")}
          value={amount}
          onChange={(next) => setAmount(normalizeMoneyAmountTyping(next))}
          inputMode="decimal"
          testId={`people-update-balance-amount-${row.contact.id}`}
        />
        <ExitsOutlineTextarea
          id={`people-update-balance-notes-${row.contact.id}`}
          label={t("personal.utang.purpose")}
          value={notes}
          onChange={setNotes}
          testId={`people-update-balance-notes-${row.contact.id}`}
        />
        <div className="people-mobile-sheet__actions flex flex-wrap gap-2">
          <Button
            type="button"
            intent="danger"
            appearance="glossy"
            emphasis="strong"
            disabled={saving}
            data-testid={`people-update-balance-cancel-${row.contact.id}`}
            onClick={onClose}
          >
            {t("people.add.cancel")}
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            className="min-w-[8rem]"
            disabled={saving}
            data-testid={`people-update-balance-save-${row.contact.id}`}
            onClick={() => void submit()}
          >
            <SaveCheckIcon className="size-4 shrink-0" />
            {saving ? t("loading.label") : t("people.edit.save")}
          </Button>
        </div>
      </div>
    </BottomSheet>
  );
}

function PeoplePreviousClosedSheet({
  open,
  row,
  onClose,
  onViewActivity,
}: {
  open: boolean;
  row: PeopleRowModel;
  onClose: () => void;
  onViewActivity: (closed: PeopleClosedUtangSnapshot) => void;
}) {
  const { t } = useI18n();
  const items = row.previousClosed;

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={t("people.previousClosed.title")}
      panelId={`people-previous-closed-sheet-${row.contact.id}`}
      testId={`people-previous-closed-sheet-${row.contact.id}`}
      closeLabel={t("people.add.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      size={items.length >= ACTIVITY_FULL_SHEET_AT ? "full" : "default"}
      panelClassName="people-mobile-sheet people-mobile-sheet--activity"
    >
      <div className="people-mobile-sheet__body people-mobile-sheet__activity flex min-h-0 min-w-0 flex-1 flex-col gap-3">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{row.contact.displayName}</p>
        {items.length === 0 ? (
          <EmptyState
            align="center"
            icon={<Archive className="size-5" strokeWidth={1.75} />}
            title={t("people.previousClosed.emptyTitle")}
            detail={t("people.previousClosed.emptyDetail")}
            testId={`people-previous-closed-empty-${row.contact.id}`}
          />
        ) : (
          <ul
            className="people-mobile-sheet__activity-list m-0 flex list-none flex-col gap-2 p-0"
            data-testid={`people-previous-closed-list-${row.contact.id}`}
          >
            {items.map((item) => (
              <li
                key={item.relationshipId}
                className="people-mobile-sheet__activity-item"
                data-testid={`people-previous-closed-item-${item.relationshipId}`}
              >
                <div className="people-mobile-sheet__activity-item-head">
                  <span
                    className={cn(
                      "people-mobile-sheet__activity-title truncate",
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
                <div className="people-mobile-sheet__activity-item-meta">
                  <span>{formatPeopleActivityWhen(item.updatedAtUtc)}</span>
                  <span>{t("personal.utang.statusClosed")}</span>
                </div>
                <Button
                  type="button"
                  intent="neutral"
                  appearance="outline"
                  size="sm"
                  className="self-start"
                  data-testid={`people-previous-closed-activity-${item.relationshipId}`}
                  onClick={() => onViewActivity(item)}
                >
                  <Activity className="size-3.5 shrink-0" aria-hidden />
                  {t("people.previousClosed.viewActivity")}
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </BottomSheet>
  );
}

function PeopleActivitySheet({
  open,
  row,
  relationshipId: relationshipIdProp,
  onClose,
}: {
  open: boolean;
  row: PeopleRowModel;
  relationshipId?: string | null;
  onClose: () => void;
}) {
  const { t } = useI18n();
  const relationshipId = relationshipIdProp ?? row.utang?.relationshipId ?? null;
  const [search, setSearch] = useState("");

  const historyQuery = useQuery({
    queryKey: ["personal", "utang", "history", relationshipId ?? ""],
    enabled: open && Boolean(relationshipId),
    queryFn: ({ signal }) => listPersonalUtangHistory(relationshipId!, signal),
  });

  useEffect(() => {
    if (!open) {
      return;
    }
    setSearch("");
  }, [open, relationshipId]);

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
    return mapped.filter((row) =>
      [row.title, row.statusText, row.entry.notes ?? "", row.entry.createdAtUtc]
        .join(" ")
        .toLowerCase()
        .includes(needle),
    );
  }, [history, needle, openingLoanEntryId, t]);

  const useFullSheet = history.length >= ACTIVITY_FULL_SHEET_AT || Boolean(needle);

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={t("personal.utang.activity")}
      panelId={`people-activity-sheet-${row.contact.id}`}
      testId={`people-activity-sheet-${row.contact.id}`}
      closeLabel={t("people.add.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      size={useFullSheet ? "full" : "default"}
      panelClassName="people-mobile-sheet people-mobile-sheet--activity"
    >
      <div className="people-mobile-sheet__body people-mobile-sheet__activity flex min-h-0 min-w-0 flex-1 flex-col gap-3">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
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
            id={`people-activity-search-${row.contact.id}`}
            label={t("personal.utang.activitySearchLabel")}
            value={search}
            onChange={setSearch}
            placeholder={t("personal.utang.activitySearchPlaceholder")}
            placeholderOnly
            clearable
            testId={`people-activity-search-${row.contact.id}`}
          />
        ) : null}
        {historyQuery.isPending ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
        ) : historyQuery.isError ? (
          <Notice tone="danger" testId={`people-activity-error-${row.contact.id}`}>
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
            testId={`people-activity-empty-${row.contact.id}`}
          />
        ) : rows.length === 0 ? (
          <EmptyState
            variant="filtered"
            align="center"
            icon={<History className="size-5" strokeWidth={1.75} />}
            title={t("personal.utang.activityFilterEmptyTitle")}
            detail={t("personal.utang.activityFilterEmptyDetail")}
            testId={`people-activity-filtered-empty-${row.contact.id}`}
          />
        ) : (
          <ul
            className="people-mobile-sheet__activity-list m-0 flex list-none flex-col gap-2 p-0"
            data-testid={`people-activity-list-${row.contact.id}`}
          >
            {rows.map(({ entry, title, statusText, showStatus }) => (
              <li
                key={entry.id}
                className="people-mobile-sheet__activity-item"
                data-testid={`people-activity-entry-${entry.id}`}
              >
                <div className="people-mobile-sheet__activity-item-head">
                  <span className="people-mobile-sheet__activity-title truncate">{title}</span>
                  <MoneyDisplay
                    amount={entry.signedDelta}
                    className="people-mobile-sheet__activity-amount shrink-0 font-semibold"
                  />
                </div>
                <div className="people-mobile-sheet__activity-item-meta">
                  <span>{formatPeopleActivityWhen(entry.createdAtUtc)}</span>
                  {showStatus ? <span>{statusText}</span> : null}
                </div>
                {entry.notes?.trim() ? (
                  <p className="people-mobile-sheet__activity-notes m-0">{entry.notes.trim()}</p>
                ) : null}
                {entry.affectsBalance !== false ? (
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {t("personal.utang.balanceAfter")}:{" "}
                    <MoneyDisplay amount={entry.balanceAfter} />
                  </p>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </div>
    </BottomSheet>
  );
}

function PeopleSettleSheet({
  open,
  row,
  onClose,
}: {
  open: boolean;
  row: PeopleRowModel;
  onClose: () => void;
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
    if (!open) {
      return;
    }
    setNote("");
    setError(null);
    settlementEntryIdRef.current = null;
  }, [open, relationshipId]);

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
      onClose();
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
    <BottomSheet
      open={open}
      onClose={() => {
        if (!saving) {
          onClose();
        }
      }}
      title={t("personal.utang.settleTitle")}
      panelId={`people-settle-sheet-${row.contact.id}`}
      testId={`people-settle-sheet-${row.contact.id}`}
      closeLabel={t("people.add.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      panelClassName="people-mobile-sheet"
    >
      <div className="people-mobile-sheet__body flex min-w-0 flex-col gap-3">
        <div className="people-mobile-sheet__settle-summary">
          <span className="people-mobile-sheet__settle-summary-item">
            <span className="text-[length:var(--exits-text-xs)] font-semibold text-muted">
              {t("personal.utang.settleAmount")}
            </span>
            <MoneyDisplay
              amount={currentBalance}
              className="text-[length:var(--exits-text-lg)] font-bold"
              testId={`people-settle-amount-${row.contact.id}`}
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
              testId={`people-settle-after-${row.contact.id}`}
            />
          </span>
        </div>
        <p className="people-mobile-sheet__settle-hint">
          {shared ? t("personal.utang.settleSharedHint") : t("personal.utang.settlePrivateHint")}
        </p>
        {error ? (
          <Notice tone="danger" testId={`people-settle-error-${row.contact.id}`}>
            {error}
          </Notice>
        ) : null}
        <ExitsOutlineInput
          id={`people-settle-note-${row.contact.id}`}
          label={t("personal.utang.settleNote")}
          value={note}
          onChange={setNote}
          maxLength={UTANG_NOTES_MAX_LENGTH}
          testId={`people-settle-note-${row.contact.id}`}
        />
        <div className="people-mobile-sheet__actions flex flex-wrap gap-2">
          <Button
            type="button"
            intent="danger"
            appearance="glossy"
            emphasis="strong"
            disabled={saving}
            data-testid={`people-settle-cancel-${row.contact.id}`}
            onClick={onClose}
          >
            {t("people.add.cancel")}
          </Button>
          <Button
            type="button"
            intent="primary"
            appearance="glossy"
            className="min-w-[8rem]"
            disabled={saving || !online}
            data-testid={`people-settle-confirm-${row.contact.id}`}
            onClick={() => void submit()}
          >
            <Check className="size-4 shrink-0" aria-hidden />
            {saving ? t("loading.label") : t("personal.utang.settleConfirm")}
          </Button>
        </div>
      </div>
    </BottomSheet>
  );
}

function PeopleUtangRecordSheet({
  open,
  row,
  mode,
  onClose,
}: {
  open: boolean;
  row: PeopleRowModel;
  mode: "lent" | "borrowed";
  onClose: () => void;
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
    if (!open) {
      return;
    }
    setAmount(formatMoneyAmountInput(0));
    setNotes("");
    setDueDate("");
    setShareWithCounterparty(false);
    setError(null);
  }, [open, mode, row.contact.id]);

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
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <BottomSheet
      open={open}
      onClose={onClose}
      title={`${mode === "lent" ? t("people.detail.iLentAction") : t("people.detail.iOweAction")} - ${row.contact.displayName}`}
      panelId={`people-utang-sheet-${row.contact.id}-${mode}`}
      testId={`people-utang-sheet-${row.contact.id}-${mode}`}
      closeLabel={t("people.add.cancel")}
      presentation="sheet-mobile-dialog-desktop"
      panelClassName="people-mobile-sheet"
    >
      <div className="people-mobile-sheet__body flex min-w-0 flex-col gap-3">
        {error ? (
          <Notice tone="danger" testId={`people-utang-sheet-error-${row.contact.id}`}>
            {error}
          </Notice>
        ) : null}
        <ExitsOutlineInput
          id={`people-utang-amount-${row.contact.id}`}
          label={t("personal.utang.amount")}
          value={amount}
          onChange={(next) => setAmount(normalizeMoneyAmountTyping(next))}
          inputMode="decimal"
          testId={`people-utang-amount-${row.contact.id}`}
        />
        <SoftGlassDateField
          label={t("personal.utang.dueDate")}
          value={dueDate}
          onChange={setDueDate}
          clearable
          testId={`people-utang-due-${row.contact.id}`}
        />
        <ExitsOutlineTextarea
          id={`people-utang-notes-${row.contact.id}`}
          label={t("personal.utang.purpose")}
          value={notes}
          onChange={setNotes}
          testId={`people-utang-notes-${row.contact.id}`}
        />
        {isConnected ? (
          <ExitsCheckbox
            checked={shareWithCounterparty}
            onCheckedChange={setShareWithCounterparty}
            label={t("personal.utang.shareWithPerson").replace("{name}", row.contact.displayName)}
            testId={`people-utang-share-${row.contact.id}`}
          />
        ) : null}
        <div className="people-mobile-sheet__actions flex flex-wrap gap-2">
          <ExitsOrgEhgButton
            type="button"
            intent="danger"
            appearance="standard"
            size="md"
            disabled={saving || createUtang.isPending}
            testId={`people-utang-cancel-${row.contact.id}`}
            onClick={onClose}
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
            testId={`people-utang-save-${row.contact.id}`}
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
    </BottomSheet>
  );
}

export function PeopleMobileRow({
  row,
  statusLabel,
  expanded,
  onExpandedChange,
}: {
  row: PeopleRowModel;
  statusLabel: string;
  expanded: boolean;
  onExpandedChange: (expanded: boolean) => void;
}) {
  const { t } = useI18n();
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [updateBalanceOpen, setUpdateBalanceOpen] = useState(false);
  const [settleOpen, setSettleOpen] = useState(false);
  const [activityOpen, setActivityOpen] = useState(false);
  const [activityRelationshipId, setActivityRelationshipId] = useState<string | null>(null);
  const [previousClosedOpen, setPreviousClosedOpen] = useState(false);
  const [utangMode, setUtangMode] = useState<"lent" | "borrowed" | null>(null);
  const { open: moreOpen, setOpen: setMoreOpen } = useDismissibleOpen(false);

  const utang = row.utang;
  const contactKind =
    row.connectionStatus === "connected" || row.connectionStatus === "local"
      ? row.connectionStatus === "connected"
        ? t("people.status.connected")
        : t("people.localContact")
      : statusLabel;
  const directionLabel = utang
    ? utang.direction === "lent"
      ? t("personal.utang.owesYou")
      : t("personal.utang.youOwe")
    : t("people.mobile.noBalance");
  const due = formatDueLabel(utang?.dueDateUtc);
  const dueLabel =
    due.kind !== "none" && due.iso
      ? `${
          due.kind === "overdue"
            ? t("personal.utang.dueOverdue")
            : due.kind === "dueSoon"
              ? t("personal.utang.dueSoon")
              : t("personal.utang.dueUpcoming")
        }: ${new Date(due.iso).toLocaleDateString()}`
      : null;
  const canLedgerActions = Boolean(utang?.relationshipId && utang.isLedgerOwner);
  const canViewActivity = Boolean(utang?.relationshipId);
  const showSettle = canLedgerActions && (utang?.balance ?? 0) > 0;
  const showPreviousClosed = row.previousClosed.length > 0;
  const showExpandActions = showSettle || canLedgerActions;

  useEffect(() => {
    if (!showExpandActions && expanded) {
      onExpandedChange(false);
    }
  }, [expanded, onExpandedChange, showExpandActions]);

  function runMoreAction(action: () => void) {
    setMoreOpen(false);
    action();
  }

  return (
    <li className="people-mobile-row-item">
      <div
        className={cn(
          "people-mobile-row",
          utang
            ? utang.direction === "lent"
              ? "people-mobile-row--lent"
              : "people-mobile-row--owe"
            : "people-mobile-row--neutral",
          expanded && "people-mobile-row--expanded",
        )}
        data-testid={`people-row-${row.contact.id}`}
        data-expanded={expanded ? "true" : "false"}
      >
        <div className="people-mobile-row__body">
          <PersonAvatar
            name={row.contact.displayName}
            size="sm"
            className="people-mobile-row__avatar"
          />
          {showExpandActions ? (
            <button
              type="button"
              className="people-mobile-row__name-hit"
              aria-expanded={expanded}
              aria-label={
                expanded ? t("people.action.collapseRow") : t("people.action.expandRow")
              }
              data-testid={`people-row-toggle-${row.contact.id}`}
              onClick={() => onExpandedChange(!expanded)}
            >
              <span className="people-mobile-row__name truncate">{row.contact.displayName}</span>
            </button>
          ) : (
            <div className="people-mobile-row__name-hit people-mobile-row__name-hit--static">
              <span className="people-mobile-row__name truncate">{row.contact.displayName}</span>
            </div>
          )}

          <span className="people-mobile-row__amount">
            {utang ? (
              <MoneyDisplay
                amount={utang.balance}
                testId={`people-row-balance-${row.contact.id}`}
              />
            ) : (
              <span className="text-muted">—</span>
            )}
          </span>

          <div className="people-mobile-row__more-wrap">
            <DropdownMenu
              open={moreOpen}
              onOpenChange={setMoreOpen}
              align="end"
              menuLabel={t("people.action.more")}
              menuClassName="people-mobile-row__more-menu"
              trigger={({ id, expanded: menuExpanded, controls, onClick, onKeyDown }) => (
                <ExitsTooltip content={t("people.action.more")}>
                  <button
                    type="button"
                    id={id}
                    className="people-mobile-row__more"
                    aria-haspopup="menu"
                    aria-expanded={menuExpanded}
                    aria-controls={controls}
                    aria-label={t("people.action.more")}
                    data-testid={`people-row-more-${row.contact.id}`}
                    onClick={onClick}
                    onKeyDown={onKeyDown}
                  >
                    <EllipsisVertical className="size-4 shrink-0" aria-hidden strokeWidth={2.25} />
                  </button>
                </ExitsTooltip>
              )}
            >
              <MenuItem
                data-testid={`people-row-lent-${row.contact.id}`}
                onSelect={() => runMoreAction(() => setUtangMode("lent"))}
              >
                <span className="people-mobile-row__menu-icon people-mobile-row__menu-icon--lent" aria-hidden>
                  <Wallet className="size-3 shrink-0" strokeWidth={2} />
                </span>
                {t("people.detail.iLentAction")}
              </MenuItem>
              <MenuItem
                data-testid={`people-row-owe-${row.contact.id}`}
                onSelect={() => runMoreAction(() => setUtangMode("borrowed"))}
              >
                <span className="people-mobile-row__menu-icon people-mobile-row__menu-icon--owe" aria-hidden>
                  <HandCoins className="size-3 shrink-0" strokeWidth={2} />
                </span>
                {t("people.detail.iOweAction")}
              </MenuItem>
              <MenuSeparator />
              <MenuItem
                data-testid={`people-row-view-${row.contact.id}`}
                onSelect={() => runMoreAction(() => setDetailsOpen(true))}
              >
                <span className="people-mobile-row__menu-icon people-mobile-row__menu-icon--people" aria-hidden>
                  <Contact className="size-3 shrink-0" strokeWidth={2} />
                </span>
                {t("people.action.viewContactDetails")}
              </MenuItem>
              <MenuItem
                data-testid={`people-row-activity-${row.contact.id}`}
                disabled={!canViewActivity}
                onSelect={() =>
                  runMoreAction(() => {
                    setActivityRelationshipId(utang?.relationshipId ?? null);
                    setActivityOpen(true);
                  })
                }
              >
                <span className="people-mobile-row__menu-icon people-mobile-row__menu-icon--info" aria-hidden>
                  <Activity className="size-3 shrink-0" strokeWidth={2} />
                </span>
                {t("personal.utang.activity")}
              </MenuItem>
              <MenuItem
                data-testid={`people-row-previous-closed-${row.contact.id}`}
                disabled={!showPreviousClosed}
                onSelect={() => runMoreAction(() => setPreviousClosedOpen(true))}
              >
                <span className="people-mobile-row__menu-icon people-mobile-row__menu-icon--neutral" aria-hidden>
                  <Archive className="size-3 shrink-0" strokeWidth={2} />
                </span>
                {t("people.action.previousClosed")}
              </MenuItem>
            </DropdownMenu>
            {showExpandActions ? (
              <button
                type="button"
                className="people-mobile-row__caret-btn"
                aria-expanded={expanded}
                aria-label={
                  expanded ? t("people.action.collapseRow") : t("people.action.expandRow")
                }
                data-testid={`people-row-caret-${row.contact.id}`}
                onClick={() => onExpandedChange(!expanded)}
              >
                <ChevronDown
                  className={cn(
                    "people-mobile-row__caret size-4 shrink-0",
                    expanded && "people-mobile-row__caret--open",
                  )}
                  strokeWidth={2.75}
                  aria-hidden
                />
              </button>
            ) : null}
          </div>

          {showExpandActions ? (
            <button
              type="button"
              className="people-mobile-row__line people-mobile-row__line--secondary people-mobile-row__secondary-hit"
              aria-expanded={expanded}
              aria-label={
                expanded ? t("people.action.collapseRow") : t("people.action.expandRow")
              }
              data-testid={`people-row-secondary-toggle-${row.contact.id}`}
              onClick={() => onExpandedChange(!expanded)}
            >
              <span
                className={cn(
                  "people-mobile-row__direction truncate",
                  utang?.direction === "lent" && "people-mobile-row__direction--lent",
                  utang?.direction === "owe" && "people-mobile-row__direction--owe",
                )}
              >
                {directionLabel}
              </span>
              <span className="people-mobile-row__meta">
                <span className="people-mobile-row__contact-kind truncate">{contactKind}</span>
              </span>
            </button>
          ) : (
            <div className="people-mobile-row__line people-mobile-row__line--secondary">
              <span
                className={cn(
                  "people-mobile-row__direction truncate",
                  utang?.direction === "lent" && "people-mobile-row__direction--lent",
                  utang?.direction === "owe" && "people-mobile-row__direction--owe",
                )}
              >
                {directionLabel}
              </span>
              <span className="people-mobile-row__meta">
                <span className="people-mobile-row__contact-kind truncate">{contactKind}</span>
              </span>
            </div>
          )}
        </div>

        {showExpandActions ? (
          <div
            className={cn(
              "people-mobile-row__actions-slot",
              expanded && "people-mobile-row__actions-slot--open",
            )}
            aria-hidden={!expanded}
          >
            <div className="people-mobile-row__actions-slot-inner">
              <div
                className="people-mobile-row__actions"
                data-testid={`people-row-actions-${row.contact.id}`}
                inert={!expanded ? true : undefined}
              >
                {dueLabel ? (
                  <p
                    className={cn(
                      "people-mobile-row__due people-mobile-row__due--expanded",
                      due.kind === "overdue" && "people-mobile-row__due--overdue",
                      due.kind === "dueSoon" && "people-mobile-row__due--soon",
                    )}
                    data-testid={`people-row-due-${row.contact.id}`}
                  >
                    {dueLabel}
                  </p>
                ) : null}
                <div
                  className={cn(
                    "people-mobile-row__actions-row",
                    !(showSettle && canLedgerActions) && "people-mobile-row__actions-row--solo",
                  )}
                >
                  {showSettle ? (
                    <Button
                      type="button"
                      intent="warning"
                      appearance="outline"
                      size="sm"
                      tabIndex={expanded ? undefined : -1}
                      data-testid={`people-row-settle-${row.contact.id}`}
                      onClick={() => setSettleOpen(true)}
                    >
                      <Scale className="size-3.5 shrink-0" aria-hidden />
                      {t("personal.utang.settle")}
                    </Button>
                  ) : null}
                  {canLedgerActions ? (
                    <Button
                      type="button"
                      intent="primary"
                      appearance="outline"
                      size="sm"
                      tabIndex={expanded ? undefined : -1}
                      data-testid={`people-row-update-balance-${row.contact.id}`}
                      onClick={() => setUpdateBalanceOpen(true)}
                    >
                      {t("personal.utang.qaUpdateBalance")}
                    </Button>
                  ) : null}
                </div>
              </div>
            </div>
          </div>
        ) : null}
      </div>

      <PeopleContactDetailsSheet
        open={detailsOpen}
        row={row}
        onClose={() => setDetailsOpen(false)}
      />
      <PeopleUpdateBalanceSheet
        open={updateBalanceOpen}
        row={row}
        onClose={() => setUpdateBalanceOpen(false)}
      />
      <PeopleSettleSheet
        open={settleOpen}
        row={row}
        onClose={() => setSettleOpen(false)}
      />
      <PeoplePreviousClosedSheet
        open={previousClosedOpen}
        row={row}
        onClose={() => setPreviousClosedOpen(false)}
        onViewActivity={(closed) => {
          setPreviousClosedOpen(false);
          setActivityRelationshipId(closed.relationshipId);
          setActivityOpen(true);
        }}
      />
      <PeopleActivitySheet
        open={activityOpen}
        row={row}
        relationshipId={activityRelationshipId}
        onClose={() => {
          setActivityOpen(false);
          setActivityRelationshipId(null);
        }}
      />
      <PeopleUtangRecordSheet
        open={utangMode !== null}
        row={row}
        mode={utangMode ?? "lent"}
        onClose={() => setUtangMode(null)}
      />
    </li>
  );
}
