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

export type TodoAgendaTab = "today" | "upcoming" | "overdue" | "open" | "completed";

export type TodoDueBucket = "none" | "today" | "upcoming" | "overdue";

export type PersonalTodoCounts = {
  today: number;
  upcoming: number;
  overdue: number;
  open: number;
  completed: number;
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

export async function listPersonalTodos(signal?: AbortSignal): Promise<PersonalTodoDto[]> {
  const raw = await platformRequest<unknown>({ path: TODOS, signal });
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

export function classifyTodoDue(dueAtUtc: string | null | undefined, now = new Date()): TodoDueBucket {
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
    if (todo.status !== "Open") return false;
    if (tab === "open") return true;
    const bucket = classifyTodoDue(todo.dueAtUtc, now);
    if (tab === "today") return bucket === "today";
    if (tab === "upcoming") return bucket === "upcoming" || bucket === "none";
    if (tab === "overdue") return bucket === "overdue";
    return false;
  });
}

export function summarizeTodoCounts(todos: PersonalTodoDto[], now = new Date()): PersonalTodoCounts {
  return {
    today: filterTodosByTab(todos, "today", now).length,
    upcoming: filterTodosByTab(todos, "upcoming", now).length,
    overdue: filterTodosByTab(todos, "overdue", now).length,
    open: filterTodosByTab(todos, "open", now).length,
    completed: filterTodosByTab(todos, "completed", now).length,
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
