import { request as playwrightRequest, type APIRequestContext } from "@playwright/test";
import { readFileSync } from "node:fs";
import { join } from "node:path";

export type JoinedBootstrapContext = {
  organizationId: string;
  subscriptionId: string;
  organizationName: string;
  ownerSessionToken: string;
  accessToken: string;
  branchId: string;
};

export type PosDeviceScope = {
  branchId: string;
  installationDeviceId: string;
};

function posHeaders(accessToken: string, scope?: PosDeviceScope): Record<string, string> {
  const headers: Record<string, string> = { Authorization: `Bearer ${accessToken}` };
  if (scope) {
    headers["X-Pos-Installation-Device-Id"] = scope.installationDeviceId;
    headers["X-Pos-Branch-Id"] = scope.branchId;
  }
  return headers;
}

const platformApiBase = () => process.env.PA_COM_07_PLATFORM_API_URL ?? "http://127.0.0.1:8091";
const posApiBase = () => process.env.PA_COM_07_POS_API_URL ?? "http://127.0.0.1:8092";

function unique(prefix: string): string {
  return `${prefix}${crypto.randomUUID().replace(/-/g, "").slice(0, 12)}`.toLowerCase();
}

async function jsonOrThrow<T>(response: Awaited<ReturnType<APIRequestContext["post"]>>): Promise<T> {
  if (!response.ok()) {
    throw new Error(`${response.status()} ${response.url()}: ${await response.text()}`);
  }
  return (await response.json()) as T;
}

export async function bootstrapGrowthOrganization(): Promise<JoinedBootstrapContext> {
  const request = await playwrightRequest.newContext();
  try {
    return await bootstrapGrowthOrganizationWithRequest(request);
  } finally {
    await request.dispose();
  }
}

export async function bootstrapGrowthOrganizationWithRequest(
  request: APIRequestContext,
): Promise<JoinedBootstrapContext> {
  const prefix = unique("pacom07");
  const email = `${prefix}@example.com`;

  // Avoid auth/register bootstrap rate limits (5 / 15 min). Testing external auth is enabled
  // by Start-PaCom07MixedValidation.ps1 for the Platform API Testing environment.
  // Use an isolated context so the session cookie is not stored on the main API client
  // (cookie would override X-ExItS-Session-Token after start-business rotates the session).
  const externalContext = await playwrightRequest.newContext();
  let personalSessionToken: string;
  try {
    const external = await externalContext.post(
      `${platformApiBase()}/api/v1/platform/auth/external/testing/complete`,
      {
        data: {
          provider: "google",
          providerSubject: crypto.randomUUID(),
          email,
          emailVerified: true,
          displayName: "PA-COM-07 Owner",
        },
      },
    );
    const loginBody = await jsonOrThrow<{ sessionToken: string }>(external);
    personalSessionToken = loginBody.sessionToken;
  } finally {
    await externalContext.dispose();
  }

  const start = await request.post(`${platformApiBase()}/api/v1/personal/start-business`, {
    headers: { "X-ExItS-Session-Token": personalSessionToken },
    data: {
      displayName: `${prefix} Store`,
      slug: prefix,
      productCode: "pinoy-business-pos",
      planKey: "growth",
      billingCycle: "Monthly",
      startAsTrial: true,
      payNow: false,
      activatePosEntitlement: true,
      activateProductAccess: true,
      assignPosOwnerRole: true,
      primaryBusinessTypeId: "a1000001-0000-4000-8000-000000000001",
    },
  });
  const started = await jsonOrThrow<{
    organizationId: string;
    subscriptionId: string;
    sessionToken: string;
    primaryBranchId?: string;
  }>(start);

  const ownerSessionToken = started.sessionToken;
  const organizationId = started.organizationId;
  const subscriptionId = started.subscriptionId;
  const branchId = started.primaryBranchId;
  if (!branchId) {
    throw new Error("Start-business did not return primaryBranchId.");
  }

  const activeSessionToken = ownerSessionToken;

  const setOrg = await request.put(`${platformApiBase()}/api/v1/platform/auth/organization-context`, {
    headers: { "X-ExItS-Session-Token": activeSessionToken },
    data: { organizationId },
  });
  await jsonOrThrow(setOrg);

  const accessToken = await issueProductAccessToken(request, activeSessionToken, organizationId);

  return {
    organizationId,
    subscriptionId,
    organizationName: `${prefix} Store`,
    ownerSessionToken: activeSessionToken,
    accessToken,
    branchId,
  };
}

export async function issueProductAccessToken(
  request: APIRequestContext,
  sessionToken: string,
  organizationId: string,
): Promise<string> {
  const grant = await request.post(`${platformApiBase()}/api/v1/platform/auth/token`, {
    headers: { "X-ExItS-Session-Token": sessionToken },
    data: {
      grantType: "session",
      organizationId,
      productCode: "pinoy-business-pos",
    },
  });
  const body = await jsonOrThrow<{ accessToken: string }>(grant);
  return body.accessToken;
}

export async function introspectAccessToken(_request: APIRequestContext, accessToken: string) {
  const isolated = await playwrightRequest.newContext();
  try {
    const response = await isolated.post(`${platformApiBase()}/api/v1/platform/auth/introspect`, {
      headers: { Authorization: `Bearer ${accessToken}` },
      data: {},
    });
    return jsonOrThrow<{
      active: boolean;
      productAccessAllowed?: boolean;
      subscriptionStatus?: string;
      enabledFeatureCodes?: string[];
      productLocalRoleCode?: string;
      mappedPosRoleCode?: string;
      organizationId?: string;
    }>(response);
  } finally {
    await isolated.dispose();
  }
}

