import type { IncomingMessage } from "node:http";

/** Strip Secure from Set-Cookie so local HTTP origins can retain the session cookie. */
export function stripSecureFlagFromSetCookie(raw: string): string {
  return raw.replace(/;\s*Secure/gi, "");
}

export function rewriteProxiedSetCookieHeaders(proxyRes: IncomingMessage): void {
  const setCookie = proxyRes.headers["set-cookie"];
  if (!setCookie) {
    return;
  }
  const values = Array.isArray(setCookie) ? setCookie : [setCookie];
  proxyRes.headers["set-cookie"] = values.map(stripSecureFlagFromSetCookie);
}
