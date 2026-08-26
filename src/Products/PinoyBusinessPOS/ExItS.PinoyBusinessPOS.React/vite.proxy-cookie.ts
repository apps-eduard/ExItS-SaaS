import type { IncomingMessage } from "node:http";

/**
 * Live-preview / non-Development Platform images often emit `Set-Cookie; Secure`.
 * Chrome accepts Secure cookies over HTTP on localhost/127.0.0.1, but rejects them
 * on Android emulator host alias `http://10.0.2.2` — so session never sticks there.
 * Vite DEV/preview proxies strip Secure so local HTTP origins can retain the cookie.
 */
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
