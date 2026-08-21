import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  cancelPersonalTodo,
  completePersonalTodo,
  createPersonalTodo,
  filterTodosByTab,
  getPersonalTodo,
  isTodoConcurrencyConflict,
  listPersonalTodos,
  localDateTimeToUtcIso,
  reopenPersonalTodo,
  summarizeTodoCounts,
  updatePersonalTodo,
  utcIsoToLocalDateTimeInput,
  type PersonalTodoDto,
  type TodoAgendaTab,
} from "@/api/platform/personal-todo-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

const TABS: { id: TodoAgendaTab; labelKey: MessageKey }[] = [
  { id: "today", labelKey: "personal.todo.filterToday" },
  { id: "upcoming", labelKey: "personal.todo.filterUpcoming" },
  { id: "overdue", labelKey: "personal.todo.filterOverdue" },
  { id: "open", labelKey: "personal.todo.filterOpen" },
  { id: "completed", labelKey: "personal.todo.filterCompleted" },
];

const PRIORITIES = ["None", "Low", "Normal", "High"] as const;
const RELATED_TYPES = [
  { value: "", labelKey: "personal.todo.relatedNone" as MessageKey },
  { value: "PersonalUtangRelationship", labelKey: "personal.todo.relatedUtang" as MessageKey },
  { value: "PersonalContact", labelKey: "personal.todo.relatedContact" as MessageKey },
  { value: "CustomerOrder", labelKey: "personal.todo.relatedOrder" as MessageKey },
  { value: "Organization", labelKey: "personal.todo.relatedOrg" as MessageKey },
];

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

function statusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Completed":
      return "personal.todo.statusCompleted";
    case "Cancelled":
      return "personal.todo.statusCancelled";
    default:
      return "personal.todo.statusOpen";
  }
}

function mutationErrorMessage(error: unknown, t: (key: MessageKey) => string): string {
  if (isTodoConcurrencyConflict(error)) return t("personal.todo.concurrencyConflict");
  if (error instanceof PlatformApiError) return error.message;
  return t("personal.todo.genericError");
}

type TodoFormState = {
  title: string;
  notes: string;
  dueAtLocal: string;
  reminderAtLocal: string;
  priority: string;
  relatedEntityType: string;
  relatedEntityId: string;
};

const emptyForm = (): TodoFormState => ({
  title: "",
  notes: "",
  dueAtLocal: "",
  reminderAtLocal: "",
  priority: "Normal",
  relatedEntityType: "",
  relatedEntityId: "",
});

function formFromTodo(todo: PersonalTodoDto): TodoFormState {
  return {
    title: todo.title,
    notes: todo.notes ?? "",
    dueAtLocal: utcIsoToLocalDateTimeInput(todo.dueAtUtc),
    reminderAtLocal: utcIsoToLocalDateTimeInput(todo.reminderAtUtc),
    priority: todo.priority || "None",
    relatedEntityType: todo.relatedEntityType ?? "",
    relatedEntityId: todo.relatedEntityId ?? "",
  };
}

function toRequestBody(form: TodoFormState) {
  const relatedType = form.relatedEntityType.trim() || null;
  const relatedId = form.relatedEntityId.trim() || null;
  return {
    title: form.title.trim(),
    notes: form.notes.trim() || null,
    dueAtUtc: localDateTimeToUtcIso(form.dueAtLocal),
    reminderAtUtc: localDateTimeToUtcIso(form.reminderAtLocal),
    priority: form.priority || "None",
    relatedEntityType: relatedType,
    relatedEntityId: relatedType ? relatedId : null,
  };
}

