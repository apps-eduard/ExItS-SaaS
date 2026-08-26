import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  activateSupplier,
  createSupplier,
  deactivateSupplier,
  getSupplier,
  isConnectedSupplier,
  listSuppliers,
  resolveSupplierSearchParams,
  updateSupplier,
} from "@/api/pos/pos-suppliers-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const supplierId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

const supplierBody = {
  supplierId,
  organizationId: workspace.organizationId,
  supplierCode: "SUP0001",
  name: "Metro Wholesale",
  contactPerson: "Ana Cruz",
  mobileNumber: "09171234567",
  telephoneNumber: null,
  email: "ana@example.com",
  addressLine1: "Quezon City",
  addressLine2: null,
  cityMunicipality: "Quezon City",
  province: "Metro Manila",
  postalCode: "1100",
  taxOrRegistrationNumber: "123-456",
  notes: null,
  status: "Active",
  connectionType: "External",
  connectedRelationshipId: null,
  createdAtUtc: "2026-08-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("pos-suppliers-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("resolves SUP… search to supplierCode and other terms to name", () => {
    expect(resolveSupplierSearchParams("SUP0001")).toEqual({ supplierCode: "SUP0001" });
    expect(resolveSupplierSearchParams("sup99")).toEqual({ supplierCode: "sup99" });
    expect(resolveSupplierSearchParams("Metro")).toEqual({ name: "Metro" });
    expect(resolveSupplierSearchParams("  ")).toEqual({});
  });

  it("lists suppliers with name/status/pagination query", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse({
        items: [supplierBody],
        totalCount: 1,
        page: 2,
        pageSize: 20,
      }),
    );

    const page = await listSuppliers(workspace, {
      name: "Metro",
      status: "Active",
      page: 2,
      pageSize: 20,
    });
    expect(page.items[0]?.name).toBe("Metro Wholesale");
    const url = String(vi.mocked(fetch).mock.calls[0][0]);
    expect(url).toContain("/api/v1/pos/suppliers");
    expect(url).toContain("name=Metro");
    expect(url).toContain("status=Active");
    expect(url).toContain("page=2");
    expect(url).toContain("pageSize=20");
  });

  it("gets, creates, updates, activates, and deactivates", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(jsonResponse(supplierBody))
      .mockResolvedValueOnce(jsonResponse(supplierBody, 201))
      .mockResolvedValueOnce(jsonResponse({ ...supplierBody, name: "Metro Updated" }))
      .mockResolvedValueOnce(jsonResponse({ ...supplierBody, status: "Inactive" }))
      .mockResolvedValueOnce(jsonResponse({ ...supplierBody, status: "Active" }));

    await expect(getSupplier(workspace, supplierId)).resolves.toMatchObject({
      name: "Metro Wholesale",
    });
    await expect(
      createSupplier(workspace, { name: "Metro Wholesale", mobileNumber: "09171234567" }),
    ).resolves.toMatchObject({ supplierId });
    await expect(
      updateSupplier(workspace, supplierId, {
        name: "Metro Updated",
        expectedUpdatedAtUtc: "2026-08-01T00:00:00Z",
      }),
    ).resolves.toMatchObject({ name: "Metro Updated" });
    await expect(deactivateSupplier(workspace, supplierId)).resolves.toMatchObject({
      status: "Inactive",
    });
    await expect(activateSupplier(workspace, supplierId)).resolves.toMatchObject({
      status: "Active",
    });

    expect(String(vi.mocked(fetch).mock.calls[1][0])).toContain("/api/v1/pos/suppliers");
    expect(vi.mocked(fetch).mock.calls[1][1]?.method).toBe("POST");
    const updateBody = JSON.parse(String(vi.mocked(fetch).mock.calls[2][1]?.body));
    expect(updateBody.expectedUpdatedAtUtc).toBe("2026-08-01T00:00:00Z");
    expect(String(vi.mocked(fetch).mock.calls[3][0])).toContain("/deactivate");
    expect(String(vi.mocked(fetch).mock.calls[4][0])).toContain("/activate");
  });

  it("detects connected vs manual suppliers", () => {
    expect(isConnectedSupplier(supplierBody)).toBe(false);
    expect(isConnectedSupplier({ ...supplierBody, connectionType: "ConnectedOrganization" })).toBe(
      true,
    );
  });
});
