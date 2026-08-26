import { parsePagedResult, type PagedResult } from "@/api/platform/paged-result";
import { platformRequest } from "@/api/platform-http";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";
import type { ProductAccessAssignment } from "@/api/organizations/organization-types";

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

export function mapProductAccessAssignment(payload: unknown): ProductAccessAssignment {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid product access assignment.");
  }
  const id = readString(record, "id", "Id");
  const userId = readString(record, "userId", "UserId");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const membershipId = readString(record, "membershipId", "MembershipId");
  const productCode = readString(record, "productCode", "ProductCode");
  const status = readString(record, "status", "Status");
  if (!id || !userId || !organizationId || !membershipId || !productCode || !status) {
    throw new Error("Invalid product access assignment.");
  }
  return {
    id,
    userId,
    organizationId,
    membershipId,
    productCode,
    status,
    grantedAtUtc: readString(record, "grantedAtUtc", "GrantedAtUtc"),
    grantedByActor: readString(record, "grantedByActor", "GrantedByActor"),
    revokedAtUtc: readString(record, "revokedAtUtc", "RevokedAtUtc"),
    revokedByActor: readString(record, "revokedByActor", "RevokedByActor"),
    reason: readString(record, "reason", "Reason"),
    createdAtUtc: readString(record, "createdAtUtc", "CreatedAtUtc"),
    updatedAtUtc: readString(record, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export type ProductAccessUrlState = {
  page: number;
  status: "" | "Active" | "Revoked";
};

export function productAccessRequestPath(
  organizationId: string,
  state: ProductAccessUrlState & { pageSize?: number },
): string {
  const params = new URLSearchParams();
  params.set("page", String(state.page));
  params.set("pageSize", String(state.pageSize ?? 20));
  if (state.status) {
    params.set("status", state.status);
  }
  return `/api/v1/platform/organizations/${organizationId}/product-access?${params.toString()}`;
}

export function listOrganizationProductAccess(
  baseUrl: string,
  organizationId: string,
  state: ProductAccessUrlState & { pageSize?: number; signal?: AbortSignal },
): Promise<PagedResult<ProductAccessAssignment>> {
  return platformRequest<unknown>(baseUrl, {
    path: productAccessRequestPath(organizationId, state),
    signal: state.signal,
  }).then((payload) => {
    const page = parsePagedResult<unknown>(payload);
    return {
      ...page,
      items: page.items.map(mapProductAccessAssignment),
    };
  });
}

export type GrantProductAccessBody = {
  userId: string;
  productCode: string;
  grantedByActor: string;
  reason?: string | null;
};

export type RevokeProductAccessBody = {
  revokedByActor: string;
  reason?: string | null;
};

export function grantProductAccess(
  baseUrl: string,
  organizationId: string,
  body: GrantProductAccessBody,
  signal?: AbortSignal,
): Promise<ProductAccessAssignment> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/product-access`,
    body,
    signal,
  }).then(mapProductAccessAssignment);
}

export function revokeProductAccess(
  baseUrl: string,
  assignmentId: string,
  body: RevokeProductAccessBody,
  signal?: AbortSignal,
): Promise<ProductAccessAssignment> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/product-access/${assignmentId}/revoke`,
    body,
    signal,
  }).then(mapProductAccessAssignment);
}

export type EffectiveProductAccess = {
  allowed: boolean;
  reasonCode: string;
  userId: string;
  organizationId: string;
  productCode: string;
  membershipId?: string;
  assignmentId?: string;
  subscriptionId?: string;
  snapshotId?: string;
  evaluatedAtUtc?: string;
  subscriptionStatus?: string;
};

function mapEffectiveProductAccess(payload: unknown): EffectiveProductAccess {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid product access evaluation.");
  }
  const allowed = record.allowed ?? record.Allowed;
  const reasonCode = readString(record, "reasonCode", "ReasonCode");
  const userId = readString(record, "userId", "UserId");
  const organizationId = readString(record, "organizationId", "OrganizationId");
  const productCode = readString(record, "productCode", "ProductCode");
  if (typeof allowed !== "boolean" || !reasonCode || !userId || !organizationId || !productCode) {
    throw new Error("Invalid product access evaluation.");
  }
  return {
    allowed,
    reasonCode,
    userId,
    organizationId,
    productCode,
    membershipId: readString(record, "membershipId", "MembershipId"),
    assignmentId: readString(record, "assignmentId", "AssignmentId"),
    subscriptionId: readString(record, "subscriptionId", "SubscriptionId"),
    snapshotId: readString(record, "snapshotId", "SnapshotId"),
    evaluatedAtUtc: readString(record, "evaluatedAtUtc", "EvaluatedAtUtc"),
    subscriptionStatus: readString(record, "subscriptionStatus", "SubscriptionStatus"),
  };
}

export function evaluateProductAccess(
  baseUrl: string,
  options: { userId: string; organizationId: string; productCode: string; signal?: AbortSignal },
): Promise<EffectiveProductAccess> {
  const params = new URLSearchParams({
    userId: options.userId,
    organizationId: options.organizationId,
    productCode: options.productCode,
  });
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/access/evaluate?${params.toString()}`,
    signal: options.signal,
  }).then(mapEffectiveProductAccess);
}
