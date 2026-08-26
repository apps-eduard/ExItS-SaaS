import type { MessageKey } from "@/lib/i18n/messages";

const ACTION_KEYS: Record<string, MessageKey> = {
  "platform.auth.login_succeeded": "dashboard.audit.action.signedIn",
  "platform.auth.login_failed": "dashboard.audit.action.signInFailed",
  "platform.auth.logout": "dashboard.audit.action.signedOut",
};

const TYPE_KEYS: Record<string, MessageKey> = {
  PlatformAuthSession: "dashboard.audit.type.authSession",
  PlatformUser: "dashboard.audit.type.platformUser",
};

const PLATFORM_USER_ACTOR = /^platform-user:([0-9a-f-]{36})$/i;

export function fallbackHumanizeCode(code: string): string {
  const spaced = code
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[._-]+/g, " ")
    .trim()
    .replace(/\s+/g, " ");
  if (!spaced) {
    return code;
  }
  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
}

export function presentAuditAction(
  code: string,
  t: (key: MessageKey) => string,
): { label: string; raw: string } {
  const key = ACTION_KEYS[code];
  return { label: key ? t(key) : fallbackHumanizeCode(code), raw: code };
}

export function presentAuditType(
  type: string,
  t: (key: MessageKey) => string,
): { label: string; raw: string } {
  const key = TYPE_KEYS[type];
  return { label: key ? t(key) : fallbackHumanizeCode(type), raw: type };
}

export function compactGuid(guid: string): string {
  const normalized = guid.replace(/-/g, "");
  if (normalized.length < 12) {
    return guid;
  }
  return `${guid.slice(0, 8)}…${guid.slice(-5)}`;
}

export function presentAuditActor(
  actorIdentifier: string,
  t: (key: MessageKey) => string,
): { label: string; detail?: string; raw: string } {
  const match = PLATFORM_USER_ACTOR.exec(actorIdentifier.trim());
  const guid = match?.[1];
  if (guid) {
    return {
      label: t("dashboard.audit.type.platformUser"),
      detail: compactGuid(guid),
      raw: actorIdentifier,
    };
  }
  return { label: actorIdentifier, raw: actorIdentifier };
}
