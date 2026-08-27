import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const personalTodoPrioritySchema = z.enum(["None", "Low", "Normal", "High"]);
export const personalTodoStatusSchema = z.enum(["Open", "Completed", "Cancelled"]);
export const personalTodoRelatedEntityTypeSchema = z.enum([
  "PersonalUtangRelationship",
  "PersonalContact",
  "CustomerOrder",
  "Organization",
]);

export const personalTodoSchema = z.object({
  id: guidSchema,
  ownerUserIdentityId: guidSchema,
  title: z.string(),
  notes: z.string().nullable().optional().default(null),
  dueAtUtc: z.string().nullable().optional().default(null),
  reminderAtUtc: z.string().nullable().optional().default(null),
  reminderNotifiedAtUtc: z.string().nullable().optional().default(null),
  priority: z.string(),
  status: z.string(),
  relatedEntityType: z.string().nullable().optional().default(null),
  relatedEntityId: guidSchema.nullable().optional().default(null),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
  completedAtUtc: z.string().nullable().optional().default(null),
  version: z.number().int(),
});

export type PersonalTodoDto = z.infer<typeof personalTodoSchema>;
export type PersonalTodoPriority = z.infer<typeof personalTodoPrioritySchema>;
export type PersonalTodoStatus = z.infer<typeof personalTodoStatusSchema>;
export type PersonalTodoRelatedEntityType = z.infer<typeof personalTodoRelatedEntityTypeSchema>;

export type CreatePersonalTodoRequest = {
  title: string;
  notes?: string | null;
  dueAtUtc?: string | null;
  reminderAtUtc?: string | null;
  priority?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
};

export type UpdatePersonalTodoRequest = CreatePersonalTodoRequest & {
  expectedVersion?: number | null;
};

export type PersonalTodoVersionRequest = {
  expectedVersion?: number | null;
};

export type TodoAgendaTab = "today" | "upcoming" | "overdue" | "open" | "completed" | "cancelled";

const TODO_AGENDA_TABS: readonly TodoAgendaTab[] = [
  "today",
  "upcoming",
  "overdue",
  "open",
  "completed",
  "cancelled",
];

export function parseTodoAgendaTab(value: string | null | undefined): TodoAgendaTab {
  if (value && TODO_AGENDA_TABS.includes(value as TodoAgendaTab)) {
    return value as TodoAgendaTab;
  }
  return "today";
}

export function todoAgendaTabHref(tab: TodoAgendaTab): string {
  return `/personal/todo?tab=${tab}`;
}

export type TodoDueBucket = "none" | "today" | "upcoming" | "overdue";

export type PersonalTodoCounts = {
  today: number;
  upcoming: number;
  overdue: number;
  open: number;
  completed: number;
  cancelled: number;
};

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

function normalizeTodo(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    id: pick(r, "id", "Id"),
    ownerUserIdentityId: pick(r, "ownerUserIdentityId", "OwnerUserIdentityId"),
    title: pick(r, "title", "Title"),
    notes: pick(r, "notes", "Notes") ?? null,
    dueAtUtc: pick(r, "dueAtUtc", "DueAtUtc") ?? null,
    reminderAtUtc: pick(r, "reminderAtUtc", "ReminderAtUtc") ?? null,
    reminderNotifiedAtUtc: pick(r, "reminderNotifiedAtUtc", "ReminderNotifiedAtUtc") ?? null,
    priority: pick(r, "priority", "Priority"),
    status: pick(r, "status", "Status"),
    relatedEntityType: pick(r, "relatedEntityType", "RelatedEntityType") ?? null,
    relatedEntityId: pick(r, "relatedEntityId", "RelatedEntityId") ?? null,
    createdAtUtc: pick(r, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
    completedAtUtc: pick(r, "completedAtUtc", "CompletedAtUtc") ?? null,
    version: Number(pick(r, "version", "Version") ?? 0),
  };
}

const TODOS = "/api/v1/personal/todos";

export async function listPersonalTodos(
  signal?: AbortSignal,
  options?: { status?: "Open" | "Completed" | "Cancelled" | "All" },
): Promise<PersonalTodoDto[]> {
  const status = options?.status;
  const query =
    status && status !== "All"
      ? `?status=${encodeURIComponent(status === "Open" ? "Open" : status)}`
      : "";
  const raw = await platformRequest<unknown>({ path: `${TODOS}${query}`, signal });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) => personalTodoSchema.parse(normalizeTodo(item)));
}

export async function getPersonalTodo(
  todoId: string,
  signal?: AbortSignal,
): Promise<PersonalTodoDto> {
  const raw = await platformRequest<unknown>({ path: `${TODOS}/${todoId}`, signal });
  return personalTodoSchema.parse(normalizeTodo(raw));
}

