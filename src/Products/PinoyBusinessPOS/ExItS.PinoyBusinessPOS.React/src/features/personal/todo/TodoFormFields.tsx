import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";

import {
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
} from "@/api/platform/personal-people-client";
import type { QuickDuePreset } from "@/api/platform/personal-todo-client";
import { quickDueLocalDateTime } from "@/api/platform/personal-todo-client";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { cn } from "@/lib/cn";
import {
  TODO_PRIORITIES,
  TODO_RELATED_TYPES,
  type TodoFormState,
} from "@/features/personal/todo/personal-todo-form";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function priorityLabelKey(priority: string): MessageKey {
  switch (priority) {
    case "Low":
      return "personal.todo.priorityLow";
    case "Normal":
      return "personal.todo.priorityNormal";
    case "High":
      return "personal.todo.priorityHigh";
    default:
      return "personal.todo.priorityNone";
  }
}

const QUICK_DUE_PRESETS: { id: QuickDuePreset; labelKey: MessageKey }[] = [
  { id: "today", labelKey: "personal.todo.quickDueToday" },
  { id: "tomorrow", labelKey: "personal.todo.quickDueTomorrow" },
  { id: "nextWeek", labelKey: "personal.todo.quickDueNextWeek" },
  { id: "none", labelKey: "personal.todo.quickDueNone" },
];

function TodoRelatedEntityPicker({
  form,
  setForm,
  idPrefix,
}: {
  form: TodoFormState;
  setForm: (next: TodoFormState) => void;
  idPrefix: string;
}) {
  const { t } = useI18n();
  const online = useBrowserOnline();

  const contactsQuery = useQuery({
    queryKey: ["personal", "people", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
    enabled: online && form.relatedEntityType === "PersonalContact",
  });

  const utangQuery = useQuery({
    queryKey: ["personal", "todo", "related-utang"],
    queryFn: async ({ signal }) => {
      const [lent, borrowed] = await Promise.all([
        listLentRelationships(signal),
        listBorrowedRelationships(signal),
      ]);
      return [...lent, ...borrowed];
    },
    enabled: online && form.relatedEntityType === "PersonalUtangRelationship",
  });

  const pickerOptions = useMemo(() => {
    if (form.relatedEntityType === "PersonalContact") {
      return (contactsQuery.data ?? []).map((contact) => ({
        id: contact.id,
        label: contact.displayName,
      }));
    }
    if (form.relatedEntityType === "PersonalUtangRelationship") {
      return (utangQuery.data ?? []).map((relationship) => ({
        id: relationship.id,
        label: `${relationship.perspective} · ${relationship.currencyCode} ${relationship.currentBalance}`,
      }));
    }
    return [];
  }, [contactsQuery.data, form.relatedEntityType, utangQuery.data]);

  if (!form.relatedEntityType) {
    return null;
  }

  const usePicker =
    online &&
    (form.relatedEntityType === "PersonalContact" ||
      form.relatedEntityType === "PersonalUtangRelationship") &&
    pickerOptions.length > 0;

  if (usePicker) {
    return (
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("personal.todo.relatedPick")}
        <select
          data-testid={`${idPrefix}-related-pick`}
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={form.relatedEntityId}
          onChange={(event) => setForm({ ...form, relatedEntityId: event.target.value })}
        >
          <option value="">{t("personal.todo.relatedPickPlaceholder")}</option>
          {pickerOptions.map((option) => (
            <option key={option.id} value={option.id}>
              {option.label}
            </option>
          ))}
        </select>
      </label>
    );
  }

  return (
    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
      {t("personal.todo.relatedId")}
      <input
        data-testid={`${idPrefix}-related-id`}
        className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
        value={form.relatedEntityId}
        onChange={(event) => setForm({ ...form, relatedEntityId: event.target.value })}
        placeholder="00000000-0000-0000-0000-000000000000"
      />
      {!online &&
      form.relatedEntityType !== "CustomerOrder" &&
      form.relatedEntityType !== "Organization" ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">
          {t("personal.todo.relatedPickerOfflineHint")}
        </span>
      ) : null}
    </label>
  );
}

