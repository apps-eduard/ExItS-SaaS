import { platformRequest } from "@/api/platform/platform-http";

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

function readNumber(raw: Record<string, unknown>, camel: string, pascal: string): number | null {
  const value = raw[camel] ?? raw[pascal];
  if (value == null || value === "") {
    return null;
  }
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

function readStringList(raw: Record<string, unknown>, camel: string, pascal: string): string[] {
  const value = raw[camel] ?? raw[pascal];
  if (!Array.isArray(value)) {
    return [];
  }
  return value.map((item) => String(item));
}

export type BranchDeliveryPolicyDto = {
  branchId: string;
  organizationId: string;
  minimumOrderAmount: number;
  baseDeliveryFee: number;
  includedDistanceKm: number;
  additionalFeePerKm: number;
  maximumDeliveryDistanceKm: number;
  freeDeliveryThreshold: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type OrganizationBranchDto = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  isPrimary: boolean;
  status: string;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  region: string | null;
  postalCode: string | null;
  countryCode: string | null;
  latitude: number | null;
  longitude: number | null;
  pickupEnabled: boolean;
  deliveryEnabled: boolean;
  customerOrderingEnabled: boolean;
  onlineOrdersPaused: boolean;
  contactPhone: string | null;
  timeZoneId: string | null;
  canOfferPickup: boolean;
  canOfferDeliveryLocation: boolean;
  customerOrderingReady: boolean;
  customerOrderingOperational: boolean;
  pickupOperational: boolean;
  deliveryOperational: boolean;
  storeStatusMessage: string | null;
  deliveryPolicy: BranchDeliveryPolicyDto | null;
};

export type BranchOperatingHoursDayDto = {
  dayOfWeek: string;
  isClosed: boolean;
  isOpen24Hours: boolean;
  openTime: string | null;
  closeTime: string | null;
};

export type BranchFulfillmentReadinessDto = {
  branchId: string;
  canUseCustomerOrdering: boolean;
  canUseDelivery: boolean;
  customerOrderingEnabled: boolean;
  pickupEnabled: boolean;
  deliveryEnabled: boolean;
  onlineOrdersPaused: boolean;
  onlineOrdersPauseReason: string | null;
  customerOrderingReady: boolean;
  pickupReady: boolean;
  deliveryReady: boolean;
  customerOrderingOperational: boolean;
  pickupOperational: boolean;
  deliveryOperational: boolean;
  missingRequirements: string[];
  reasonCodes: string[];
  storeOpenStatus: string | null;
  storeIsOpenNow: boolean;
  storeStatusMessage: string | null;
};

export type UpdateBranchRequest = {
  name?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  region?: string | null;
  postalCode?: string | null;
  countryCode?: string | null;
  status?: string | null;
  latitude?: number | null;
  longitude?: number | null;
  clearCoordinates?: boolean | null;
  contactPhone?: string | null;
  timeZoneId?: string | null;
};

export type UpsertBranchOperatingHoursRequest = {
  days: BranchOperatingHoursDayDto[];
};

export type UpdateBranchFulfillmentSettingsRequest = {
  customerOrderingEnabled?: boolean | null;
  pickupEnabled?: boolean | null;
  deliveryEnabled?: boolean | null;
};

export type SetBranchOnlineOrdersPausedRequest = {
  paused: boolean;
  reason?: string | null;
};

export type UpsertBranchDeliveryPolicyRequest = {
  minimumOrderAmount: number;
  baseDeliveryFee: number;
  includedDistanceKm: number;
  additionalFeePerKm: number;
  maximumDeliveryDistanceKm: number;
  freeDeliveryThreshold?: number | null;
};

function normalizeDeliveryPolicy(raw: unknown): BranchDeliveryPolicyDto | null {
  if (raw == null) {
    return null;
  }
  const r = asRecord(raw);
  return {
    branchId: String(r.branchId ?? r.BranchId ?? ""),
    organizationId: String(r.organizationId ?? r.OrganizationId ?? ""),
    minimumOrderAmount: Number(r.minimumOrderAmount ?? r.MinimumOrderAmount ?? 0),
    baseDeliveryFee: Number(r.baseDeliveryFee ?? r.BaseDeliveryFee ?? 0),
    includedDistanceKm: Number(r.includedDistanceKm ?? r.IncludedDistanceKm ?? 0),
    additionalFeePerKm: Number(r.additionalFeePerKm ?? r.AdditionalFeePerKm ?? 0),
    maximumDeliveryDistanceKm: Number(
      r.maximumDeliveryDistanceKm ?? r.MaximumDeliveryDistanceKm ?? 0,
    ),
    freeDeliveryThreshold: readNumber(r, "freeDeliveryThreshold", "FreeDeliveryThreshold"),
    createdAtUtc: String(r.createdAtUtc ?? r.CreatedAtUtc ?? ""),
    updatedAtUtc: String(r.updatedAtUtc ?? r.UpdatedAtUtc ?? ""),
  };
}

export function normalizeOrganizationBranch(raw: unknown): OrganizationBranchDto {
  const r = asRecord(raw);
  return {
    id: String(r.id ?? r.Id ?? ""),
    organizationId: String(r.organizationId ?? r.OrganizationId ?? ""),
    code: String(r.code ?? r.Code ?? ""),
    name: String(r.name ?? r.Name ?? ""),
    isPrimary: readBool(r, "isPrimary", "IsPrimary"),
    status: String(r.status ?? r.Status ?? ""),
    addressLine1: readString(r, "addressLine1", "AddressLine1"),
    addressLine2: readString(r, "addressLine2", "AddressLine2"),
    city: readString(r, "city", "City"),
    region: readString(r, "region", "Region"),
    postalCode: readString(r, "postalCode", "PostalCode"),
    countryCode: readString(r, "countryCode", "CountryCode"),
    latitude: readNumber(r, "latitude", "Latitude"),
    longitude: readNumber(r, "longitude", "Longitude"),
    pickupEnabled: readBool(r, "pickupEnabled", "PickupEnabled"),
    deliveryEnabled: readBool(r, "deliveryEnabled", "DeliveryEnabled"),
    customerOrderingEnabled: readBool(r, "customerOrderingEnabled", "CustomerOrderingEnabled"),
    onlineOrdersPaused: readBool(r, "onlineOrdersPaused", "OnlineOrdersPaused"),
    contactPhone: readString(r, "contactPhone", "ContactPhone"),
    timeZoneId: readString(r, "timeZoneId", "TimeZoneId"),
    canOfferPickup: readBool(r, "canOfferPickup", "CanOfferPickup"),
    canOfferDeliveryLocation: readBool(r, "canOfferDeliveryLocation", "CanOfferDeliveryLocation"),
    customerOrderingReady: readBool(r, "customerOrderingReady", "CustomerOrderingReady"),
    customerOrderingOperational: readBool(
      r,
      "customerOrderingOperational",
      "CustomerOrderingOperational",
    ),
    pickupOperational: readBool(r, "pickupOperational", "PickupOperational"),
    deliveryOperational: readBool(r, "deliveryOperational", "DeliveryOperational"),
    storeStatusMessage: readString(r, "storeStatusMessage", "StoreStatusMessage"),
    deliveryPolicy: normalizeDeliveryPolicy(r.deliveryPolicy ?? r.DeliveryPolicy),
  };
}

export function normalizeOperatingHoursDay(raw: unknown): BranchOperatingHoursDayDto {
  const r = asRecord(raw);
  return {
    dayOfWeek: String(r.dayOfWeek ?? r.DayOfWeek ?? ""),
    isClosed: readBool(r, "isClosed", "IsClosed"),
    isOpen24Hours: readBool(r, "isOpen24Hours", "IsOpen24Hours"),
    openTime: readString(r, "openTime", "OpenTime"),
    closeTime: readString(r, "closeTime", "CloseTime"),
  };
}

export function normalizeFulfillmentReadiness(raw: unknown): BranchFulfillmentReadinessDto {
  const r = asRecord(raw);
  return {
    branchId: String(r.branchId ?? r.BranchId ?? ""),
    canUseCustomerOrdering: readBool(r, "canUseCustomerOrdering", "CanUseCustomerOrdering"),
    canUseDelivery: readBool(r, "canUseDelivery", "CanUseDelivery"),
    customerOrderingEnabled: readBool(r, "customerOrderingEnabled", "CustomerOrderingEnabled"),
    pickupEnabled: readBool(r, "pickupEnabled", "PickupEnabled"),
    deliveryEnabled: readBool(r, "deliveryEnabled", "DeliveryEnabled"),
    onlineOrdersPaused: readBool(r, "onlineOrdersPaused", "OnlineOrdersPaused"),
    onlineOrdersPauseReason: readString(r, "onlineOrdersPauseReason", "OnlineOrdersPauseReason"),
    customerOrderingReady: readBool(r, "customerOrderingReady", "CustomerOrderingReady"),
    pickupReady: readBool(r, "pickupReady", "PickupReady"),
    deliveryReady: readBool(r, "deliveryReady", "DeliveryReady"),
    customerOrderingOperational: readBool(
      r,
      "customerOrderingOperational",
      "CustomerOrderingOperational",
    ),
    pickupOperational: readBool(r, "pickupOperational", "PickupOperational"),
    deliveryOperational: readBool(r, "deliveryOperational", "DeliveryOperational"),
    missingRequirements: readStringList(r, "missingRequirements", "MissingRequirements"),
    reasonCodes: readStringList(r, "reasonCodes", "ReasonCodes"),
    storeOpenStatus: readString(r, "storeOpenStatus", "StoreOpenStatus"),
    storeIsOpenNow: readBool(r, "storeIsOpenNow", "StoreIsOpenNow"),
    storeStatusMessage: readString(r, "storeStatusMessage", "StoreStatusMessage"),
  };
}

export async function listOrganizationBranchesForFulfillment(
  organizationId: string,
  signal?: AbortSignal,
): Promise<OrganizationBranchDto[]> {
  const body = await platformRequest<unknown>({
    path: branchesBase(organizationId),
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizeOrganizationBranch);
}

export async function updateOrganizationBranch(
  organizationId: string,
  branchId: string,
  request: UpdateBranchRequest,
  signal?: AbortSignal,
): Promise<OrganizationBranchDto> {
  const body = await platformRequest<unknown>({
    method: "PUT",
    path: branchPath(organizationId, branchId),
    body: request,
    signal,
  });
  return normalizeOrganizationBranch(body);
}

export async function getBranchOperatingHours(
  organizationId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<BranchOperatingHoursDayDto[]> {
  const body = await platformRequest<unknown>({
    path: `${branchPath(organizationId, branchId)}/operating-hours`,
    signal,
  });
  const items = Array.isArray(body) ? body : [];
  return items.map(normalizeOperatingHoursDay);
}

export async function upsertBranchOperatingHours(
  organizationId: string,
  branchId: string,
  request: UpsertBranchOperatingHoursRequest,
  signal?: AbortSignal,
): Promise<BranchFulfillmentReadinessDto> {
  const body = await platformRequest<unknown>({
    method: "PUT",
    path: `${branchPath(organizationId, branchId)}/operating-hours`,
    body: request,
    signal,
  });
  return normalizeFulfillmentReadiness(body);
}

export async function getBranchFulfillmentReadiness(
  organizationId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<BranchFulfillmentReadinessDto> {
  const body = await platformRequest<unknown>({
    path: `${branchPath(organizationId, branchId)}/fulfillment-readiness`,
    signal,
  });
  return normalizeFulfillmentReadiness(body);
}

export async function updateBranchFulfillmentSettings(
  organizationId: string,
  branchId: string,
  request: UpdateBranchFulfillmentSettingsRequest,
  signal?: AbortSignal,
): Promise<BranchFulfillmentReadinessDto> {
  const body = await platformRequest<unknown>({
    method: "PUT",
    path: `${branchPath(organizationId, branchId)}/fulfillment-settings`,
    body: request,
    signal,
  });
  return normalizeFulfillmentReadiness(body);
}

export async function setBranchOnlineOrdersPaused(
  organizationId: string,
  branchId: string,
  request: SetBranchOnlineOrdersPausedRequest,
  signal?: AbortSignal,
): Promise<BranchFulfillmentReadinessDto> {
  const body = await platformRequest<unknown>({
    method: "POST",
    path: `${branchPath(organizationId, branchId)}/online-orders-pause`,
    body: request,
    signal,
  });
  return normalizeFulfillmentReadiness(body);
}

export async function upsertBranchDeliveryPolicy(
  organizationId: string,
  branchId: string,
  request: UpsertBranchDeliveryPolicyRequest,
  signal?: AbortSignal,
): Promise<BranchDeliveryPolicyDto> {
  const body = await platformRequest<unknown>({
    method: "PUT",
    path: `${branchPath(organizationId, branchId)}/delivery-policy`,
    body: request,
    signal,
  });
  const policy = normalizeDeliveryPolicy(body);
  if (!policy) {
    throw new Error("Delivery policy response was empty.");
  }
  return policy;
}
