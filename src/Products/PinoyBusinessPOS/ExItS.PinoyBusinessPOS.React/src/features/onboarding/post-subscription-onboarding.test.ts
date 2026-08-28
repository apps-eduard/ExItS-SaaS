import { afterEach, describe, expect, it, vi } from "vitest";
import { PosApiError } from "@/api/pos/pos-http";
import {
  ensureOnboardingProgress,
  getOnboardingProgress,
} from "@/api/pos/pos-onboarding-client";
import {
  POST_SUBSCRIPTION_ONBOARDING_STORAGE_KEY,
  clearPendingPostSubscriptionOnboarding,
  loadPostSubscriptionOnboardingProgress,
  pendingPostSubscriptionForOrganization,
  sameOrganizationId,
  writePendingPostSubscriptionOnboarding,
} from "@/features/onboarding/post-subscription-onboarding";

vi.mock("@/api/pos/pos-onboarding-client", () => ({
  getOnboardingProgress: vi.fn(),
  ensureOnboardingProgress: vi.fn(),
}));

const orgId = "37c4c64c-728d-40a3-80c5-df0cf7629d25";
const workspace = { organizationId: orgId, branchId: null };
const progress = {
  organizationId: orgId,
  organizationSetupStatus: "NotStarted" as const,
  businessSetupStatus: "NotStarted" as const,
  productTemplateStatus: "NotStarted" as const,
  overallStatus: "InProgress" as const,
  primaryBusinessTypeId: null,
  updatedAtUtc: "2026-08-27T00:00:00.000Z",
  createdAtUtc: "2026-08-27T00:00:00.000Z",
};

const getProgress = vi.mocked(getOnboardingProgress);
const ensureProgress = vi.mocked(ensureOnboardingProgress);

describe("post-subscription onboarding pending", () => {
  afterEach(() => {
    sessionStorage.clear();
    getProgress.mockReset();
    ensureProgress.mockReset();
  });

  it("matches organization ids case-insensitively", () => {
    expect(sameOrganizationId(orgId, orgId.toUpperCase())).toBe(true);
    expect(pendingPostSubscriptionForOrganization(orgId)).toBeNull();
    writePendingPostSubscriptionOnboarding({
      organizationId: orgId.toUpperCase(),
      primaryBusinessTypeId: "11111111-1111-4111-8111-111111111111",
    });
    expect(pendingPostSubscriptionForOrganization(orgId)?.organizationId).toBe(orgId.toUpperCase());
  });
});

describe("loadPostSubscriptionOnboardingProgress", () => {
  afterEach(() => {
    sessionStorage.clear();
    getProgress.mockReset();
    ensureProgress.mockReset();
    vi.useRealTimers();
  });

  it("ensures progress when Start a Business left a pending flag and GET is 404", async () => {
    writePendingPostSubscriptionOnboarding({
      organizationId: orgId,
      primaryBusinessTypeId: "11111111-1111-4111-8111-111111111111",
    });
    getProgress.mockRejectedValue(
      new PosApiError(404, { detail: "not found", errorCode: "pos.onboarding.progress.not_found" }),
    );
    ensureProgress.mockResolvedValue(progress);

    await expect(loadPostSubscriptionOnboardingProgress(workspace)).resolves.toEqual(progress);
    expect(getProgress).toHaveBeenCalledTimes(1);
    expect(ensureProgress).toHaveBeenCalledWith(
      workspace,
      { primaryBusinessTypeId: "11111111-1111-4111-8111-111111111111" },
      undefined,
    );
    expect(sessionStorage.getItem(POST_SUBSCRIPTION_ONBOARDING_STORAGE_KEY)).not.toBeNull();
  });

  it("does not treat abort as missing setup", async () => {
    writePendingPostSubscriptionOnboarding({ organizationId: orgId });
    const abort = Object.assign(new Error("aborted"), { name: "AbortError" });
    getProgress.mockRejectedValue(
      new PosApiError(404, { detail: "not found", errorCode: "pos.onboarding.progress.not_found" }),
    );
    ensureProgress.mockRejectedValue(abort);

    await expect(loadPostSubscriptionOnboardingProgress(workspace)).rejects.toBe(abort);
  });

  it("retries post-subscribe 403 until the grant is ready", async () => {
    vi.useFakeTimers();
    writePendingPostSubscriptionOnboarding({ organizationId: orgId });
    getProgress
      .mockRejectedValueOnce(
        new PosApiError(403, {
          detail: "unavailable",
          errorCode: "pos.development_headers.unavailable",
        }),
      )
      .mockResolvedValueOnce(progress);

    const promise = loadPostSubscriptionOnboardingProgress(workspace);
    await vi.advanceTimersByTimeAsync(250);
    await expect(promise).resolves.toEqual(progress);
    expect(getProgress).toHaveBeenCalledTimes(2);
  });

  it("returns null only for a casual visit with no progress row", async () => {
    getProgress.mockRejectedValue(
      new PosApiError(404, { detail: "not found", errorCode: "pos.onboarding.progress.not_found" }),
    );

    await expect(loadPostSubscriptionOnboardingProgress(workspace)).resolves.toBeNull();
    expect(ensureProgress).not.toHaveBeenCalled();
  });

  it("rethrows non-404 load failures", async () => {
    getProgress.mockRejectedValue(new PosApiError(403, { detail: "denied" }));

    await expect(loadPostSubscriptionOnboardingProgress(workspace)).rejects.toMatchObject({
      status: 403,
    });
  });
});

describe("clearPendingPostSubscriptionOnboarding", () => {
  afterEach(() => {
    sessionStorage.clear();
  });

  it("removes the pending flag", () => {
    writePendingPostSubscriptionOnboarding({ organizationId: orgId });
    clearPendingPostSubscriptionOnboarding();
    expect(pendingPostSubscriptionForOrganization(orgId)).toBeNull();
  });
});
