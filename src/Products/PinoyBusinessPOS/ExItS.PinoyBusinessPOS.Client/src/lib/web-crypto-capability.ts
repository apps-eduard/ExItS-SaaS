export class WebCryptoUnavailableError extends Error {
  constructor(message = "Web Crypto is unavailable in this browser context.") {
    super(message);
    this.name = "WebCryptoUnavailableError";
  }
}

export function isWebCryptoSubtleAvailable(): boolean {
  return typeof crypto !== "undefined" && crypto.subtle !== undefined;
}

export function isWebCryptoSecureContext(): boolean {
  return typeof window !== "undefined" && window.isSecureContext;
}

export function assertWebCryptoSubtleAvailable(): void {
  if (!isWebCryptoSubtleAvailable()) {
    throw new WebCryptoUnavailableError();
  }
}
