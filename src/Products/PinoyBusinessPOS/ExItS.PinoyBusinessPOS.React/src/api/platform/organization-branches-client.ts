import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";
import {
  normalizeOrganizationBranch,
  type OrganizationBranchDto,
  type UpdateBranchRequest,
} from "@/api/platform/branch-fulfillment-client";
import {
  normalizeBranchType,
  type OrganizationBranchType,
} from "@/features/branches/branch-type";

function branchesBase(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/branches`;
}

function branchPath(organizationId: string, branchId: string): string {
  return `${branchesBase(organizationId)}/${branchId}`;
}

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null ? (value as Record<string, unknown>) : {};
}

function readString(raw: Record<string, unknown>, camel: string, pascal: string): string | null {
  const value = raw[camel] ?? raw[pascal];
  if (value == null) {
    return null;
  }
  return String(value);
}

function readBool(
  raw: Record<string, unknown>,
  camel: string,
  pascal: string,
  fallback = false,
): boolean {
  const value = raw[camel] ?? raw[pascal];
  return typeof value === "boolean" ? value : fallback;
}

function readInt(raw: Record<string, unknown>, camel: string, pascal: string, fallback = 0): number {
  const value = raw[camel] ?? raw[pascal];
  if (value == null || value === "") {
    return fallback;
  }
  const n = Number(value);
  return Number.isFinite(n) ? Math.trunc(n) : fallback;
}

export type BranchCapacityDto = {
  used: number;
  allowed: number;
};

export type BranchManagementSummaryItemDto = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  branchType: OrganizationBranchType;
  isPrimary: boolean;
  status: string;
  city: string | null;
  region: string | null;
  addressLine1: string | null;
  pickupEnabled: boolean;
  deliveryEnabled: boolean;
  customerOrderingEnabled: boolean;
  assignedStaffCount: number;
  activeDeviceCount: number;
  areaId: string | null;
  areaName: string | null;
  pickupSectionsComplete: number;
  pickupSectionsTotal: number;
  deliverySectionsComplete: number;
  deliverySectionsTotal: number;
};

export type BranchStaffAccessItemDto = {
  membershipId: string;
  userId: string;
  displayName: string;
  membershipRole: string;
  membershipStatus: string;
  posRoleCode: string | null;
  posRoleDisplay: string | null;
  hasExplicitAccess: boolean;
  hasOrganizationWideAccess: boolean;
};

export type CreateBranchRequest = {
  code: string;
  name: string;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  region?: string | null;
  postalCode?: string | null;
  countryCode?: string | null;
  contactPhone?: string | null;
  timeZoneId?: string | null;
  pickupEnabled?: boolean;
  deliveryEnabled?: boolean;
  customerOrderingEnabled?: boolean;
  branchType?: OrganizationBranchType;
};

export type GovernanceCriticalActionBody = {
  reason?: string | null;
  stepUpToken?: string | null;
};

export type OrganizationBranchesClientResult<T> =
  | { ok: true; value: T }
  | { ok: false; status: number; body: PlatformProblemDetails | null; errorCode?: string };

async function wrap<T>(fn: () => Promise<T>): Promise<OrganizationBranchesClientResult<T>> {
  try {
    return { ok: true, value: await fn() };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return {
        ok: false,
        status: error.status,
        body: error.problem,
        errorCode: error.errorCode,
      };
    }
    throw error;
  }
}

function normalizeCapacity(raw: Record<string, unknown>): BranchCapacityDto {
  return {
    used: Number(raw.used ?? raw.Used ?? 0),
    allowed: Number(raw.allowed ?? raw.Allowed ?? 0),
  };
}

function normalizeSummaryItem(raw: unknown): BranchManagementSummaryItemDto {
  const r = asRecord(raw);
  return {
    id: String(r.id ?? r.Id ?? ""),
    organizationId: String(r.organizationId ?? r.OrganizationId ?? ""),
    code: String(r.code ?? r.Code ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    branchType: normalizeBranchType(r.branchType ?? r.BranchType),
    isPrimary: readBool(r, "isPrimary", "IsPrimary"),
    status: String(r.status ?? r.Status ?? ""),
    city: readString(r, "city", "City"),
    region: readString(r, "region", "Region"),
    addressLine1: readString(r, "addressLine1", "AddressLine1"),
    pickupEnabled: readBool(r, "pickupEnabled", "PickupEnabled"),
    deliveryEnabled: readBool(r, "deliveryEnabled", "DeliveryEnabled"),
    customerOrderingEnabled: readBool(r, "customerOrderingEnabled", "CustomerOrderingEnabled"),
    assignedStaffCount: readInt(r, "assignedStaffCount", "AssignedStaffCount"),
    activeDeviceCount: readInt(r, "activeDeviceCount", "ActiveDeviceCount"),
    areaId: readString(r, "areaId", "AreaId"),
    areaName: readString(r, "areaName", "AreaName"),
    pickupSectionsComplete: readInt(r, "pickupSectionsComplete", "PickupSectionsComplete"),
    pickupSectionsTotal: readInt(r, "pickupSectionsTotal", "PickupSectionsTotal", 2),
    deliverySectionsComplete: readInt(r, "deliverySectionsComplete", "DeliverySectionsComplete"),
    deliverySectionsTotal: readInt(r, "deliverySectionsTotal", "DeliverySectionsTotal", 5),
  };
}

function normalizeStaffAccessItem(raw: unknown): BranchStaffAccessItemDto {
  const r = asRecord(raw);
  return {
    membershipId: String(r.membershipId ?? r.MembershipId ?? ""),
    userId: String(r.userId ?? r.UserId ?? ""),
    displayName: String(r.displayName ?? r.DisplayName ?? ""),
    membershipRole: String(r.membershipRole ?? r.MembershipRole ?? ""),
    membershipStatus: String(r.membershipStatus ?? r.MembershipStatus ?? ""),
    posRoleCode: readString(r, "posRoleCode", "PosRoleCode"),
    posRoleDisplay: readString(r, "posRoleDisplay", "PosRoleDisplay"),
    hasExplicitAccess: readBool(r, "hasExplicitAccess", "HasExplicitAccess"),
    hasOrganizationWideAccess: readBool(r, "hasOrganizationWideAccess", "HasOrganizationWideAccess"),
  };
}

export async function getBranchCapacity(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<BranchCapacityDto>> {
  return wrap(async () => {
    const payload = await platformRequest<Record<string, unknown>>({
      method: "GET",
      path: `${branchesBase(organizationId)}/capacity`,
      signal,
    });
    return normalizeCapacity(payload);
  });
}

export async function listBranchManagementSummaries(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<BranchManagementSummaryItemDto[]>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: `${branchesBase(organizationId)}/management-summary`,
      signal,
    });
    const list = Array.isArray(payload) ? payload : [];
    return list.map(normalizeSummaryItem);
  });
}

export async function createOrganizationBranch(
  organizationId: string,
  body: CreateBranchRequest,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "POST",
      path: branchesBase(organizationId),
      body: {
        code: body.code,
        name: body.name,
        addressLine1: body.addressLine1 ?? null,
        addressLine2: body.addressLine2 ?? null,
        city: body.city ?? null,
        region: body.region ?? null,
        postalCode: body.postalCode ?? null,
        countryCode: body.countryCode ?? null,
        contactPhone: body.contactPhone ?? null,
        timeZoneId: body.timeZoneId ?? null,
        pickupEnabled: body.pickupEnabled ?? false,
        deliveryEnabled: body.deliveryEnabled ?? false,
        customerOrderingEnabled: body.customerOrderingEnabled ?? false,
        branchType: body.branchType ?? "Retail",
      },
      signal,
    });
    return normalizeOrganizationBranch(payload);
  });
}

export async function updateOrganizationBranchDetails(
  organizationId: string,
  branchId: string,
  request: UpdateBranchRequest,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "PUT",
      path: branchPath(organizationId, branchId),
      body: request,
      signal,
    });
    return normalizeOrganizationBranch(payload);
  });
}

export async function getOrganizationBranch(
  organizationId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto | null>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: branchesBase(organizationId),
      signal,
    });
    const list = Array.isArray(payload) ? payload : [];
    const match = list
      .map(normalizeOrganizationBranch)
      .find((branch) => branch.id === branchId);
    return match ?? null;
  });
}

export async function listBranchStaffAccess(
  organizationId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<BranchStaffAccessItemDto[]>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "GET",
      path: `${branchPath(organizationId, branchId)}/staff-access`,
      signal,
    });
    const list = Array.isArray(payload) ? payload : [];
    return list.map(normalizeStaffAccessItem);
  });
}

async function postBranchGovernanceAction(
  organizationId: string,
  branchId: string,
  action: "suspend" | "reactivate" | "archive" | "set-primary",
  body: GovernanceCriticalActionBody,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return wrap(async () => {
    const payload = await platformRequest<unknown>({
      method: "POST",
      path: `${branchPath(organizationId, branchId)}/${action}`,
      body: {
        reason: body.reason ?? null,
        stepUpToken: body.stepUpToken ?? null,
      },
      signal,
    });
    return normalizeOrganizationBranch(payload);
  });
}

export function suspendOrganizationBranch(
  organizationId: string,
  branchId: string,
  body: GovernanceCriticalActionBody,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return postBranchGovernanceAction(organizationId, branchId, "suspend", body, signal);
}

export function reactivateOrganizationBranch(
  organizationId: string,
  branchId: string,
  body: GovernanceCriticalActionBody,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return postBranchGovernanceAction(organizationId, branchId, "reactivate", body, signal);
}

export function archiveOrganizationBranch(
  organizationId: string,
  branchId: string,
  body: GovernanceCriticalActionBody,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return postBranchGovernanceAction(organizationId, branchId, "archive", body, signal);
}

export function setPrimaryOrganizationBranch(
  organizationId: string,
  branchId: string,
  body: GovernanceCriticalActionBody,
  signal?: AbortSignal,
): Promise<OrganizationBranchesClientResult<OrganizationBranchDto>> {
  return postBranchGovernanceAction(organizationId, branchId, "set-primary", body, signal);
}
