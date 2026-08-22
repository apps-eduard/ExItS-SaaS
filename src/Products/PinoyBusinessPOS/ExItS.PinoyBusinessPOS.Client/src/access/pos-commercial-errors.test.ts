import { describe, expect, it } from "vitest";
import { PosApiError } from "@/api/pos/pos-http";
import { mapCommercialAccessErrorKey } from "@/access/pos-commercial-errors";

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
});
