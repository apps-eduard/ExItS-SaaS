import { describe, expect, it } from "vitest";
import type { PersonalUtangEntryDto } from "@/api/platform/personal-utang-client";
import type { PersonalTodoDto } from "@/api/platform/personal-todo-client";
import type { CustomerOrderListItemDto } from "@/api/pos/pos-customer-orders-client";
import {
  buildHomeRecentActivityItems,
  formatHomeActivityWhen,
  selectRecentHistoryRelationships,
} from "@/features/personal/home-recent-activity";
import type { MessageKey } from "@/i18n/messages";
import type { UtangAccountRow } from "@/features/personal/utang/utang-workspace";

const t = (key: MessageKey) => key;

function account(partial: Partial<UtangAccountRow>): UtangAccountRow {
  return {
    relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    perspective: "lent",
    displayName: "Ada",
    currentBalance: 100,
    dueDateUtc: null,
    updatedAtUtc: "2026-11-01T12:00:00Z",
    isSharedLedger: false,
    isLedgerOwner: true,
    status: "Active",
    dueKind: "none",
    ...partial,
  };
}

function entry(partial: Partial<PersonalUtangEntryDto>): PersonalUtangEntryDto {
  return {
    id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    entryType: "Loan",
    amount: 50,
    signedDelta: 50,
    balanceAfter: 50,
    notes: null,
    dueDateUtc: null,
    createdByUserIdentityId: "11111111-1111-1111-1111-111111111111",
    createdAtUtc: "2026-11-08T10:00:00Z",
    status: "Confirmed",
    resolvedByUserIdentityId: null,
    resolvedAtUtc: null,
    disputeReason: null,
    canConfirm: false,
    canDispute: false,
    canCancel: false,
    affectsBalance: true,
    isSharedLedger: false,
    intent: "Regular",
    settlementBalanceSnapshot: null,
    isSettlement: false,
    confirmationSource: "None",
    wasAutoSynced: false,
    ...partial,
  };
}

function todo(partial: Partial<PersonalTodoDto>): PersonalTodoDto {
  return {
    id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    ownerUserIdentityId: "11111111-1111-1111-1111-111111111111",
    title: "Buy milk",
    notes: null,
    dueAtUtc: null,
    reminderAtUtc: null,
    reminderNotifiedAtUtc: null,
    priority: "Normal",
    status: "Open",
    relatedEntityType: null,
    relatedEntityId: null,
    createdAtUtc: "2026-11-07T09:00:00Z",
    updatedAtUtc: "2026-11-07T09:00:00Z",
    completedAtUtc: null,
    version: 1,
    ...partial,
  };
}

function order(partial: Partial<CustomerOrderListItemDto>): CustomerOrderListItemDto {
  return {
    orderId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    orderNumber: "42",
    status: "Accepted",
    fulfillmentStatus: "Accepted",
    paymentStatus: "Unpaid",
    paymentMethod: "Cash",
    fulfillmentType: "Pickup",
    fulfillmentBranchId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
    branchNameSnapshot: "Corner Store",
    customerDisplayName: "Ana",
    total: 120.5,
    createdAtUtc: "2026-11-06T08:00:00Z",
    updatedAtUtc: "2026-11-09T08:00:00Z",
    lineCount: 2,
    sellerOrganizationId: "ffffffff-ffff-ffff-ffff-ffffffffffff",
    ...partial,
  };
}