function TodoFormFields({
  form,
  setForm,
  idPrefix,
}: {
  form: TodoFormState;
  setForm: (next: TodoFormState) => void;
  idPrefix: string;
}) {
  const { t } = useI18n();
  return (
    <>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("personal.todo.titleField")}
        <input
          data-testid={`${idPrefix}-title`}
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={form.title}
          onChange={(e) => setForm({ ...form, title: e.target.value })}
          required
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("personal.todo.notes")}
        <textarea
          data-testid={`${idPrefix}-notes`}
          className="min-h-20 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
          value={form.notes}
          onChange={(e) => setForm({ ...form, notes: e.target.value })}
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("personal.todo.dueAt")}
        <input
          data-testid={`${idPrefix}-due`}
          type="datetime-local"
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={form.dueAtLocal}
          onChange={(e) => setForm({ ...form, dueAtLocal: e.target.value })}
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("personal.todo.reminderAt")}
        <input
          data-testid={`${idPrefix}-reminder`}
          type="datetime-local"
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={form.reminderAtLocal}
          onChange={(e) => setForm({ ...form, reminderAtLocal: e.target.value })}
        />
      </label>
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("personal.todo.priority")}
        <select
          data-testid={`${idPrefix}-priority`}
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
          value={form.priority}
          onChange={(e) => setForm({ ...form, priority: e.target.value })}
        >
          {PRIORITIES.map((p) => (
            <option key={p} value={p}>
              {t(priorityLabelKey(p))}
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
          onChange={(e) =>
            setForm({
              ...form,
              relatedEntityType: e.target.value,
              relatedEntityId: e.target.value ? form.relatedEntityId : "",
            })
          }
        >
          {RELATED_TYPES.map((opt) => (
            <option key={opt.value || "none"} value={opt.value}>
              {t(opt.labelKey)}
            </option>
          ))}
        </select>
      </label>
      {form.relatedEntityType ? (
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("personal.todo.relatedId")}
          <input
            data-testid={`${idPrefix}-related-id`}
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={form.relatedEntityId}
            onChange={(e) => setForm({ ...form, relatedEntityId: e.target.value })}
            placeholder="00000000-0000-0000-0000-000000000000"
          />
        </label>
      ) : null}
    </>
  );
}

