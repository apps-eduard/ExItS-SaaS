import {
  ensureAccountProfile,
  listAccountProfiles,
  selectAccountProfile,
  type BrowserSessionSnapshot,
} from "@/api/auth";
import { toUserFacingError } from "@/api/user-facing-error";
import {
  isOrganizationContextLocked,
  sessionAccountClass,
} from "./account-class";

function isActiveProfile(status: string): boolean {
  return status.localeCompare("Active", undefined, { sensitivity: "accent" }) === 0;
}

/**
 * Owner Personal ↔ Organization switch: select Personal AccountProfile via Platform APIs.
 * Staff principals (`OrganizationContextLocked`) must never use this path.
 * Does not invent membership or bypass backend authorization.
 */
export async function ensurePersonalSessionProfile(input: {
  session: BrowserSessionSnapshot | null;
  refreshSession: () => Promise<unknown>;
  signal?: AbortSignal;
}): Promise<
  | { ok: true; session: BrowserSessionSnapshot }
  | { ok: false; detail: string; status?: number }
> {
  if (isOrganizationContextLocked(input.session)) {
    return {
      ok: false,
      detail: "Organization staff sessions cannot switch to Personal.",
      status: 403,
    };
  }

  const currentClass = sessionAccountClass(input.session);
  if (currentClass === "Personal") {
    return { ok: true, session: input.session! };
  }
  if (currentClass === "Platform") {
    return {
      ok: false,
      detail: "Platform sessions cannot open Personal Home.",
      status: 403,
    };
  }

  try {
    const listed = await listAccountProfiles(input.signal);
    if (!listed.ok) {
      return {
        ok: false,
        detail: toUserFacingError(
          listed.error,
          "Account profiles could not be loaded.",
        ),
        status: listed.status,
      };
    }

    let personalProfile = listed.profiles.find(
      (profile) =>
        profile.accountClass.localeCompare("Personal", undefined, {
          sensitivity: "accent",
        }) === 0 && isActiveProfile(profile.status),
    );

    if (!personalProfile) {
      const ensured = await ensureAccountProfile("Personal", input.signal);
      if (!ensured.ok) {
        return {
          ok: false,
          detail: toUserFacingError(
            ensured.error,
            "Personal account profile could not be ensured.",
          ),
          status: ensured.status,
        };
      }
      personalProfile = ensured.profile;
    }

    const selected = await selectAccountProfile(personalProfile.id, input.signal);
    if (!selected.ok) {
      return {
        ok: false,
        detail: toUserFacingError(
          selected.error,
          "Personal account profile could not be selected.",
        ),
        status: selected.status,
      };
    }

    await input.refreshSession();
    return { ok: true, session: selected.session };
  } catch (error) {
    return {
      ok: false,
      detail: toUserFacingError(error, "Could not switch to the Personal account."),
    };
  }
}

/** Unlocked Organization owner sessions may enter Personal Home after authorized profile select. */
export function shouldEnsurePersonalHomeProfile(
  session: BrowserSessionSnapshot | null | undefined,
): boolean {
  if (!session) return false;
  if (isOrganizationContextLocked(session)) return false;
  return sessionAccountClass(session) === "Organization";
}
