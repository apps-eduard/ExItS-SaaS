import {
  clearCachedSecurityToken,
  currentSession,
  listAccountProfiles,
  selectAccountProfile,
  type AccountProfile,
  type PlatformSession,
} from "./platform-client";

function sameClass(value: string | null | undefined, expected: string) {
  return value?.trim().toLowerCase() === expected;
}

function activeProfile(profiles: AccountProfile[], accountClass: string) {
  return profiles.find((profile) => sameClass(profile.accountClass, accountClass) && sameClass(profile.status, "active"));
}

/**
 * After login, bind an existing authorized Personal profile when the identity has one.
 * Does not create a profile and does not move a locked Organization staff session.
 */
export async function preferAuthorizedPersonalProfile(): Promise<PlatformSession | null> {
  const session = await currentSession();
  if (!session || session.organizationContextLocked) return session;
  if (sameClass(session.accountClass, "personal") || sameClass(session.accountClass, "platform")) return session;
  try {
    const personal = activeProfile(await listAccountProfiles(), "personal");
    if (!personal) return session;
    clearCachedSecurityToken();
    await selectAccountProfile(personal.id);
    return (await currentSession()) ?? session;
  } catch {
    return session;
  }
}

/** Bind an existing authorized Personal profile. Returns false when none is available. */
export async function switchToAuthorizedPersonalProfile(): Promise<boolean> {
  const session = await currentSession();
  if (!session || session.organizationContextLocked || sameClass(session.accountClass, "platform")) return false;
  if (sameClass(session.accountClass, "personal")) return true;
  const personal = activeProfile(await listAccountProfiles(), "personal");
  if (!personal) return false;
  clearCachedSecurityToken();
  await selectAccountProfile(personal.id);
  const next = await currentSession();
  return sameClass(next?.accountClass, "personal");
}

/** Bind an existing authorized Organization profile. Returns false when none is available. */
export async function switchToAuthorizedOrganizationProfile(): Promise<boolean> {
  const session = await currentSession();
  if (!session || session.organizationContextLocked || sameClass(session.accountClass, "platform")) return false;
  const organization = activeProfile(await listAccountProfiles(), "organization");
  if (!organization) return false;
  clearCachedSecurityToken();
  await selectAccountProfile(organization.id);
  const next = await currentSession();
  return sameClass(next?.accountClass, "organization");
}
