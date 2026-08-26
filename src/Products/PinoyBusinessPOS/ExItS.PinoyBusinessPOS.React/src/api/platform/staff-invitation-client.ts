import {
  platformRequest,
  PlatformApiError,
  type PlatformProblemDetails,
} from "@/api/platform/platform-http";

export const INVITATION_REQUIRES_AUTHENTICATED_PERSONAL =
  "application.invitation.requires_authenticated_personal";
export const INVITATION_PERSONAL_EMAIL_UNVERIFIED =
  "application.invitation.personal_email_unverified";
export const INVITATION_NOT_FOUND = "application.invitation.not_found";

export type OrganizationInvitationWire = {
  id: string;
  organizationId: string;
  invitationType?: string;
  email: string;
  role: string;
  status: string;
  acceptToken?: string | null;
  inviteeDisplayName?: string | null;
  expiresAtUtc?: string;
};

export type AcceptInvitationResultWire = {
  userId: string;
  staffLogin: string;
  contactEmail: string;
  organizationDisplayName: string;
  organizationId: string;
  membershipId: string;
  role: string;
  linkedPersonalUserId?: string | null;
};

export type AcceptInvitationBody = {
  token: string;
  password: string;
  displayName?: string | null;
};

function invitationCreatePath(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/invitations`;
}

/** MAUI-preferred accept twins (auth prefix). */
export const ACCEPT_INVITATION_ANONYMOUS_PATH =
  "/api/v1/platform/auth/organization-invitations/accept";
export const ACCEPT_INVITATION_AS_PERSONAL_PATH =
  "/api/v1/platform/auth/organization-invitations/accept-as-personal";

export async function createStaffInvitation(input: {
  organizationId: string;
  contactEmail: string;
  displayName?: string;
}): Promise<
  | { ok: true; invitation: OrganizationInvitationWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const body = await platformRequest<OrganizationInvitationWire>({
      method: "POST",
      path: invitationCreatePath(input.organizationId),
      body: {
        email: input.contactEmail.trim(),
        role: "OrganizationMember",
        displayName: input.displayName?.trim() || null,
      },
    });
    return { ok: true, invitation: body };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function acceptInvitationAnonymous(
  input: AcceptInvitationBody,
): Promise<
  | { ok: true; result: AcceptInvitationResultWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const body = await platformRequest<AcceptInvitationResultWire>({
      method: "POST",
      path: ACCEPT_INVITATION_ANONYMOUS_PATH,
      body: {
        token: input.token.trim(),
        password: input.password,
        displayName: input.displayName ?? null,
      },
    });
    return { ok: true, result: body };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function acceptInvitationAsPersonal(
  input: AcceptInvitationBody,
): Promise<
  | { ok: true; result: AcceptInvitationResultWire }
  | { ok: false; status: number; body: PlatformProblemDetails | null }
> {
  try {
    const body = await platformRequest<AcceptInvitationResultWire>({
      method: "POST",
      path: ACCEPT_INVITATION_AS_PERSONAL_PATH,
      body: {
        token: input.token.trim(),
        password: input.password,
        displayName: input.displayName ?? null,
      },
    });
    return { ok: true, result: body };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}
