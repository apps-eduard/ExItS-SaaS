import { describe, expect, it, vi } from "vitest";
import { resolveReturnMutationId } from "@/features/returns/return-mutation-id";

describe("resolveReturnMutationId", () => {
  it("reuses pending ReturnId on ordinary retry", () => {
    const pending = "11111111-2222-4333-8444-555555555555";
    const createId = vi.fn(() => ({ ok: true as const, id: "should-not-be-called" }));
    const result = resolveReturnMutationId(pending, createId);
    expect(result).toEqual({ ok: true, id: pending, reused: true });
    expect(createId).not.toHaveBeenCalled();
  });

  it("creates a new id when none is pending", () => {
    const result = resolveReturnMutationId(null, () => ({
      ok: true,
      id: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
    }));
    expect(result).toEqual({
      ok: true,
      id: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
      reused: false,
    });
  });

  it("fail-closes when creation fails and none pending", () => {
    const result = resolveReturnMutationId(null, () => ({
      ok: false,
      reason: "secure_randomness_unavailable",
    }));
    expect(result.ok).toBe(false);
    expect(result.reused).toBe(false);
  });
});
