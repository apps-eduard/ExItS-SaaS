import { mapEntitlementSnapshot } from "@/api/organizations/organization-client";
import type { EntitlementSnapshot, FeatureOverride } from "@/api/organizations/entitlement-list-query";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";

function entitlementsPath(organizationId: string, productCode: string, suffix: string): string {
  return `/api/v1/platform/organizations/${organizationId}/products/${encodeURIComponent(productCode)}/entitlements${suffix}`;
}

function requireSnapshot(payload: unknown): EntitlementSnapshot {
  return mapEntitlementSnapshot(payload);
}

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

function readNumber(record: Record<string, unknown>, ...keys: string[]): number | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
  }
  return undefined;
}

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return undefined;
}

export function mapFeatureOverride(payload: unknown): FeatureOverride {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid feature override.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const productCode = readString(record, "productCode", "ProductCode");
  const featureCode = readString(record, "featureCode", "FeatureCode");
  const enabled = readBoolean(record, "enabled", "Enabled");
  const status = readString(record, "status", "Status");
  if (!id || !organizationId || !productCode || !featureCode || enabled === undefined || !status) {
    throw new Error("Invalid feature override.");
  }
  return {
    id,
    organizationId,
    productCode,
    featureCode,
    enabled,
    status,
    numericLimit: readNumber(record, "numericLimit", "NumericLimit"),
    reason: readString(record, "reason", "Reason"),
    effectiveFromUtc: readString(record, "effectiveFromUtc", "EffectiveFromUtc"),
    expiresAtUtc: readString(record, "expiresAtUtc", "ExpiresAtUtc"),
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    createdByUserId: readString(record, "createdByUserId", "CreatedByUserId"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
    revokedAtUtc: readString(record, "revokedAtUtc", "RevokedAtUtc"),
    revokedByUserId: readString(record, "revokedByUserId", "RevokedByUserId"),
    revocationReason: readString(record, "revocationReason", "RevocationReason"),
  };
}

export type GenerateEntitlementSnapshotBody = {
  expectedNextVersion?: number | null;
};

export type ReconcileEntitlementBody = {
  reason?: string | null;
};

export type CreateFeatureOverrideBody = {
  featureCode: string;
  enabled: boolean;
  reason: string;
  createdByUserId: string;
  numericLimit?: number | null;
  expiresAtUtc?: string | null;
};

export type RevokeFeatureOverrideBody = {
  reason: string;
  revokedByUserId: string;
};

export function generateEntitlementSnapshot(
  baseUrl: string,
  organizationId: string,
  productCode: string,
  body?: GenerateEntitlementSnapshotBody,
  signal?: AbortSignal,
): Promise<EntitlementSnapshot> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: entitlementsPath(organizationId, productCode, "/snapshots"),
    body: body ?? {},
    signal,
  }).then(requireSnapshot);
}

export function reconcileEntitlementSnapshot(
  baseUrl: string,
  organizationId: string,
  productCode: string,
  body?: ReconcileEntitlementBody,
  signal?: AbortSignal,
): Promise<EntitlementSnapshot> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: entitlementsPath(organizationId, productCode, "/reconcile"),
    body: body ?? {},
    signal,
  }).then(requireSnapshot);
}

export function createFeatureOverride(
  baseUrl: string,
  organizationId: string,
  productCode: string,
  body: CreateFeatureOverrideBody,
  signal?: AbortSignal,
): Promise<FeatureOverride> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/products/${encodeURIComponent(productCode)}/feature-overrides`,
    body,
    signal,
  }).then(mapFeatureOverride);
}

export function revokeFeatureOverride(
  baseUrl: string,
  overrideId: string,
  body: RevokeFeatureOverrideBody,
  signal?: AbortSignal,
): Promise<FeatureOverride> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/feature-overrides/${overrideId}/revoke`,
    body,
    signal,
  }).then(mapFeatureOverride);
}
