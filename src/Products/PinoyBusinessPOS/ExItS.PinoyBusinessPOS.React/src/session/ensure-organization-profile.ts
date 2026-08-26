import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import {
  ensureAccountProfile,
  listAccountProfiles,
  selectAccountProfile,
} from "@/api/platform/platform-auth-client";
import { sessionAccountClass } from "@/session/account-class";

function isActiveProfile(status: string): boolean {
  return status.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

/**
 * MAUI parity: Personal owners with memberships must ensure + select an Organization
 * AccountProfile before organization-context / workspace bind.
 * Does not invent membership or product access.
 */
export async function ensureOrganizationSessionProfile(input: {
  session: BrowserSessionSnapshot | null;
  refreshSession: () => Promise<unknown>;
}): Promise<{ ok: true; session: BrowserSessionSnapshot } | { ok: false; detail: string }> {
  const currentClass = sessionAccountClass(input.session);
  if (currentClass === "Organization") {
    return { ok: true, session: input.session! };
  }
  if (currentClass === "Platform") {
    return { ok: false, detail: "Platform sessions cannot open organization POS workspaces." };
  }

  const listed = await listAccountProfiles();
  if (!listed.ok) {
    return { ok: false, detail: listed.body?.detail ?? "Account profiles could not be loaded." };
  }

  let organizationProfile = listed.profiles.find(
    (profile) =>
      profile.accountClass.localeCompare("Organization", undefined, { sensitivity: "accent" }) ===
        0 && isActiveProfile(profile.status),
  );

  if (!organizationProfile) {
    const ensured = await ensureAccountProfile("Organization");
    if (!ensured.ok) {
      return {
        ok: false,
        detail: ensured.body?.detail ?? "Organization account profile could not be ensured.",
      };
    }
    organizationProfile = ensured.profile;
  }

  const selected = await selectAccountProfile(organizationProfile.id);
  if (!selected.ok) {
    return {
      ok: false,
      detail: selected.body?.detail ?? "Organization account profile could not be selected.",
    };
  }

  await input.refreshSession();
  return { ok: true, session: selected.session };
}
