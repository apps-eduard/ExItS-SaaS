import { beforeEach, describe, expect, it, vi } from "vitest";
import { lookupPublicStoreLanding } from "@/api/platform/public-store-client";

const platformRequest = vi.hoisted(() => vi.fn());

vi.mock("@/api/platform/platform-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/platform-http")>(
    "@/api/platform/platform-http",
  );
  return {
    ...actual,
    platformRequest: platformRequest,
  };
});

describe("public-store-client", () => {
  beforeEach(() => {
    platformRequest.mockReset();
  });

  it("looks up minimal public store landing", async () => {
    platformRequest.mockResolvedValue({
      PublicOrganizationId: "ORG123456",
      DisplayName: "Kizy Store",
      OrderingAvailable: true,
    });
    const dto = await lookupPublicStoreLanding("ORG123456");
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({ path: "/api/v1/public/stores/ORG123456" }),
    );
    expect(dto).toEqual({
      publicOrganizationId: "ORG123456",
      displayName: "Kizy Store",
      orderingAvailable: true,
    });
  });
});