describe("home-recent-activity", () => {
  it("formats activity dates", () => {
    expect(formatHomeActivityWhen("not-a-date")).toBe("");
    expect(formatHomeActivityWhen("2026-11-01T12:00:00Z")).toMatch(/Nov/);
  });

  it("selects newest relationships for bounded history fetch", () => {
    const selected = selectRecentHistoryRelationships(
      [
        account({ relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1", updatedAtUtc: "2026-11-01T00:00:00Z" }),
        account({ relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2", updatedAtUtc: "2026-11-03T00:00:00Z" }),
        account({ relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3", updatedAtUtc: "2026-11-02T00:00:00Z" }),
      ],
      2,
    );
    expect(selected.map((row) => row.relationshipId)).toEqual([
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2",
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3",
    ]);
  });

  it("merges mixed sources and sorts newest first", () => {
    const items = buildHomeRecentActivityItems({
      notifications: [
        {
          id: "11111111-1111-1111-1111-111111111111",
          title: "Connection",
          preview: "Ada sent you a connection request",
          relatedType: "PersonalConnectionRequest",
          relatedId: null,
          isRead: false,
          createdAtUtc: "2026-11-08T12:00:00Z",
          readAtUtc: null,
        },
      ],
      history: [
        {
          relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Ben",
          perspective: "lent",
          isLedgerOwner: true,
          entries: [
            entry({
              id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
              entryType: "Payment",
              amount: 25,
              createdAtUtc: "2026-11-10T10:00:00Z",
            }),
          ],
        },
      ],
      orders: [order({ updatedAtUtc: "2026-11-09T15:00:00Z" })],
      todos: [
        todo({
          completedAtUtc: "2026-11-07T18:00:00Z",
          status: "Completed",
        }),
      ],
      accounts: [account({})],
      t,
    });

    expect(items).toHaveLength(4);
    expect(items.map((item) => item.id)).toEqual([
      "utang-entry:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1",
      "order:dddddddd-dddd-dddd-dddd-dddddddddddd",
      "notification:11111111-1111-1111-1111-111111111111",
      "todo-completed:cccccccc-cccc-cccc-cccc-cccccccccccc",
    ]);
    expect(items[0]?.href).toBe("/personal/utang/relationships/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    expect(items[0]?.detail).toContain("25");
    expect(items[0]?.icon).toBe("utang-payment");
    expect(items[1]?.href).toBe("/personal/orders/dddddddd-dddd-dddd-dddd-dddddddddddd");
    expect(items[2]?.href).toBe("/personal/invitations");
    expect(items[3]?.href).toBe("/personal/todo/cccccccc-cccc-cccc-cccc-cccccccccccc");
  });

  it("dedupes order notifications against order history", () => {
    const orderId = "dddddddd-dddd-dddd-dddd-dddddddddddd";
    const items = buildHomeRecentActivityItems({
      notifications: [
        {
          id: "11111111-1111-1111-1111-111111111112",
          title: "Order accepted",
          preview: "Corner Store accepted your order",
          relatedType: "CustomerOrderAccepted",
          relatedId: orderId,
          isRead: false,
          createdAtUtc: "2026-11-09T16:00:00Z",
          readAtUtc: null,
        },
      ],
      orders: [order({ orderId, updatedAtUtc: "2026-11-09T15:00:00Z", total: 99 })],
      t,
    });
    expect(items).toHaveLength(1);
    expect(items[0]?.id).toBe(`order:${orderId}`);
    expect(items[0]?.detail).toContain("99");
    expect(items[0]?.href).toBe(`/personal/orders/${orderId}`);
  });

  it("dedupes todo notifications against todo activity", () => {
    const todoId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
    const items = buildHomeRecentActivityItems({
      notifications: [
        {
          id: "11111111-1111-1111-1111-111111111113",
          title: "To-do reminder",
          preview: "Buy milk",
          relatedType: "PersonalTodo",
          relatedId: todoId,
          isRead: false,
          createdAtUtc: "2026-11-08T08:00:00Z",
          readAtUtc: null,
        },
      ],
      todos: [todo({ id: todoId, createdAtUtc: "2026-11-07T09:00:00Z" })],
      t,
    });
    expect(items).toHaveLength(1);
    expect(items[0]?.id).toBe(`todo-created:${todoId}`);
    expect(items[0]?.title).toBe("personal.home.activityTodoCreated");
  });

  it("marks shared ledger history as read-only and links to relationship", () => {
    const items = buildHomeRecentActivityItems({
      history: [
        {
          relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Cara",
          perspective: "owe",
          isLedgerOwner: false,
          entries: [
            entry({
              id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2",
              entryType: "Adjustment",
              amount: 10,
              status: "Confirmed",
              resolvedAtUtc: "2026-11-11T11:00:00Z",
              createdAtUtc: "2026-11-10T11:00:00Z",
              isSharedLedger: true,
            }),
          ],
        },
      ],
      t,
    });
    expect(items).toHaveLength(1);
    expect(items[0]?.icon).toBe("utang-shared");
    expect(items[0]?.detail).toContain("personal.utang.readOnly");
    expect(items[0]?.at).toBe("2026-11-11T11:00:00Z");
    expect(items[0]?.href).toBe("/personal/utang/relationships/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  });

  it("caps unified feed at five newest events", () => {
    const items = buildHomeRecentActivityItems({
      notifications: Array.from({ length: 3 }, (_, i) => ({
        id: `11111111-1111-1111-1111-11111111111${i}`,
        title: `Notice ${i}`,
        preview: `Preview ${i}`,
        relatedType: "PersonalConnectionRequest",
        relatedId: null,
        isRead: false,
        createdAtUtc: `2026-11-${String(10 - i).padStart(2, "0")}T08:00:00Z`,
        readAtUtc: null,
      })),
      todos: Array.from({ length: 4 }, (_, i) =>
        todo({
          id: `cccccccc-cccc-cccc-cccc-ccccccccccc${i}`,
          title: `Task ${i}`,
          createdAtUtc: `2026-11-${String(12 - i).padStart(2, "0")}T08:00:00Z`,
        }),
      ),
      t,
    });
    expect(items).toHaveLength(5);
    expect(items[0]?.detail).toBe("Task 0");
  });

  it("falls back to relationship summaries only when typed sources are empty", () => {
    const items = buildHomeRecentActivityItems({
      notifications: [],
      history: [],
      orders: [],
      todos: [],
      accounts: [
        account({
          relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
          updatedAtUtc: "2026-11-02T00:00:00Z",
          perspective: "owe",
        }),
        account({
          relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2",
          updatedAtUtc: "2026-11-03T00:00:00Z",
          perspective: "lent",
          displayName: "Ben",
        }),
      ],
      t,
    });
    expect(items[0]?.id).toBe("utang-relationship:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
    expect(items[0]?.title).toBe("personal.home.activityMoneyLent");
    expect(items[0]?.href).toBe(
      "/personal/utang/relationships/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2",
    );
    expect(items[1]?.href).toBe(
      "/personal/utang/relationships/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1",
    );
  });

  it("keeps available history when notifications are missing", () => {
    const items = buildHomeRecentActivityItems({
      notifications: undefined,
      history: [
        {
          relationshipId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Ada",
          perspective: "lent",
          isLedgerOwner: true,
          entries: [entry({ createdAtUtc: "2026-11-10T10:00:00Z" })],
        },
      ],
      t,
    });
    expect(items).toHaveLength(1);
    expect(items[0]?.id).toBe("utang-entry:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  });
});
