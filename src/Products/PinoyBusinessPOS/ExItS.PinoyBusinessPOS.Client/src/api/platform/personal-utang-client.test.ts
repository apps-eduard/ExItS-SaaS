import { describe, expect, it, vi } from "vitest";
import {
  formatDueLabel,
  isUtangConcurrencyConflict,
  listPersonalContacts,
} from "@/api/platform/personal-utang-client";
import { PlatformApiError } from "@/api/platform/platform-http";

describe("personal-utang-client", () => {
  it("parses contact list payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => [
          {
            Id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            DisplayName: "Ana",
            Phone: null,
            Email: null,
            LinkedUserIdentityId: null,
            Status: "Active",
            CreatedAtUtc: "2026-08-21T00:00:00Z",
          },
        ],
        text: async () => "",
      })),
    );

    const contacts = await listPersonalContacts();
    expect(contacts).toHaveLength(1);
    expect(contacts[0]?.displayName).toBe("Ana");
    vi.unstubAllGlobals();
  });

  it("detects concurrency conflicts", () => {
    expect(
      isUtangConcurrencyConflict(
        new PlatformApiError(409, { errorCode: "application.concurrency_conflict" }),
      ),
    ).toBe(true);
    expect(isUtangConcurrencyConflict(new PlatformApiError(400, { errorCode: "x" }))).toBe(false);
  });

  it("classifies due dates", () => {
    const now = new Date("2026-08-21T12:00:00Z");
    expect(formatDueLabel("2026-08-20T00:00:00Z", now).kind).toBe("overdue");
    expect(formatDueLabel("2026-08-22T00:00:00Z", now).kind).toBe("dueSoon");
    expect(formatDueLabel("2026-09-21T00:00:00Z", now).kind).toBe("upcoming");
    expect(formatDueLabel(null, now).kind).toBe("none");
  });
});
