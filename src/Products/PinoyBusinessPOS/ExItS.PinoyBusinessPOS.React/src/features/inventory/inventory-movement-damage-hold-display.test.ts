import { describe, expect, it } from "vitest";
import {
  parseDamageHoldDecisionFromReason,
  resolveDamageHoldDecisionDisplay,
} from "@/features/inventory/inventory-movement-damage-hold-display";

describe("inventory-movement-damage-hold-display", () => {
  it("parses return-to-source + replacement from reason", () => {
    const d = parseDamageHoldDecisionFromReason(
      "Transfer damage hold TR-260922-001 · Return to source · Replacement requested",
    );
    expect(d).toEqual({
      custodyLabelKey: "transfer.custody.returnToSource",
      followUpLabelKey: "transfer.decision.replacementRequested",
    });
  });

  it("parses keep-at-destination + accepted from reason", () => {
    const d = parseDamageHoldDecisionFromReason(
      "Transfer damage hold TR-260922-001 · Keep at destination · Accepted — no replacement",
    );
    expect(d).toEqual({
      custodyLabelKey: "transfer.custody.keepAtDestination",
      followUpLabelKey: "transfer.decision.acceptedNoReplacement",
    });
  });

  it("prefers custody DTO over reason for TransferDamageHold", () => {
    const d = resolveDamageHoldDecisionDisplay(
      {
        movementType: "TransferDamageHold",
        reason: "Transfer damage hold TR-260922-001 · Keep at destination · Replacement requested",
        sourceId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
      },
      {
        receiptLineId: "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
        decision: "ReturnToSource",
        followUpIntent: "RequestReplacement",
      },
    );
    expect(d?.custodyLabelKey).toBe("transfer.custody.returnToSource");
    expect(d?.followUpLabelKey).toBe("transfer.decision.replacementRequested");
  });
});
