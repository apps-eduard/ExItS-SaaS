import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import {
  getDurableInstallationDeviceId,
  peekDurableInstallationDeviceId,
} from "@/workspace/browser-installation-identity";
import { authorizedPosDeviceContext, type PosDeviceContext } from "@/workspace/pos-device-context";
import type { BoundWorkspace } from "@/workspace/types";

/** Matches MAUI OfflineOperatingGrant.CurrentSchemaVersion (org + device binding). */
export const OFFLINE_OPERATING_GRANT_SCHEMA_VERSION = 3;

export const OFFLINE_OPERATING_GRANT_STORE_KEY = "exits.pos-client.offline-operating-grants.v1";

/** Browser PWA grant window — shorter than MAUI defaults; PIN unlock not yet implemented. */
export const OFFLINE_OPERATING_GRANT_DURATION_MS = 7 * 24 * 60 * 60 * 1000;

export type OfflineGrantScopeKind = "Organization" | "Personal";

export type StoredOfflineOperatingGrant = {
  schemaVersion: typeof OFFLINE_OPERATING_GRANT_SCHEMA_VERSION;
  userId: string;
  scopeKind: OfflineGrantScopeKind;
  organizationId: string | null;
  organizationDisplayName: string;
  branchId: string | null;
  branchName: string | null;
  installationDeviceId: string;
  posDeviceId: string | null;
  roleCode: string | null;
  displayName: string | null;
  username: string | null;
  issuedAtUtc: string;
  lastOnlineValidatedAtUtc: string;
  expiresAtUtc: string;
  integrity: string;
};

type GrantStoreDocument = {
  version: 1;
  grants: Record<string, StoredOfflineOperatingGrant>;
};

export type ColdStartGrantEvaluation =
  | { ok: true; grant: StoredOfflineOperatingGrant }
  | { ok: false; reason: ColdStartGrantDenialReason };

export type ColdStartGrantDenialReason =
  | "storage_unavailable"
  | "no_installation_device"
  | "no_grant"
  | "grant_expired"
  | "device_mismatch"
  | "integrity_failed"
  | "invalid_scope"
  | "user_mismatch";

export type EstablishOfflineOperatingGrantInput = {
  userId: string;
  scopeKind: OfflineGrantScopeKind;
  organizationId: string | null;
  organizationDisplayName: string;
  branchId: string | null;
  branchName: string | null;
  installationDeviceId: string;
  posDeviceId: string | null;
  roleCode?: string | null;
  displayName?: string | null;
  username?: string | null;
  now?: Date;
};

function canUseLocalStorage(): boolean {
  if (typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return false;
  }
  try {
    const probe = `${OFFLINE_OPERATING_GRANT_STORE_KEY}.probe`;
    window.localStorage.setItem(probe, "1");
    window.localStorage.removeItem(probe);
    return true;
  } catch {
    return false;
  }
}

