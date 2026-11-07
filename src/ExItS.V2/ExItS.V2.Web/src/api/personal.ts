import { platformRequest } from "./platform-http";
import { asRecord, bool, num, nullableStr, str } from "./widget";

export type PersonalDashboardDto = {
  utangAvailable: boolean;
  totalLentBalance: number;
  totalBorrowedBalance: number;
  sharedWithMeLentBalance: number;
  sharedWithMeBorrowedBalance: number;
  contactCount: number;
  activeRelationshipCount: number;
  sharedWithMeActiveCount: number;
  pendingConfirmationCount: number;
};

export type PersonalTodoDto = {
  id: string;
  title: string;
  dueAtUtc: string | null;
  reminderAtUtc: string | null;
  priority: string;
  status: string;
  updatedAtUtc: string;
  createdAtUtc: string;
};

export type PersonalTodoCounts = {
  today: number;
  upcoming: number;
  overdue: number;
  open: number;
  completed: number;
};

export async function getPersonalDashboard(
  signal?: AbortSignal,
): Promise<PersonalDashboardDto> {
  const raw = asRecord(
    await platformRequest<unknown>({
      path: "/api/v1/personal/dashboard",
      signal,
    }),
  );
  return {
    utangAvailable: bool(raw, "utangAvailable", "UtangAvailable"),
    totalLentBalance: num(raw, "totalLentBalance", "TotalLentBalance"),
    totalBorrowedBalance: num(raw, "totalBorrowedBalance", "TotalBorrowedBalance"),
    sharedWithMeLentBalance: num(raw, "sharedWithMeLentBalance", "SharedWithMeLentBalance"),
    sharedWithMeBorrowedBalance: num(
      raw,
      "sharedWithMeBorrowedBalance",
      "SharedWithMeBorrowedBalance",
    ),
    contactCount: num(raw, "contactCount", "ContactCount"),
    activeRelationshipCount: num(
      raw,
      "activeRelationshipCount",
      "ActiveRelationshipCount",
    ),
    sharedWithMeActiveCount: num(
      raw,
      "sharedWithMeActiveCount",
      "SharedWithMeActiveCount",
    ),
    pendingConfirmationCount: num(
      raw,
      "pendingConfirmationCount",
      "PendingConfirmationCount",
    ),
  };
}

export async function listPersonalTodos(
  signal?: AbortSignal,
): Promise<PersonalTodoDto[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/todos",
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  return items.map((item) => {
    const r = asRecord(item);
    return {
      id: str(r, "id", "Id"),
      title: str(r, "title", "Title"),
      dueAtUtc: nullableStr(r, "dueAtUtc", "DueAtUtc"),
      reminderAtUtc: nullableStr(r, "reminderAtUtc", "ReminderAtUtc"),
      priority: str(r, "priority", "Priority") || "None",
      status: str(r, "status", "Status") || "Open",
      updatedAtUtc: str(r, "updatedAtUtc", "UpdatedAtUtc"),
      createdAtUtc: str(r, "createdAtUtc", "CreatedAtUtc"),
    };
  });
}

function startOfLocalDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function addLocalDays(date: Date, days: number): Date {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
}

type TodoDueBucket = "none" | "overdue" | "today" | "upcoming";

function classifyTodoDue(
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

/** Mirrors original personal-todo-client summarizeTodoCounts tab rules. */
export function summarizeTodoCounts(
  todos: PersonalTodoDto[],
  now = new Date(),
): PersonalTodoCounts {
  let today = 0;
  let upcoming = 0;
  let overdue = 0;
  let open = 0;
  let completed = 0;
  for (const todo of todos) {
    if (todo.status === "Completed") {
      completed += 1;
      continue;
    }
    if (todo.status === "Cancelled") continue;
    if (todo.status !== "Open") continue;
    open += 1;
    const bucket = classifyTodoDue(todo.dueAtUtc, now);
    if (bucket === "today") today += 1;
    else if (bucket === "overdue") overdue += 1;
    else upcoming += 1; // includes no due date
  }
  return { today, upcoming, overdue, open, completed };
}
