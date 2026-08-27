import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Check,
  ChevronDown,
  ChevronRight,
  ListPlus,
  Loader2,
  Pencil,
  RefreshCw,
  RotateCcw,
  Save,
  Search,
  SlidersHorizontal,
  X,
} from "lucide-react";
import {
  cancelPersonalTodo,
  completePersonalTodo,
  createPersonalTodo,
  filterAndSortTodosForTab,
  getPersonalTodo,
  isTodoConcurrencyConflict,
  listPersonalTodos,
  parseTodoAgendaTab,
  priorityToneClass,
  reopenPersonalTodo,
  summarizeTodoCounts,
  todoEmptyStateKeys,
  updatePersonalTodo,
  type PersonalTodoDto,
  type TodoAgendaTab,
} from "@/api/platform/personal-todo-client";
import {
  TodoFormFields,
  TodoRelatedEntityLink,
} from "@/features/personal/todo/TodoFormFields";
import {
  applyTodoDeepLinkPrefill,
  emptyTodoForm,
  todoFormFromDto,
  todoFormToRequestBody,
  type TodoFormState,
} from "@/features/personal/todo/personal-todo-form";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { usePersonalOfflineContext } from "@/offline/personal-offline-context";
import {
  cachePersonalTodo,
  cachePersonalTodos,
  getCachedPersonalTodo,
  listCachedPersonalTodos,
  type CachedPersonalTodo,
} from "@/offline/personal-todo-cache";
import { type PersonalTodoTransition } from "@/offline/personal-todo-offline";

const TABS: { id: TodoAgendaTab; labelKey: MessageKey }[] = [
  { id: "today", labelKey: "personal.todo.filterToday" },
  { id: "upcoming", labelKey: "personal.todo.filterUpcoming" },
  { id: "overdue", labelKey: "personal.todo.filterOverdue" },
  { id: "open", labelKey: "personal.todo.filterOpen" },
  { id: "completed", labelKey: "personal.todo.filterCompleted" },
  { id: "cancelled", labelKey: "personal.todo.filterCancelled" },
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

function todoStatusTone(status: string): "open" | "completed" | "cancelled" {
  switch (status) {
    case "Completed":
      return "completed";
    case "Cancelled":
      return "cancelled";
    default:
      return "open";
  }
}

function TodoMetaLine({ todo }: { todo: PersonalTodoDto }) {
  const { t } = useI18n();
  const tone = todoStatusTone(todo.status);
  const priorityClass = priorityToneClass(todo.priority);
  return (
    <p className="personal-todo-meta m-0 truncate text-[length:var(--exits-text-sm)] text-muted">
      <span className={cn("personal-todo-meta__chip", `personal-todo-meta__chip--${tone}`)}>
        {t(statusLabelKey(todo.status))}
      </span>
      <span className="personal-todo-meta__sep" aria-hidden>
        ·
      </span>
      <span
        className={cn(
          "personal-todo-meta__chip",
          priorityClass,
        )}
      >
        {t(priorityLabelKey(todo.priority))}
      </span>
      <span className="personal-todo-meta__sep" aria-hidden>
        ·
      </span>
      <span>
        {todo.dueAtUtc
          ? `${t("personal.todo.dueLabel")}: ${new Date(todo.dueAtUtc).toLocaleString()}`
          : t("personal.todo.noDue")}
      </span>
    </p>
  );
}

function TodoConflictBanner({
  onReload,
}: {
  onReload: () => void;
}) {
  const { t } = useI18n();
  return (
    <div
      className="personal-todo-conflict-banner exits-animate-toolbar flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
      role="alert"
      data-testid="todo-conflict-banner"
    >
      <p className="m-0 text-[length:var(--exits-text-sm)]">{t("personal.todo.concurrencyConflictDetail")}</p>
      <Button type="button" className="min-h-11 w-full sm:w-auto" onClick={onReload}>
        <RefreshCw className="size-4 shrink-0" aria-hidden />
        {t("personal.todo.reloadAndRetry")}
      </Button>
    </div>
  );
}

function TodoActionIcon({ pending, children }: { pending: boolean; children: ReactNode }) {
  if (pending) {
    return <Loader2 className="personal-todo-btn-icon size-4 shrink-0 animate-spin" aria-hidden />;
  }
  return <>{children}</>;
}

function mutationErrorMessage(error: unknown, t: (key: MessageKey) => string): string {
  if (isTodoConcurrencyConflict(error)) return t("personal.todo.concurrencyConflict");
  if (error instanceof PlatformApiError) return error.message;
  return t("personal.todo.genericError");
}

function loadErrorDetail(error: unknown, t: (key: MessageKey) => string): string {
  if (error instanceof PlatformApiError) {
    return error.problem.detail ?? error.message;
  }
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }
  return t("personal.todo.loadErrorDetail");
}

