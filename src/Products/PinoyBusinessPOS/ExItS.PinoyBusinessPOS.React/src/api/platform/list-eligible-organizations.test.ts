import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("@/api/platform/platform-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/platform-http")>(
    "@/api/platform/platform-http",
  );
  return {
    ...actual,
    platformRequest: vi.fn(),
  };
});

import { platformRequest } from "@/api/platform/platform-http";
import { listEligibleOrganizations } from "@/api/platform/platform-auth-client";

describe("listEligibleOrganizations", () => {
  afterEach(() => {
    vi.mocked(platformRequest).mockReset();
  });

  it("accepts a bare organizations array", async () => {
    vi.mocked(platformRequest).mockResolvedValueOnce([
      { organizationId: "o1", displayName: "One", slug: "one" },
    ]);
    const result = await listEligibleOrganizations();
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.organizations).toHaveLength(1);
    }
  });

  it("accepts a wrapped { organizations } payload", async () => {
    vi.mocked(platformRequest).mockResolvedValueOnce({
      organizations: [{ organizationId: "o2", displayName: "Two", slug: "two" }],
    });
    const result = await listEligibleOrganizations();
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.organizations[0]?.organizationId).toBe("o2");
    }
  });

  it("rejects non-array payloads instead of throwing at iteration sites", async () => {
    vi.mocked(platformRequest).mockResolvedValueOnce({ unexpected: true });
    const result = await listEligibleOrganizations();
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.status).toBe(502);
    }
  });
});
