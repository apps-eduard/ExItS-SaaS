import { platformRequest } from "@/api/platform-http";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";
import type { EnabledProduct, ProductLocalRoleGrant } from "@/api/organizations/organization-types";

function asRecord(payload: unknown): Record<string, unknown> | null {
  if (typeof payload !== "object" || payload === null) {
    return null;
  }
  return payload as Record<string, unknown>;
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

function readBoolean(record: Record<string, unknown>, ...keys: string[]): boolean | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "boolean") {
      return value;
    }
  }
  return undefined;
}

export function mapEnabledProduct(payload: unknown): EnabledProduct {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid enabled product.");
  }
  const productCode = readString(record, "productCode", "ProductCode");
  const displayName = readString(record, "displayName", "DisplayName");
  const entitlementActive = readBoolean(record, "entitlementActive", "EntitlementActive");
  const productAccessAssigned = readBoolean(record, "productAccessAssigned", "ProductAccessAssigned");
  const productLocalRoleGranted = readBoolean(
    record,
    "productLocalRoleGranted",
    "ProductLocalRoleGranted",
  );
  const canLaunch = readBoolean(record, "canLaunch", "CanLaunch");
  const reasonCode = readString(record, "reasonCode", "ReasonCode");
  if (
    !productCode ||
    !displayName ||
    entitlementActive === undefined ||
    productAccessAssigned === undefined ||
    productLocalRoleGranted === undefined ||
    canLaunch === undefined ||
    !reasonCode
  ) {
    throw new Error("Invalid enabled product.");
  }
  return {
    productCode,
    displayName,
    entitlementActive,
    productAccessAssigned,
    productLocalRoleGranted,
    canLaunch,
    reasonCode,
    productLocalRoleCode: readString(record, "productLocalRoleCode", "ProductLocalRoleCode"),
    mappedPosRoleCode: readString(record, "mappedPosRoleCode", "MappedPosRoleCode"),
    subscriptionStatus: readString(record, "subscriptionStatus", "SubscriptionStatus"),
    productId: readString(record, "productId", "ProductId"),
    productKey: readString(record, "productKey", "ProductKey"),
    productDisplayName: readString(record, "productDisplayName", "ProductDisplayName"),
    entitlementStatus: readString(record, "entitlementStatus", "EntitlementStatus"),
    provisioningStatus: readString(record, "provisioningStatus", "ProvisioningStatus"),
    organizationRole: readString(record, "organizationRole", "OrganizationRole"),
    productRole: readString(record, "productRole", "ProductRole"),
    denialReasonCode: readString(record, "denialReasonCode", "DenialReasonCode"),
    denialReasonDisplay: readString(record, "denialReasonDisplay", "DenialReasonDisplay"),
  };
}

export function mapEnabledProducts(payload: unknown): EnabledProduct[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid enabled product list.");
  }
  return payload.map(mapEnabledProduct);
}

