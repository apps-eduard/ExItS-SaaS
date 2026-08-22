import { describe, expect, it, vi } from "vitest";
import {
  assertWebCryptoSubtleAvailable,
  isWebCryptoSubtleAvailable,
  WebCryptoUnavailableError,
} from "@/lib/web-crypto-capability";

describe("web crypto capability", () => {
  it("detects missing crypto.subtle", () => {
    vi.stubGlobal("crypto", { getRandomValues: (bytes: Uint8Array) => bytes });
    expect(isWebCryptoSubtleAvailable()).toBe(false);
    expect(() => assertWebCryptoSubtleAvailable()).toThrow(WebCryptoUnavailableError);
    vi.unstubAllGlobals();
  });
});
