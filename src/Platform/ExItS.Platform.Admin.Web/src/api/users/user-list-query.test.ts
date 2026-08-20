import { describe, expect, it } from "vitest";
import {
  parseUserListSearchParams,
  sanitizeUserDirectory,
  usersListRequestPath,
} from "@/api/users/user-list-query";

describe("user list query", () => {
  it("maps navigation aliases to API directory values", () => {
    expect(sanitizeUserDirectory("platform")).toBe("PlatformStaff");
    expect(sanitizeUserDirectory("needs-review")).toBe("Unassigned");
    expect(sanitizeUserDirectory("PlatformStaff")).toBe("PlatformStaff");
    expect(sanitizeUserDirectory("bogus")).toBeNull();
  });

  it("treats status=needs-review as Unassigned directory", () => {
    const state = parseUserListSearchParams(new URLSearchParams("status=needs-review"));
    expect(state.directory).toBe("Unassigned");
    expect(state.status).toBe("");
  });

  it("builds the server path with actual API parameters", () => {
    expect(
      usersListRequestPath({
        page: 2,
        pageSize: 20,
        directory: "Organization",
        status: "Suspended",
        search: "oli",
        sortBy: "Email",
        sortDesc: true,
      }),
    ).toBe(
      "/api/v1/platform/users?page=2&pageSize=20&status=Suspended&search=oli&directory=Organization&sortBy=Email&sortDesc=true",
    );
  });
});
