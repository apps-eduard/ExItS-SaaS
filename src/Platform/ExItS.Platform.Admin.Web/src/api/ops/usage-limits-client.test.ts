import { describe, expect, it } from "vitest";
import { parsePagedResult } from "@/api/platform/paged-result";

describe("usage-limits-client", () => {
  it("parses usage limit rows from paged payload", () => {
    const page = parsePagedResult<{
      organizationId: string;
      featureCode: string;
      usageStatus: string;
    }>({
      items: [
        {
          organizationId: "00000000-0000-0000-0000-000000000001",
          featureCode: "plan-max-active-staff",
          usageStatus: "Measured",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 25,
    });

    expect(page.items).toHaveLength(1);
    expect(page.items[0]?.usageStatus).toBe("Measured");
  });
});