export function PersonalTodoHubPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<TodoAgendaTab>("today");
  const [form, setForm] = useState<TodoFormState>(emptyForm);
  const [formError, setFormError] = useState<string | null>(null);

  const todosQuery = useQuery({
    queryKey: ["personal", "todos"],
    queryFn: ({ signal }) => listPersonalTodos(signal),
  });

  const createMutation = useMutation({
    mutationFn: () => createPersonalTodo(toRequestBody(form)),
    onSuccess: async () => {
      setForm(emptyForm());
      setFormError(null);
      await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
    },
    onError: (error) => setFormError(mutationErrorMessage(error, t)),
  });

  const actionMutation = useMutation({
    mutationFn: async ({
      action,
      todo,
    }: {
      action: "complete" | "reopen" | "cancel";
      todo: PersonalTodoDto;
    }) => {
      const body = { expectedVersion: todo.version };
      if (action === "complete") return completePersonalTodo(todo.id, body);
      if (action === "reopen") return reopenPersonalTodo(todo.id, body);
      return cancelPersonalTodo(todo.id, body);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
    },
  });

  const filtered = useMemo(() => {
    if (!todosQuery.data) return [];
    return filterTodosByTab(todosQuery.data, tab);
  }, [todosQuery.data, tab]);

  const counts = useMemo(
    () => (todosQuery.data ? summarizeTodoCounts(todosQuery.data) : null),
    [todosQuery.data],
  );

  if (todosQuery.isPending) return <LoadingSkeleton label={t("personal.todo.loading")} />;
  if (todosQuery.isError) {
    return (
      <div className="flex flex-col gap-3">
        <ErrorState
          title={t("personal.todo.loadErrorTitle")}
          detail={t("personal.todo.loadErrorDetail")}
        />
        <Button type="button" className="min-h-11 w-fit" onClick={() => void todosQuery.refetch()}>
          {t("personal.home.retry")}
        </Button>
      </div>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-todo-hub">
      <PageHeader title={t("personal.todo.title")} description={t("personal.todo.lede")} />

      <div
        className="flex flex-wrap gap-2"
        role="tablist"
        aria-label={t("personal.todo.filters")}
        data-testid="personal-todo-filters"
      >
        {TABS.map((item) => {
          const count =
            counts == null
              ? 0
              : item.id === "today"
                ? counts.today
                : item.id === "upcoming"
                  ? counts.upcoming
                  : item.id === "overdue"
                    ? counts.overdue
                    : item.id === "open"
                      ? counts.open
                      : counts.completed;
          return (
            <Button
              key={item.id}
              type="button"
              role="tab"
              aria-selected={tab === item.id}
              variant={tab === item.id ? "default" : "ghost"}
              className="min-h-11"
              data-testid={`todo-tab-${item.id}`}
              onClick={() => setTab(item.id)}
            >
              {t(item.labelKey)} ({count})
            </Button>
          );
        })}
      </div>

      <form
        className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
        data-testid="todo-create-form"
        onSubmit={(event) => {
          event.preventDefault();
          if (!form.title.trim()) {
            setFormError(t("personal.todo.titleRequired"));
            return;
          }
          createMutation.mutate();
        }}
      >
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("personal.todo.createTitle")}
        </h2>
        <TodoFormFields form={form} setForm={setForm} idPrefix="todo-create" />
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
          data-testid="todo-create-submit"
        >
          {t("personal.todo.add")}
        </Button>
      </form>

      {filtered.length === 0 ? (
        <EmptyState title={t("personal.todo.emptyTitle")} detail={t("personal.todo.emptyDetail")} />
      ) : (
        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="todo-list">
          {filtered.map((item) => (
            <li
              key={item.id}
              className="rounded-[var(--exits-radius-md)] border border-border px-3 py-3"
              data-testid={`todo-item-${item.id}`}
            >
              <div className="flex min-w-0 flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                <div className="min-w-0">
                  <Link
                    to={`/personal/todo/${item.id}`}
                    className="font-semibold text-foreground underline-offset-2 hover:underline"
                  >
                    {item.title}
                  </Link>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {t(statusLabelKey(item.status))} · {t(priorityLabelKey(item.priority))}
                    {item.dueAtUtc
                      ? ` · ${t("personal.todo.dueLabel")}: ${new Date(item.dueAtUtc).toLocaleString()}`
                      : ` · ${t("personal.todo.noDue")}`}
                  </p>
                  {item.notes ? (
                    <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {item.notes}
                    </p>
                  ) : null}
                </div>
                <div className="flex flex-wrap gap-2">
                  {item.status === "Open" ? (
                    <>
                      <Button
                        type="button"
                        className="min-h-11"
                        data-testid={`todo-complete-${item.id}`}
                        disabled={actionMutation.isPending}
                        onClick={() => actionMutation.mutate({ action: "complete", todo: item })}
                      >
                        {t("personal.todo.complete")}
                      </Button>
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11"
                        data-testid={`todo-cancel-${item.id}`}
                        disabled={actionMutation.isPending}
                        onClick={() => actionMutation.mutate({ action: "cancel", todo: item })}
                      >
                        {t("personal.todo.cancel")}
                      </Button>
                    </>
                  ) : null}
                  {item.status === "Completed" || item.status === "Cancelled" ? (
                    <Button
                      type="button"
                      className="min-h-11"
                      data-testid={`todo-reopen-${item.id}`}
                      disabled={actionMutation.isPending}
                      onClick={() => actionMutation.mutate({ action: "reopen", todo: item })}
                    >
                      {t("personal.todo.reopen")}
                    </Button>
                  ) : null}
                  <Button asChild variant="ghost" className="min-h-11">
                    <Link to={`/personal/todo/${item.id}`}>{t("personal.todo.edit")}</Link>
                  </Button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export function PersonalTodoDetailPage() {
  const { t } = useI18n();
  const { todoId = "" } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [form, setForm] = useState<TodoFormState | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);

  const todoQuery = useQuery({
    queryKey: ["personal", "todos", todoId],
    queryFn: ({ signal }) => getPersonalTodo(todoId, signal),
    enabled: Boolean(todoId),
  });

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
    await queryClient.invalidateQueries({ queryKey: ["personal", "todos", todoId] });
  };

  const saveMutation = useMutation({
    mutationFn: ({ current, next }: { current: PersonalTodoDto; next: TodoFormState }) =>
      updatePersonalTodo(current.id, {
        ...toRequestBody(next),
        expectedVersion: current.version,
      }),
    onSuccess: async () => {
      setEditing(false);
      setForm(null);
      setFormError(null);
      await invalidate();
    },
    onError: (error) => setFormError(mutationErrorMessage(error, t)),
  });

  const actionMutation = useMutation({
    mutationFn: async ({
      action,
      todo,
    }: {
      action: "complete" | "reopen" | "cancel";
      todo: PersonalTodoDto;
    }) => {
      const body = { expectedVersion: todo.version };
      if (action === "complete") return completePersonalTodo(todo.id, body);
      if (action === "reopen") return reopenPersonalTodo(todo.id, body);
      return cancelPersonalTodo(todo.id, body);
    },
    onSuccess: async () => {
      await invalidate();
    },
    onError: (error) => setFormError(mutationErrorMessage(error, t)),
  });

  if (todoQuery.isPending) return <LoadingSkeleton label={t("personal.todo.loading")} />;
  if (todoQuery.isError || !todoQuery.data) {
    return (
      <div className="flex flex-col gap-3">
        <ErrorState
          title={t("personal.todo.loadErrorTitle")}
          detail={t("personal.todo.loadErrorDetail")}
        />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/personal/todo">{t("personal.todo.back")}</Link>
        </Button>
      </div>
    );
  }

  const todo = todoQuery.data;
  const activeForm = form ?? formFromTodo(todo);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-todo-detail">
      <PageHeader title={t("personal.todo.detailTitle")} description={todo.title} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/todo">{t("personal.todo.back")}</Link>
      </Button>

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("personal.todo.status")}: {t(statusLabelKey(todo.status))} · {t(priorityLabelKey(todo.priority))}
      </p>

      {editing ? (
        <form
          className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
          data-testid="todo-edit-form"
          onSubmit={(event) => {
            event.preventDefault();
            if (!activeForm.title.trim()) {
              setFormError(t("personal.todo.titleRequired"));
              return;
            }
            saveMutation.mutate({ current: todo, next: activeForm });
          }}
        >
          <TodoFormFields
            form={activeForm}
            setForm={(next) => {
              setForm(next);
            }}
            idPrefix="todo-edit"
          />
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
              disabled={saveMutation.isPending}
              data-testid="todo-edit-save"
            >
              {t("personal.todo.save")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => {
                setEditing(false);
                setForm(null);
                setFormError(null);
              }}
            >
              {t("personal.todo.cancel")}
            </Button>
          </div>
        </form>
      ) : (
        <div className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border p-3">
          {todo.notes ? <p className="m-0 whitespace-pre-wrap">{todo.notes}</p> : null}
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {todo.dueAtUtc
              ? `${t("personal.todo.dueLabel")}: ${new Date(todo.dueAtUtc).toLocaleString()}`
              : t("personal.todo.noDue")}
          </p>
          {todo.reminderAtUtc ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("personal.todo.reminderAt")}: {new Date(todo.reminderAtUtc).toLocaleString()}
            </p>
          ) : null}
          {todo.relatedEntityType ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {todo.relatedEntityType}
              {todo.relatedEntityId ? ` · ${todo.relatedEntityId}` : ""}
            </p>
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
            {todo.status === "Open" ? (
              <>
                <Button
                  type="button"
                  className="min-h-11"
                  data-testid="todo-detail-complete"
                  disabled={actionMutation.isPending}
                  onClick={() => actionMutation.mutate({ action: "complete", todo })}
                >
                  {t("personal.todo.complete")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  data-testid="todo-detail-cancel"
                  disabled={actionMutation.isPending}
                  onClick={() => actionMutation.mutate({ action: "cancel", todo })}
                >
                  {t("personal.todo.cancel")}
                </Button>
              </>
            ) : null}
            {todo.status === "Completed" || todo.status === "Cancelled" ? (
              <Button
                type="button"
                className="min-h-11"
                data-testid="todo-detail-reopen"
                disabled={actionMutation.isPending}
                onClick={() => actionMutation.mutate({ action: "reopen", todo })}
              >
                {t("personal.todo.reopen")}
              </Button>
            ) : null}
            {todo.status === "Open" ? (
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                data-testid="todo-detail-edit"
                onClick={() => {
                  setForm(formFromTodo(todo));
                  setEditing(true);
                }}
              >
                {t("personal.todo.edit")}
              </Button>
            ) : null}
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => navigate("/personal/todo")}
            >
              {t("personal.todo.back")}
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
