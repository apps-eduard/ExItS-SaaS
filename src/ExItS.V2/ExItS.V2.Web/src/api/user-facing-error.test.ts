import { describe, expect, it } from "vitest";
import { PlatformApiError } from "./platform-http";
import { sanitizeUserFacingMessage, toUserFacingError } from "./user-facing-error";

describe("user-facing error sanitization", () => {
  it("strips API paths and account-class denial wording", () => {
    const raw =
      "Account class 'Personal' is not allowed to call '/api/v1/platform/organizations/11111111-1111-1111-1111-111111111111/branches'.";
    expect(sanitizeUserFacingMessage(raw, "Permission denied.")).toBe(
      "Permission denied.",
    );
  });

  it("maps 403 Platform errors to a friendly message", () => {
    const error = new PlatformApiError(403, {
      detail:
        "Account class 'Personal' is not allowed to call '/api/v1/platform/organizations/x/branches'.",
      errorCode: "application.auth.account_scope_denied",
    });
    expect(toUserFacingError(error)).toBe(
      "You do not have permission for this action.",
    );
  });
});
