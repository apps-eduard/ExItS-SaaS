import { describe, expect, it } from "vitest";
import {
  customerLinkStatusLabelKey,
  extractPersonalExItsIdFromNotes,
  mapPlatformCustomerLinkStatus,
  resolveDisplayedPersonalExItsId,
} from "@/features/customers/customer-link-status";

describe("customer-link-status mapping", () => {
  it("maps Platform statuses including Active→Linked", () => {
    expect(mapPlatformCustomerLinkStatus("NotLinked")).toBe("NotLinked");
    expect(mapPlatformCustomerLinkStatus("Pending")).toBe("Pending");
    expect(mapPlatformCustomerLinkStatus("Linked")).toBe("Linked");
    expect(mapPlatformCustomerLinkStatus("Active")).toBe("Linked");
    expect(mapPlatformCustomerLinkStatus("Declined")).toBe("Declined");
    expect(mapPlatformCustomerLinkStatus("Expired")).toBe("Expired");
    expect(mapPlatformCustomerLinkStatus("Revoked")).toBe("Revoked");
    expect(mapPlatformCustomerLinkStatus("Weird")).toBe("Unavailable");
    expect(mapPlatformCustomerLinkStatus(null)).toBe("Unavailable");
  });

  it("uses MAUI-parity label keys", () => {
    expect(customerLinkStatusLabelKey("Pending")).toBe("customers.linkStatus.pending");
    expect(customerLinkStatusLabelKey("Linked")).toBe("customers.linkStatus.linked");
    expect(customerLinkStatusLabelKey("NotLinked")).toBe("customers.linkStatus.notLinked");
  });

  it("extracts exits-id from notes without exposing it as link status", () => {
    const parsed = extractPersonalExItsIdFromNotes("Walk-in note\nexits-id:EX-1234-5678\nKeep me");
    expect(parsed.exItsId).toBe("EX-1234-5678");
    expect(parsed.notesWithoutExItsTag).toBe("Walk-in note\nKeep me");
  });

  it("prefers linkedPersonalPublicUserId field for EX-ID display", () => {
    expect(
      resolveDisplayedPersonalExItsId({
        linkedPersonalPublicUserId: "EX-AAAA-BBBB",
        notes: "exits-id:EX-OTHER",
      }),
    ).toBe("EX-AAAA-BBBB");
  });
});
