import type { ServerSignedOfflineOperatingGrantDto } from "@/api/pos/pos-offline-operating-grant-client";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import {
  getDurableInstallationDeviceId,
  peekDurableInstallationDeviceId,
} from "@/workspace/browser-installation-identity";
import { authorizedPosDeviceContext, type PosDeviceContext } from "@/workspace/pos-device-context";
import type { BoundWorkspace } from "@/workspace/types";
import {
  canonicalizeOfflineOperatingGrant,
  scopeKindToNumeric,
  verifyOfflineOperatingGrantSignature,
} from "@/offline/server-signed-offline-grant";
import { organizationWebAllowsOfflineSession } from "@/runtime/organization-web-runtime-policy";

/** Matches server ServerSignedOfflineOperatingGrant.CurrentSchemaVersion. */
export const OFFLINE_OPERATING_GRANT_SCHEMA_VERSION = 4;

export const OFFLINE_OPERATING_GRANT_STORE_KEY = "exits.pos-client.offline-operating-grants.v1";

/** Legacy FIX01 schema — rejected unless migrated to server-signed v4. */
export const LEGACY_OFFLINE_OPERATING_GRANT_SCHEMA_VERSION = 3;

export type OfflineGrantScopeKind = "Organization" | "Personal";

export type StoredOfflineOperatingGrant = {
  grantId: string;
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
  signature: string;
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
  | "signature_failed"
  | "unsupported_schema"
  | "invalid_scope"
  | "user_mismatch";

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

function isLegacyV3Grant(raw: unknown): boolean {
  if (typeof raw !== "object" || raw === null) {
    return false;
  }
  const record = raw as Record<string, unknown>;
  return record.schemaVersion === LEGACY_OFFLINE_OPERATING_GRANT_SCHEMA_VERSION && "integrity" in record;
}

export function mapServerGrantDto(dto: ServerSignedOfflineOperatingGrantDto): StoredOfflineOperatingGrant {
  return {
    grantId: dto.grantId,
    schemaVersion: OFFLINE_OPERATING_GRANT_SCHEMA_VERSION,
    userId: dto.userId,
    scopeKind: dto.scopeKind,
    organizationId: dto.organizationId,
    organizationDisplayName: dto.organizationDisplayName,
    branchId: dto.branchId,
    branchName: dto.branchName,
    installationDeviceId: dto.installationDeviceId,
    posDeviceId: dto.posDeviceId,
    roleCode: dto.roleCode,
    displayName: dto.displayName,
    username: dto.username,
    issuedAtUtc: dto.issuedAtUtc,
    lastOnlineValidatedAtUtc: dto.lastOnlineValidatedAtUtc,
    expiresAtUtc: dto.expiresAtUtc,
    signature: dto.signature,
  };
}

export async function verifyGrantSignature(grant: StoredOfflineOperatingGrant): Promise<boolean> {
  if (grant.schemaVersion !== OFFLINE_OPERATING_GRANT_SCHEMA_VERSION) {
    return false;
  }
  const canonical = canonicalizeOfflineOperatingGrant({
    grantId: grant.grantId,
    schemaVersion: grant.schemaVersion,
    userId: grant.userId,
    scopeKind: scopeKindToNumeric(grant.scopeKind),
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
  return verifyOfflineOperatingGrantSignature(canonical, grant.signature);
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

/** Persist a server-issued grant. The browser cannot mint grants locally. */
export function persistServerSignedGrant(grant: StoredOfflineOperatingGrant): boolean {
  if (grant.schemaVersion !== OFFLINE_OPERATING_GRANT_SCHEMA_VERSION || !grant.signature?.trim()) {
    return false;
  }
  const store = readStore();
  store.grants[grant.userId] = grant;
  return writeStore(store);
}

export function persistServerSignedGrantFromApi(dto: ServerSignedOfflineOperatingGrantDto): boolean {
  return persistServerSignedGrant(mapServerGrantDto(dto));
}

export function peekStoredOfflineGrant(userId: string): StoredOfflineOperatingGrant | null {
  const store = readStore();
  return store.grants[userId] ?? null;
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
  /** Opt into Organization cold-start for engine tests / future Capacitor. */
  allowOrganizationOfflineEngine?: boolean;
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

  for (const raw of Object.values(store.grants)) {
    if (isLegacyV3Grant(raw)) {
      return { ok: false, reason: "unsupported_schema" };
    }
  }

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
    if (grant.scopeKind === "Organization") {
      // Organization Web/PWA is online-only: do not cold-start into an org offline session.
      // Personal grants remain eligible for Personal offline (separate task).
      // Engine / future Capacitor tests may pass allowOrganizationOfflineEngine.
      if (
        !organizationWebAllowsOfflineSession() &&
        !options?.allowOrganizationOfflineEngine
      ) {
        return false;
      }
      if (!isOrganizationOfflineGrant(grant, nowMs)) {
        return false;
      }
    }
    return grant.schemaVersion === OFFLINE_OPERATING_GRANT_SCHEMA_VERSION;
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
    if (!(await verifyGrantSignature(grant))) {
      continue;
    }
    return { ok: true, grant };
  }

  return { ok: false, reason: "signature_failed" };
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
): "offline.coldStartLocked" | "offline.coldStartReconnect" | "offline.unsupportedGrantSchema" {
  if (reason === "unsupported_schema") {
    return "offline.unsupportedGrantSchema";
  }
  if (reason === "no_grant" || reason === "grant_expired") {
    return "offline.coldStartReconnect";
  }
  return "offline.coldStartLocked";
}
