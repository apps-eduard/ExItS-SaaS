import { describe, expect, it } from "vitest";
import { initialsFromIdentity } from "@/lib/identity/initials";

describe("initialsFromIdentity", () => {
  it("uses the first and last name letters", () => {
    expect(initialsFromIdentity("Olivia Mendoza")).toBe("OM");
    expect(initialsFromIdentity("Rafael Torres")).toBe("RT");
  });

  it("falls back from displayName to username then email local-part", () => {
    expect(initialsFromIdentity(null, "rafael.torres")).toBe("RT");
    expect(initialsFromIdentity(undefined, undefined, "olivia.mendoza@exits.local")).toBe("OM");
  });

  it("returns null when initials cannot be derived", () => {
    expect(initialsFromIdentity("   ", "", "")).toBeNull();
    expect(initialsFromIdentity(null, null, null)).toBeNull();
  });
});
