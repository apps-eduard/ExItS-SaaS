import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  acceptIncomingOrder,
  applyBuyerProductPricing,
  cancelConnectionRequest,
  approveConnection,
  assertNotInventoryMutationUrl,
  bulkMutateBuyerProductShares,
  createBuyerProductAndLink,
  declineConnection,
  declineIncomingOrder,
  fulfillIncomingOrder,
  INVENTORY_MUTATION_PATH_MARKERS,
  isShareFilterSharedOnly,
  linkProduct,
  listLinks,
  listRelationships,
  prepareIncomingOrder,
  queryBuyerProductShares,
  requestConnection,
  searchExposedCatalog,
  setBuyerProductShares,
  unlinkProduct,
} from "@/api/pos/pos-connected-suppliers-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const relationshipId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const exposureId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
const productId = "ffffffff-ffff-4fff-8fff-ffffffffffff";
const linkId = "99999999-9999-4999-8999-999999999999";

const relationshipBody = {
  relationshipId,
  buyerOrganizationId: workspace.organizationId,
  supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
  status: "Pending",
  requestedAtUtc: "2026-08-01T00:00:00Z",
  requestedByUserId: null,
  respondedAtUtc: null,
  respondedByUserId: null,
  disconnectedAtUtc: null,
  createdAtUtc: "2026-08-01T00:00:00Z",
  updatedAtUtc: "2026-08-01T00:00:00Z",
  counterpartyDisplayName: "Buyer Co",
  counterpartyPublicOrganizationId: "ORG000001",
};

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("pos-connected-suppliers-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("documents EXPOSABLE ≠ SHARED via shareFilter shared-only semantics", () => {
    expect(isShareFilterSharedOnly("shared")).toBe(true);
    expect(isShareFilterSharedOnly("Shared")).toBe(true);
    expect(isShareFilterSharedOnly("all")).toBe(false);
    expect(isShareFilterSharedOnly("notShared")).toBe(false);
    expect(isShareFilterSharedOnly(undefined)).toBe(false);
  });

  it("requests, approves, declines, and lists relationships", async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(jsonResponse(relationshipBody, 201))
      .mockResolvedValueOnce(jsonResponse({ ...relationshipBody, status: "Declined" }))
      .mockResolvedValueOnce(jsonResponse({ ...relationshipBody, status: "Active" }))
      .mockResolvedValueOnce(jsonResponse({ ...relationshipBody, status: "Declined" }))
      .mockResolvedValueOnce(jsonResponse([relationshipBody]));

    await expect(
      requestConnection(workspace, {
        supplierPublicOrganizationIdOrQrPayload: "ORG000099",
      }),
    ).resolves.toMatchObject({ relationshipId });

    await expect(cancelConnectionRequest(workspace, relationshipId)).resolves.toMatchObject({
      status: "Declined",
    });

    await expect(approveConnection(workspace, relationshipId)).resolves.toMatchObject({
      status: "Active",
    });
    await expect(declineConnection(workspace, relationshipId)).resolves.toMatchObject({
      status: "Declined",
    });
    await expect(listRelationships(workspace, "supplier")).resolves.toHaveLength(1);

    const urls = vi.mocked(fetch).mock.calls.map((call) => String(call[0]));
    expect(urls[0]).toContain("/connected-suppliers/relationships/request");
    expect(urls[1]).toContain(`/relationships/${relationshipId}/cancel`);
    expect(urls[2]).toContain(`/relationships/${relationshipId}/approve`);
    expect(urls[3]).toContain(`/relationships/${relationshipId}/decline`);
    expect(urls[4]).toContain("view=supplier");
    for (const url of urls) {
      assertNotInventoryMutationUrl(url);
    }
  });

  it("queries shares, sets shares, bulk mutates, and applies pricing without inventory calls", async () => {
    const share = {
      shareId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      relationshipId,
      buyerOrganizationId: workspace.organizationId,
      supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
      supplierProductId: productId,
      isShared: true,
      buyerSpecificPoPrice: 12.5,
      effectiveSupplierOrderPrice: 12.5,
      syncVersion: 1,
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
      nameSnapshot: "Rice 25kg",
      skuSnapshot: "RICE25",
      defaultPoPrice: 15,
    };

    vi.mocked(fetch)
      .mockResolvedValueOnce(
        jsonResponse({
          items: [share],
          matchingCount: 1,
          eligibleCount: 5,
          sharedCount: 1,
          page: 1,
          pageSize: 25,
          categories: [],
        }),
      )
      .mockResolvedValueOnce(jsonResponse([share]))
      .mockResolvedValueOnce(jsonResponse({ affectedCount: 1 }))
      .mockResolvedValueOnce(jsonResponse({ affectedCount: 1 }));

    const queried = await queryBuyerProductShares(workspace, relationshipId, {
      shareFilter: "shared",
      page: 1,
    });
    expect(queried.sharedCount).toBe(1);
    expect(queried.items.every((item) => item.isShared)).toBe(true);

    await setBuyerProductShares(workspace, relationshipId, [
      { supplierProductId: productId, isShared: true, buyerSpecificPoPrice: 12.5 },
    ]);
    await bulkMutateBuyerProductShares(workspace, relationshipId, {
      operation: "Share",
      productIds: [productId],
    });
    await applyBuyerProductPricing(workspace, relationshipId, {
      mode: "FixedPrice",
      productIds: [productId],
      fixedPrice: 12.5,
    });

    const urls = vi.mocked(fetch).mock.calls.map((call) => String(call[0]));
    expect(urls[0]).toContain("shareFilter=shared");
    expect(urls[0]).toContain("/buyer-product-shares");
    expect(urls[2]).toContain("/buyer-product-shares/bulk");
    expect(urls[3]).toContain("/pricing/apply");
    for (const url of urls) {
      assertNotInventoryMutationUrl(url);
      for (const marker of INVENTORY_MUTATION_PATH_MARKERS) {
        expect(url.toLowerCase()).not.toContain(marker.toLowerCase());
      }
    }
  });

  it("searches catalog and links / create-and-link / unlink without inventory mutation", async () => {
    const exposure = {
      exposureId,
      supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
      productId,
      skuSnapshot: "RICE25",
      nameSnapshot: "Rice 25kg",
      categoryNameSnapshot: null,
      unitOfMeasureCode: "Bag",
      supplierOrderPrice: 15,
      isOrderable: true,
      isExposed: true,
      syncVersion: 1,
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
    };
    const link = {
      linkId,
      relationshipId,
      buyerOrganizationId: workspace.organizationId,
      supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
      buyerProductId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      supplierProductId: productId,
      supplierSkuSnapshot: "RICE25",
      supplierNameSnapshot: "Rice 25kg",
      unitOfMeasureCode: "Bag",
      lastKnownOrderPrice: 15,
      isActive: true,
      syncVersion: 1,
      createdAtUtc: "2026-08-01T00:00:00Z",
      updatedAtUtc: "2026-08-01T00:00:00Z",
    };

    vi.mocked(fetch)
      .mockResolvedValueOnce(
        jsonResponse({ items: [exposure], totalCount: 1, page: 1, pageSize: 25 }),
      )
      .mockResolvedValueOnce(jsonResponse(link))
      .mockResolvedValueOnce(
        jsonResponse({
          link,
          buyerProductId: link.buyerProductId,
          buyerProductName: "Rice 25kg",
          buyerSku: "RICE25",
          buyerSellingPrice: 20,
          createdNewProduct: true,
          alreadyLinked: false,
        }),
      )
      .mockResolvedValueOnce(jsonResponse([link]))
      .mockResolvedValueOnce(jsonResponse({}));

    await searchExposedCatalog(workspace, relationshipId, { query: "rice", page: 2 });
    await linkProduct(workspace, relationshipId, {
      exposureId,
      buyerProductId: link.buyerProductId,
    });
    await createBuyerProductAndLink(workspace, relationshipId, {
      exposureId,
      name: "Rice 25kg",
      unitOfMeasure: "Bag",
      sellingPrice: 20,
      businessUsage: "Resale",
    });
    await listLinks(workspace, relationshipId);
    await unlinkProduct(workspace, linkId);

    const urls = vi.mocked(fetch).mock.calls.map((call) => String(call[0]));
    expect(urls[0]).toContain("/catalog");
    expect(urls[0]).toContain("page=2");
    expect(urls[1]).toContain("/links");
    expect(urls[2]).toContain("/links/create-and-link");
    expect(urls[4]).toContain(`/links/${linkId}`);
    expect(vi.mocked(fetch).mock.calls[4][1]?.method).toBe("DELETE");
    for (const url of urls) {
      assertNotInventoryMutationUrl(url);
    }
  });

  it("incoming order accept/decline/prepare/fulfill never hit inventory mutation paths", async () => {
    const order = {
      connectedPurchaseOrderId: relationshipId,
      relationshipId,
      buyerOrganizationId: "11111111-1111-4111-8111-111111111111",
      supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
      buyerPurchaseOrderId: "33333333-3333-4333-8333-333333333333",
      buyerPoNumber: "PO-1",
      orderDate: "2026-09-04",
      status: "New",
      totalAmount: 12,
      createdAtUtc: "2026-09-04T00:00:00Z",
      updatedAtUtc: "2026-09-04T00:00:00Z",
      lines: [],
      displayStatus: "New",
    };
    vi.mocked(fetch).mockImplementation(() => Promise.resolve(jsonResponse(order)));

    await acceptIncomingOrder(workspace, relationshipId);
    await declineIncomingOrder(workspace, relationshipId, { declineReason: "OutOfStock" });
    await prepareIncomingOrder(workspace, relationshipId);
    await fulfillIncomingOrder(workspace, relationshipId);

    const urls = vi.mocked(fetch).mock.calls.map((call) => String(call[0]));
    expect(urls.some((u) => u.includes("/incoming-orders/") && u.endsWith("/accept"))).toBe(true);
    for (const url of urls) {
      assertNotInventoryMutationUrl(url);
      for (const marker of INVENTORY_MUTATION_PATH_MARKERS) {
        expect(url.toLowerCase()).not.toContain(marker.toLowerCase());
      }
    }
  });
});
