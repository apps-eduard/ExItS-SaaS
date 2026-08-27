import { describe, expect, it } from "vitest";
import { resolveCustomerListConnectionBadge } from "@/features/customers/customer-list-connection";

/**
 * POS EX-ID fields must not advertise Platform "Linked"/"Connected" on the list
 * without the org-wide overlay (no N+1).
 */
describe("CustomersListPage link meta", () => {
  it("uses ExItS ID instead of Connected for POS-local public ids", () => {
    const kind = resolveCustomerListConnectionBadge(
      {
        linkedPersonalPublicUserId: "EX-1234-5678",
        platformBusinessCustomerId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      },
      null,
    );
    expect(kind).toBe("exits-id");
    expect(kind).not.toBe("connected");
  });
});
