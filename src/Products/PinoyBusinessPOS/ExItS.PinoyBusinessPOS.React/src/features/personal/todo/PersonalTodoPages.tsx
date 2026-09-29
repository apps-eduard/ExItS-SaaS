import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, ListPlus, Loader2, Pencil, Plus, RefreshCw, RotateCcw, Save, Search, SlidersHorizontal, Users, X } from "lucide-react";
import {
  cancelPersonalTodo,
  completePersonalTodo,
  createPersonalTodo,
  buildPersonalTodoListGroups,
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
import {
  PersonalTodoFilterRail,
  PersonalTodoTasklistGroup,
  PersonalTodoTaskRow,
  PERSONAL_TODO_FILTER_TABS,
} from "@/features/personal/todo/PersonalTodoTasklistUi";
import { SHELL_DESKTOP_MIN_PX } from "@/features/shell/shell-breakpoints";
import { useMediaMin } from "@/hooks/useMediaQuery";
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

const TABS = PERSONAL_TODO_FILTER_TABS;

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
      <Button type="button" className="w-full sm:w-auto" onClick={onReload}>
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
  const isDesktop = useMediaMin(SHELL_DESKTOP_MIN_PX);

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

  const listGroups = useMemo(
    () => buildPersonalTodoListGroups([...todos], tab, { search: searchQuery }),
    [todos, tab, searchQuery],
  );
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
            className="personal-error-retry w-full"
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

  function renderTodoRow(item: PersonalTodoDto) {
    const isActing = actionMutation.isPending && activeTodoId === item.id;
    const isExiting = exitingIds.has(item.id);
    return (
      <PersonalTodoTaskRow
        key={item.id}
        item={item}
        isActing={isActing}
        isExiting={isExiting}
        pendingLocal={pendingById.has(item.id)}
        offlineBlocked={offlineBlocked}
        peekMode={false}
        selected={false}
        onSelect={() => undefined}
        onComplete={() => actionMutation.mutate({ action: "complete", todo: item })}
        onCancel={() => actionMutation.mutate({ action: "cancel", todo: item })}
        onReopen={() => actionMutation.mutate({ action: "reopen", todo: item })}
      />
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

      <div
        className={cn(
          "personal-todo-tasklist-layout",
          isDesktop && "personal-todo-tasklist-layout--desktop",
        )}
        data-testid="personal-todo-tasklist-layout"
      >
        <aside
          className="personal-todo-tasklist-rail catalog-form-section exits-animate-panel"
          data-testid="personal-todo-tasklist-rail"
        >
          <Button
            type="button"
            appearance="outline"
            className="personal-todo-tasklist-rail__new w-full"
            data-testid="todo-new-task"
            onClick={() => {
              setCreateFormOpen(true);
              setCreateAdvancedOpen(false);
            }}
          >
            <Plus className="size-4 shrink-0" aria-hidden />
            {t("personal.todo.newTask")}
          </Button>
          <PersonalTodoFilterRail activeTab={tab} counts={counts} onChange={changeTab} />
        </aside>

        <section
          className="personal-todo-tasklist-card catalog-form-section exits-animate-panel"
          data-testid="personal-todo-tasklist-card"
          aria-label={t("personal.todo.cardTitle")}
        >
          {!isDesktop ? (
            <header className="personal-todo-tasklist-card__header">
              <h2 className="personal-todo-tasklist-card__title m-0">{t("personal.todo.cardTitle")}</h2>
              <Button
                type="button"
                appearance="outline"
                className="personal-todo-tasklist-card__new"
                data-testid="todo-new-task-mobile"
                onClick={() => setCreateFormOpen(true)}
              >
                <Plus className="size-4 shrink-0" aria-hidden />
                {t("personal.todo.newTask")}
              </Button>
            </header>
          ) : null}

          {!isDesktop ? (
            <div className="personal-todo-tasklist-card__filters exits-animate-toolbar">
              <UnderlineTabBar
                className="personal-todo-tasklist-filters"
                items={TABS.map((item) => {
                  const count = counts == null ? null : counts[item.id];
                  return {
                    key: item.id,
                    label: t(item.labelKey),
                    count,
                    testId: `todo-mobile-tab-${item.id}`,
                  };
                })}
                activeKey={tab}
                onChange={(key) => changeTab(key as TodoAgendaTab)}
                ariaLabel={t("personal.todo.filters")}
                testId="personal-todo-mobile-filters"
              />
            </div>
          ) : null}

          <label className="personal-todo-search personal-todo-tasklist-card__search flex flex-col gap-1">
            <span className="sr-only">{t("personal.todo.searchLabel")}</span>
            <span className="personal-todo-search__field personal-todo-tasklist-card__search-field">
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

          {createFormOpen ? (
            <section
              className="personal-todo-create-shell personal-todo-tasklist-card__create"
              data-testid="todo-create-shell"
            >
              <div className="personal-todo-create-toggle personal-todo-create-toggle--open">
                <span className="personal-todo-create-toggle__lead">
                  <ListPlus
                    className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0"
                    aria-hidden
                  />
                  <span className="personal-todo-create-toggle__label">
                    {t("personal.todo.createTitle")}
                  </span>
                </span>
                <Button
                  type="button"
                  appearance="ghost"
                  size="icon"
                  aria-label={t("personal.todo.closeDetail")}
                  data-testid="todo-create-close"
                  onClick={() => {
                    setCreateFormOpen(false);
                    setCreateAdvancedOpen(false);
                    setFormError(null);
                  }}
                >
                  <X className="size-4" aria-hidden />
                </Button>
              </div>
              <div className="personal-todo-create-collapse personal-todo-create-collapse--open">
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
                          className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                          value={form.title}
                          onChange={(event) => setForm({ ...form, title: event.target.value })}
                          required
                        />
                      </label>
                      <Button
                        type="submit"
                        className="personal-todo-submit w-full sm:w-auto"
                        disabled={createMutation.isPending || offlineBlocked}
                        data-testid="todo-create-submit"
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
          ) : null}

          {filtered.length === 0 ? (
            <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
              title={t(emptyState.titleKey)}
              detail={t(emptyState.detailKey)}
            />
          ) : (
            <div className="personal-todo-tasklist-card__groups" data-testid="todo-list">
              {listGroups.map((group) => (
                <PersonalTodoTasklistGroup key={group.id} group={group}>
                  {group.items.map((item) => renderTodoRow(item))}
                </PersonalTodoTasklistGroup>
              ))}
            </div>
          )}
        </section>
      </div>
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
              className="personal-todo-edit-form__action"
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
              className="personal-todo-edit-form__action"
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
                  className="personal-todo-row__action"
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
                  className="personal-todo-row__action"
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
                  className="personal-todo-row__action"
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
                  className="personal-todo-row__action"
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
                  className="personal-todo-row__action"
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
                className="personal-todo-reactivate personal-todo-row__action"
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
