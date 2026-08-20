import { describe, expect, it } from "vitest";
import {
  classifyWorkspaceBindFailure,
  workspaceBindFailureTitleKey,
} from "@/workspace/workspace-bind-error";

describe("workspace bind error classification", () => {
  it("OWNER_VALID_ACCESS path: product access_denied reason stays product denial", () => {
    const failure = classifyWorkspaceBindFailure({
      reason: "access_denied",
      status: 403,
      detail: "subscription_inactive",
    });
    expect(failure.kind).toBe("product_access_denied");
    expect(failure.detailKey).toBe("accessDenied.detail");
    expect(workspaceBindFailureTitleKey(failure.kind)).toBe("accessDenied.title");
  });

  it("STALE_OR_INVALID_SESSION: bearer inactive is session expired, not product denial", () => {
    const failure = classifyWorkspaceBindFailure({
      status: 401,
      errorCode: "pos.actor.required",
      detail: "Bearer access token is inactive or invalid.",
    });
    expect(failure.kind).toBe("session_expired");
    expect(failure.detailKey).toBe("accessDenied.sessionExpired");
    expect(workspaceBindFailureTitleKey(failure.kind)).toBe("accessDenied.sessionTitle");
  });

  it("does not surface engineering bearer text as the user detail key", () => {
    const failure = classifyWorkspaceBindFailure({
      status: 401,
      detail: "Bearer access token is inactive or invalid.",
    });
    expect(failure.detailKey).not.toContain("Bearer");
    expect(failure.technicalDetail).toMatch(/Bearer/i);
  });

  it("NO_PRODUCT_ACCESS error codes remain denied", () => {
    const failure = classifyWorkspaceBindFailure({
      status: 403,
      errorCode: "application.auth.product_access_denied",
      detail: "not_entitled",
    });
    expect(failure.kind).toBe("product_access_denied");
  });

  it("service unavailable for rate limit / platform auth outage", () => {
    expect(
      classifyWorkspaceBindFailure({
        status: 429,
        errorCode: "platform.rate_limit.exceeded",
      }).kind,
    ).toBe("service_unavailable");
    expect(
      classifyWorkspaceBindFailure({
        status: 503,
        errorCode: "pos.platform_auth.unavailable",
      }).kind,
    ).toBe("service_unavailable");
  });

  it("branch not found from operational-branch is not product denial", () => {
    const failure = classifyWorkspaceBindFailure({
      status: 404,
      errorCode: "pos.customer_order.branch.not_found",
      detail: "The selected branch is not an Active branch in this organization.",
    });
    expect(failure.kind).toBe("branch_not_accessible");
    expect(workspaceBindFailureTitleKey(failure.kind)).toBe("accessDenied.branchTitle");
  });
});
