/**
 * Request correlation IDs for diagnostics/headers only.
 * Not used for auth, session, CSRF, reset, or other security tokens.
 */
export function createCorrelationId(): string {
  const webCrypto = globalThis.crypto;

  if (webCrypto && typeof webCrypto.randomUUID === "function") {
    try {
      return webCrypto.randomUUID();
    } catch {
      // Some browsers expose randomUUID but reject it outside secure contexts (HTTP LAN).
    }
  }

  if (webCrypto && typeof webCrypto.getRandomValues === "function") {
    const bytes = new Uint8Array(16);
    webCrypto.getRandomValues(bytes);
    // RFC 4122 version 4
    bytes[6] = (bytes[6]! & 0x0f) | 0x40;
    bytes[8] = (bytes[8]! & 0x3f) | 0x80;
    const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
  }

  // Last-resort diagnostics-only id for environments without Web Crypto.
  return `corr-${Date.now().toString(16)}-${Math.random().toString(16).slice(2, 10)}`;
}
