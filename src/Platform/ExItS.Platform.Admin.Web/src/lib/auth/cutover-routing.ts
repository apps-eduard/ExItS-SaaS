import type { WebWorkspaceItem } from "@/api/auth/auth-types";
import { sanitizeReturnPath } from "@/lib/auth/safe-return-path";

export const WORKSPACE_APPS = {
  platform: "platform",
  organization: "organization",
  personal: "personal",
} as const;

export type ExperienceOrigins = {
  organization?: string | null;
  personal?: string | null;
};

const LOCAL_REACT_CLIENT_ORIGIN = "http://127.0.0.1:5177";
const PREVIEW_ADMIN_HOST = "admin.exitsapps.com";
const PREVIEW_PERSONAL_ORIGIN = "https://my.exitsapps.com";
const PREVIEW_POS_ORIGIN = "https://pos.exitsapps.com";

export function resolveExperienceOrigins(input: {
  organization?: string | null;
  personal?: string | null;
  localValidation: boolean;
  pageHost?: string | null;
}): ExperienceOrigins {
  const organization = sanitizeOrigin(input.organization);
  const personal = sanitizeOrigin(input.personal);
  if (organization || personal) {
    return {
      organization: organization ?? personal,
      personal: personal ?? organization,
    };
  }
  if ((input.pageHost ?? "").trim().toLowerCase() === PREVIEW_ADMIN_HOST) {
    return {
      organization: PREVIEW_POS_ORIGIN,
      personal: PREVIEW_PERSONAL_ORIGIN,
    };
  }
  if (input.localValidation) {
    return {
      organization: LOCAL_REACT_CLIENT_ORIGIN,
      personal: LOCAL_REACT_CLIENT_ORIGIN,
    };
  }
  return {};
}

export function sanitizeOrigin(raw: string | null | undefined): string | null {
  if (!raw || raw.trim().length === 0) {
    return null;
  }
  let parsed: URL;
  try {
    parsed = new URL(raw.trim());
  } catch {
    return null;
  }
  if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
    return null;
  }
  if (parsed.username || parsed.password) {
    return null;
  }
  return parsed.origin;
}

/**
 * After admin-site sign-in, a non-platform account leaves this host.
 * An organization membership opens POS. Otherwise the account opens Personal.
 */
export function choosePostAdminLoginWorkspace(
  items: readonly WebWorkspaceItem[],
  selectedOrganizationId?: string | null,
): WebWorkspaceItem | null {
  const organizations = items.filter(
    (item) => item.app.toLowerCase() === WORKSPACE_APPS.organization,
  );
  if (organizations.length > 0) {
    const selected = selectedOrganizationId
      ? organizations.find((item) => item.organizationId === selectedOrganizationId)
      : undefined;
    return selected ?? organizations[0] ?? null;
  }
  return items.find((item) => item.app.toLowerCase() === WORKSPACE_APPS.personal) ?? null;
}

export function chooseAutomaticWorkspace(items: readonly WebWorkspaceItem[]): WebWorkspaceItem | null {
  if (items.length === 1) {
    return items[0] ?? null;
  }
  const hasOrganization = items.some(
    (item) => item.app.toLowerCase() === WORKSPACE_APPS.organization,
  );
  if (!hasOrganization) {
    return items.find((item) => item.app.toLowerCase() === WORKSPACE_APPS.platform) ?? null;
  }
  return null;
}

export function workspaceReturnPath(item: WebWorkspaceItem): string {
  const app = item.app.toLowerCase();
  if (app === WORKSPACE_APPS.personal) {
    return "/personal";
  }
  if (app === WORKSPACE_APPS.organization) {
    return "/";
  }
  return "/admin";
}

export type WorkspaceLaunch =
  | { kind: "platform"; path: string }
  | { kind: "handoff"; origin: string; targetApp: string; returnPath: string; organizationId: string | null }
  | { kind: "origin-missing" };

export function planWorkspaceLaunch(
  item: WebWorkspaceItem,
  origins: ExperienceOrigins,
): WorkspaceLaunch {
  const app = item.app.toLowerCase();
  if (app === WORKSPACE_APPS.platform) {
    return { kind: "platform", path: "/admin" };
  }
  const origin =
    app === WORKSPACE_APPS.personal
      ? sanitizeOrigin(origins.personal)
      : app === WORKSPACE_APPS.organization
        ? sanitizeOrigin(origins.organization)
        : null;
  if (!origin) {
    return { kind: "origin-missing" };
  }
  return {
    kind: "handoff",
    origin,
    targetApp: app,
    returnPath: workspaceReturnPath(item),
    organizationId: item.organizationId,
  };
}

export function buildSessionEstablishUrl(
  origin: string,
  ticket: string,
  returnPath: string,
): string | null {
  const safeOrigin = sanitizeOrigin(origin);
  const safeTicket = ticket.trim();
  const safePath = sanitizeReturnPath(returnPath) ?? "/";
  if (!safeOrigin || safeTicket.length === 0 || safeTicket.length > 128) {
    return null;
  }
  const url = new URL("/session/establish", safeOrigin);
  url.searchParams.set("ticket", safeTicket);
  url.searchParams.set("returnPath", safePath);
  return url.toString();
}

export function replaceBrowserLocation(url: string): void {
  window.location.assign(url);
}

export function readExternalCallbackQuery(params: URLSearchParams): {
  sessionToken: string;
  suggestRecoveryEmail: string | null;
  returnPath: string | null;
  unsafeReturn: boolean;
} {
  const sessionToken = params.get("sessionToken")?.trim() ?? "";
  const suggest = params.get("suggestRecoveryEmail")?.trim() ?? "";
  const rawReturn = params.get("return");
  if (rawReturn && sanitizeReturnPath(rawReturn) === null) {
    return {
      sessionToken,
      suggestRecoveryEmail: suggest.length > 0 ? suggest : null,
      returnPath: null,
      unsafeReturn: true,
    };
  }
  return {
    sessionToken,
    suggestRecoveryEmail: suggest.length > 0 ? suggest : null,
    returnPath: sanitizeReturnPath(rawReturn),
    unsafeReturn: false,
  };
}

export function externalChallengeUrl(apiBaseUrl: string, provider: "google" | "facebook", pageOrigin: string): string | null {
  const origin = sanitizeOrigin(pageOrigin);
  if (!origin) {
    return null;
  }
  const returnUrl = new URL("/admin/external-login-callback", origin).toString();
  const base = apiBaseUrl.replace(/\/+$/, "");
  return `${base}/api/v1/platform/auth/external/${provider}/challenge?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export type ProductEntryDecision =
  | { status: "allowed"; accessToken: string; expiresAtUtc: string; productCode: string | null }
  | { status: "denied"; reasonCode: string | null };

export function decideProductEntry(input: {
  accessToken: string;
  expiresAtUtc: string;
  productCode: string | null;
  productAccessAllowed: boolean | null;
  productAccessReasonCode: string | null;
}): ProductEntryDecision {
  if (input.productAccessAllowed === false) {
    return { status: "denied", reasonCode: input.productAccessReasonCode };
  }
  return {
    status: "allowed",
    accessToken: input.accessToken,
    expiresAtUtc: input.expiresAtUtc,
    productCode: input.productCode,
  };
}
