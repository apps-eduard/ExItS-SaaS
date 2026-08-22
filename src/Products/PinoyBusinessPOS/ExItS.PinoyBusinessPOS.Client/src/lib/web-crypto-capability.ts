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

/** Emulator dev on http://10.0.2.2 needs HTTPS for crypto.subtle; Vite dev serves basic SSL. */
export function resolveEmulatorHttpsDevUrl(): string | null {
  if (typeof window === "undefined" || window.isSecureContext) {
    return null;
  }
  const { protocol, hostname, port, pathname, search, hash } = window.location;
  if (protocol !== "http:" || hostname !== "10.0.2.2") {
    return null;
  }
  return `https://${hostname}${port ? `:${port}` : ""}${pathname}${search}${hash}`;
}
