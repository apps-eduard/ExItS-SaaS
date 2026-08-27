import { isOfflinePinAndDekConfigured } from "@/offline/local-store-key";
import {
  isGrantExpired,
  isOrganizationOfflineGrant,
  OFFLINE_OPERATING_GRANT_STORE_KEY,
  verifyGrantSignature,
  type StoredOfflineOperatingGrant,
} from "@/offline/offline-operating-grant";
import { organizationWebAllowsOfflineSession } from "@/runtime/organization-web-runtime-policy";
import { personalWebAllowsOfflineSession } from "@/runtime/personal-web-runtime-policy";
import { peekDurableInstallationDeviceId } from "@/workspace/browser-installation-identity";

export type OfflinePinProfile = {
  userId: string;
  displayName: string;
  organizationDisplayName: string;
  branchName: string | null;
  grant: StoredOfflineOperatingGrant;
};

type GrantStoreDocument = {
  version: 1;
  grants: Record<string, StoredOfflineOperatingGrant>;
};

function readGrantStore(): GrantStoreDocument {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return { version: 1, grants: {} };
  }
  try {
    const raw = window.localStorage.getItem(OFFLINE_OPERATING_GRANT_STORE_KEY);
    if (!raw) {
      return { version: 1, grants: {} };
    }
    const parsed = JSON.parse(raw) as Partial<GrantStoreDocument>;
    if (parsed?.version !== 1 || typeof parsed.grants !== "object" || parsed.grants === null) {
      return { version: 1, grants: {} };
    }
    return { version: 1, grants: parsed.grants as Record<string, StoredOfflineOperatingGrant> };
  } catch {
    return { version: 1, grants: {} };
  }
}

function profileLabel(grant: StoredOfflineOperatingGrant): string {
  return grant.displayName?.trim() || grant.username?.trim() || grant.userId;
}

/**
 * Lists users on this installation with a valid server-signed grant and offline PIN enrollment.
 * Branch/org context comes from each grant — never from role alone.
 *
 * Web/PWA online-only policies hide Organization and Personal profiles unless
 * `allowOfflineEngine` is set (unit tests / future Capacitor).
 */
export async function listEligibleOfflinePinProfiles(
  now: number = Date.now(),
  options?: { allowOfflineEngine?: boolean },
): Promise<OfflinePinProfile[]> {
  const installationDeviceId = peekDurableInstallationDeviceId()?.trim();
  if (!installationDeviceId) {
    return [];
  }

  const profiles: OfflinePinProfile[] = [];
  for (const grant of Object.values(readGrantStore().grants)) {
    if (grant.installationDeviceId !== installationDeviceId) {
      continue;
    }
    if (isGrantExpired(grant, now)) {
      continue;
    }
    if (grant.scopeKind === "Organization") {
      if (!organizationWebAllowsOfflineSession() && !options?.allowOfflineEngine) {
        continue;
      }
      if (!isOrganizationOfflineGrant(grant, now)) {
        continue;
      }
    }
    if (
      grant.scopeKind === "Personal" &&
      !personalWebAllowsOfflineSession() &&
      !options?.allowOfflineEngine
    ) {
      continue;
    }
    if (!(await verifyGrantSignature(grant))) {
      continue;
    }
    if (!isOfflinePinAndDekConfigured(grant.userId)) {
      continue;
    }
    profiles.push({
      userId: grant.userId,
      displayName: profileLabel(grant),
      organizationDisplayName: grant.organizationDisplayName,
      branchName: grant.branchName,
      grant,
    });
  }

  return profiles.sort((left, right) => left.displayName.localeCompare(right.displayName));
}

/** True when this installation has a stored grant that expired (PIN must not bypass expiry). */
export function hasExpiredOfflineGrantOnInstallation(now: number = Date.now()): boolean {
  const installationDeviceId = peekDurableInstallationDeviceId()?.trim();
  if (!installationDeviceId) {
    return false;
  }
  return Object.values(readGrantStore().grants).some(
    (grant) =>
      grant.installationDeviceId === installationDeviceId && isGrantExpired(grant, now),
  );
}
