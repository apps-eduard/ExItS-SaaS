import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  ACCEPT_INVITATION_ANONYMOUS_PATH,
  ACCEPT_INVITATION_AS_PERSONAL_PATH,
  acceptInvitationAnonymous,
  acceptInvitationAsPersonal,
  createStaffInvitation,
  INVITATION_REQUIRES_AUTHENTICATED_PERSONAL,
} from "@/api/platform/staff-invitation-client";
import { PlatformApiError } from "@/api/platform/platform-http";

const platformRequest = vi.hoisted(() => vi.fn());

vi.mock("@/api/platform/platform-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/platform-http")>(
    "@/api/platform/platform-http",
  );
  return {
    ...actual,
    platformRequest: platformRequest,
  };
});

describe("staff-invitation-client", () => {
  beforeEach(() => {
    platformRequest.mockReset();
  });

  it("creates staff invitation with contact email and OrganizationMember role", async () => {
    platformRequest.mockResolvedValueOnce({
      id: "inv-1",
      organizationId: "org-1",
      email: "contact@example.com",
      role: "OrganizationMember",
      status: "Pending",
      acceptToken: "token-once",
    });

    const result = await createStaffInvitation({
      organizationId: "org-1",
      contactEmail: "  contact@example.com ",
      displayName: "Maria",
    });

    expect(result.ok).toBe(true);
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        method: "POST",
        path: "/api/v1/platform/organizations/org-1/invitations",
        body: {
          email: "contact@example.com",
          role: "OrganizationMember",
          displayName: "Maria",
        },
      }),
    );
  });

  it("posts anonymous accept to auth twin path", async () => {
    platformRequest.mockResolvedValueOnce({
      userId: "u1",
      staffLogin: "maria@ORG123456",
      contactEmail: "contact@example.com",
      organizationDisplayName: "Kizy",
      organizationId: "org-1",
      membershipId: "m1",
      role: "OrganizationMember",
      linkedPersonalUserId: null,
    });

    const result = await acceptInvitationAnonymous({
      token: "tok",
      password: "Staff-Pass-1!",
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.result.staffLogin).toBe("maria@ORG123456");
      expect(result.result.linkedPersonalUserId).toBeNull();
    }
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        method: "POST",
        path: ACCEPT_INVITATION_ANONYMOUS_PATH,
      }),
    );
  });

  it("posts Personal accept to accept-as-personal path", async () => {
    platformRequest.mockResolvedValueOnce({
      userId: "u2",
      staffLogin: "paul@ORG907757",
      contactEmail: "paul@gmail.com",
      organizationDisplayName: "Org A",
      organizationId: "org-a",
      membershipId: "m2",
      role: "OrganizationMember",
      linkedPersonalUserId: "personal-1",
    });

    const result = await acceptInvitationAsPersonal({
      token: "tok",
      password: "Staff-Pass-2!",
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.result.linkedPersonalUserId).toBe("personal-1");
    }
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        path: ACCEPT_INVITATION_AS_PERSONAL_PATH,
      }),
    );
  });

  it("surfaces InvitationRequiresAuthenticatedPersonal from anonymous accept", async () => {
    platformRequest.mockRejectedValueOnce(
      new PlatformApiError(409, {
        errorCode: INVITATION_REQUIRES_AUTHENTICATED_PERSONAL,
        detail: "Sign in with your Personal account to accept this invitation.",
      }),
    );

    const result = await acceptInvitationAnonymous({
      token: "tok",
      password: "Staff-Pass-1!",
    });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.status).toBe(409);
      expect(result.body?.errorCode).toBe(INVITATION_REQUIRES_AUTHENTICATED_PERSONAL);
    }
  });
});
