import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import {
  mapPlatformUserDetail,
  mapPlatformUserListItem,
} from "@/api/users/user-client";
import type { PlatformUserDetail } from "@/api/users/user-types";
import { withQuery } from "@/lib/http/query-string";

export type CreatePlatformUserRequest = {
  displayName: string;
  email: string;
  platformRole: string;
  username?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  employeeCode?: string | null;
  sendEmailVerification?: boolean;
  requireEmailVerification?: boolean;
  initialPassword?: string | null;
};

export type UpdatePlatformUserRequest = {
  displayName: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  employeeCode?: string | null;
};

export type LifecycleReasonRequest = {
  reason?: string | null;
  global?: boolean;
  actorPassword?: string | null;
  mfaCode?: string | null;
};

export type ReactivatePlatformUserRequest = {
  reason?: string | null;
  actorPassword?: string | null;
  mfaCode?: string | null;
  global?: boolean;
};

export type PlatformCredentialStatus = {
  userId: string;
  hasPassword: boolean;
  emailVerified: boolean;
  emailVerifiedAtUtc?: string | null;
  isLockedOut: boolean;
  lockoutEndUtc?: string | null;
  failedAccessCount: number;
  passwordChangedAtUtc?: string | null;
};

export type OrganizationMembership = {
  id: string;
  organizationId: string;
  userId: string;
  role: string;
  status: string;
  roleDisplay?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type ProductAccessAssignment = {
  id: string;
  userId: string;
  organizationId: string;
  membershipId: string;
  productCode: string;
  status: string;
  grantedAtUtc: string;
};

function asRecord(payload: unknown): Record<string, unknown> | null {
  return typeof payload === "object" && payload !== null ? (payload as Record<string, unknown>) : null;
}

function readString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return undefined;
}

function readBool(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return false;
}

function readNumber(record: Record<string, unknown>, ...keys: string[]): number {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return 0;
}

export function mapCredentialStatus(payload: unknown): PlatformCredentialStatus {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid credential status.");
  }
  const userId = readString(record, "userId", "UserId");
  if (!userId) {
    throw new Error("Invalid credential status.");
  }
  return {
    userId,
    hasPassword: readBool(record, "hasPassword", "HasPassword"),
    emailVerified: readBool(record, "emailVerified", "EmailVerified"),
    emailVerifiedAtUtc: readString(record, "emailVerifiedAtUtc", "EmailVerifiedAtUtc") ?? null,
    isLockedOut: readBool(record, "isLockedOut", "IsLockedOut"),
    lockoutEndUtc: readString(record, "lockoutEndUtc", "LockoutEndUtc") ?? null,
    failedAccessCount: readNumber(record, "failedAccessCount", "FailedAccessCount"),
    passwordChangedAtUtc: readString(record, "passwordChangedAtUtc", "PasswordChangedAtUtc") ?? null,
  };
}

export function mapOrganizationMembership(payload: unknown): OrganizationMembership {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid membership.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const userId = readString(record, "userId", "UserId");
  const role = readString(record, "role", "Role");
  const status = readString(record, "status", "Status");
  const createdAtUtc = readString(record, "createdAtUtc", "CreatedAtUtc");
  const updatedAtUtc = readString(record, "updatedAtUtc", "UpdatedAtUtc");
  if (!id || !organizationId || !userId || !role || !status || !createdAtUtc || !updatedAtUtc) {
    throw new Error("Invalid membership.");
  }
  return {
    id,
    organizationId,
    userId,
    role,
    status,
    roleDisplay: readString(record, "roleDisplay", "RoleDisplay") ?? null,
    createdAtUtc,
    updatedAtUtc,
  };
}

export function mapProductAccess(payload: unknown): ProductAccessAssignment {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid product access.");
  }
  const id = readString(record, "id", "Id");
  const userId = readString(record, "userId", "UserId");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const membershipId = readString(record, "membershipId", "MembershipId");
  const productCode = readString(record, "productCode", "ProductCode");
  const status = readString(record, "status", "Status");
  const grantedAtUtc = readString(record, "grantedAtUtc", "GrantedAtUtc");
  if (!id || !userId || !organizationId || !membershipId || !productCode || !status || !grantedAtUtc) {
    throw new Error("Invalid product access.");
  }
  return { id, userId, organizationId, membershipId, productCode, status, grantedAtUtc };
}