export function TodoQuickDueChips({
  activeDueLocal,
  onSelect,
  testIdPrefix,
}: {
  activeDueLocal: string;
  onSelect: (dueAtLocal: string) => void;
  testIdPrefix: string;
}) {
  const { t } = useI18n();

  return (
    <div className="personal-todo-quick-due" data-testid={`${testIdPrefix}-quick-due`}>
      <span className="personal-todo-quick-due__label">{t("personal.todo.quickDueLabel")}</span>
      <div className="personal-todo-quick-due__chips">
        {QUICK_DUE_PRESETS.map((preset) => {
          const value = quickDueLocalDateTime(preset.id);
          const active = preset.id === "none" ? !activeDueLocal : activeDueLocal === value;
          return (
            <button
              key={preset.id}
              type="button"
              className={cn(
                "personal-todo-quick-due__chip",
                active && "personal-todo-quick-due__chip--active",
              )}
              data-testid={`${testIdPrefix}-quick-due-${preset.id}`}
              onClick={() => onSelect(value)}
            >
              {t(preset.labelKey)}
            </button>
          );
        })}
      </div>
    </div>
  );
}

export function TodoFormFields({
  form,
  setForm,
  idPrefix,
  titleAutoFocus = false,
  showAdvanced = true,
  includeTitle = true,
}: {
  form: TodoFormState;
  setForm: (next: TodoFormState) => void;
  idPrefix: string;
  titleAutoFocus?: boolean;
  showAdvanced?: boolean;
  includeTitle?: boolean;
}) {
  const { t } = useI18n();

  return (
    <>
      {includeTitle ? (
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor={`${idPrefix}-title`}
        >
          {t("personal.todo.titleField")}
          <input
            id={`${idPrefix}-title`}
            data-testid={`${idPrefix}-title`}
            autoFocus={titleAutoFocus}
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={form.title}
            onChange={(event) => setForm({ ...form, title: event.target.value })}
            required
          />
        </label>
      ) : null}

      {showAdvanced ? (
        <>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.todo.notes")}
            <textarea
              data-testid={`${idPrefix}-notes`}
              className="min-h-20 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
              value={form.notes}
              onChange={(event) => setForm({ ...form, notes: event.target.value })}
            />
          </label>

          <TodoQuickDueChips
            activeDueLocal={form.dueAtLocal}
            testIdPrefix={idPrefix}
            onSelect={(dueAtLocal) => setForm({ ...form, dueAtLocal })}
          />

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.todo.dueAt")}
            <input
              data-testid={`${idPrefix}-due`}
              type="datetime-local"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={form.dueAtLocal}
              onChange={(event) => setForm({ ...form, dueAtLocal: event.target.value })}
            />
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.todo.reminderAt")}
            <input
              data-testid={`${idPrefix}-reminder`}
              type="datetime-local"
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={form.reminderAtLocal}
              onChange={(event) => setForm({ ...form, reminderAtLocal: event.target.value })}
            />
            {form.reminderAtLocal ? (
              <span className="text-[length:var(--exits-text-xs)] text-muted">
                {t("personal.todo.reminderServerHint")}
              </span>
            ) : null}
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.todo.priority")}
            <select
              data-testid={`${idPrefix}-priority`}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={form.priority}
              onChange={(event) => setForm({ ...form, priority: event.target.value })}
            >
              {TODO_PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {t(priorityLabelKey(priority))}
                </option>
              ))}
            </select>
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("personal.todo.relatedType")}
            <select
              data-testid={`${idPrefix}-related-type`}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              value={form.relatedEntityType}
              onChange={(event) =>
                setForm({
                  ...form,
                  relatedEntityType: event.target.value,
                  relatedEntityId: event.target.value ? form.relatedEntityId : "",
                })
              }
            >
              {TODO_RELATED_TYPES.map((option) => (
                <option key={option.value || "none"} value={option.value}>
                  {t(option.labelKey)}
                </option>
              ))}
            </select>
          </label>

          <TodoRelatedEntityPicker form={form} setForm={setForm} idPrefix={idPrefix} />
        </>
      ) : null}
    </>
  );
}

export function TodoRelatedEntityLink({
  relatedEntityType,
  relatedEntityId,
  label,
}: {
  relatedEntityType: string | null | undefined;
  relatedEntityId: string | null | undefined;
  label: string;
}) {
  const { t } = useI18n();
  if (!relatedEntityType || !relatedEntityId) {
    return null;
  }

  const href =
    relatedEntityType === "PersonalContact"
      ? `/personal/people/${relatedEntityId}`
      : relatedEntityType === "PersonalUtangRelationship"
        ? `/personal/utang/relationships/${relatedEntityId}`
        : null;

  return (
    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
      {t("personal.todo.relatedLinkLabel")}:{" "}
      {href ? (
        <Link className="font-semibold text-primary no-underline" to={href}>
          {label}
        </Link>
      ) : (
        <span>
          {relatedEntityType} · {relatedEntityId}
        </span>
      )}
    </p>
  );
}
