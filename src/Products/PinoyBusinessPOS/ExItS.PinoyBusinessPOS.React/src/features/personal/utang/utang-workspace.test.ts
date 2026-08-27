import { describe, expect, it } from "vitest";
import {
  buildHomeAttentionItems,
  filterUtangAccounts,
  mergeUtangAccounts,
  resolveRelationshipContactName,
  sortUtangAccounts,
  type UtangAccountRow,
} from "@/features/personal/utang/utang-workspace";

const contactId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const meId = "11111111-1111-1111-1111-111111111111";
const otherUserId = "22222222-2222-2222-2222-222222222222";

describe("utang-workspace", () => {
  it("merges lent and borrowed summaries without duplicating ids", () => {
    const contacts = [{ id: contactId, displayName: "Ana", linkedUserIdentityId: null }];
    const lent = [
      {
        id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        perspective: "Lent",
        creditorUserIdentityId: meId,
        creditorContactId: null,
        debtorUserIdentityId: null,
        debtorContactId: contactId,
        currencyCode: "PHP",
        currentBalance: 100,
        dueDateUtc: null,
        status: "Active",
        version: 1,
        updatedAtUtc: "2026-08-21T00:00:00Z",
        isSharedLedger: false,
        isPrivate: true,
      },
    ];
    const rows = mergeUtangAccounts(lent as never, [], contacts);
    expect(rows).toHaveLength(1);
    expect(rows[0].displayName).toBe("Ana");
    expect(rows[0].perspective).toBe("lent");
  });

  it("resolves linked shared ledger names via counterparty user identity", () => {
    const contacts = [
      {
        id: contactId,
        displayName: "Kizy",
        linkedUserIdentityId: otherUserId,
      },
    ];
    const sharedLent = {
      id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      perspective: "Lent",
      creditorUserIdentityId: meId,
      creditorContactId: null,
      debtorUserIdentityId: otherUserId,
      debtorContactId: null,
      currencyCode: "PHP",
      currentBalance: 434,
      dueDateUtc: null,
      status: "Active",
      version: 1,
      updatedAtUtc: "2026-08-21T00:00:00Z",
      isSharedLedger: true,
      isPrivate: false,
    };
    expect(resolveRelationshipContactName(contacts, sharedLent as never)).toBe("Kizy");
    expect(mergeUtangAccounts([sharedLent as never], [], contacts)[0].displayName).toBe("Kizy");

    const sharedBorrowed = {
      ...sharedLent,
      id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
      perspective: "Borrowed",
      creditorUserIdentityId: otherUserId,
      debtorUserIdentityId: meId,
    };
    expect(resolveRelationshipContactName(contacts, sharedBorrowed as never)).toBe("Kizy");
  });

  it("sorts overdue before upcoming", () => {
    const rows: UtangAccountRow[] = [
      {
        relationshipId: "1",
        perspective: "lent",
        displayName: "B",
        currentBalance: 10,
        dueDateUtc: "2099-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-01T00:00:00Z",
        isSharedLedger: false,
        status: "Active",
        dueKind: "upcoming",
      },
      {
        relationshipId: "2",
        perspective: "owe",
        displayName: "A",
        currentBalance: 20,
        dueDateUtc: "2020-01-01T00:00:00Z",
        updatedAtUtc: "2026-01-02T00:00:00Z",
        isSharedLedger: false,
        status: "Active",
        dueKind: "overdue",
      },
    ];
    expect(sortUtangAccounts(rows).map((r) => r.relationshipId)).toEqual(["2", "1"]);
  });

  it("filters by segment and search", () => {
    const rows: UtangAccountRow[] = [
      {
        relationshipId: "1",
        perspective: "lent",
        displayName: "Ana Reyes",
        currentBalance: 10,
        dueDateUtc: null,
        updatedAtUtc: "2026-01-01T00:00:00Z",
        isSharedLedger: false,
        status: "Active",
        dueKind: "none",
      },
      {
        relationshipId: "2",
        perspective: "owe",
        displayName: "Ben Cruz",
        currentBalance: 5,
        dueDateUtc: null,
        updatedAtUtc: "2026-01-01T00:00:00Z",
        isSharedLedger: false,
        status: "Active",
        dueKind: "none",
      },
    ];
    expect(filterUtangAccounts(rows, "lent", "").map((r) => r.relationshipId)).toEqual(["1"]);
    expect(filterUtangAccounts(rows, "all", "ben").map((r) => r.relationshipId)).toEqual(["2"]);
  });

  it("builds attention items for pending and overdue", () => {
    const overdue: UtangAccountRow = {
      relationshipId: "2",
      perspective: "lent",
      displayName: "Ana",
      currentBalance: 50,
      dueDateUtc: "2020-01-01T00:00:00Z",
      updatedAtUtc: "2026-01-01T00:00:00Z",
      isSharedLedger: false,
      status: "Active",
      dueKind: "overdue",
    };
    const items = buildHomeAttentionItems({
      pendingConfirmationCount: 2,
      accounts: [overdue],
    });
    expect(items[0].kind).toBe("pendingConfirmation");
    expect(items[0].count).toBe(2);
    expect(items[1].kind).toBe("overdue");
    expect(items[1].displayName).toBe("Ana");
  });
});
