import { describe, expect, it, vi } from "vitest";
import {
  classifyTodoDue,
  filterTodosByTab,
  isTodoConcurrencyConflict,
  listPersonalTodos,
  summarizeTodoCounts,
  type PersonalTodoDto,
} from "@/api/platform/personal-todo-client";
import { PlatformApiError } from "@/api/platform/platform-http";

const ownerId = "11111111-1111-1111-1111-111111111111";

function todo(partial: Partial<PersonalTodoDto> & Pick<PersonalTodoDto, "id" | "title">): PersonalTodoDto {
  return {
    ownerUserIdentityId: ownerId,
    notes: null,
    dueAtUtc: null,
    reminderAtUtc: null,
    priority: "Normal",
    status: "Open",
    relatedEntityType: null,
    relatedEntityId: null,
    createdAtUtc: "2026-08-21T00:00:00Z",
    updatedAtUtc: "2026-08-21T00:00:00Z",
    completedAtUtc: null,
    version: 1,
    ...partial,
  };
}

describe("personal-todo-client", () => {
  it("parses PascalCase todo list payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => [
          {
            Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            OwnerUserIdentityId: ownerId,
            Title: "Pay rent",
            Notes: null,
            DueAtUtc: "2026-08-21T10:00:00Z",
            ReminderAtUtc: null,
            Priority: "High",
            Status: "Open",
            RelatedEntityType: null,
            RelatedEntityId: null,
            CreatedAtUtc: "2026-08-20T00:00:00Z",
            UpdatedAtUtc: "2026-08-20T00:00:00Z",
            CompletedAtUtc: null,
            Version: 2,
          },
        ],
        text: async () => "",
      })),
    );

    const todos = await listPersonalTodos();
    expect(todos).toHaveLength(1);
    expect(todos[0]?.title).toBe("Pay rent");
    expect(todos[0]?.priority).toBe("High");
    expect(todos[0]?.version).toBe(2);
    vi.unstubAllGlobals();
  });

  it("detects concurrency conflicts", () => {
    expect(
      isTodoConcurrencyConflict(
        new PlatformApiError(409, { errorCode: "application.concurrency_conflict" }),
      ),
    ).toBe(true);
    expect(isTodoConcurrencyConflict(new PlatformApiError(400, { errorCode: "x" }))).toBe(false);
  });

  it("classifies due buckets and agenda tabs", () => {
    const now = new Date("2026-08-21T12:00:00");
    expect(classifyTodoDue("2026-08-20T23:00:00", now)).toBe("overdue");
    expect(classifyTodoDue("2026-08-21T18:00:00", now)).toBe("today");
    expect(classifyTodoDue("2026-08-22T01:00:00", now)).toBe("upcoming");
    expect(classifyTodoDue(null, now)).toBe("none");

    const items = [
      todo({ id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", title: "A", dueAtUtc: "2026-08-21T15:00:00" }),
      todo({ id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", title: "B", dueAtUtc: "2026-08-25T15:00:00" }),
      todo({ id: "cccccccc-cccc-cccc-cccc-cccccccccccc", title: "C", dueAtUtc: "2026-08-19T15:00:00" }),
      todo({ id: "dddddddd-dddd-dddd-dddd-dddddddddddd", title: "D", dueAtUtc: null }),
      todo({
        id: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        title: "E",
        status: "Completed",
        dueAtUtc: "2026-08-21T15:00:00",
      }),
    ];

    expect(filterTodosByTab(items, "today", now).map((t) => t.title)).toEqual(["A"]);
    expect(filterTodosByTab(items, "upcoming", now).map((t) => t.title)).toEqual(["B", "D"]);
    expect(filterTodosByTab(items, "overdue", now).map((t) => t.title)).toEqual(["C"]);
    expect(filterTodosByTab(items, "open", now)).toHaveLength(4);
    expect(filterTodosByTab(items, "completed", now).map((t) => t.title)).toEqual(["E"]);

    expect(summarizeTodoCounts(items, now)).toEqual({
      today: 1,
      upcoming: 2,
      overdue: 1,
      open: 4,
      completed: 1,
    });
  });
});
