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
import {
  ExitsOutlineInput,
  ExitsOutlineSelect,
  ExitsOutlineTextarea,
} from "@/components/exits/ExitsOutlineField";
import { Button } from "@/components/ui/button";
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
        value: contact.id,
        label: contact.displayName,
      }));
    }
    if (form.relatedEntityType === "PersonalUtangRelationship") {
      return (utangQuery.data ?? []).map((relationship) => ({
        value: relationship.id,
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
      <ExitsOutlineSelect
        label={t("personal.todo.relatedPick")}
        value={form.relatedEntityId}
        options={[
          { value: "", label: t("personal.todo.relatedPickPlaceholder") },
          ...pickerOptions,
        ]}
        onChange={(relatedEntityId) => setForm({ ...form, relatedEntityId })}
        testId={`${idPrefix}-related-pick`}
      />
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-1">
      <ExitsOutlineInput
        label={t("personal.todo.relatedId")}
        value={form.relatedEntityId}
        onChange={(relatedEntityId) => setForm({ ...form, relatedEntityId })}
        placeholder="00000000-0000-0000-0000-000000000000"
        testId={`${idPrefix}-related-id`}
      />
      {!online &&
      form.relatedEntityType !== "CustomerOrder" &&
      form.relatedEntityType !== "Organization" ? (
        <span className="text-[length:var(--exits-text-xs)] text-muted">
          {t("personal.todo.relatedPickerOfflineHint")}
        </span>
      ) : null}
    </div>
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
            <Button
              key={preset.id}
              type="button"
              intent={active ? "primary" : "neutral"}
              appearance={active ? "glossy" : "outline"}
              shape="standard"
              size="sm"
              className="personal-todo-quick-due__chip"
              data-testid={`${testIdPrefix}-quick-due-${preset.id}`}
              onClick={() => onSelect(value)}
            >
              {t(preset.labelKey)}
            </Button>
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

  const priorityOptions = useMemo(
    () =>
      TODO_PRIORITIES.map((priority) => ({
        value: priority,
        label: t(priorityLabelKey(priority)),
      })),
    [t],
  );

  const relatedTypeOptions = useMemo(
    () =>
      TODO_RELATED_TYPES.map((option) => ({
        value: option.value,
        label: t(option.labelKey),
      })),
    [t],
  );

  return (
    <>
      {includeTitle ? (
        <div className="personal-todo-form__title-field">
          <ExitsOutlineInput
            id={`${idPrefix}-title`}
            label={t("personal.todo.titleField")}
            value={form.title}
            onChange={(title) => setForm({ ...form, title })}
            autoFocus={titleAutoFocus}
            required
            testId={`${idPrefix}-title`}
          />
        </div>
      ) : null}

      {showAdvanced ? (
        <>
          <ExitsOutlineTextarea
            label={t("personal.todo.notes")}
            value={form.notes}
            onChange={(notes) => setForm({ ...form, notes })}
            rows={3}
            testId={`${idPrefix}-notes`}
          />

          <TodoQuickDueChips
            activeDueLocal={form.dueAtLocal}
            testIdPrefix={idPrefix}
            onSelect={(dueAtLocal) => setForm({ ...form, dueAtLocal })}
          />

          <ExitsOutlineInput
            label={t("personal.todo.dueAt")}
            type="datetime-local"
            value={form.dueAtLocal}
            onChange={(dueAtLocal) => setForm({ ...form, dueAtLocal })}
            testId={`${idPrefix}-due`}
          />

          <div className="flex min-w-0 flex-col gap-1">
            <ExitsOutlineInput
              label={t("personal.todo.reminderAt")}
              type="datetime-local"
              value={form.reminderAtLocal}
              onChange={(reminderAtLocal) => setForm({ ...form, reminderAtLocal })}
              testId={`${idPrefix}-reminder`}
            />
            {form.reminderAtLocal ? (
              <span className="text-[length:var(--exits-text-xs)] text-muted">
                {t("personal.todo.reminderServerHint")}
              </span>
            ) : null}
          </div>

          <ExitsOutlineSelect
            label={t("personal.todo.priority")}
            value={form.priority}
            options={priorityOptions}
            onChange={(priority) => setForm({ ...form, priority })}
            testId={`${idPrefix}-priority`}
          />

          <ExitsOutlineSelect
            label={t("personal.todo.relatedType")}
            value={form.relatedEntityType}
            options={relatedTypeOptions}
            onChange={(relatedEntityType) =>
              setForm({
                ...form,
                relatedEntityType,
                relatedEntityId: relatedEntityType ? form.relatedEntityId : "",
              })
            }
            testId={`${idPrefix}-related-type`}
          />

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
