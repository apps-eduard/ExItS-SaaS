import { isPublicUserId, normalizePublicUserId } from "@/lib/exits-qr/envelope";

/** Public QR origin. Never encode localhost into a shareable Personal QR. */
export const PERSONAL_CONNECT_PUBLIC_ORIGIN = "https://my.exitsapps.com";

export function buildPersonalConnectPath(publicUserId: string): string {
  const id = normalizePublicUserId(publicUserId);
  if (!isPublicUserId(id)) {
    throw new Error("Invalid public ExItS ID.");
  }
  return `/connect/${id}`;
}

export function buildCanonicalPersonalConnectUrl(publicUserId: string): string {
  return `${PERSONAL_CONNECT_PUBLIC_ORIGIN}${buildPersonalConnectPath(publicUserId)}`;
}

export function normalizePersonalConnectPath(path: string | null | undefined): string | null {
  if (!path) {
    return null;
  }
  const [pathname] = path.split(/[?#]/);
  if (!pathname || pathname.includes("\\") || pathname.includes("//")) {
    return null;
  }
  const match = pathname.match(/^\/connect\/(EX-\d{4}-\d{4})$/i);
  if (!match || !isPublicUserId(match[1])) {
    return null;
  }
  return `/connect/${normalizePublicUserId(match[1])}`;
}

export function isPersonalConnectPath(path: string | null | undefined): path is string {
  return normalizePersonalConnectPath(path) !== null;
}

export function buildPersonalExternalAuthReturnPath(
  continuePath: string | null | undefined,
): string {
  const normalized = normalizePersonalConnectPath(continuePath);
  if (!normalized) {
    return "/external-login-callback?target=personal";
  }
  return `/external-login-callback?target=personal&continue=${encodeURIComponent(normalized)}`;
}
