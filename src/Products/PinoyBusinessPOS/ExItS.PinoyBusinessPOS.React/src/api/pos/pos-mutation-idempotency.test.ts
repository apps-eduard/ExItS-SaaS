import { afterEach, describe, expect, it, vi } from "vitest";
import {
  IDEMPOTENCY_KEY_HEADER,
  OPERATION_ID_HEADER,
  OPERATION_TYPE_HEADER,
  PAYLOAD_HASH_HEADER,
  buildPosMutationIdempotencyHeaders,
  sha256Hex,
} from "@/api/pos/pos-mutation-idempotency";

const EMPTY_SHA256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
const ABC_SHA256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
const UNICODE = "café — 日本語";

describe("sha256Hex / pos mutation idempotency", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("matches NIST empty and abc vectors with Web Crypto", async () => {
    expect(crypto.subtle).toBeDefined();
    expect(await sha256Hex("")).toBe(EMPTY_SHA256);
    expect(await sha256Hex("abc")).toBe(ABC_SHA256);
  });

  it("matches NIST empty and abc vectors when crypto.subtle is unavailable", async () => {
    const originalCrypto = globalThis.crypto;
    vi.stubGlobal("crypto", {
      ...originalCrypto,
      subtle: undefined,
    });
    expect(crypto.subtle).toBeUndefined();
    expect(await sha256Hex("")).toBe(EMPTY_SHA256);
    expect(await sha256Hex("abc")).toBe(ABC_SHA256);
  });

  it("produces identical UTF-8 hashes on both Web Crypto and fallback paths", async () => {
    const withSubtle = await sha256Hex(UNICODE);
    expect(withSubtle).toMatch(/^[0-9a-f]{64}$/);

    const originalCrypto = globalThis.crypto;
    vi.stubGlobal("crypto", {
      ...originalCrypto,
      subtle: undefined,
    });
    const withoutSubtle = await sha256Hex(UNICODE);
    expect(withoutSubtle).toBe(withSubtle);
  });

  it("builds lowercase 64-char payload hash headers without subtle", async () => {
    const originalCrypto = globalThis.crypto;
    vi.stubGlobal("crypto", {
      ...originalCrypto,
      subtle: undefined,
      randomUUID: originalCrypto.randomUUID?.bind(originalCrypto),
    });

    const entityId = "11111111-2222-4333-8444-555555555555";
    const payload = JSON.stringify({ name: "PWA-0001", description: null });
    const headers = await buildPosMutationIdempotencyHeaders(entityId, payload, "pos.register.create");

    expect(headers[IDEMPOTENCY_KEY_HEADER]).toBe("11111111222243338444555555555555");
    expect(headers[OPERATION_ID_HEADER]).toBe(entityId);
    expect(headers[OPERATION_TYPE_HEADER]).toBe("pos.register.create");
    expect(headers[PAYLOAD_HASH_HEADER]).toMatch(/^[0-9a-f]{64}$/);
    expect(headers[PAYLOAD_HASH_HEADER]).toBe(await sha256Hex(payload));
  });
});