function readStore(): GrantStoreDocument {
  if (!canUseLocalStorage()) {
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

function writeStore(document: GrantStoreDocument): boolean {
  if (!canUseLocalStorage()) {
    return false;
  }
  try {
    window.localStorage.setItem(OFFLINE_OPERATING_GRANT_STORE_KEY, JSON.stringify(document));
    return true;
  } catch {
    return false;
  }
}

function canonicalGrantPayload(
  grant: Omit<StoredOfflineOperatingGrant, "integrity">,
): string {
  return JSON.stringify({
    schemaVersion: grant.schemaVersion,
    userId: grant.userId,
    scopeKind: grant.scopeKind,
    organizationId: grant.organizationId,
    organizationDisplayName: grant.organizationDisplayName,
    branchId: grant.branchId,
    branchName: grant.branchName,
    installationDeviceId: grant.installationDeviceId,
    posDeviceId: grant.posDeviceId,
    roleCode: grant.roleCode,
    displayName: grant.displayName,
    username: grant.username,
    issuedAtUtc: grant.issuedAtUtc,
    lastOnlineValidatedAtUtc: grant.lastOnlineValidatedAtUtc,
    expiresAtUtc: grant.expiresAtUtc,
  });
}

async function deriveIntegrityKey(installationDeviceId: string): Promise<CryptoKey> {
  const material = new TextEncoder().encode(
    `exits-offline-grant-integrity:v1:${installationDeviceId}`,
  );
  const hash = await crypto.subtle.digest("SHA-256", material);
  return crypto.subtle.importKey(
    "raw",
    hash,
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign", "verify"],
  );
}

export async function computeGrantIntegrity(
  grant: Omit<StoredOfflineOperatingGrant, "integrity">,
): Promise<string> {
  const key = await deriveIntegrityKey(grant.installationDeviceId);
  const signature = await crypto.subtle.sign(
    "HMAC",
    key,
    new TextEncoder().encode(canonicalGrantPayload(grant)),
  );
  return [...new Uint8Array(signature)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

async function verifyGrantIntegrity(grant: StoredOfflineOperatingGrant): Promise<boolean> {
  if (!grant.integrity?.trim()) {
    return false;
  }
  const { integrity, ...rest } = grant;
  const expected = await computeGrantIntegrity(rest);
  return expected === integrity;
}

export function isOrganizationOfflineGrant(
  grant: StoredOfflineOperatingGrant,
  now: number = Date.now(),
): boolean {
  if (grant.schemaVersion !== OFFLINE_OPERATING_GRANT_SCHEMA_VERSION) {
    return false;
  }
  if (grant.scopeKind !== "Organization") {
    return false;
  }
  if (!grant.organizationId || !grant.branchId || !grant.posDeviceId) {
    return false;
  }
  const expiresAt = Date.parse(grant.expiresAtUtc);
  return Number.isFinite(expiresAt) && now < expiresAt;
}

export function isGrantExpired(
  grant: StoredOfflineOperatingGrant,
  now: number = Date.now(),
): boolean {
  const expiresAt = Date.parse(grant.expiresAtUtc);
  return !Number.isFinite(expiresAt) || now >= expiresAt;
}

export async function establishOfflineOperatingGrant(
  input: EstablishOfflineOperatingGrantInput,
): Promise<StoredOfflineOperatingGrant | null> {
  const now = input.now ?? new Date();
  const issuedAtUtc = now.toISOString();
  const expiresAtUtc = new Date(now.getTime() + OFFLINE_OPERATING_GRANT_DURATION_MS).toISOString();
  const withoutIntegrity: Omit<StoredOfflineOperatingGrant, "integrity"> = {
    schemaVersion: OFFLINE_OPERATING_GRANT_SCHEMA_VERSION,
    userId: input.userId,
    scopeKind: input.scopeKind,
    organizationId: input.organizationId,
    organizationDisplayName: input.organizationDisplayName,
    branchId: input.branchId,
    branchName: input.branchName,
    installationDeviceId: input.installationDeviceId,
    posDeviceId: input.posDeviceId,
    roleCode: input.roleCode ?? null,
    displayName: input.displayName ?? null,
    username: input.username ?? null,
    issuedAtUtc,
    lastOnlineValidatedAtUtc: issuedAtUtc,
    expiresAtUtc,
  };
  const integrity = await computeGrantIntegrity(withoutIntegrity);
  const grant: StoredOfflineOperatingGrant = { ...withoutIntegrity, integrity };

  const store = readStore();
  store.grants[input.userId] = grant;
  if (!writeStore(store)) {
    return null;
  }
  return grant;
}

export function clearOfflineOperatingGrant(userId: string): void {
  const store = readStore();
  if (!store.grants[userId]) {
    return;
  }
  delete store.grants[userId];
  writeStore(store);
}

export function clearAllOfflineOperatingGrants(): void {
  if (!canUseLocalStorage()) {
    return;
  }
  try {
    window.localStorage.removeItem(OFFLINE_OPERATING_GRANT_STORE_KEY);
  } catch {
    // ignore
  }
}

export async function evaluateColdStartOfflineGrant(options?: {
  userId?: string | null;
  installationDeviceId?: string | null;
  now?: Date;
}): Promise<ColdStartGrantEvaluation> {
  const durable = getDurableInstallationDeviceId();
  const installationDeviceId =
    options?.installationDeviceId?.trim() ||
    peekDurableInstallationDeviceId() ||
    (durable.ok ? durable.installationDeviceId : null);

  if (!installationDeviceId) {
    return { ok: false, reason: "no_installation_device" };
  }
  if (!canUseLocalStorage()) {
    return { ok: false, reason: "storage_unavailable" };
  }

  const nowMs = (options?.now ?? new Date()).getTime();
  const store = readStore();
  const candidates = Object.values(store.grants).filter((grant) => {
    if (options?.userId && grant.userId !== options.userId) {
      return false;
    }
    if (grant.installationDeviceId !== installationDeviceId) {
      return false;
    }
    if (isGrantExpired(grant, nowMs)) {
      return false;
    }
    if (grant.scopeKind === "Organization" && !isOrganizationOfflineGrant(grant, nowMs)) {
      return false;
    }
    return true;
  });

  if (candidates.length === 0) {
    return { ok: false, reason: "no_grant" };
  }

  candidates.sort(
    (left, right) =>
      Date.parse(right.lastOnlineValidatedAtUtc) - Date.parse(left.lastOnlineValidatedAtUtc),
  );

  for (const grant of candidates) {
    if (grant.installationDeviceId !== installationDeviceId) {
      continue;
    }
    if (!(await verifyGrantIntegrity(grant))) {
      continue;
    }
    return { ok: true, grant };
  }

  return { ok: false, reason: "integrity_failed" };
}

export function synthesizeSessionFromGrant(
  grant: StoredOfflineOperatingGrant,
): BrowserSessionSnapshot {
  return {
    userId: grant.userId,
    username: grant.username ?? undefined,
    displayName: grant.displayName ?? grant.organizationDisplayName,
    email: undefined,
    accountClass: grant.scopeKind === "Personal" ? "Personal" : "Organization",
    homeOrganizationId:
      grant.scopeKind === "Organization" ? (grant.organizationId ?? undefined) : undefined,
    organizationContextLocked: grant.scopeKind === "Organization",
    selectedOrganizationId: grant.organizationId ?? undefined,
    selectedOrganizationDisplayName: grant.organizationDisplayName,
    organizationSelectionState: grant.organizationId ? "Selected" : undefined,
  };
}

export function buildBoundWorkspaceFromGrant(
  grant: StoredOfflineOperatingGrant,
): BoundWorkspace | null {
  if (!isOrganizationOfflineGrant(grant) || !grant.organizationId || !grant.branchId) {
    return null;
  }
  return {
    organizationId: grant.organizationId,
    organizationDisplayName: grant.organizationDisplayName,
    branchId: grant.branchId,
    branchName: grant.branchName ?? grant.branchId,
    experience: "start_selling",
  };
}

export function buildPosDeviceFromGrant(grant: StoredOfflineOperatingGrant): PosDeviceContext | null {
  if (
    !isOrganizationOfflineGrant(grant) ||
    !grant.posDeviceId ||
    !grant.branchId ||
    !grant.installationDeviceId
  ) {
    return null;
  }
  return authorizedPosDeviceContext({
    installationDeviceId: grant.installationDeviceId,
    posDeviceId: grant.posDeviceId,
    registeredBranchId: grant.branchId,
  });
}

/** Capability facts for cold-start offline sell — never includes bearer/access tokens. */
export function buildColdStartSessionGrantFacts(
  grant: StoredOfflineOperatingGrant,
): SessionGrantResponse {
  return {
    accessToken: "",
    productAccessAllowed:
      grant.scopeKind === "Organization" && Boolean(grant.posDeviceId && grant.branchId),
    productAccessReasonCode: null,
    mappedPosRoleCode: grant.roleCode,
    membershipRole: null,
    organizationManagementAuthority:
      grant.roleCode?.localeCompare("Owner", undefined, { sensitivity: "accent" }) === 0,
  };
}

export function mapColdStartDenialToMessageKey(
  reason: ColdStartGrantDenialReason,
): "offline.coldStartLocked" | "offline.coldStartReconnect" {
  if (reason === "no_grant" || reason === "grant_expired") {
    return "offline.coldStartReconnect";
  }
  return "offline.coldStartLocked";
}
