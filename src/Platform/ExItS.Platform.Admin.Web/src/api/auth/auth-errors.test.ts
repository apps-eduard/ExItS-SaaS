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

  it("maps unexpected server failures to unknown", () => {
    expect(
      classifySignInFailure(new PlatformApiError(500, { title: "Error", detail: "boom" })),
    ).toBe("unknown");
  });

  it("maps network failures separately", () => {
    expect(classifySignInFailure(new TypeError("Failed to fetch"))).toBe("network");
  });
});

describe("classifyCredentialWorkflowFailure", () => {
  it("treats duplicate email as email_conflict without using login semantics", () => {
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(409, {
          errorCode: AUTH_ERROR_CODES.emailConflict,
          detail: "An account with this email already exists.",
        }),
      ),
    ).toBe("email_conflict");
  });

  it("maps invalid and expired tokens to invalid_token", () => {
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.credentialTokenInvalid }),
      ),
    ).toBe("invalid_token");
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(401, { errorCode: AUTH_ERROR_CODES.credentialTokenExpired }),
      ),
    ).toBe("invalid_token");
  });

  it("maps password policy failures", () => {
    expect(
      classifyCredentialWorkflowFailure(
        new PlatformApiError(400, {
          errorCode: AUTH_ERROR_CODES.passwordInvalid,
          detail: "Password must be at least 12 characters.",
        }),
      ),
    ).toBe("password_invalid");
  });
});
