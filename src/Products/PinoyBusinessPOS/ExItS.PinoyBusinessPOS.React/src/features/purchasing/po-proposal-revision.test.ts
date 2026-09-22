import { describe, expect, it } from "vitest";
import {
  buildProposalRevisionFromBuyerPo,
  buildProposalRevisionFromConnectedOrder,
  formatProposalChangeSummary,
} from "@/features/purchasing/po-proposal-revision";
import type { ConnectedPurchaseOrder } from "@/api/pos/pos-connected-suppliers-client";
import type { PosPurchaseOrderDto } from "@/api/pos/pos-purchase-orders-client";

const appleId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const bananaId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

function proposedOrder(): ConnectedPurchaseOrder {
  return {
    connectedPurchaseOrderId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
    relationshipId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
    buyerOrganizationId: "11111111-1111-4111-8111-111111111111",
    supplierOrganizationId: "22222222-2222-4222-8222-222222222222",
    buyerPurchaseOrderId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
    buyerPoNumber: "260917-001",
    orderDate: "2026-09-17",
    notes: null,
    status: "ChangesProposed",
    totalAmount: 1120,
    createdAtUtc: "2026-09-17T00:00:00Z",
    updatedAtUtc: "2026-09-17T01:00:00Z",
    lines: [
      {
        productId: appleId,
        nameSnapshot: "Apple",
        skuSnapshot: "PH-FRU-APPLE",
        qty: 4,
        unitPriceSnapshot: 200,
        lineTotal: 800,
        unitOfMeasureCode: "Kilogram",
        proposedQty: 4,
        proposedLineTotal: 800,
        availability: "Available",
      },
      {
        productId: bananaId,
        nameSnapshot: "Banana Lakatan",
        skuSnapshot: "PH-FRU-BANANA",
        qty: 4,
        unitPriceSnapshot: 80,
        lineTotal: 320,
        unitOfMeasureCode: "Kilogram",
        proposedQty: 2,
        proposedLineTotal: 160,
        availability: "Available",
      },
    ],
    displayStatus: "ChangesProposed",
    proposedTotalAmount: 960,
    inventoryReservationState: "TemporaryProposal",
    inventoryReservationExpiresAtUtc: "2026-09-18T01:00:00Z",
    changesProposedAtUtc: "2026-09-17T01:00:00Z",
    paymentTerm: "Cash",
    paymentTermLabel: "Cash",
  };
}

describe("po-proposal-revision", () => {
  it("builds supplier revision with requested vs proposed and totals", () => {
    const revision = buildProposalRevisionFromConnectedOrder(proposedOrder());
    expect(revision.originalTotal).toBe(1120);
    expect(revision.proposedTotal).toBe(960);
    expect(revision.difference).toBe(-160);
    expect(revision.reservationExpiresAtUtc).toBe("2026-09-18T01:00:00Z");

    const apple = revision.lines.find((l) => l.productId === appleId)!;
    expect(apple.changed).toBe(false);
    expect(apple.proposedLineTotal).toBe(800);

    const banana = revision.lines.find((l) => l.productId === bananaId)!;
    expect(banana.changed).toBe(true);
    expect(banana.requestedQty).toBe(4);
    expect(banana.proposedQty).toBe(2);
    expect(banana.proposedLineTotal).toBe(160);
    expect(formatProposalChangeSummary(banana)).toMatch(/4.*Kg.*2.*Kg/i);
  });

  it("recalculates proposed line total when price changes", () => {
    const order = proposedOrder();
    order.lines[1] = {
      ...order.lines[1]!,
      proposedQty: 2,
      proposedUnitPrice: 90,
      proposedLineTotal: 180,
    };
    order.proposedTotalAmount = 980;
    const revision = buildProposalRevisionFromConnectedOrder(order);
    const banana = revision.lines.find((l) => l.productId === bananaId)!;
    expect(banana.priceChanged).toBe(true);
    expect(banana.proposedUnitCost).toBe(90);
    expect(banana.proposedLineTotal).toBe(180);
    expect(revision.proposedTotal).toBe(980);
  });

  it("builds the same revision shape for buyer connected lines", () => {
    const supplier = buildProposalRevisionFromConnectedOrder(proposedOrder());
    const buyerPo = {
      purchaseOrderId: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
      organizationId: "11111111-1111-4111-8111-111111111111",
      status: "Ordered",
      orderDate: "2026-09-17",
      createdAtUtc: "2026-09-17T00:00:00Z",
      updatedAtUtc: "2026-09-17T01:00:00Z",
      supplierId: "99999999-9999-4999-8999-999999999999",
      lines: [
        {
          lineId: "11111111-1111-4111-8111-111111111101",
          productId: appleId,
          lineNumber: 1,
          nameSnapshot: "Apple",
          orderedQty: 4,
          unitPurchaseCost: 200,
          lineTotal: 800,
          receivedQty: 0,
          outstandingQty: 4,
          uomSnapshot: "Kilogram",
        },
        {
          lineId: "11111111-1111-4111-8111-111111111102",
          productId: bananaId,
          lineNumber: 2,
          nameSnapshot: "Banana Lakatan",
          orderedQty: 4,
          unitPurchaseCost: 80,
          lineTotal: 320,
          receivedQty: 0,
          outstandingQty: 4,
          uomSnapshot: "Kilogram",
        },
      ],
      displayStatus: "ChangesNeedApproval",
      proposedTotalAmount: 960,
      connectedLines: proposedOrder().lines,
      inventoryReservationExpiresAtUtc: "2026-09-18T01:00:00Z",
      changesProposedAtUtc: "2026-09-17T01:00:00Z",
    } as PosPurchaseOrderDto;

    const buyer = buildProposalRevisionFromBuyerPo(buyerPo)!;
    expect(buyer.originalTotal).toBe(supplier.originalTotal);
    expect(buyer.proposedTotal).toBe(supplier.proposedTotal);
    expect(buyer.lines.find((l) => l.productId === bananaId)?.proposedQty).toBe(2);
    expect(buyer.lines.find((l) => l.productId === bananaId)?.proposedLineTotal).toBe(160);
    expect(buyer.reservationExpiresAtUtc).toBe(supplier.reservationExpiresAtUtc);
  });
});
