import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { useCatalogProductImageUrl } from "@/features/sell/use-catalog-product-image";

const getCatalogProductImage = vi.fn();

vi.mock("@/api/pos/pos-catalog-client", () => ({
  getCatalogProductImage: (...args: unknown[]) => getCatalogProductImage(...args),
}));

describe("useCatalogProductImageUrl", () => {
  beforeEach(() => {
    getCatalogProductImage.mockReset();
    getCatalogProductImage.mockResolvedValue(new Blob(["x"], { type: "image/png" }));
    if (typeof URL.createObjectURL !== "function") {
      Object.defineProperty(URL, "createObjectURL", {
        configurable: true,
        writable: true,
        value: () => "blob:test",
      });
    }
    if (typeof URL.revokeObjectURL !== "function") {
      Object.defineProperty(URL, "revokeObjectURL", {
        configurable: true,
        writable: true,
        value: () => undefined,
      });
    }
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("does not re-fetch when workspace object identity changes but ids stay the same", async () => {
    const create = vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:test-1");
    const revoke = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => undefined);

    const workspaceA = { organizationId: "org", branchId: "branch" };
    const { rerender } = renderHook(
      ({ workspace }) => useCatalogProductImageUrl(workspace, "p1", true, 1),
      { initialProps: { workspace: workspaceA } },
    );

    await waitFor(() => {
      expect(getCatalogProductImage).toHaveBeenCalledTimes(1);
    });

    rerender({ workspace: { organizationId: "org", branchId: "branch" } });
    await waitFor(() => {
      expect(getCatalogProductImage).toHaveBeenCalledTimes(1);
    });

    create.mockRestore();
    revoke.mockRestore();
  });
});
