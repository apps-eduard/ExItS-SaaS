import { describe, expect, it } from "vitest";
import { PosApiError } from "@/api/pos/pos-http";
import {
  commercialAccessStateMessageKey,
  describeCommercialAccessError,
  describePosApiError,
  mapCommercialAccessErrorKey,
  resolveCommercialAccessState,
} from "@/access/pos-commercial-errors";

describe("mapCommercialAccessErrorKey", () => {
  it("maps commercial capability denial to plan copy", () => {
    expect(
      mapCommercialAccessErrorKey(
        new PosApiError(403, { errorCode: "pos.commercial.capability_denied", detail: "denied" }),
      ),
    ).toBe("commercial.notIncludedInPlan");
  });

  it("maps unknown commercial context", () => {
    expect(
      mapCommercialAccessErrorKey(
        new PosApiError(403, { errorCode: "pos.commercial.access_unknown", detail: "missing" }),
      ),
    ).toBe("commercial.accessUnavailable");
  });

  it("maps device capacity exceeded", () => {
    expect(
      mapCommercialAccessErrorKey(
        new PosApiError(409, {
          errorCode: "application.pos_device.capacity_exceeded",
          detail: "limit",
        }),
      ),
    ).toBe("devices.capacity.limitReached");
  });

  it("maps product access denial to product unavailable copy", () => {
    expect(
      mapCommercialAccessErrorKey(
        new PosApiError(403, {
          errorCode: "application.auth.product_access_denied",
          detail: "denied",
        }),
      ),
    ).toBe("commercial.productUnavailable");
  });

  it("maps suspended subscription detail", () => {
    expect(
      mapCommercialAccessErrorKey(
        new PosApiError(403, {
          errorCode: "pos.commercial.capability_denied",
          detail: "Subscription is suspended.",
        }),
      ),
    ).toBe("commercial.subscriptionSuspended");
  });
});

describe("resolveCommercialAccessState", () => {
  it("classifies denied product access from session grant", () => {
    expect(
      resolveCommercialAccessState({
        productAccessAllowed: false,
        productAccessReasonCode: "product_access_missing",
      }),
    ).toBe("product_unavailable");
  });

  it("classifies suspended reason from session grant", () => {
    expect(
      resolveCommercialAccessState({
        productAccessAllowed: false,
        productAccessReasonCode: "subscription_ineligible",
      }),
    ).toBe("suspended");
  });

  it("returns allowed when product access is granted", () => {
    expect(
      resolveCommercialAccessState({
        productAccessAllowed: true,
      }),
    ).toBe("allowed");
  });
});

describe("describePosApiError", () => {
  const t = (key: string) => key;

  it("prefers commercial mapper over raw detail", () => {
    expect(
      describePosApiError(
        new PosApiError(403, { errorCode: "pos.commercial.capability_denied", detail: "internal" }),
        t,
      ),
    ).toBe("commercial.notIncludedInPlan");
  });

  it("falls back to generic key for unknown errors", () => {
    expect(describePosApiError(new Error("boom"), t, "reports.loadError")).toBe("boom");
  });
});

describe("commercialAccessStateMessageKey", () => {
  it("returns null for allowed state", () => {
    expect(commercialAccessStateMessageKey("allowed")).toBeNull();
  });

  it("maps suspended to i18n key", () => {
    expect(commercialAccessStateMessageKey("suspended")).toBe("commercial.subscriptionSuspended");
  });
});

describe("describeCommercialAccessError", () => {
  it("returns translated commercial copy", () => {
    expect(
      describeCommercialAccessError(
        new PosApiError(403, { errorCode: "pos.commercial.access_unknown", detail: "x" }),
        (key) => `translated:${key}`,
      ),
    ).toBe("translated:commercial.accessUnavailable");
  });
});
