import { beforeEach, describe, expect, it, vi } from "vitest";
import { listCustomers } from "@/api/pos/pos-customers-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const customerId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

describe("pos-customers-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("lists Active customers with search", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          items: [
            {
              customerId,
              organizationId: workspace.organizationId,
              displayName: "Juan Dela Cruz",
              mobileNumber: "09171234567",
              status: "Active",
              createdAtUtc: "2026-08-01T00:00:00Z",
              updatedAtUtc: "2026-08-01T00:00:00Z",
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
        { status: 200, headers: { "Content-Type": "application/json" } },
      ),
    );

    const page = await listCustomers(workspace, { search: "Juan", status: "Active" });
    expect(page.items[0]?.displayName).toBe("Juan Dela Cruz");
    const url = String(vi.mocked(fetch).mock.calls[0][0]);
    expect(url).toContain("/api/v1/pos/customers");
    expect(url).toContain("status=Active");
    expect(url).toContain("search=Juan");
  });
});
