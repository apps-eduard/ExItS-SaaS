import { describe, expect, it, vi } from "vitest";
import {
  assertWebCryptoSubtleAvailable,
  isWebCryptoSubtleAvailable,
  resolveEmulatorLoopbackDevUrl,
  WebCryptoUnavailableError,
} from "@/lib/web-crypto-capability";

describe("web crypto capability", () => {
  it("detects missing crypto.subtle", () => {
    vi.stubGlobal("crypto", { getRandomValues: (bytes: Uint8Array) => bytes });
    expect(isWebCryptoSubtleAvailable()).toBe(false);
    expect(() => assertWebCryptoSubtleAvailable()).toThrow(WebCryptoUnavailableError);
    vi.unstubAllGlobals();
  });

  it("builds emulator loopback URL on insecure 10.0.2.2", () => {
    vi.stubGlobal("window", {
      isSecureContext: false,
      location: {
        protocol: "http:",
        hostname: "10.0.2.2",
        port: "5177",
        pathname: "/offline-pin-setup",
        search: "",
        hash: "",
      },
    });
    expect(resolveEmulatorLoopbackDevUrl()).toBe("http://127.0.0.1:5177/offline-pin-setup");
    vi.unstubAllGlobals();
  });
});
