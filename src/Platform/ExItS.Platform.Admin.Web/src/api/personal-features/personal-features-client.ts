import { platformRequest } from "@/api/platform-http";

export type PersonalFeatureDefinition = {
  featureCode: string;
  displayName: string;
  isActive: boolean;
  rewardPointsPrice: number | null;
  defaultEntitlementDurationDays: number | null;
  isRewardRedeemable: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type UpdatePersonalFeatureDefinitionRequest = {
  displayName: string;
  isActive: boolean;
  rewardPointsPrice?: number | null;
  defaultEntitlementDurationDays?: number | null;
  expectedUpdatedAtUtc?: string | null;
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

function readNumberOrNull(record: Record<string, unknown>, ...keys: string[]): number | null {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "number" && Number.isFinite(value)) {
      return value;
    }
    if (value === null) {
      return null;
    }
  }
  return null;
}

export function mapPersonalFeatureDefinition(payload: unknown): PersonalFeatureDefinition {
  const record = asRecord(payload);
  if (!record) {
    throw new Error("Invalid personal feature definition.");
  }
  const featureCode = readString(record, "featureCode", "FeatureCode");
  const displayName = readString(record, "displayName", "DisplayName");
  const createdAtUtc = readString(record, "createdAtUtc", "CreatedAtUtc");
  const updatedAtUtc = readString(record, "updatedAtUtc", "UpdatedAtUtc");
  if (!featureCode || !displayName || !createdAtUtc || !updatedAtUtc) {
    throw new Error("Invalid personal feature definition.");
  }
  return {
    featureCode,
    displayName,
    isActive: readBool(record, "isActive", "IsActive"),
    rewardPointsPrice: readNumberOrNull(record, "rewardPointsPrice", "RewardPointsPrice"),
    defaultEntitlementDurationDays: readNumberOrNull(
      record,
      "defaultEntitlementDurationDays",
      "DefaultEntitlementDurationDays",
    ),
    isRewardRedeemable: readBool(record, "isRewardRedeemable", "IsRewardRedeemable"),
    createdAtUtc,
    updatedAtUtc,
  };
}

export function listPersonalFeatureDefinitions(
  baseUrl: string,
  signal?: AbortSignal,
): Promise<PersonalFeatureDefinition[]> {
  return platformRequest<unknown>(baseUrl, {
    path: "/api/v1/platform/personal/features",
    signal,
  }).then((payload) => {
    if (!Array.isArray(payload)) {
      throw new Error("Invalid personal features list response.");
    }
    return payload.map(mapPersonalFeatureDefinition);
  });
}

export function getPersonalFeatureDefinition(
  baseUrl: string,
  featureCode: string,
  signal?: AbortSignal,
): Promise<PersonalFeatureDefinition> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/personal/features/${encodeURIComponent(featureCode)}`,
    signal,
  }).then(mapPersonalFeatureDefinition);
}

export function updatePersonalFeatureDefinition(
  baseUrl: string,
  featureCode: string,
  body: UpdatePersonalFeatureDefinitionRequest,
  signal?: AbortSignal,
): Promise<PersonalFeatureDefinition> {
  return platformRequest<unknown>(baseUrl, {
    path: `/api/v1/platform/personal/features/${encodeURIComponent(featureCode)}`,
    method: "PATCH",
    body: {
      displayName: body.displayName,
      isActive: body.isActive,
      rewardPointsPrice: body.rewardPointsPrice ?? null,
      defaultEntitlementDurationDays: body.defaultEntitlementDurationDays ?? null,
      expectedUpdatedAtUtc: body.expectedUpdatedAtUtc ?? null,
    },
    signal,
  }).then(mapPersonalFeatureDefinition);
}
