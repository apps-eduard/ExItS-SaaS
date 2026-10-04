import { describe, expect, it } from "vitest";
import { splitUtangInvitations } from "@/features/personal/social/PersonalSocialPages";
import type { PersonalUtangInvitationDto } from "@/api/platform/personal-social-client";

const me = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const other = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

function invite(id: string, invitedBy: string): PersonalUtangInvitationDto {
  return {
    id,
    debtRelationshipId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    inviteeContactId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    invitedByUserIdentityId: invitedBy,
    inviteTargetEmailMasked: null,
    status: "Pending",
    createdAtUtc: "2026-10-04T00:00:00Z",
    updatedAtUtc: "2026-10-04T00:00:00Z",
    expiresAtUtc: "2026-10-05T00:00:00Z",
    acceptedAtUtc: null,
    declinedAtUtc: null,
    revokedAtUtc: null,
    acceptedByUserIdentityId: null,
    acceptToken: null,
  };
}

describe("splitUtangInvitations", () => {
  it("keeps sent invitations separate from received ones", () => {
    const split = splitUtangInvitations(
      [invite("11111111-1111-1111-1111-111111111111", me), invite("22222222-2222-2222-2222-222222222222", other)],
      me,
    );
    expect(split.sent.map((item) => item.id)).toEqual(["11111111-1111-1111-1111-111111111111"]);
    expect(split.received.map((item) => item.id)).toEqual(["22222222-2222-2222-2222-222222222222"]);
  });
});
