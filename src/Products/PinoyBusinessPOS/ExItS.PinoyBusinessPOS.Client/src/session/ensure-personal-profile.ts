import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import {
  ensureAccountProfile,
  listAccountProfiles,
  selectAccountProfile,
} from "@/api/platform/platform-auth-client";
import { isOrganizationContextLocked, sessionAccountClass } from "@/session/account-class";

function isActiveProfile(status: string): boolean {
  return status.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

/**
 * Owner Personal ↔ Organization switch: select Personal AccountProfile.
 * Staff principals (`OrganizationContextLocked`) must never use this path.
 */
export async function ensurePersonalSessionProfile(input: {
  session: BrowserSessionSnapshot | null;
  refreshSession: () => Promise<unknown>;
}): Promise<{ ok: true; session: BrowserSessionSnapshot } | { ok: false; detail: string }> {
  if (isOrganizationContextLocked(input.session)) {
    return {
      ok: false,
      detail: "Organization staff sessions cannot switch to Personal.",
    };
  }

  const currentClass = sessionAccountClass(input.session);
  if (currentClass === "Personal") {
    return { ok: true, session: input.session! };
  }
  if (currentClass === "Platform") {
    return { ok: false, detail: "Platform sessions cannot open Personal." };
  }

  const listed = await listAccountProfiles();
  if (!listed.ok) {
    return { ok: false, detail: listed.body?.detail ?? "Account profiles could not be loaded." };
  }

  let personalProfile = listed.profiles.find(
    (profile) =>
      profile.accountClass.localeCompare("Personal", undefined, { sensitivity: "accent" }) === 0 &&
      isActiveProfile(profile.status),
  );

  if (!personalProfile) {
    const ensured = await ensureAccountProfile("Personal");
    if (!ensured.ok) {
      return {
        ok: false,
        detail: ensured.body?.detail ?? "Personal account profile could not be ensured.",
      };
    }
    personalProfile = ensured.profile;
  }

  const selected = await selectAccountProfile(personalProfile.id);
  if (!selected.ok) {
    return {
      ok: false,
      detail: selected.body?.detail ?? "Personal account profile could not be selected.",
    };
  }

  await input.refreshSession();
  return { ok: true, session: selected.session };
}