export function listEnabledProducts(
  baseUrl: string,
  organizationId: string,
  signal?: AbortSignal,
): Promise<EnabledProduct[]> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/organizations/${organizationId}/enabled-products`,
    signal,
  }).then(mapEnabledProducts);
}

export function mapProductLocalRoleGrant(payload: unknown): ProductLocalRoleGrant {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid product local role grant.");
  }
  const id = readString(record, "id", "Id");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const userIdentityId = readString(record, "userIdentityId", "UserIdentityId");
  const productCode = readString(record, "productCode", "ProductCode");
  const roleCode = readString(record, "roleCode", "RoleCode");
  const mappedPosRoleCode = readString(record, "mappedPosRoleCode", "MappedPosRoleCode");
  const status = readString(record, "status", "Status");
  if (!id || !organizationId || !userIdentityId || !productCode || !roleCode || !mappedPosRoleCode || !status) {
    throw new Error("Invalid product local role grant.");
  }
  return {
    id,
    organizationId,
    userIdentityId,
    productCode,
    roleCode,
    mappedPosRoleCode,
    status,
    grantedAtUtc: readString(record, "grantedAtUtc", "GrantedAtUtc"),
    grantedByUserIdentityId: readString(record, "grantedByUserIdentityId", "GrantedByUserIdentityId"),
    source: readString(record, "source", "Source"),
    revokedAtUtc: readString(record, "revokedAtUtc", "RevokedAtUtc"),
    userDisplayName: readString(record, "userDisplayName", "UserDisplayName"),
    productDisplayName: readString(record, "productDisplayName", "ProductDisplayName"),
    roleDisplay: readString(record, "roleDisplay", "RoleDisplay"),
    productKey: readString(record, "productKey", "ProductKey"),
  };
}

export function mapProductLocalRoleGrants(payload: unknown): ProductLocalRoleGrant[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid product local role grant list.");
  }
  return payload.map(mapProductLocalRoleGrant);
}

export function listProductLocalRoles(
  baseUrl: string,
  organizationId: string,
  options: { status?: string; signal?: AbortSignal } = {},
): Promise<ProductLocalRoleGrant[]> {
  const params = new URLSearchParams();
  if (options.status) {
    params.set("status", options.status);
  }
  const query = params.toString();
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/organizations/${organizationId}/product-local-roles${query ? `?${query}` : ""}`,
    signal: options.signal,
  }).then(mapProductLocalRoleGrants);
}

export type AssignProductLocalRoleBody = {
  userIdentityId: string;
  productCode: string;
  roleCode: string;
  reason?: string | null;
};

export type RevokeProductLocalRoleBody = {
  reason?: string | null;
};

export function assignProductLocalRole(
  baseUrl: string,
  organizationId: string,
  body: AssignProductLocalRoleBody,
  signal?: AbortSignal,
): Promise<ProductLocalRoleGrant> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/organizations/${organizationId}/product-local-roles`,
    body,
    signal,
  }).then(mapProductLocalRoleGrant);
}

export function revokeProductLocalRole(
  baseUrl: string,
  organizationId: string,
  grantId: string,
  body: RevokeProductLocalRoleBody,
  signal?: AbortSignal,
): Promise<ProductLocalRoleGrant> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/organizations/${organizationId}/product-local-roles/${grantId}/revoke`,
    body,
    signal,
  }).then(mapProductLocalRoleGrant);
}

export type ProductLaunchResult = {
  productCode: string;
  canOperate: boolean;
  productLocalRoleCode?: string;
  mappedPosRoleCode?: string;
  launchPath: string;
  reasonCode: string;
};

function mapProductLaunchResult(payload: unknown): ProductLaunchResult {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid product launch result.");
  }
  const productCode = readString(record, "productCode", "ProductCode");
  const canOperate = readBoolean(record, "canOperate", "CanOperate");
  const launchPath = readString(record, "launchPath", "LaunchPath");
  const reasonCode = readString(record, "reasonCode", "ReasonCode");
  if (!productCode || canOperate === undefined || !launchPath || !reasonCode) {
    throw new Error("Invalid product launch result.");
  }
  return {
    productCode,
    canOperate,
    launchPath,
    reasonCode,
    productLocalRoleCode: readString(record, "productLocalRoleCode", "ProductLocalRoleCode"),
    mappedPosRoleCode: readString(record, "mappedPosRoleCode", "MappedPosRoleCode"),
  };
}

export function launchProduct(
  baseUrl: string,
  organizationId: string,
  productCode: string,
  signal?: AbortSignal,
): Promise<ProductLaunchResult> {
  const encoded = encodeURIComponent(productCode);
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/organizations/${organizationId}/products/${encoded}/launch`,
    signal,
  }).then(mapProductLaunchResult);
}

export const PRODUCT_LOCAL_ROLE_CODES = ["Owner", "Manager", "Cashier", "Viewer"] as const;
