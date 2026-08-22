import { describe, expect, it } from "vitest";
import { classifyCredentialWorkflowFailure, classifySignInFailure } from "@/api/auth/auth-errors";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import { PlatformApiError } from "@/api/platform-http";

describe("classifySignInFailure", () => {
  it("maps login_failed and 401 to invalid credentials", () => {
    expect(
      classifySignInFailure(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.loginFailed, detail: "stack" }),
      ),
    ).toBe("invalid_credentials");
    expect(classifySignInFailure(new PlatformApiError(401, { title: "Unauthorized" }))).toBe(
      "invalid_credentials",
    );
  });

  it("maps login_failed on 400 to invalid credentials", () => {
    expect(
      classifySignInFailure(
        new PlatformApiError(400, { errorCode: AUTH_ERROR_CODES.loginFailed, detail: "stack" }),
      ),
    ).toBe("invalid_credentials");
  });

  it("maps unexpected server failures to service unavailable", () => {
    expect(
      classifySignInFailure(new PlatformApiError(500, { title: "Error", detail: "boom" })),
    ).toBe("service_unavailable");
    expect(classifySignInFailure(new PlatformApiError(503, { title: "Service Unavailable" }))).toBe(
      "service_unavailable",
    );
  });

  it("maps 403 to sign in denied and 429 to rate limited", () => {
    expect(classifySignInFailure(new PlatformApiError(403, { title: "Forbidden" }))).toBe(
      "sign_in_denied",
    );
    expect(
      classifySignInFailure(
        new PlatformApiError(429, { errorCode: AUTH_ERROR_CODES.rateLimitExceeded }),
      ),
    ).toBe("rate_limited");
  });

  it("maps network failures separately", () => {
    expect(classifySignInFailure(new TypeError("Failed to fetch"))).toBe("network");
  });
});

describe("classifyCredentialWorkflowFailure", () => {
  it("maps invalid and expired tokens separately", () => {
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.credentialTokenInvalid }),
      ),
    ).toBe("invalid_token");
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.credentialTokenExpired }),
      ),
    ).toBe("expired_token");
  });

  it("maps password policy failures without treating them as connectivity", () => {
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(400, {
          errorCode: AUTH_ERROR_CODES.passwordInvalid,
          detail: "Password must be at least 12 characters.",
        }),
      ),
    ).toBe("password_invalid");
  });

  it("maps rate limiting separately from network failures", () => {
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(429, { errorCode: AUTH_ERROR_CODES.rateLimitExceeded }),
      ),
    ).toBe("rate_limited");
  });

  it("maps upstream proxy failures to service unavailable", () => {
    expect(classifyCredentialWorkflowFailure(new PlatformApiError(502, { title: "Bad Gateway" }))).toBe(
      "service_unavailable",
    );
    expect(
      classifyCredentialWorkflowFailure(new PlatformApiError(503, { title: "Service Unavailable" })),
    ).toBe("service_unavailable");
  });

  it("maps fetch TypeError to network only", () => {
    expect(classifyCredentialWorkflowFailure(new TypeError("Failed to fetch"))).toBe("network");
  });
});
