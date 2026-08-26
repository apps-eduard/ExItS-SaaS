import { describe, expect, it, vi, afterEach } from "vitest";
import { createSecureMutationId, isUuidV4 } from "@/lib/secure-mutation-id";

describe("createSecureMutationId", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("prefers crypto.randomUUID and returns a valid UUID", () => {
    const fixed = "a1b2c3d4-e5f6-4789-a012-3456789abcde";
    vi.stubGlobal("crypto", {
      randomUUID: () => fixed,
      getRandomValues: undefined,
    });

    const result = createSecureMutationId();
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.id).toBe(fixed);
      expect(isUuidV4(result.id)).toBe(true);
    }
  });

  it("falls back to getRandomValues UUID v4 when randomUUID is missing", () => {
    vi.stubGlobal("crypto", {
      randomUUID: undefined,
      getRandomValues: (arr: Uint8Array) => {
        for (let i = 0; i < arr.length; i += 1) {
          arr[i] = (i * 17 + 3) % 256;
        }
        return arr;
      },
    });

    const result = createSecureMutationId();
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(isUuidV4(result.id)).toBe(true);
      expect(result.id).not.toBe("00000000-0000-4000-8000-000000000000");
    }
  });

  it("produces distinct IDs across getRandomValues calls", () => {
    let call = 0;
    vi.stubGlobal("crypto", {
      randomUUID: undefined,
      getRandomValues: (arr: Uint8Array) => {
        call += 1;
        for (let i = 0; i < arr.length; i += 1) {
          arr[i] = (call * 31 + i * 13) % 256;
        }
        return arr;
      },
    });

    const a = createSecureMutationId();
    const b = createSecureMutationId();
    expect(a.ok && b.ok).toBe(true);
    if (a.ok && b.ok) {
      expect(a.id).not.toBe(b.id);
      expect(isUuidV4(a.id)).toBe(true);
      expect(isUuidV4(b.id)).toBe(true);
    }
  });

  it("fail-closes when secure randomness is unavailable", () => {
    vi.stubGlobal("crypto", {
      randomUUID: undefined,
      getRandomValues: undefined,
    });

    const result = createSecureMutationId();
    expect(result).toEqual({ ok: false, reason: "secure_randomness_unavailable" });
  });

  it("does not use a hardcoded all-zero constant when randomness exists", () => {
    vi.stubGlobal("crypto", {
      randomUUID: undefined,
      getRandomValues: (arr: Uint8Array) => {
        for (let i = 0; i < arr.length; i += 1) {
          arr[i] = 0xab;
        }
        return arr;
      },
    });

    const result = createSecureMutationId();
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.id).not.toBe("00000000-0000-4000-8000-000000000000");
      expect(isUuidV4(result.id)).toBe(true);
    }
  });

  it("fail-closed path does not emit the former constant GUID", () => {
    vi.stubGlobal("crypto", {
      randomUUID: undefined,
      getRandomValues: undefined,
    });

    const result = createSecureMutationId();
    expect(result.ok).toBe(false);
    expect(result).not.toEqual(
      expect.objectContaining({ id: "00000000-0000-4000-8000-000000000000" }),
    );
  });
});