export async function createPersonalTodo(
  body: CreatePersonalTodoRequest,
  signal?: AbortSignal,
): Promise<PersonalTodoDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: TODOS,
    body,
    signal,
  });
  return personalTodoSchema.parse(normalizeTodo(raw));
}

export async function updatePersonalTodo(
  todoId: string,
  body: UpdatePersonalTodoRequest,
  signal?: AbortSignal,
): Promise<PersonalTodoDto> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: `${TODOS}/${todoId}`,
    body,
    signal,
  });
  return personalTodoSchema.parse(normalizeTodo(raw));
}

export async function completePersonalTodo(
  todoId: string,
  body?: PersonalTodoVersionRequest,
  signal?: AbortSignal,
): Promise<PersonalTodoDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${TODOS}/${todoId}/complete`,
    body: body ?? {},
    signal,
  });
  return personalTodoSchema.parse(normalizeTodo(raw));
}

export async function reopenPersonalTodo(
  todoId: string,
  body?: PersonalTodoVersionRequest,
  signal?: AbortSignal,
): Promise<PersonalTodoDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${TODOS}/${todoId}/reopen`,
    body: body ?? {},
    signal,
  });
  return personalTodoSchema.parse(normalizeTodo(raw));
}

export async function cancelPersonalTodo(
  todoId: string,
  body?: PersonalTodoVersionRequest,
  signal?: AbortSignal,
): Promise<PersonalTodoDto> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `${TODOS}/${todoId}/cancel`,
    body: body ?? {},
    signal,
  });
  return personalTodoSchema.parse(normalizeTodo(raw));
}

export function isTodoConcurrencyConflict(error: unknown): boolean {
  if (!error || typeof error !== "object") return false;
  const err = error as { status?: number; errorCode?: string };
  return err.status === 409 && err.errorCode === "application.concurrency_conflict";
}

function startOfLocalDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function addLocalDays(date: Date, days: number): Date {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

export function classifyTodoDue(
  dueAtUtc: string | null | undefined,
  now = new Date(),
): TodoDueBucket {
  if (!dueAtUtc) return "none";
  const due = new Date(dueAtUtc);
  if (Number.isNaN(due.getTime())) return "none";
  const dayStart = startOfLocalDay(now);
  const nextDay = addLocalDays(dayStart, 1);
  if (due < dayStart) return "overdue";
  if (due < nextDay) return "today";
  return "upcoming";
}

export function filterTodosByTab(
  todos: PersonalTodoDto[],
  tab: TodoAgendaTab,
  now = new Date(),
): PersonalTodoDto[] {
  return todos.filter((todo) => {
    if (tab === "completed") return todo.status === "Completed";
    if (tab === "cancelled") return todo.status === "Cancelled";
    if (todo.status !== "Open") return false;
    if (tab === "open") return true;
    const bucket = classifyTodoDue(todo.dueAtUtc, now);
    if (tab === "today") return bucket === "today";
    if (tab === "upcoming") return bucket === "upcoming" || bucket === "none";
    if (tab === "overdue") return bucket === "overdue";
    return false;
  });
}

export function summarizeTodoCounts(
  todos: PersonalTodoDto[],
  now = new Date(),
): PersonalTodoCounts {
  return {
    today: filterTodosByTab(todos, "today", now).length,
    upcoming: filterTodosByTab(todos, "upcoming", now).length,
    overdue: filterTodosByTab(todos, "overdue", now).length,
    open: filterTodosByTab(todos, "open", now).length,
    completed: filterTodosByTab(todos, "completed", now).length,
    cancelled: filterTodosByTab(todos, "cancelled", now).length,
  };
}

/** Convert datetime-local value to ISO UTC, or null when empty/invalid. */
export function localDateTimeToUtcIso(value: string): string | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const parsed = new Date(trimmed);
  if (Number.isNaN(parsed.getTime())) return null;
  return parsed.toISOString();
}

