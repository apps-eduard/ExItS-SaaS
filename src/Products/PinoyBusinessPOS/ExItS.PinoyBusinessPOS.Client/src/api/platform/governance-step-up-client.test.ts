import { afterEach, describe, expect, it, vi } from "vitest";
import {
  classifyGovernanceStepUpFailure,
  issueGovernanceStepUp,
  issuePosDeviceRevokeStepUp,
  POS_DEVICE_REVOKE_ACTION,
  TARGET_POS_DEVICE,
} from "@/api/platform/governance-step-up-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

const ORG_ID = "11111111-1111-4111-8111-111111111111";
const DEVICE_ID = "22222222-2222-4222-8222-222222222222";

function stubFetch(
  handler: (url: string, init: RequestInit | undefined) => Record<string, unknown>,
) {
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) => {
    if (url.includes("/antiforgery/")) {
      return {
        ok: true,
        status: 200,
        json: async () => ({ headerName: "X-CSRF", token: "csrf" }),
      };
    }
    const response = handler(url, init);
    return response;
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

describe("governance-step-up-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("pins the POS device revoke action code and target type", () => {
    expect(POS_DEVICE_REVOKE_ACTION).toBe("platform.pos_device.revoke");
    expect(TARGET_POS_DEVICE).toBe("PosDevice");
  });

  it("posts the scoped step-up request and normalizes the token", async () => {
    const fetchMock = stubFetch(() => ({
      ok: true,
      status: 200,
      json: async () => ({
        StepUpToken: "opaque-token",
        ExpiresAtUtc: "2026-08-22T01:05:00Z",
        ActionCode: POS_DEVICE_REVOKE_ACTION,
        TargetType: TARGET_POS_DEVICE,
        TargetId: DEVICE_ID,
      }),
    }));

    const result = await issuePosDeviceRevokeStepUp(ORG_ID, DEVICE_ID, "correct horse");
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.value.stepUpToken).toBe("opaque-token");
    expect(result.value.targetId).toBe(DEVICE_ID);

    const call = fetchMock.mock.calls.find(([url]) => String(url).includes("/governance/step-up"));
    expect(call).toBeDefined();
    expect(String(call?.[0])).toBe(
      `/platform-api/api/v1/platform/organizations/${ORG_ID}/governance/step-up`,
    );
    expect(JSON.parse(String(call?.[1]?.body))).toEqual({
      actionCode: POS_DEVICE_REVOKE_ACTION,
      targetType: TARGET_POS_DEVICE,
      targetId: DEVICE_ID,
      currentPassword: "correct horse",
    });
  });

  it("refuses to call the API without a password", async () => {
    const fetchMock = stubFetch(() => ({ ok: true, status: 200, json: async () => ({}) }));
    const result = await issueGovernanceStepUp(ORG_ID, {
      actionCode: POS_DEVICE_REVOKE_ACTION,
      targetType: TARGET_POS_DEVICE,
      targetId: DEVICE_ID,
      currentPassword: "   ",
    });
    expect(result).toMatchObject({ ok: false, reason: "password_required" });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("maps a wrong password to a friendly reason", async () => {
    stubFetch(() => ({
      ok: false,
      status: 400,
      json: async () => ({
        detail: "Password verification failed.",
        errorCode: "application.auth.current_password_invalid",
      }),
    }));

    const result = await issuePosDeviceRevokeStepUp(ORG_ID, DEVICE_ID, "nope");
    expect(result).toMatchObject({ ok: false, reason: "wrong_password", status: 400 });
  });

  it.each([
    ["application.auth.governance_step_up_expired", "expired"],
    ["application.auth.governance_step_up_consumed", "consumed"],
    ["application.auth.governance_step_up_invalid", "invalid_scope"],
    ["application.credential.password_invalid", "wrong_password"],
    ["application.auth.step_up_required", "password_required"],
  ] as const)("classifies %s as %s", (errorCode, expected) => {
    expect(classifyGovernanceStepUpFailure(400, errorCode)).toBe(expected);
  });

  it("classifies an unauthenticated or forbidden response as not allowed", () => {
    expect(classifyGovernanceStepUpFailure(403, undefined)).toBe("not_allowed");
    expect(classifyGovernanceStepUpFailure(500, undefined)).toBe("unavailable");
  });
});
