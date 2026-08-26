import { describe, expect, it } from "vitest";
import {
  organizationInvitationsRequestPath,
  organizationMembersRequestPath,
  organizationPeopleSearchParams,
  parseOrganizationPeopleSearchParams,
} from "@/api/organizations/people-list-query";

describe("organization people query", () => {
  it("builds member and invitation paths with supported query params only", () => {
    expect(
      organizationMembersRequestPath("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", {
        status: "Active",
        page: 2,
        pageSize: 20,
      }),
    ).toBe(
      "/api/v1/platform/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/members?status=Active&page=2&pageSize=20",
    );
    expect(
      organizationInvitationsRequestPath("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", {
        page: 1,
      }),
    ).toBe(
      "/api/v1/platform/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/invitations?page=1&pageSize=20",
    );
    expect(
      organizationMembersRequestPath("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", {
        page: 1,
      }),
    ).not.toMatch(/search|sortBy/);
  });

  it("parses namespaced URL state and ignores invalid statuses", () => {
    const parsed = parseOrganizationPeopleSearchParams(
      new URLSearchParams(
        "tab=invitations&membersPage=2&membersStatus=Active&invitationsStatus=Nope",
      ),
    );
    expect(parsed.tab).toBe("invitations");
    expect(parsed.membersPage).toBe(2);
    expect(parsed.membersStatus).toBe("Active");
    expect(parsed.invitationsStatus).toBe("");
    expect(organizationPeopleSearchParams(parsed).get("tab")).toBe("invitations");
  });
});
