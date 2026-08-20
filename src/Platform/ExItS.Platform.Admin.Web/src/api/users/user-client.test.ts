import { describe, expect, it } from "vitest";
import { mapPlatformUserDetail, mapPlatformUserListItem } from "@/api/users/user-client";
import { parsePlatformUserId } from "@/api/users/user-id";

describe("parsePlatformUserId", () => {
  it("accepts canonical GUID values", () => {
    expect(parsePlatformUserId("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")).toBe(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    );
  });

  it("rejects invalid identifiers", () => {
    expect(parsePlatformUserId("not-a-guid")).toBeNull();
    expect(parsePlatformUserId(undefined)).toBeNull();
  });
});

describe("mapPlatformUserDetail", () => {
  it("maps detail fields and organization scope without inventing values", () => {
    const mapped = mapPlatformUserDetail({
      id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      displayName: "Olivia Mendoza",
      username: "olivia",
      email: "olivia@example.test",
      status: "Active",
      accountClasses: ["Platform"],
      organizationNames: ["Northwind"],
      organizations: [{ name: "Northwind", role: "Owner", roleDisplay: "Owner" }],
      firstName: "Olivia",
      lastName: "Mendoza",
      phone: "+639171234567",
      createdAtUtc: "2026-01-01T08:00:00Z",
      updatedAtUtc: "2026-08-01T08:00:00Z",
    });
    expect(mapped.displayName).toBe("Olivia Mendoza");
    expect(mapped.organizations).toEqual([
      { name: "Northwind", role: "Owner", roleDisplay: "Owner" },
    ]);
    expect(mapped.firstName).toBe("Olivia");
    expect(mapped.phone).toBe("+639171234567");
  });
});

describe("mapPlatformUserListItem", () => {
  it("maps list item fields", () => {
    const mapped = mapPlatformUserListItem({
      id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      displayName: "Olivia Mendoza",
      username: "olivia",
      email: "olivia@example.test",
      status: "Active",
      accountClasses: ["Platform"],
      organizationNames: [],
    });
    expect(mapped.username).toBe("olivia");
  });
});
