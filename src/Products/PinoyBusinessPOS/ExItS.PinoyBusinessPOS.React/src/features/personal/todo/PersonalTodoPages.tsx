import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  Check,
  FoldVertical,
  Loader2,
  Pencil,
  Plus,
  RefreshCw,
  RotateCcw,
  Save,
  Search,
  Trash2,
  UnfoldVertical,
  Users,
  X,
} from "lucide-react";
import {
  cancelPersonalTodo,
  completePersonalTodo,
  createPersonalTodo,
  deletePersonalTodo,
  filterAndSortTodosForTab,
  getPersonalTodo,
  isTodoConcurrencyConflict,
  listPersonalTodos,
  parseTodoAgendaTab,
  priorityTextToneClass,
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
import { FormDrawer } from "@/components/exits/FormDrawer";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import {
  PersonalTodoFilterRail,
  PersonalTodoTaskRow,
  type PersonalTodoExpandAllCommand,
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
  removeCachedPersonalTodo,
  type CachedPersonalTodo,
} from "@/offline/personal-todo-cache";
import { type PersonalTodoTransition } from "@/offline/personal-todo-offline";

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
  const [searchQuery, setSearchQuery] = useState("");
  const [allRowsExpanded, setAllRowsExpanded] = useState(true);
  const [expandAllCommand, setExpandAllCommand] = useState<PersonalTodoExpandAllCommand | null>(
    null,
  );
  const [conflictBanner, setConflictBanner] = useState(false);
  const [form, setForm] = useState<TodoFormState>(emptyTodoForm);
  const [formError, setFormError] = useState<string | null>(null);
  const [cachedTodos, setCachedTodos] = useState<CachedPersonalTodo[]>([]);
  const [exitingIds, setExitingIds] = useState<Set<string>>(() => new Set());
  const [activeTodoId, setActiveTodoId] = useState<string | null>(null);
  const [pendingDelete, setPendingDelete] = useState<PersonalTodoDto | null>(null);
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

  function closeCreateDrawer() {
    setCreateFormOpen(false);
    setFormError(null);
    setForm(emptyTodoForm());
  }

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!online) {
        throw new Error("online-required");
      }
      await createPersonalTodo(todoFormToRequestBody(form));
    },
    onSuccess: async () => {
      closeCreateDrawer();
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

  const deleteMutation = useMutation({
    mutationFn: async (todo: PersonalTodoDto) => {
      if (!online) {
        throw new Error("online-required");
      }
      await deletePersonalTodo(todo.id, { expectedVersion: todo.version });
    },
    onMutate: (todo) => {
      setActiveTodoId(todo.id);
      setExitingIds((prev) => new Set(prev).add(todo.id));
    },
    onSuccess: async (_data, todo) => {
      setPendingDelete(null);
      if (offline) {
        await removeCachedPersonalTodo(offline.db, todo.id);
        setCachedTodos((prev) => prev.filter((row) => row.id !== todo.id));
      }
      await new Promise((resolve) => window.setTimeout(resolve, 180));
      await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
    },
    onSettled: (_data, _error, todo) => {
      setActiveTodoId(null);
      if (todo) {
        window.setTimeout(() => {
          setExitingIds((prev) => {
            const next = new Set(prev);
            next.delete(todo.id);
            return next;
          });
        }, 320);
      }
    },
    onError: (error) => {
      setExitingIds((prev) => {
        if (!pendingDelete) return prev;
        const next = new Set(prev);
        next.delete(pendingDelete.id);
        return next;
      });
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

  function toggleAllRowsExpanded() {
    const nextExpanded = !allRowsExpanded;
    setAllRowsExpanded(nextExpanded);
    setExpandAllCommand({ expanded: nextExpanded, token: Date.now() });
  }

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
    const isActing =
      (actionMutation.isPending || deleteMutation.isPending) && activeTodoId === item.id;
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
        expandAllCommand={expandAllCommand}
        onExpandedChange={(expanded) => {
          if (!expanded) {
            setAllRowsExpanded(false);
          }
        }}
        onSelect={() => undefined}
        onComplete={() => actionMutation.mutate({ action: "complete", todo: item })}
        onCancel={() => actionMutation.mutate({ action: "cancel", todo: item })}
        onReopen={() => actionMutation.mutate({ action: "reopen", todo: item })}
        onDelete={() => setPendingDelete(item)}
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
            data-testid="todo-new-todo"
            onClick={() => setCreateFormOpen(true)}
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
          <div className="personal-todo-tasklist-card__search-row">
            <label className="personal-todo-search personal-todo-tasklist-card__search min-w-0 flex-1">
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
            <Button
              type="button"
              intent="info"
              appearance="solid"
              emphasis="soft"
              size="icon"
              className="personal-todo-tasklist-card__expand-toggle"
              data-testid="todo-toggle-all-rows"
              disabled={filtered.length === 0}
              aria-label={
                allRowsExpanded ? t("uiStandards.collapseAll") : t("uiStandards.expandAll")
              }
              aria-pressed={allRowsExpanded}
              title={
                allRowsExpanded ? t("uiStandards.collapseAll") : t("uiStandards.expandAll")
              }
              onClick={toggleAllRowsExpanded}
            >
              {allRowsExpanded ? (
                <FoldVertical className="size-4" aria-hidden />
              ) : (
                <UnfoldVertical className="size-4" aria-hidden />
              )}
            </Button>
          </div>

          {filtered.length === 0 ? (
            <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
              title={t(emptyState.titleKey)}
              detail={t(emptyState.detailKey)}
            />
          ) : (
            <ul className="personal-todo-tasklist-card__groups m-0 list-none p-0" data-testid="todo-list">
              {filtered.map((item) => renderTodoRow(item))}
            </ul>
          )}
        </section>
      </div>

      <FormDrawer
        open={createFormOpen}
        onOpenChange={(next) => {
          if (!next) {
            closeCreateDrawer();
          } else {
            setCreateFormOpen(true);
          }
        }}
        title={t("personal.todo.createTitle")}
        description={t("personal.todo.lede")}
        testId="todo-create-drawer"
        saveTestId="todo-create-submit"
        cancelTestId="todo-create-close"
        closeLabel={t("personal.todo.closeDetail")}
        cancelLabel={t("personal.todo.closeDetail")}
        saveLabel={t("personal.todo.add")}
        saving={createMutation.isPending}
        saveDisabled={offlineBlocked || !form.title.trim()}
        onSave={() => {
          if (!form.title.trim()) {
            setFormError(t("personal.todo.titleRequired"));
            return;
          }
          createMutation.mutate();
        }}
        size="md"
        dirty={Boolean(form.title.trim() || form.notes.trim() || form.dueAtLocal || form.reminderAtLocal)}
        confirmUnsavedOnClose
      >
        {formError ? (
          <Notice tone="danger" className="mb-3" testId="todo-create-error">
            {formError}
          </Notice>
        ) : null}
        {!online ? (
          <div className="mb-3 flex flex-col gap-2">
            <OfflineNotice message={t("offline.requiredPersonalTodo")} />
            {form.reminderAtLocal ? (
              <OfflineNotice message={t("offline.todoNoReminders")} />
            ) : null}
          </div>
        ) : null}
        <div className="personal-todo-create-form flex flex-col gap-3" data-testid="todo-create-form">
          <TodoFormFields
            form={form}
            setForm={setForm}
            idPrefix="todo-create"
            titleAutoFocus
            showAdvanced
            includeTitle
          />
        </div>
      </FormDrawer>

      <ConfirmationDialog
        open={Boolean(pendingDelete)}
        title={t("personal.todo.deleteConfirmTitle")}
        detail={t("personal.todo.deleteConfirmDetail")}
        confirmLabel={t("personal.todo.deleteConfirm")}
        cancelLabel={t("personal.todo.closeDetail")}
        confirmTone="danger"
        busy={deleteMutation.isPending}
        onCancel={() => {
          if (!deleteMutation.isPending) {
            setPendingDelete(null);
          }
        }}
        onConfirm={() => {
          if (pendingDelete) {
            deleteMutation.mutate(pendingDelete);
          }
        }}
        testId="todo-delete-confirm"
      />
    </div>
  );
}

export function PersonalTodoDetailPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { todoId = "" } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const wantsEdit = searchParams.get("edit") === "1";
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offline = usePersonalOfflineContext();
  const editFormRef = useRef<HTMLFormElement>(null);
  /** Blocks ?edit=1 from re-opening after Save/Cancel while the URL is still catching up. */
  const suppressAutoEditRef = useRef(false);
  const [form, setForm] = useState<TodoFormState | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [conflictBanner, setConflictBanner] = useState(false);
  const [editing, setEditing] = useState(false);
  const [cachedTodo, setCachedTodo] = useState<CachedPersonalTodo | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  function clearEditQueryParam() {
    if (searchParams.get("edit") !== "1") {
      return;
    }
    setSearchParams(
      (prev) => {
        const nextParams = new URLSearchParams(prev);
        nextParams.delete("edit");
        return nextParams;
      },
      { replace: true },
    );
  }

  function exitEditMode() {
    suppressAutoEditRef.current = true;
    setEditing(false);
    setForm(null);
    setFormError(null);
    clearEditQueryParam();
  }

  async function applyTodoUpdate(updated: PersonalTodoDto) {
    queryClient.setQueryData(["personal", "todos", updated.id], updated);
    queryClient.setQueryData<PersonalTodoDto[]>(["personal", "todos"], (previous) =>
      previous
        ? previous.map((row) => (row.id === updated.id ? updated : row))
        : previous,
    );
    if (offline) {
      await cachePersonalTodo(offline.db, offline.scopeBinding, updated);
      setCachedTodo({
        ...updated,
        origin: "Server",
        serverId: updated.id,
        pendingLocalChange: false,
      });
    }
  }

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
    if (!wantsEdit) {
      suppressAutoEditRef.current = false;
      return;
    }
    if (editing || suppressAutoEditRef.current) {
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
    // Consume deep-link immediately so Save cannot re-enter edit from stale ?edit=1.
    clearEditQueryParam();
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
      return updatePersonalTodo(current.id, {
        ...todoFormToRequestBody(next),
        expectedVersion: current.version,
      });
    },
    onSuccess: async (updated) => {
      await applyTodoUpdate(updated);
      setConflictBanner(false);
      exitEditMode();
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
        return completePersonalTodo(todo.id, body);
      }
      if (action === "reopen") {
        return reopenPersonalTodo(todo.id, body);
      }
      return cancelPersonalTodo(todo.id, body);
    },
    onSuccess: async (updated) => {
      await applyTodoUpdate(updated);
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

  const deleteMutation = useMutation({
    mutationFn: async (todo: PersonalTodoDto) => {
      if (!online) {
        throw new Error("online-required");
      }
      await deletePersonalTodo(todo.id, { expectedVersion: todo.version });
    },
    onSuccess: async (_data, todo) => {
      setDeleteConfirmOpen(false);
      if (offline) {
        await removeCachedPersonalTodo(offline.db, todo.id);
      }
      await queryClient.invalidateQueries({ queryKey: ["personal", "todos"] });
      await queryClient.removeQueries({ queryKey: ["personal", "todos", todo.id] });
      navigate(personalPageBackNav.todo.to);
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
        <span className={cn("font-semibold", priorityTextToneClass(todo.priority))}>
          {t(priorityLabelKey(todo.priority))}
        </span>
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
                exitEditMode();
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
          <p className="m-0 text-[length:var(--exits-text-md)] font-semibold" data-testid="todo-detail-title">
            {todo.title}
          </p>
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
              todo.status === "Cancelled" && "personal-todo-row__actions--cancelled",
            )}
          >
            {todo.status === "Open" ? (
              <>
                <Button
                  type="button"
                  intent="neutral"
                  appearance="outline"
                  className="personal-todo-row__action"
                  data-testid="todo-detail-back"
                  onClick={() => navigate(personalPageBackNav.todo.to)}
                >
                  <ArrowLeft className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  {t("personal.todo.back")}
                </Button>
                <Button
                  type="button"
                  intent="success"
                  appearance="solid"
                  emphasis="soft"
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
                  intent="info"
                  appearance="solid"
                  emphasis="soft"
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
                  intent="warning"
                  appearance="outline"
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
                  intent="primary"
                  appearance="outline"
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
                  intent="warning"
                  appearance="outline"
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
              <>
                <Button
                  type="button"
                  intent="primary"
                  appearance="outline"
                  className="personal-todo-reactivate personal-todo-row__action"
                  data-testid="todo-detail-reactivate"
                  disabled={actionMutation.isPending || deleteMutation.isPending || offlineBlocked}
                  onClick={() => actionMutation.mutate({ action: "reopen", todo })}
                >
                  <TodoActionIcon pending={actionMutation.isPending}>
                    <RotateCcw className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.reactivate")}
                </Button>
                <Button
                  type="button"
                  intent="danger"
                  appearance="outline"
                  className="personal-todo-row__action"
                  data-testid="todo-detail-delete"
                  disabled={actionMutation.isPending || deleteMutation.isPending || offlineBlocked}
                  onClick={() => setDeleteConfirmOpen(true)}
                >
                  <TodoActionIcon pending={deleteMutation.isPending}>
                    <Trash2 className="personal-todo-btn-icon size-4 shrink-0" aria-hidden />
                  </TodoActionIcon>
                  {t("personal.todo.delete")}
                </Button>
              </>
            ) : null}
          </div>
        </section>
      )}

      <ConfirmationDialog
        open={deleteConfirmOpen}
        title={t("personal.todo.deleteConfirmTitle")}
        detail={t("personal.todo.deleteConfirmDetail")}
        confirmLabel={t("personal.todo.deleteConfirm")}
        cancelLabel={t("personal.todo.closeDetail")}
        confirmTone="danger"
        busy={deleteMutation.isPending}
        onCancel={() => {
          if (!deleteMutation.isPending) {
            setDeleteConfirmOpen(false);
          }
        }}
        onConfirm={() => {
          deleteMutation.mutate(todo);
        }}
        testId="todo-detail-delete-confirm"
      />
    </div>
  );
}
