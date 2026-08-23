import { platformRequest } from "@/api/platform-http";

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
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

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return false;
}

export type ResolvedPublicUser = {
  publicUserId: string;
  userIdentityId: string;
  displayName: string;
  maskedEmail?: string;
  status: string;
  isSelf: boolean;
};

export type ResolvedPublicOrganization = {
  publicOrganizationId: string;
  organizationId: string;
  displayName: string;
  status: string;
};

export function mapResolvedPublicUser(payload: unknown): ResolvedPublicUser {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid resolved public user.");
  }
  const publicUserId = readString(record, "publicUserId", "PublicUserId");
  const userIdentityId = readString(record, "userIdentityId", "UserIdentityId");
  const displayName = readString(record, "displayName", "DisplayName");
  const status = readString(record, "status", "Status");
  if (!publicUserId || !userIdentityId || !displayName || !status) {
    throw new Error("Invalid resolved public user.");
  }
  return {
    publicUserId,
    userIdentityId,
    displayName,
    maskedEmail: readString(record, "maskedEmail", "MaskedEmail"),
    status,
    isSelf: readBoolean(record, "isSelf", "IsSelf"),
  };
}

export function mapResolvedPublicOrganization(payload: unknown): ResolvedPublicOrganization {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid resolved public organization.");
  }
  const publicOrganizationId = readString(
    record,
    "publicOrganizationId",
    "PublicOrganizationId",
  );
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const displayName = readString(record, "displayName", "DisplayName");
  const status = readString(record, "status", "Status");
  if (!publicOrganizationId || !organizationId || !displayName || !status) {
    throw new Error("Invalid resolved public organization.");
  }
  return {
    publicOrganizationId,
    organizationId,
    displayName,
    status,
  };
}

export function resolvePublicUserId(
  baseUrl: string,
  publicUserIdOrQrPayload: string,
  purpose = "platform-admin-support",
  signal?: AbortSignal,
): Promise<ResolvedPublicUser> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/users/resolve-public-id",
    method: "POST",
    body: {
      publicUserIdOrQrPayload,
      purpose,
    },
    signal,
  }).then(mapResolvedPublicUser);
}

export function resolvePublicOrganizationId(
  baseUrl: string,
  publicOrganizationIdOrQrPayload: string,
  purpose = "platform-admin-support",
  signal?: AbortSignal,
): Promise<ResolvedPublicOrganization> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/organizations/resolve-public-id",
    method: "POST",
    body: {
      publicOrganizationIdOrQrPayload,
      purpose,
    },
    signal,
  }).then(mapResolvedPublicOrganization);
}

const GUID_PATTERN =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function isGuid(value: string): boolean {
  return GUID_PATTERN.test(value.trim());
}
