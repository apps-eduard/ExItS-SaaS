/**
 * Live Local Validation harness for POS workspace bind (HOTFIX-01/02/03).
 * Reads LOCAL_VALIDATION_SHARED_PASSWORD from deploy/docker/.env.local-validation when unset.
 */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const POS_ORIGIN = process.env.POS_ORIGIN ?? "http://127.0.0.1:5177";
const POS_API_ORIGIN = process.env.POS_API_ORIGIN ?? "http://127.0.0.1:8092";
const PLATFORM_PREFIX = `${POS_ORIGIN}/platform-api`;
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

function resolvePlatformBranchId(branch) {
  const resolved = branch?.id ?? branch?.branchId ?? branch?.Id ?? branch?.BranchId ?? null;
  return typeof resolved === "string" && resolved.trim().length > 0 ? resolved.trim() : null;
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

async function request(path, { method = "GET", body, xsrf, base = PLATFORM_PREFIX, bearer } = {}) {
  const headers = {
    Accept: "application/json",
    ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
    ...(xsrf ? { "X-XSRF-TOKEN": xsrf } : {}),
    ...(bearer ? { Authorization: `Bearer ${bearer}` } : {}),
    ...(cookieHeader() ? { Cookie: cookieHeader() } : {}),
  };
  const response = await fetch(`${base}${path}`, {
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
    json = null;
  }
  return { status: response.status, json, text };
}

async function bootstrapToken() {
  const tokenRes = await request("/api/v1/platform/antiforgery/token");
  if (tokenRes.status !== 200 || !tokenRes.json?.token) {
    throw new Error(`ANTIFORGERY_TOKEN_ENDPOINT=BLOCKER status=${tokenRes.status}`);
  }
  return tokenRes.json.token;
}

async function bindBranch(branch) {
  const branchId = resolvePlatformBranchId(branch);
  if (!branchId) {
    throw new Error(`branch list missing id for ${branch?.name ?? "unknown branch"}`);
  }

  const orgToken = await bootstrapToken();
  let res = await request("/api/v1/platform/auth/organization-context", {
    method: "PUT",
    body: { organizationId: ORG_ID },
    xsrf: orgToken,
  });
  if (res.status >= 400) {
    throw new Error(
      `org-context ${branch.name} status=${res.status} errorCode=${res.json?.errorCode ?? "?"}`,
    );
  }

  const branchToken = await bootstrapToken();
  res = await request(`/api/v1/platform/organizations/${ORG_ID}/branch-context`, {
    method: "PUT",
    body: { branchId },
    xsrf: branchToken,
  });
  if (res.status >= 400) {
    throw new Error(
      `branch-context ${branch.name} status=${res.status} errorCode=${res.json?.errorCode ?? "?"} branchId=${branchId}`,
    );
  }

  const grantToken = await bootstrapToken();
  res = await request("/api/v1/platform/auth/token", {
    method: "POST",
    body: {
      grantType: "session",
      organizationId: ORG_ID,
      productCode: "pinoy-business-pos",
    },
    xsrf: grantToken,
  });
  if (res.status >= 400) {
    throw new Error(
      `session-grant ${branch.name} status=${res.status} errorCode=${res.json?.errorCode ?? "?"}`,
    );
  }

  const accessToken = res.json?.accessToken;
  if (!accessToken) {
    throw new Error(`session-grant ${branch.name} missing accessToken`);
  }

  const pos = await request(
    "/api/v1/pos/operational-branch",
    {
      method: "PUT",
      base: POS_API_ORIGIN,
      bearer: accessToken,
      body: {
        branchId,
        fromBranchId: null,
        deviceBoundBranchId: null,
      },
    },
  );
  if (pos.status >= 400) {
    throw new Error(
      `pos-operational-branch ${branch.name} status=${pos.status} errorCode=${pos.json?.errorCode ?? "?"}`,
    );
  }

  return { branchId, accessToken };
}

async function main() {
  const password = loadSharedPassword();
  const login = await request("/api/v1/platform/auth/login", {
    method: "POST",
    body: { usernameOrEmail: "kizy@gmail.com", password },
  });
  if (login.status !== 200) {
    throw new Error(`LOGIN=BLOCKER status=${login.status}`);
  }

  const without = await request("/api/v1/platform/auth/organization-context", {
    method: "PUT",
    body: { organizationId: ORG_ID },
  });
  if (without.status !== 400) {
    throw new Error(`EXPECTED_ANTIFORGERY_REJECTION status=${without.status}`);
  }

  const token = await bootstrapToken();
  const withHeader = await request("/api/v1/platform/auth/organization-context", {
    method: "PUT",
    body: { organizationId: ORG_ID },
    xsrf: token,
  });
  if (withHeader.status !== 200 && withHeader.status !== 204) {
    throw new Error(`ORG_CONTEXT=BLOCKER status=${withHeader.status}`);
  }

  const branches = await request(`/api/v1/platform/organizations/${ORG_ID}/branches`);
  const branchList = Array.isArray(branches.json) ? branches.json : [];
  const main = branchList.find((b) => /main branch/i.test(b.name ?? ""));
  const second = branchList.find((b) => /kizy store 02/i.test(b.name ?? ""));

  if (main) await bindBranch(main);
  if (second) await bindBranch(second);

  const logoutToken = await bootstrapToken();
  const logout = await request("/api/v1/platform/auth/logout", {
    method: "POST",
    xsrf: logoutToken,
  });
  if (logout.status !== 204 && logout.status !== 200) {
    throw new Error(`LOGOUT=BLOCKER status=${logout.status}`);
  }

  const me = await request("/api/v1/platform/auth/me");
  const relogin = await request("/api/v1/platform/auth/login", {
    method: "POST",
    body: { usernameOrEmail: "kizy@gmail.com", password },
  });

  console.log(
    JSON.stringify(
      {
        ANTIFORGERY_TOKEN_ENDPOINT: "PASS",
        ANTIFORGERY_HEADER_PRESENT: "YES",
        WORKSPACE_MAIN_BRANCH_LIVE: main ? "PASS" : "BLOCKER",
        WORKSPACE_SECOND_BRANCH_LIVE: second ? "PASS" : "BLOCKER",
        SESSION_GRANT_AFTER_BRANCH_BIND: main && second ? "PASS" : "BLOCKER",
        POS_API_AFTER_BRANCH_BIND: main && second ? "PASS" : "BLOCKER",
        LOGOUT_LIVE: "PASS",
        LOGIN_AFTER_LOGOUT_LIVE: relogin.status === 200 ? "PASS" : "BLOCKER",
        ME_AFTER_LOGOUT: me.status,
      },
      null,
      2,
    ),
  );
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
