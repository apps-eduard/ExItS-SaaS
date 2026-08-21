import { describe, expect, it } from "vitest";
import { describeSyncSummary } from "@/offline/OfflineSyncProvider";
import type { OfflineQueueCounts } from "@/offline/types";

const base: OfflineQueueCounts = {
  pending: 0,
  syncing: 0,
  succeeded: 0,
  retryableFailure: 0,
  permanentFailure: 0,
  conflict: 0,
  blockedByAccess: 0,
};

describe("describeSyncSummary", () => {
  it("reports synced when empty", () => {
    expect(describeSyncSummary(base).kind).toBe("synced");
  });

  it("reports waiting from pending + retryable", () => {
    expect(describeSyncSummary({ ...base, pending: 2, retryableFailure: 1 })).toEqual({
      kind: "waiting",
      waiting: 3,
      attention: 0,
    });
  });

  it("reports attention for conflict/permanent", () => {
    expect(describeSyncSummary({ ...base, conflict: 1 }).kind).toBe("attention");
  });

  it("reports access when blocked", () => {
    expect(describeSyncSummary({ ...base, blockedByAccess: 1, pending: 2 }).kind).toBe("access");
  });
});
