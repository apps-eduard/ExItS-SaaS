/**
 * POS-HOTFIX-03 branch-context diagnostic harness.
 * Captures exact errorCode/traceId for Main Branch and Kizy Store 02 binds.
 */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const PLATFORM_ORIGIN = process.env.PLATFORM_ORIGIN ?? "http://127.0.0.1:8091";
const ORG_ID = "ca023f5b-925e-4aa5-a843-d48c4c06fa14";

function loadSharedPassword() {
  if (process.env.LOCAL_VALIDATION_SHARED_PASSWORD?.trim()) {
    return process.env.LOCAL_VALIDATION_SHARED_PASSWORD.trim();
  }
  const envPath = join(
    dirname(fileURLToPath(import.meta.url)),
    "../../../../../deploy/docker/.env.local-validation",
  );
  const text = readFileSync(envPath, "utf8");
  const match = text.match(/^LOCAL_VALIDATION_SHARED_PASSWORD=(.+)$/m);
  if (!match?.[1]?.trim()) {
    throw new Error("LOCAL_VALIDATION_SHARED_PASSWORD is not configured.");
  }
  return match[1].trim();
}

const jar = new Map();

function storeSetCookies(response) {
  const raw = response.headers.getSetCookie?.() ?? [];
  for (const line of raw) {
    const [pair] = line.split(";");
    const eq = pair.indexOf("=");
    if (eq <= 0) continue;
    jar.set(pair.slice(0, eq).trim(), pair.slice(eq + 1).trim());
  }
}

function cookieHeader() {
  if (jar.size === 0) return undefined;
  return [...jar.entries()].map(([k, v]) => `${k}=${v}`).join("; ");
}

async function request(path, { method = "GET", body, xsrf } = {}) {
  const headers = {
    Accept: "application/json",
    ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
    ...(xsrf ? { "X-XSRF-TOKEN": xsrf } : {}),
    ...(cookieHeader() ? { Cookie: cookieHeader() } : {}),
  };
  const response = await fetch(`${PLATFORM_ORIGIN}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  storeSetCookies(response);
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: response.status, json, text };
}

async function bootstrapToken() {
  const tokenRes = await request("/api/v1/platform/antiforgery/token");
  if (tokenRes.status !== 200 || !tokenRes.json?.token) {
    throw new Error(`ANTIFORGERY status=${tokenRes.status}`);
  }
  return tokenRes.json.token;
}

function pickBranchId(branch) {
  return branch?.branchId ?? branch?.id ?? branch?.Id ?? null;
}

async function bindBranchDiagnostic(branch, label) {
  const branchId = pickBranchId(branch);
  const orgToken = await bootstrapToken();
  const orgCtx = await request("/api/v1/platform/auth/organization-context", {
    method: "PUT",
    body: { organizationId: ORG_ID },
    xsrf: orgToken,
  });

  const branchToken = await bootstrapToken();
  const branchCtx = await request(
    `/api/v1/platform/organizations/${ORG_ID}/branch-context`,
    {
      method: "PUT",
      body: { branchId },
      xsrf: branchToken,
    },
  );

  return {
    label,
    branchName: branch?.name ?? branch?.Name ?? null,
    branchIdFromList: branchId,
    branchRawKeys: branch ? Object.keys(branch) : [],
    organizationContext: {
      status: orgCtx.status,
      errorCode: orgCtx.json?.errorCode ?? null,
      traceId: orgCtx.json?.traceId ?? null,
    },
    branchContext: {
      status: branchCtx.status,
      errorCode: branchCtx.json?.errorCode ?? null,
      traceId: branchCtx.json?.traceId ?? null,
      detail: branchCtx.json?.detail ?? null,
      requestBody: { branchId },
    },
  };
}

async function main() {
  const password = loadSharedPassword();
  console.log(`PLATFORM_ORIGIN=${PLATFORM_ORIGIN}`);

  const login = await request("/api/v1/platform/auth/login", {
    method: "POST",
    body: { usernameOrEmail: "kizy@gmail.com", password },
  });
  if (login.status !== 200) {
    console.log(JSON.stringify({ LOGIN: login }, null, 2));
    process.exit(1);
  }

  const me = await request("/api/v1/platform/auth/me");
  const orgToken = await bootstrapToken();
  const orgCtx = await request("/api/v1/platform/auth/organization-context", {
    method: "PUT",
    body: { organizationId: ORG_ID },
    xsrf: orgToken,
  });

  const branches = await request(`/api/v1/platform/organizations/${ORG_ID}/branches`);
  const branchList = Array.isArray(branches.json) ? branches.json : [];
  const main = branchList.find((b) => /main branch/i.test(b.name ?? b.Name ?? ""));
  const second = branchList.find((b) => /kizy store 02/i.test(b.name ?? b.Name ?? ""));

  const results = {
    me: {
      status: me.status,
      accountClass: me.json?.accountClass ?? null,
      selectedOrganizationId: me.json?.selectedOrganizationId ?? null,
    },
    organizationContext: {
      status: orgCtx.status,
      errorCode: orgCtx.json?.errorCode ?? null,
    },
    branchListCount: branchList.length,
    branches: branchList.map((b) => ({
      id: pickBranchId(b),
      name: b.name ?? b.Name,
      code: b.code ?? b.Code,
      status: b.status ?? b.Status,
      isPrimary: b.isPrimary ?? b.IsPrimary,
      organizationId: b.organizationId ?? b.OrganizationId,
    })),
    mainBranch: main ? await bindBranchDiagnostic(main, "Main Branch") : { error: "not found in list" },
    secondBranch: second
      ? await bindBranchDiagnostic(second, "Kizy Store 02")
      : { error: "not found in list" },
  };

  console.log(JSON.stringify(results, null, 2));
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
