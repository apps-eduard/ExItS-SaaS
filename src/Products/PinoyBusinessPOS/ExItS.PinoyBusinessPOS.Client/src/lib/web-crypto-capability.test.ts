import { describe, expect, it, vi } from "vitest";
import {
  assertWebCryptoSubtleAvailable,
  isWebCryptoSubtleAvailable,
  resolveEmulatorHttpsDevUrl,
  WebCryptoUnavailableError,
} from "@/lib/web-crypto-capability";

describe("web crypto capability", () => {
  it("detects missing crypto.subtle", () => {
    vi.stubGlobal("crypto", { getRandomValues: (bytes: Uint8Array) => bytes });
    expect(isWebCryptoSubtleAvailable()).toBe(false);
    expect(() => assertWebCryptoSubtleAvailable()).toThrow(WebCryptoUnavailableError);
    vi.unstubAllGlobals();
  });

  it("builds emulator HTTPS upgrade URL on insecure 10.0.2.2", () => {
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
    expect(resolveEmulatorHttpsDevUrl()).toBe("https://10.0.2.2:5177/offline-pin-setup");
    vi.unstubAllGlobals();
  });
});
