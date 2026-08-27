import { describe, expect, it, vi } from "vitest";

import {
  cascadeResolveSiblingRequests,
  groupByKey,
  siblingRequestIds,
  sortByNewestUtc,
} from "@/features/personal/consent-request-groups";

describe("consent-request-groups", () => {
  it("groups items by key and picks newest primary", () => {
    const groups = groupByKey(
      [
        { id: "a", orgId: "org-1", createdAtUtc: "2026-01-01T00:00:00Z" },
        { id: "b", orgId: "org-1", createdAtUtc: "2026-01-03T00:00:00Z" },
        { id: "c", orgId: "org-2", createdAtUtc: "2026-01-02T00:00:00Z" },
      ],
      (item) => item.orgId,
      sortByNewestUtc((item) => item.createdAtUtc),
    );

    expect(groups).toHaveLength(2);
    expect(groups[0]?.primary.id).toBe("b");
    expect(groups[0]?.duplicateCount).toBe(2);
    expect(groups[1]?.primary.id).toBe("c");
  });

  it("returns sibling ids excluding primary", () => {
    expect(
      siblingRequestIds("primary", [{ id: "primary" }, { id: "other-1" }, { id: "other-2" }]),
    ).toEqual(["other-1", "other-2"]);
  });

  it("resolves siblings and ignores individual failures", async () => {
    const resolve = vi
      .fn()
      .mockResolvedValueOnce(undefined)
      .mockRejectedValueOnce(new Error("already resolved"));

    await cascadeResolveSiblingRequests(["one", "two"], resolve);

    expect(resolve).toHaveBeenCalledTimes(2);
  });
});
