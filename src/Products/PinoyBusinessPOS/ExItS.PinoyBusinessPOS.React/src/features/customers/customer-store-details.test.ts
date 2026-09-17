import { describe, expect, it } from "vitest";
import {
  composeSellerLocalNotes,
  extractDeliveryInstructions,
} from "@/features/customers/customer-store-details";

describe("customer-store-details", () => {
  it("extracts delivery instructions and preserves internal notes", () => {
    const parsed = extractDeliveryInstructions(
      "Front desk\ndelivery-instructions:Leave at gate\nexits-id:EX-0456-4139",
    );
    expect(parsed.deliveryInstructions).toBe("Leave at gate");
    expect(parsed.internalNotes).toBe("Front desk");
  });

  it("composes notes with delivery + exits-id without dropping either", () => {
    const notes = composeSellerLocalNotes({
      internalNotes: "VIP",
      deliveryInstructions: "Call first",
      personalExItsId: "EX-0456-4139",
    });
    expect(notes).toContain("VIP");
    expect(notes).toContain("delivery-instructions:Call first");
    expect(notes).toContain("exits-id:EX-0456-4139");
  });
});
