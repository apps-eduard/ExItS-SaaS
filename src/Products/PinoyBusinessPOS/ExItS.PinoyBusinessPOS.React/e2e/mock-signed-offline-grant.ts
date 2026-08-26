import { webcrypto } from "node:crypto";
import { DEVICE_ID, FIXED_INSTALL_ID } from "./mock-sell-ready";
import { E2E_BRANCH_ID, E2E_ORG_ID, E2E_USER_ID } from "./mock-bound-session";

const DEV_PRIVATE_KEY_PEM = `-----BEGIN PRIVATE KEY-----
MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgJuN+Pa6hk6BZUISu
lodghNrUkSR+VQsjrIW49hJ21dihRANCAASSV3pYY5NEuiiPYCs/ZRXZL6dNW0DJ
8VhI3X4k2jMfgEoBV/n9zUzAIZMsJ6XfzAHR+cz3/VxgoQYquH3GV0Lt
-----END PRIVATE KEY-----`;

async function signGrantCanonical(canonical: string): Promise<string> {
  const pemBody = DEV_PRIVATE_KEY_PEM.replace(/-----[^-]+-----/g, "").replace(/\s/g, "");
  const der = Buffer.from(pemBody, "base64");
  const key = await webcrypto.subtle.importKey(
    "pkcs8",
    der,
    { name: "ECDSA", namedCurve: "P-256" },
    false,
    ["sign"],
  );
  const signature = await webcrypto.subtle.sign(
    { name: "ECDSA", hash: "SHA-256" },
    key,
    new TextEncoder().encode(canonical),
  );
  return Buffer.from(signature).toString("hex");
}

export async function buildSignedOfflineGrantDto() {
  const grantId = "33333333-3333-4333-8333-333333333333";
  const issuedAtUtc = "2026-01-01T12:00:00.000Z";
  const expiresAtUtc = "2030-01-01T12:00:00.000Z";
  const canonical = [
    "v1",
    grantId,
    "4",
    E2E_USER_ID,
    "0",
    E2E_ORG_ID,
    "Kizy Store",
    E2E_BRANCH_ID,
    "Main Branch",
    FIXED_INSTALL_ID,
    DEVICE_ID,
    "Cashier",
    "Kizy Uy",
    "kizy",
    String(Math.floor(Date.parse(issuedAtUtc) / 1000)),
    String(Math.floor(Date.parse(issuedAtUtc) / 1000)),
    String(Math.floor(Date.parse(expiresAtUtc) / 1000)),
  ].join("|");
  const signature = await signGrantCanonical(canonical);
  return {
    grantId,
    schemaVersion: 4,
    userId: E2E_USER_ID,
    scopeKind: "Organization",
    organizationId: E2E_ORG_ID,
    organizationDisplayName: "Kizy Store",
    branchId: E2E_BRANCH_ID,
    branchName: "Main Branch",
    installationDeviceId: FIXED_INSTALL_ID,
    posDeviceId: DEVICE_ID,
    roleCode: "Cashier",
    displayName: "Kizy Uy",
    username: "kizy",
    issuedAtUtc,
    lastOnlineValidatedAtUtc: issuedAtUtc,
    expiresAtUtc,
    signature,
  };
}
