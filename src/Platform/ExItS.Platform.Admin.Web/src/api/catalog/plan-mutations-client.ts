import { mapCatalogPlan, mapCatalogPlanVersion } from "@/api/catalog/plan-catalog-client";
import type { CatalogPlan, CatalogPlanVersion } from "@/api/catalog/plan-catalog-types";
import { commercialMutationRequest } from "@/api/commercial/commercial-http";

function productPlanPath(productCode: string, planId: string, suffix = ""): string {
  return `/api/v1/platform/catalog/products/${encodeURIComponent(productCode)}/plans/${planId}${suffix}`;
}

function requirePlan(payload: unknown): CatalogPlan {
  const mapped = mapCatalogPlan(payload);
  if (!mapped) {
    throw new Error("Invalid catalog plan.");
  }
  return mapped;
}

function requirePlanVersion(payload: unknown): CatalogPlanVersion {
  const mapped = mapCatalogPlanVersion(payload);
  if (!mapped) {
    throw new Error("Invalid catalog plan version.");
  }
  return mapped;
}

export type UpdatePlanCommercialBody = {
  displayName: string;
  description?: string | null;
  maxBranches: number;
  maxActiveStaff: number;
  maxActivePosDevices: number;
  maxActiveBusinessTypes: number;
  maxAreas: number;
  customerCreditEnabled: boolean;
  advancedReportsEnabled: boolean;
  exportEnabled: boolean;
  trialAllowed: boolean;
  defaultTrialDays: number;
  sortOrder: number;
  monthlyPrice: number;
  annualPrice: number;
  currencyCode: string;
  expectedUpdatedAtUtc?: string | null;
};

export type CreatePlanBody = {
  code: string;
  displayName: string;
  description?: string | null;
  maxBranches?: number;
  maxActiveStaff?: number;
  maxActivePosDevices?: number;
  maxActiveBusinessTypes?: number;
  maxAreas?: number;
  customerCreditEnabled?: boolean;
  advancedReportsEnabled?: boolean;
  exportEnabled?: boolean;
  trialAllowed?: boolean;
  defaultTrialDays?: number;
  sortOrder?: number;
  monthlyPrice?: number;
  annualPrice?: number;
  currencyCode?: string;
};

export type CreateDraftPlanVersionBody = {
  versionNumber: number;
  billingPeriod: string;
  trialEligible: boolean;
  grants?: Array<{ featureCode: string; enabled: boolean; numericLimit?: number | null }>;
  effectiveFromUtc?: string | null;
  effectiveToUtc?: string | null;
};

export type UpsertDraftFeatureGrantBody = {
  featureCode: string;
  enabled: boolean;
  numericLimit?: number | null;
};

export function updatePlanCommercial(
  baseUrl: string,
  productCode: string,
  planId: string,
  body: UpdatePlanCommercialBody,
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PATCH",
    path: productPlanPath(productCode, planId, "/commercial"),
    body,
    signal,
  }).then(requirePlan);
}

export function activatePlan(
  baseUrl: string,
  productCode: string,
  planId: string,
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPlanPath(productCode, planId, "/activate"),
    signal,
  }).then(requirePlan);
}

export function deactivatePlan(
  baseUrl: string,
  productCode: string,
  planId: string,
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPlanPath(productCode, planId, "/deactivate"),
    signal,
  }).then(requirePlan);
}

export function retirePlan(
  baseUrl: string,
  productCode: string,
  planId: string,
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPlanPath(productCode, planId, "/retire"),
    signal,
  }).then(requirePlan);
}

export function renamePlan(
  baseUrl: string,
  productCode: string,
  planId: string,
  body: { displayName: string; expectedUpdatedAtUtc?: string | null },
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PATCH",
    path: productPlanPath(productCode, planId, "/rename"),
    body,
    signal,
  }).then(requirePlan);
}

export function createPlan(
  baseUrl: string,
  productCode: string,
  body: CreatePlanBody,
  signal?: AbortSignal,
): Promise<CatalogPlan> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: `/api/v1/platform/catalog/products/${encodeURIComponent(productCode)}/plans`,
    body,
    signal,
  }).then(requirePlan);
}

export function createDraftPlanVersion(
  baseUrl: string,
  productCode: string,
  planId: string,
  body: CreateDraftPlanVersionBody,
  signal?: AbortSignal,
): Promise<CatalogPlanVersion> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPlanPath(productCode, planId, "/versions/draft"),
    body,
    signal,
  }).then(requirePlanVersion);
}

export function publishPlanVersion(
  baseUrl: string,
  productCode: string,
  planId: string,
  versionNumber: number,
  signal?: AbortSignal,
): Promise<CatalogPlanVersion> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "POST",
    path: productPlanPath(productCode, planId, `/versions/${versionNumber}/publish`),
    signal,
  }).then(requirePlanVersion);
}

export function upsertDraftFeatureGrant(
  baseUrl: string,
  productCode: string,
  planId: string,
  versionNumber: number,
  body: UpsertDraftFeatureGrantBody,
  signal?: AbortSignal,
): Promise<CatalogPlanVersion> {
  return commercialMutationRequest<unknown>(baseUrl, {
    method: "PUT",
    path: productPlanPath(
      productCode,
      planId,
      `/versions/${versionNumber}/feature-grants/${encodeURIComponent(body.featureCode)}`,
    ),
    body,
    signal,
  }).then(requirePlanVersion);
}