/** Format UTC ISO for datetime-local input in local timezone. */
export function utcIsoToLocalDateTimeInput(iso: string | null | undefined): string {
  if (!iso) return "";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export type QuickDuePreset = "today" | "tomorrow" | "nextWeek" | "none";

/** Build datetime-local value for a local calendar day at the given clock time. */
export function localDateTimeInputForLocalDay(
  date: Date,
  hour = 9,
  minute = 0,
): string {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(hour)}:${pad(minute)}`;
}

export function quickDueLocalDateTime(preset: QuickDuePreset, now = new Date()): string {
  const dayStart = startOfLocalDay(now);
  switch (preset) {
    case "today":
      return localDateTimeInputForLocalDay(dayStart, 17, 0);
    case "tomorrow":
      return localDateTimeInputForLocalDay(addLocalDays(dayStart, 1), 9, 0);
    case "nextWeek":
      return localDateTimeInputForLocalDay(addLocalDays(dayStart, 7), 9, 0);
    default:
      return "";
  }
}

/** Server-aligned ordering: due items first (ascending due), then newest updated. */
export function sortPersonalTodos(todos: readonly PersonalTodoDto[]): PersonalTodoDto[] {
  return [...todos].sort((left, right) => {
    const leftHasDue = left.dueAtUtc ? 1 : 0;
    const rightHasDue = right.dueAtUtc ? 1 : 0;
    if (leftHasDue !== rightHasDue) {
      return rightHasDue - leftHasDue;
    }
    if (left.dueAtUtc && right.dueAtUtc) {
      const dueCompare =
        new Date(left.dueAtUtc).getTime() - new Date(right.dueAtUtc).getTime();
      if (dueCompare !== 0) {
        return dueCompare;
      }
    }
    return new Date(right.updatedAtUtc).getTime() - new Date(left.updatedAtUtc).getTime();
  });
}

export function filterTodosBySearch(
  todos: readonly PersonalTodoDto[],
  query: string,
): PersonalTodoDto[] {
  const normalized = query.trim().toLowerCase();
  if (!normalized) {
    return [...todos];
  }
  return todos.filter((todo) => {
    if (todo.title.toLowerCase().includes(normalized)) {
      return true;
    }
    return (todo.notes?.toLowerCase().includes(normalized) ?? false);
  });
}

export function filterAndSortTodosForTab(
  todos: readonly PersonalTodoDto[],
  tab: TodoAgendaTab,
  options?: { search?: string; now?: Date },
): PersonalTodoDto[] {
  const now = options?.now ?? new Date();
  const search = options?.search ?? "";
  const tabbed = filterTodosByTab([...todos], tab, now);
  const searched = filterTodosBySearch(tabbed, search);
  return sortPersonalTodos(searched);
}

export type TodoAgendaTabEmptyKey = {
  titleKey:
    | "personal.todo.emptyTodayTitle"
    | "personal.todo.emptyUpcomingTitle"
    | "personal.todo.emptyOverdueTitle"
    | "personal.todo.emptyOpenTitle"
    | "personal.todo.emptyCompletedTitle"
    | "personal.todo.emptyCancelledTitle"
    | "personal.todo.emptySearchTitle";
  detailKey:
    | "personal.todo.emptyTodayDetail"
    | "personal.todo.emptyUpcomingDetail"
    | "personal.todo.emptyOverdueDetail"
    | "personal.todo.emptyOpenDetail"
    | "personal.todo.emptyCompletedDetail"
    | "personal.todo.emptyCancelledDetail"
    | "personal.todo.emptySearchDetail";
};

export function todoEmptyStateKeys(
  tab: TodoAgendaTab,
  hasSearch: boolean,
): TodoAgendaTabEmptyKey {
  if (hasSearch) {
    return {
      titleKey: "personal.todo.emptySearchTitle",
      detailKey: "personal.todo.emptySearchDetail",
    };
  }
  switch (tab) {
    case "today":
      return {
        titleKey: "personal.todo.emptyTodayTitle",
        detailKey: "personal.todo.emptyTodayDetail",
      };
    case "upcoming":
      return {
        titleKey: "personal.todo.emptyUpcomingTitle",
        detailKey: "personal.todo.emptyUpcomingDetail",
      };
    case "overdue":
      return {
        titleKey: "personal.todo.emptyOverdueTitle",
        detailKey: "personal.todo.emptyOverdueDetail",
      };
    case "open":
      return {
        titleKey: "personal.todo.emptyOpenTitle",
        detailKey: "personal.todo.emptyOpenDetail",
      };
    case "completed":
      return {
        titleKey: "personal.todo.emptyCompletedTitle",
        detailKey: "personal.todo.emptyCompletedDetail",
      };
    default:
      return {
        titleKey: "personal.todo.emptyCancelledTitle",
        detailKey: "personal.todo.emptyCancelledDetail",
      };
  }
}

export function relatedEntityHref(
  relatedEntityType: string | null | undefined,
  relatedEntityId: string | null | undefined,
): string | null {
  if (!relatedEntityType || !relatedEntityId) {
    return null;
  }
  switch (relatedEntityType) {
    case "PersonalContact":
      return `/personal/people/${relatedEntityId}`;
    case "PersonalUtangRelationship":
      return `/personal/utang/relationships/${relatedEntityId}`;
    default:
      return null;
  }
}

export function priorityRank(priority: string): number {
  switch (priority) {
    case "High":
      return 3;
    case "Normal":
      return 2;
    case "Low":
      return 1;
    default:
      return 0;
  }
}

export function priorityToneClass(priority: string): string | null {
  switch (priority) {
    case "High":
      return "personal-todo-meta__chip--priority-high";
    case "Low":
      return "personal-todo-meta__chip--priority-low";
    default:
      return null;
  }
}