export function createPlatformUser(
  baseUrl: string,
  body: CreatePlatformUserRequest,
  signal?: AbortSignal,
): Promise<PlatformUserDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/users",
    method: "POST",
    body,
    signal,
  }).then(mapPlatformUserDetail);
}

export function updatePlatformUser(
  baseUrl: string,
  userId: string,
  body: UpdatePlatformUserRequest,
  signal?: AbortSignal,
): Promise<PlatformUserDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}`,
    method: "PUT",
    body,
    signal,
  }).then(mapPlatformUserDetail);
}

export function suspendPlatformUser(
  baseUrl: string,
  userId: string,
  body: LifecycleReasonRequest,
  signal?: AbortSignal,
): Promise<PlatformUserDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/suspend`,
    method: "POST",
    body,
    signal,
  }).then(mapPlatformUserDetail);
}

export function reactivatePlatformUser(
  baseUrl: string,
  userId: string,
  body: ReactivatePlatformUserRequest,
  signal?: AbortSignal,
): Promise<PlatformUserDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/reactivate`,
    method: "POST",
    body,
    signal,
  }).then(mapPlatformUserDetail);
}

export function deactivatePlatformUser(
  baseUrl: string,
  userId: string,
  body: LifecycleReasonRequest,
  signal?: AbortSignal,
): Promise<PlatformUserDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/deactivate`,
    method: "POST",
    body,
    signal,
  }).then(mapPlatformUserDetail);
}

export function movePlatformUserToSuspended(
  baseUrl: string,
  userId: string,
  body: LifecycleReasonRequest,
  signal?: AbortSignal,
): Promise<PlatformUserDetail> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/move-to-suspended`,
    method: "POST",
    body,
    signal,
  }).then(mapPlatformUserDetail);
}

export function getPlatformUserCredentials(
  baseUrl: string,
  userId: string,
  signal?: AbortSignal,
): Promise<PlatformCredentialStatus> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/credentials`,
    signal,
  }).then(mapCredentialStatus);
}

export function setPlatformUserPassword(
  baseUrl: string,
  userId: string,
  password: string,
  signal?: AbortSignal,
): Promise<PlatformCredentialStatus> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/credentials/password`,
    method: "PUT",
    body: { password },
    signal,
  }).then(mapCredentialStatus);
}

export function unlockPlatformUserCredential(
  baseUrl: string,
  userId: string,
  signal?: AbortSignal,
): Promise<PlatformCredentialStatus> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/credentials/unlock`,
    method: "POST",
    body: {},
    signal,
  }).then(mapCredentialStatus);
}

export function markPlatformUserEmailVerified(
  baseUrl: string,
  userId: string,
  signal?: AbortSignal,
): Promise<PlatformCredentialStatus> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/users/${encodeURIComponent(userId)}/credentials/email-verified`,
    method: "POST",
    body: {},
    signal,
  }).then(mapCredentialStatus);
}

export function listPlatformUserMemberships(
  baseUrl: string,
  userId: string,
  signal?: AbortSignal,
): Promise<PagedResult<OrganizationMembership>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery(`/api/v1/platform/users/${encodeURIComponent(userId)}/memberships`, {
      page: 1,
      pageSize: 50,
    }),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return { ...page, items: page.items.map(mapOrganizationMembership) };
  });
}

export function listPlatformUserProductAccess(
  baseUrl: string,
  userId: string,
  signal?: AbortSignal,
): Promise<PagedResult<ProductAccessAssignment>> {
  return platformRequest<unknown>(baseUrl, {
    path: withQuery(`/api/v1/platform/users/${encodeURIComponent(userId)}/product-access`, {
      page: 1,
      pageSize: 50,
    }),
    signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return { ...page, items: page.items.map(mapProductAccess) };
  });
}

/** Re-export for callers that need list mapping after create. */
export { mapPlatformUserListItem };