export async function readDeviceCapacity(
  request: APIRequestContext,
  organizationId: string,
  sessionToken: string,
): Promise<{ used: number; allowed: number }> {
  const response = await request.get(
    `${platformApiBase()}/api/v1/platform/organizations/${organizationId}/pos-devices/capacity`,
    { headers: { "X-ExItS-Session-Token": sessionToken } },
  );
  return jsonOrThrow(response);
}

export async function registerDevice(
  request: APIRequestContext,
  organizationId: string,
  branchId: string,
  sessionToken: string,
  installationId: string,
  friendlyName: string,
): Promise<{ ok: boolean; status: number; errorCode?: string }> {
  const response = await request.post(
    `${platformApiBase()}/api/v1/platform/organizations/${organizationId}/pos-devices/register`,
    {
      headers: { "X-ExItS-Session-Token": sessionToken },
      data: {
        branchId,
        installationDeviceId: installationId,
        friendlyName,
      },
    },
  );
  if (response.ok()) {
    return { ok: true, status: response.status() };
  }
  const problem = (await response.json().catch(() => ({}))) as { errorCode?: string };
  return { ok: false, status: response.status(), errorCode: problem.errorCode };
}

export async function posGet(
  request: APIRequestContext,
  path: string,
  accessToken: string,
): Promise<{ status: number }> {
  const response = await request.get(`${posApiBase()}${path}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  return { status: response.status() };
}

export async function posPost(
  request: APIRequestContext,
  path: string,
  accessToken: string,
  data?: unknown,
  scope?: PosDeviceScope,
): Promise<{ status: number }> {
  const response = await request.post(`${posApiBase()}${path}`, {
    headers: posHeaders(accessToken, scope),
    data,
  });
  return { status: response.status() };
}

export async function ensureOpenShift(
  request: APIRequestContext,
  accessToken: string,
): Promise<string> {
  const current = await request.get(`${posApiBase()}/api/v1/pos/cashier-shifts/current`, {
    headers: posHeaders(accessToken),
  });
  if (current.ok()) {
    const existing = (await current.json()) as { shiftId?: string };
    if (existing.shiftId) {
      return existing.shiftId;
    }
  }

  const registerBody = await request.post(`${posApiBase()}/api/v1/pos/registers`, {
    headers: posHeaders(accessToken),
    data: { name: `Register ${unique("reg")}` },
  });
  const registerJson = await jsonOrThrow<{ registerId: string }>(registerBody);

  const open = await request.post(`${posApiBase()}/api/v1/pos/cashier-shifts`, {
    headers: posHeaders(accessToken),
    data: {
      registerId: registerJson.registerId,
      openingCashAmount: 0,
    },
  });
  const opened = await jsonOrThrow<{ shiftId: string }>(open);
  return opened.shiftId;
}

export async function createCatalogProduct(request: APIRequestContext, accessToken: string): Promise<string> {
  const response = await request.post(`${posApiBase()}/api/v1/pos/catalog/products`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: {
      name: "Bigas",
      unitOfMeasure: "Kilogram",
      sellingPrice: 50,
      sku: unique("sku"),
    },
  });
  const body = await jsonOrThrow<{ productId: string }>(response);
  return body.productId;
}

export async function checkoutSale(
  request: APIRequestContext,
  accessToken: string,
  productId: string,
  scope: PosDeviceScope,
  shiftId: string,
): Promise<{ status: number }> {
  return posPost(
    request,
    "/api/v1/pos/sales",
    accessToken,
    {
      saleId: crypto.randomUUID(),
      shiftId,
      lines: [{ productId, quantity: 1 }],
      paymentMethod: "Cash",
      amountTendered: 50,
    },
    scope,
  );
}

export type PaCom07Provenance = {
  platformAdminRuntimeSha: string;
  platformApiRuntimeSha: string;
  posApiRuntimeSha: string;
  strictCommercialValidation: "ON";
  developmentGrantMerge: "OFF";
};

export function loadProvenanceFromDisk(): PaCom07Provenance {
  const override = process.env.PA_COM_07_PROVENANCE_PATH;
  const appData = process.env.LOCALAPPDATA ?? process.env.APPDATA ?? "";
  const path = override ?? join(appData, "ExItS", "LocalValidation", "pa-com-07-provenance.json");
  const raw = readFileSync(path, "utf8");
  return JSON.parse(raw) as PaCom07Provenance;
}

export async function readProvenance(request: APIRequestContext): Promise<PaCom07Provenance> {
  const provenanceUrl = process.env.PA_COM_07_PROVENANCE_URL;
  if (provenanceUrl) {
    const response = await request.get(provenanceUrl);
    return jsonOrThrow<PaCom07Provenance>(response);
  }

  try {
    return loadProvenanceFromDisk();
  }
  catch {
    // Fall back to env when disk provenance is unavailable (CI injection).
  }

  const [platformHealth, posHealth] = await Promise.all([
    request.get(`${platformApiBase()}/health`),
    request.get(`${posApiBase()}/health`),
  ]);
  if (!platformHealth.ok() || !posHealth.ok()) {
    throw new Error("Platform or POS health check failed before joined scenario.");
  }

  return {
    platformAdminRuntimeSha: process.env.PA_COM_07_PLATFORM_ADMIN_SHA ?? "unknown",
    platformApiRuntimeSha: process.env.PA_COM_07_PLATFORM_API_SHA ?? "unknown",
    posApiRuntimeSha: process.env.PA_COM_07_POS_API_SHA ?? "unknown",
    strictCommercialValidation: "ON",
    developmentGrantMerge: "OFF",
  };
}
