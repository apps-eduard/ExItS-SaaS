import { describe, expect, it } from "vitest";
import {
  connectionRelationshipPrecedence,
  mapLinkedStatementHttpToDataLoad,
  mapOrgLinkStatusToRelationship,
  pickHigherConnectionState,
} from "@/features/customer-connection/connection-state";

describe("connection-state", () => {
  it("maps platform org statuses", () => {
    expect(mapOrgLinkStatusToRelationship("Linked")).toBe("Linked");
    expect(mapOrgLinkStatusToRelationship("Active")).toBe("Linked");
    expect(mapOrgLinkStatusToRelationship("Pending")).toBe("Pending");
    expect(mapOrgLinkStatusToRelationship("Unavailable")).toBe("Unavailable");
  });

  it("prefers Blocked then Linked over Declined", () => {
    expect(pickHigherConnectionState("Declined", "Pending")).toBe("Pending");
    expect(pickHigherConnectionState("Revoked", "Linked")).toBe("Linked");
    expect(pickHigherConnectionState("Linked", "Blocked")).toBe("Blocked");
    expect(connectionRelationshipPrecedence("Blocked")).toBeGreaterThan(
      connectionRelationshipPrecedence("Linked"),
    );
  });

  it("keeps linked statement HTTP failures as data-load states", () => {
    expect(mapLinkedStatementHttpToDataLoad(404)).toBe("HistoryNotReady");
    expect(mapLinkedStatementHttpToDataLoad(403)).toBe("Forbidden");
    expect(mapLinkedStatementHttpToDataLoad(503)).toBe("Error");
  });
});