/** Marks a to-do whose local change is still waiting in the outbox. */
function WaitingChip({ pending }: { pending: boolean }) {
  const { t } = useI18n();
  if (!pending) return null;
  return (
    <span className="text-[length:var(--exits-text-xs)] text-muted" data-testid="todo-waiting-chip">
      {t("offline.personalWaitingBadge")}
    </span>
  );
}

function OfflineNotice({ message }: { message: string }) {
  return (
    <p
      className="m-0 text-[length:var(--exits-text-sm)] text-muted"
      data-testid="todo-offline-notice"
    >
      {message}
    </p>
  );
}

export function PersonalTodoHubPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const [searchParams, setSearchParams] = useSearchParams();
  const [tab, setTab] = useState<TodoAgendaTab>(() => parseTodoAgendaTab(searchParams.get("tab")));
  const [createFormOpen, setCreateFormOpen] = useState(() => searchParams.get("add") === "1");
  const [createAdvancedOpen, setCreateAdvancedOpen] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [conflictBanner, setConflictBanner] = useState(false);
  const [form, setForm] = useState<TodoFormState>(emptyTodoForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [cachedTodos, setCachedTodos] = useState<CachedPersonalTodo[]>([]);
  const [exitingIds, setExitingIds] = useState<Set<string>>(() => new Set());
  const [activeTodoId, setActiveTodoId] = useState<string | null>(null);

  useEffect(() => {
    setTab(parseTodoAgendaTab(searchParams.get("tab")));
  }, [searchParams]);

  useEffect(() => {
    if (searchParams.get("add") !== "1") {
      return;
    }
    setCreateFormOpen(true);
    setSearchParams(
      (prev) => {
        const nextParams = new URLSearchParams(prev);
        nextParams.delete("add");
        return nextParams;
      },
      { replace: true },
    );
  }, [searchParams, setSearchParams]);

  useEffect(() => {
    const relatedType = searchParams.get("relatedType");
    const relatedId = searchParams.get("relatedId");
    if (!relatedType || !relatedId) {
      return;
    }
    setCreateFormOpen(true);
    setCreateAdvancedOpen(true);
    setForm((current) => applyTodoDeepLinkPrefill(current, relatedType, relatedId));
    setSearchParams(
      (prev) => {
        const nextParams = new URLSearchParams(prev);
        nextParams.delete("relatedType");
        nextParams.delete("relatedId");
        return nextParams;
      },
      { replace: true },
    );
  }, [searchParams, setSearchParams]);

  function changeTab(next: TodoAgendaTab) {
    setTab(next);
    setSearchParams(
      (prev) => {
        const nextParams = new URLSearchParams(prev);
        nextParams.set("tab", next);
        return nextParams;
      },
      { replace: true },
    );
  }

  const todosQuery = useQuery({
    queryKey: ["personal", "todos"],
    queryFn: ({ signal }) => listPersonalTodos(signal),
    enabled: online,
    meta: { suppressGlobalError: true, operation: "list personal todos" },
  });

  useEffect(() => {
    if (!offline || !todosQuery.data) {
      return;
    }
    void cachePersonalTodos(offline.db, offline.scopeBinding, todosQuery.data);
  }, [offline, todosQuery.data]);

  useEffect(() => {
    if (!offline) {
      return;
    }
    let cancelled = false;
    void listCachedPersonalTodos(offline.db, offline.scopeBinding).then((rows) => {
      if (!cancelled) {
        setCachedTodos(rows);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [offline, todosQuery.dataUpdatedAt]);

  const usingCache = !online || todosQuery.isError;

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!online) {
        throw new Error("online-required");
      }
      await createPersonalTodo(todoFormToRequestBody(form));
    },
    onSuccess: async () => {
      setForm(emptyTodoForm());
      setFormError(null);
      setCreateFormOpen(false);
      setCreateAdvancedOpen(false);
      setConflictBanner(false);
      await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setFormError(t("offline.requiredPersonalTodo"));
        return;
      }
      if (isTodoConcurrencyConflict(error)) {
        setConflictBanner(true);
      }
      setFormError(mutationErrorMessage(error, t));
    },
  });

  const actionMutation = useMutation({
    mutationFn: async ({
      action,
      todo,
    }: {
      action: PersonalTodoTransition;
      todo: PersonalTodoDto;
    }) => {
      if (!online) {
        throw new Error("online-required");
      }
      const body = { expectedVersion: todo.version };
      if (action === "complete") {
        await completePersonalTodo(todo.id, body);
        return;
      }
      if (action === "reopen") {
        await reopenPersonalTodo(todo.id, body);
        return;
      }
      await cancelPersonalTodo(todo.id, body);
    },
    onMutate: (variables) => {
      setActiveTodoId(variables.todo.id);
      if (
        variables.action === "reopen" &&
        variables.todo.status === "Cancelled" &&
        tab === "cancelled"
      ) {
        setExitingIds((prev) => new Set(prev).add(variables.todo.id));
      }
      if (variables.action === "complete" || variables.action === "cancel") {
        setExitingIds((prev) => new Set(prev).add(variables.todo.id));
      }
    },
    onSuccess: async (_data, variables) => {
      const delay =
        variables.action === "reopen" && variables.todo.status === "Cancelled" ? 280 : 180;
      await new Promise((resolve) => window.setTimeout(resolve, delay));
      await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
      if (variables.action === "cancel") {
        setTab("cancelled");
      } else if (variables.action === "reopen") {
        setTab("open");
      }
    },
    onSettled: (_data, _error, variables) => {
      setActiveTodoId(null);
      if (variables) {
        window.setTimeout(() => {
          setExitingIds((prev) => {
            const next = new Set(prev);
            next.delete(variables.todo.id);
            return next;
          });
        }, 320);
      }
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setFormError(t("offline.requiredPersonalTodo"));
        return;
      }
      if (isTodoConcurrencyConflict(error)) {
        setConflictBanner(true);
      }
      setFormError(mutationErrorMessage(error, t));
    },
  });

  const todos: CachedPersonalTodo[] | PersonalTodoDto[] = usingCache
    ? cachedTodos.length > 0
      ? cachedTodos
      : (todosQuery.data ?? [])
    : (todosQuery.data ?? []);

  const filtered = useMemo(
    () => filterAndSortTodosForTab([...todos], tab, { search: searchQuery }),
    [todos, tab, searchQuery],
  );
  const emptyState = todoEmptyStateKeys(tab, searchQuery.trim().length > 0);
  const counts = useMemo(() => summarizeTodoCounts([...todos]), [todos]);
  const pendingById = useMemo(
    () => new Set(cachedTodos.filter((row) => row.pendingLocalChange).map((row) => row.id)),
    [cachedTodos],
  );

  if (online && todosQuery.isPending) {
    return <LoadingSkeleton label={t("personal.todo.loading")} />;
  }
  const activeTabLabel = t(TABS.find((item) => item.id === tab)?.labelKey ?? "personal.todo.title");
  const offlineBlocked = !online;

  if (online && todosQuery.isError && cachedTodos.length === 0) {
    return (
      <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-todo-hub-error">
        <PageHeader
          title={t("personal.todo.title")}
          description={t("personal.todo.lede")}
          backTo={personalPageBackNav.home.to}
          backLabel={t(personalPageBackNav.home.labelKey)}
          backTestId="page-header-back-todo-hub"
        />
        <ErrorState
          title={t("personal.todo.loadErrorTitle")}
          detail={loadErrorDetail(todosQuery.error, t)}
          error={todosQuery.error}
          operation="list personal todos"
        />
        <div className="exits-animate-toolbar flex w-full justify-center">
          <Button
            type="button"
            className="personal-error-retry min-h-11 w-full"
            onClick={() => void todosQuery.refetch()}
            data-testid="todo-hub-retry"
          >
            <RefreshCw className="size-4 shrink-0" aria-hidden />
            {t("personal.home.retry")}
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div
      className="personal-page personal-todo-hub exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-todo-hub"
    >
      <PageHeader
        title={t("personal.todo.title")}
        description={t("personal.todo.lede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-todo-hub"
      />

      {usingCache ? <OfflineNotice message={t("offline.todoCachedNotice")} /> : null}

      {conflictBanner ? (
        <TodoConflictBanner
          onReload={() => {
            setConflictBanner(false);
            void queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
          }}
        />
      ) : null}

      <div className="exits-animate-toolbar">
        <UnderlineTabBar
          items={TABS.map((item) => {
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
                      : item.id === "completed"
                        ? counts.completed
                        : counts.cancelled;
            return {
              key: item.id,
              label: `${t(item.labelKey)} (${count})`,
              testId: `todo-tab-${item.id}`,
            };
          })}
          activeKey={tab}
          onChange={(key) => changeTab(key as TodoAgendaTab)}
          ariaLabel={t("personal.todo.filters")}
          testId="personal-todo-filters"
        />
      </div>

      <label className="personal-todo-search exits-animate-toolbar flex flex-col gap-1">
        <span className="sr-only">{t("personal.todo.searchLabel")}</span>
        <span className="personal-todo-search__field">
          <Search className="personal-todo-search__icon size-4 shrink-0" aria-hidden />
          <input
            type="search"
            className="personal-todo-search__input"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder={t("personal.todo.searchPlaceholder")}
            data-testid="todo-search"
          />
        </span>
      </label>

      <section
        className="personal-todo-create-shell catalog-form-section exits-animate-panel personal-section"
        data-testid="todo-create-shell"
      >
        <button
          type="button"
          className={cn(
            "personal-todo-create-toggle",
            createFormOpen && "personal-todo-create-toggle--open",
          )}
          data-testid="todo-create-toggle"
          aria-expanded={createFormOpen}
          aria-controls="todo-create-panel"
          onClick={() => setCreateFormOpen((open) => !open)}
        >
          <span className="personal-todo-create-toggle__lead">
            <ListPlus
              className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0"
              aria-hidden
            />
            <span className="personal-todo-create-toggle__label">{t("personal.todo.createTitle")}</span>
          </span>
          <ChevronDown
            className={cn(
              "personal-todo-create-toggle__chevron size-4 shrink-0",
              createFormOpen && "personal-todo-create-toggle__chevron--open",
            )}
            aria-hidden
          />
        </button>

        <div
          id="todo-create-panel"
          className={cn(
            "personal-todo-create-collapse",
            createFormOpen && "personal-todo-create-collapse--open",
          )}
          aria-hidden={!createFormOpen}
        >
          <div className="personal-todo-create-collapse__inner">
            <form
              className="personal-todo-create-form flex flex-col gap-2"
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
              <div className="personal-todo-quick-add flex flex-col gap-2 sm:flex-row sm:items-end">
                <label
                  className="flex min-w-0 flex-1 flex-col gap-1 text-[length:var(--exits-text-sm)]"
                  htmlFor="todo-create-title"
                >
                  {t("personal.todo.titleField")}
                  <input
                    id="todo-create-title"
                    data-testid="todo-create-title"
                    className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                    value={form.title}
                    onChange={(event) => setForm({ ...form, title: event.target.value })}
                    required
                  />
                </label>
                <Button
                  type="submit"
                  className="personal-todo-submit min-h-11 w-full sm:w-auto"
                  disabled={createMutation.isPending || offlineBlocked}
                  data-testid="todo-create-submit"
                  tabIndex={createFormOpen ? undefined : -1}
                >
                  <TodoActionIcon pending={createMutation.isPending}>
                    <ListPlus className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.add")}
                </Button>
              </div>

              <button
                type="button"
                className="personal-todo-advanced-toggle"
                data-testid="todo-create-advanced-toggle"
                aria-expanded={createAdvancedOpen}
                onClick={() => setCreateAdvancedOpen((open) => !open)}
              >
                <SlidersHorizontal className="size-4 shrink-0" aria-hidden />
                {createAdvancedOpen
                  ? t("personal.todo.hideMoreOptions")
                  : t("personal.todo.moreOptions")}
              </button>

              {createAdvancedOpen ? (
                <TodoFormFields
                  form={form}
                  setForm={setForm}
                  idPrefix="todo-create-advanced"
                  includeTitle={false}
                />
              ) : null}

              {!online ? (
                <>
                  <OfflineNotice message={t("offline.requiredPersonalTodo")} />
                  {form.reminderAtLocal ? (
                    <OfflineNotice message={t("offline.todoNoReminders")} />
                  ) : null}
                </>
              ) : null}
              {formError ? (
                <p
                  role="alert"
                  className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
                >
                  {formError}
                </p>
              ) : null}
            </form>
          </div>
        </div>
      </section>

      {filtered.length === 0 ? (
        <EmptyState title={t(emptyState.titleKey)} detail={t(emptyState.detailKey)} />
      ) : (
        <section
          className="personal-todo-list-section catalog-form-section exits-animate-panel personal-section gap-2"
          aria-label={activeTabLabel}
        >
          <h2 className="catalog-form-section__title text-muted">{activeTabLabel}</h2>
          <ul className="exits-list personal-todo-list m-0 grid list-none gap-2 p-0" data-testid="todo-list">
            {filtered.map((item) => {
              const hasActions =
                item.status === "Open" ||
                item.status === "Completed" ||
                item.status === "Cancelled";
              const isActing = actionMutation.isPending && activeTodoId === item.id;
              const isExiting = exitingIds.has(item.id);
              return (
                <li
                  key={item.id}
                  className={cn(isExiting && "personal-todo-list__item--exit")}
                >
                  <div
                    className={cn(
                      "exits-list__card personal-todo-row",
                      item.priority === "High" && "personal-todo-row--priority-high",
                      isExiting && "personal-todo-row--exit",
                    )}
                    data-testid={`todo-item-${item.id}`}
                  >
                    <div className="personal-todo-row__body">
                      <div className="personal-todo-row__lead">
                        {item.status === "Open" ? (
                          <button
                            type="button"
                            className="personal-todo-row__check"
                            data-testid={`todo-check-${item.id}`}
                            aria-label={t("personal.todo.complete")}
                            disabled={actionMutation.isPending || offlineBlocked}
                            onClick={() =>
                              actionMutation.mutate({ action: "complete", todo: item })
                            }
                          >
                            {isActing ? (
                              <Loader2 className="size-4 animate-spin" aria-hidden />
                            ) : (
                              <span className="personal-todo-row__check-box" aria-hidden />
                            )}
                          </button>
                        ) : (
                          <span
                            className={cn(
                              "personal-todo-row__check personal-todo-row__check--done",
                              item.status === "Completed" && "personal-todo-row__check--completed",
                            )}
                            aria-hidden
                          />
                        )}
                        <Link
                          to={`/personal/todo/${item.id}`}
                          className="personal-todo-row__content min-w-0 flex-1 text-foreground no-underline"
                        >
                          <p className="exits-list__name m-0 truncate font-semibold">{item.title}</p>
                          <TodoMetaLine todo={item} />
                          {item.notes ? (
                            <p className="m-0 mt-1 line-clamp-2 text-[length:var(--exits-text-sm)] text-muted">
                              {item.notes}
                            </p>
                          ) : null}
                        </Link>
                      </div>
                      <WaitingChip pending={pendingById.has(item.id)} />
                      {hasActions ? (
                        <div
                          className={cn(
                            "personal-todo-row__actions",
                            item.status === "Open" && "personal-todo-row__actions--open",
                            item.status === "Completed" && "personal-todo-row__actions--completed",
                            item.status === "Cancelled" && "personal-todo-row__actions--solo",
                          )}
                        >
                          {item.status === "Open" ? (
                            <>
                              <Button
                                type="button"
                                className="personal-todo-row__action min-h-11"
                                data-testid={`todo-complete-${item.id}`}
                                disabled={actionMutation.isPending || offlineBlocked}
                                onClick={() =>
                                  actionMutation.mutate({ action: "complete", todo: item })
                                }
                              >
                                <TodoActionIcon pending={isActing}>
                                  <Check className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                                </TodoActionIcon>
                                {t("personal.todo.complete")}
                              </Button>
                              <Button
                                asChild
                                variant="outline"
                                className="personal-todo-row__action min-h-11"
                                data-testid={`todo-edit-${item.id}`}
                              >
                                <Link to={`/personal/todo/${item.id}?edit=1`}>
                                  <Pencil className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                                  {t("personal.todo.edit")}
                                </Link>
                              </Button>
                              <Button
                                type="button"
                                variant="ghost"
                                className="personal-todo-row__action min-h-11"
                                data-testid={`todo-cancel-${item.id}`}
                                disabled={actionMutation.isPending || offlineBlocked}
                                onClick={() =>
                                  actionMutation.mutate({ action: "cancel", todo: item })
                                }
                              >
                                <TodoActionIcon pending={isActing}>
                                  <X className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                                </TodoActionIcon>
                                {t("personal.todo.cancel")}
                              </Button>
                            </>
                          ) : null}
                          {item.status === "Completed" ? (
                            <>
                              <Button
                                type="button"
                                className="personal-todo-row__action min-h-11"
                                data-testid={`todo-reopen-${item.id}`}
                                disabled={actionMutation.isPending || offlineBlocked}
                                onClick={() => actionMutation.mutate({ action: "reopen", todo: item })}
                              >
                                <TodoActionIcon pending={isActing}>
                                  <RotateCcw className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                                </TodoActionIcon>
                                {t("personal.todo.reopen")}
                              </Button>
                              <Button
                                type="button"
                                variant="ghost"
                                className="personal-todo-row__action min-h-11"
                                data-testid={`todo-cancel-${item.id}`}
                                disabled={actionMutation.isPending || offlineBlocked}
                                onClick={() =>
                                  actionMutation.mutate({ action: "cancel", todo: item })
                                }
                              >
                                <TodoActionIcon pending={isActing}>
                                  <X className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                                </TodoActionIcon>
                                {t("personal.todo.cancel")}
                              </Button>
                            </>
                          ) : null}
                          {item.status === "Cancelled" ? (
                            <Button
                              type="button"
                              className="personal-todo-reactivate personal-todo-row__action min-h-11"
                              data-testid={`todo-reactivate-${item.id}`}
                              disabled={actionMutation.isPending || offlineBlocked}
                              onClick={() => actionMutation.mutate({ action: "reopen", todo: item })}
                            >
                              <TodoActionIcon pending={isActing}>
                                <RotateCcw className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                              </TodoActionIcon>
                              {t("personal.todo.reactivate")}
                            </Button>
                          ) : null}
                        </div>
                      ) : null}
                    </div>
                    <Link
                      to={`/personal/todo/${item.id}`}
                      className="personal-todo-row__nav"
                      aria-label={`${t("personal.todo.detailTitle")}: ${item.title}`}
                    >
                      <ChevronRight className="personal-todo-row__chevron size-4 shrink-0" aria-hidden />
                    </Link>
                  </div>
                </li>
              );
            })}
          </ul>
        </section>
      )}
    </div>
  );
}

export function PersonalTodoDetailPage() {
  const { t } = useI18n();
  const { todoId = "" } = useParams();
  const [searchParams] = useSearchParams();
  const wantsEdit = searchParams.get("edit") === "1";
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const editFormRef = useRef<HTMLFormElement>(null);
  const [form, setForm] = useState<TodoFormState | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [conflictBanner, setConflictBanner] = useState(false);
  const [editing, setEditing] = useState(false);
  const [cachedTodo, setCachedTodo] = useState<CachedPersonalTodo | null>(null);

  const todoQuery = useQuery({
    queryKey: ["personal", "todos", todoId],
    queryFn: ({ signal }) => getPersonalTodo(todoId, signal),
    enabled: Boolean(todoId) && online,
    meta: { suppressGlobalError: true, operation: "get personal todo" },
  });

  useEffect(() => {
    if (!editing) {
      return;
    }
    editFormRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [editing]);

  useEffect(() => {
    if (!offline || !todoQuery.data) {
      return;
    }
    void cachePersonalTodo(offline.db, offline.scopeBinding, todoQuery.data);
  }, [offline, todoQuery.data]);

  useEffect(() => {
    if (!offline || !todoId) {
      return;
    }
    let cancelled = false;
    void getCachedPersonalTodo(offline.db, offline.scopeBinding, todoId).then((row) => {
      if (!cancelled) {
        setCachedTodo(row);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [offline, todoId, todoQuery.dataUpdatedAt]);

  const usingCache = !online || todoQuery.isError;

  useEffect(() => {
    if (!wantsEdit || editing) {
      return;
    }
    const todo = usingCache
      ? cachedTodo ?? todoQuery.data ?? null
      : (todoQuery.data ?? null);
    if (!todo || todo.status !== "Open") {
      return;
    }
    setForm(todoFormFromDto(todo));
    setEditing(true);
  }, [cachedTodo, editing, todoQuery.data, usingCache, wantsEdit]);

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
    await queryClient.invalidateQueries({ queryKey: ["personal", "todos", todoId] });
  };

  const saveMutation = useMutation({
    mutationFn: async ({ current, next }: { current: PersonalTodoDto; next: TodoFormState }) => {
      if (!online) {
        throw new Error("online-required");
      }
      await updatePersonalTodo(current.id, {
        ...todoFormToRequestBody(next),
        expectedVersion: current.version,
      });
    },
    onSuccess: async () => {
      setEditing(false);
      setForm(null);
      setFormError(null);
      setConflictBanner(false);
      await invalidate();
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setFormError(t("offline.requiredPersonalTodo"));
        return;
      }
      if (isTodoConcurrencyConflict(error)) {
        setConflictBanner(true);
      }
      setFormError(mutationErrorMessage(error, t));
    },
  });

  const actionMutation = useMutation({
    mutationFn: async ({
      action,
      todo,
    }: {
      action: PersonalTodoTransition;
      todo: PersonalTodoDto;
    }) => {
      if (!online) {
        throw new Error("online-required");
      }
      const body = { expectedVersion: todo.version };
      if (action === "complete") {
        await completePersonalTodo(todo.id, body);
        return;
      }
      if (action === "reopen") {
        await reopenPersonalTodo(todo.id, body);
        return;
      }
      await cancelPersonalTodo(todo.id, body);
    },
    onSuccess: async () => {
      await invalidate();
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "online-required") {
        setFormError(t("offline.requiredPersonalTodo"));
        return;
      }
      if (isTodoConcurrencyConflict(error)) {
        setConflictBanner(true);
      }
      setFormError(mutationErrorMessage(error, t));
    },
  });

  if (online && todoQuery.isPending) return <LoadingSkeleton label={t("personal.todo.loading")} />;

  const todo: PersonalTodoDto | null = usingCache
    ? cachedTodo ?? todoQuery.data ?? null
    : (todoQuery.data ?? null);
  if (!todo) {
    return (
      <div className="personal-page exits-page flex flex-col gap-3">
        <PageHeader
          title={t("personal.todo.detailTitle")}
          backTo={personalPageBackNav.todo.to}
          backLabel={t("personal.todo.back")}
          backTestId="page-header-back-todo-detail"
        />
        <ErrorState
          title={t("personal.todo.loadErrorTitle")}
          detail={
            usingCache && !todoQuery.data
              ? t("offline.todoNotCached")
              : loadErrorDetail(todoQuery.error, t)
          }
          error={usingCache ? undefined : todoQuery.error}
          operation="get personal todo"
        />
      </div>
    );
  }

  const activeForm = form ?? todoFormFromDto(todo);
  const offlineBlocked = !online;

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-todo-detail">
      <PageHeader
        title={t("personal.todo.detailTitle")}
        subtitle={todo.title}
        description={t("personal.todo.lede")}
        backTo={personalPageBackNav.todo.to}
        backLabel={t("personal.todo.back")}
        backTestId="page-header-back-todo-detail"
      />

      {usingCache ? <OfflineNotice message={t("offline.todoCachedNotice")} /> : null}

      {conflictBanner ? (
        <TodoConflictBanner
          onReload={() => {
            setConflictBanner(false);
            void invalidate();
          }}
        />
      ) : null}

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("personal.todo.status")}: {t(statusLabelKey(todo.status))} ·{" "}
        {t(priorityLabelKey(todo.priority))}
      </p>
      <WaitingChip pending={cachedTodo?.pendingLocalChange === true} />

      {editing ? (
        <form
          ref={editFormRef}
          className={cn(
            "catalog-form-section exits-animate-panel personal-section flex flex-col gap-2",
            "catalog-form-section--editing",
          )}
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
          <h2 className="catalog-form-section__title">{t("personal.todo.edit")}</h2>
          <TodoFormFields
            form={activeForm}
            setForm={(next) => {
              setForm(next);
            }}
            idPrefix="todo-edit"
            titleAutoFocus
          />
          {!online ? <OfflineNotice message={t("offline.requiredPersonalTodo")} /> : null}
          {formError ? (
            <p
              role="alert"
              className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            >
              {formError}
            </p>
          ) : null}
          <div className="personal-todo-edit-form__actions">
            <Button
              type="submit"
              className="personal-todo-edit-form__action min-h-11"
              disabled={saveMutation.isPending || offlineBlocked}
              data-testid="todo-edit-save"
            >
              <TodoActionIcon pending={saveMutation.isPending}>
                <Save className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
              </TodoActionIcon>
              {t("personal.todo.save")}
            </Button>
            <Button
              type="button"
              variant="outline"
              className="personal-todo-edit-form__action min-h-11"
              data-testid="todo-edit-cancel"
              onClick={() => {
                setEditing(false);
                setForm(null);
                setFormError(null);
              }}
            >
              <X className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
              {t("personal.todo.cancel")}
            </Button>
          </div>
        </form>
      ) : (
        <section className="catalog-form-section exits-animate-panel personal-section flex flex-col gap-2">
          <h2 className="catalog-form-section__title text-muted">{t("personal.todo.detailTitle")}</h2>
          {todo.notes ? <p className="m-0 whitespace-pre-wrap">{todo.notes}</p> : null}
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {todo.dueAtUtc
              ? `${t("personal.todo.dueLabel")}: ${new Date(todo.dueAtUtc).toLocaleString()}`
              : t("personal.todo.noDue")}
          </p>
          {todo.reminderAtUtc ? (
            <>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.todo.reminderAt")}: {new Date(todo.reminderAtUtc).toLocaleString()}
                {" · "}
                {todo.reminderNotifiedAtUtc
                  ? t("personal.todo.reminderDelivered")
                  : t("personal.todo.reminderPending")}
              </p>
              {online ? (
                <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                  {t("personal.todo.reminderServerHint")}
                </p>
              ) : (
                <OfflineNotice message={t("offline.todoNoReminders")} />
              )}
            </>
          ) : null}
          {todo.relatedEntityType && todo.relatedEntityId ? (
            <TodoRelatedEntityLink
              relatedEntityType={todo.relatedEntityType}
              relatedEntityId={todo.relatedEntityId}
              label={todo.relatedEntityId}
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
            className={cn(
              "personal-todo-row__actions",
              todo.status === "Open" && "personal-todo-row__actions--open",
              todo.status === "Completed" && "personal-todo-row__actions--completed",
              todo.status === "Cancelled" && "personal-todo-row__actions--solo",
            )}
          >
            {todo.status === "Open" ? (
              <>
                <Button
                  type="button"
                  className="personal-todo-row__action min-h-11"
                  data-testid="todo-detail-complete"
                  disabled={actionMutation.isPending || offlineBlocked}
                  onClick={() => actionMutation.mutate({ action: "complete", todo })}
                >
                  <TodoActionIcon pending={actionMutation.isPending}>
                    <Check className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.complete")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="personal-todo-row__action min-h-11"
                  data-testid="todo-detail-edit"
                  onClick={() => {
                    setForm(todoFormFromDto(todo));
                    setEditing(true);
                  }}
                >
                  <Pencil className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  {t("personal.todo.edit")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="personal-todo-row__action min-h-11"
                  data-testid="todo-detail-cancel"
                  disabled={actionMutation.isPending || offlineBlocked}
                  onClick={() => actionMutation.mutate({ action: "cancel", todo })}
                >
                  <TodoActionIcon pending={actionMutation.isPending}>
                    <X className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.cancel")}
                </Button>
              </>
            ) : null}
            {todo.status === "Completed" ? (
              <>
                <Button
                  type="button"
                  className="personal-todo-row__action min-h-11"
                  data-testid="todo-detail-reopen"
                  disabled={actionMutation.isPending || offlineBlocked}
                  onClick={() => actionMutation.mutate({ action: "reopen", todo })}
                >
                  <TodoActionIcon pending={actionMutation.isPending}>
                    <RotateCcw className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.reopen")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="personal-todo-row__action min-h-11"
                  data-testid="todo-detail-cancel-completed"
                  disabled={actionMutation.isPending || offlineBlocked}
                  onClick={() => actionMutation.mutate({ action: "cancel", todo })}
                >
                  <TodoActionIcon pending={actionMutation.isPending}>
                    <X className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.cancel")}
                </Button>
              </>
            ) : null}
            {todo.status === "Cancelled" ? (
              <Button
                type="button"
                className="personal-todo-reactivate personal-todo-row__action min-h-11"
                data-testid="todo-detail-reactivate"
                disabled={actionMutation.isPending || offlineBlocked}
                onClick={() => actionMutation.mutate({ action: "reopen", todo })}
              >
                <TodoActionIcon pending={actionMutation.isPending}>
                  <RotateCcw className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                </TodoActionIcon>
                {t("personal.todo.reactivate")}
              </Button>
            ) : null}
          </div>
        </section>
      )}
    </div>
  );
}
