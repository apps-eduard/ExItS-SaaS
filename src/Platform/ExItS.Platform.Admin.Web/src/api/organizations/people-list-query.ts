import {
  INVITATION_STATUSES,
  MEMBERSHIP_STATUSES,
  ORGANIZATION_PEOPLE_PAGE_SIZE,
  type InvitationStatus,
  type MembershipStatus,
} from "@/api/organizations/organization-types";
import { withQuery } from "@/lib/http/query-string";

export type PeopleTab = "members" | "invitations";

export type OrganizationPeopleUrlState = {
  tab: PeopleTab;
  membersPage: number;
  membersStatus: MembershipStatus | "";
  invitationsPage: number;
  invitationsStatus: InvitationStatus | "";
};

export function isMembershipStatus(value: string): value is MembershipStatus {
  return (MEMBERSHIP_STATUSES as readonly string[]).includes(value);
}

export function isInvitationStatus(value: string): value is InvitationStatus {
  return (INVITATION_STATUSES as readonly string[]).includes(value);
}

function parsePage(raw: string | null): number {
  const value = Number(raw ?? "1");
  return Number.isFinite(value) && value >= 1 ? Math.floor(value) : 1;
}

export function parseOrganizationPeopleSearchParams(
  params: URLSearchParams,
): OrganizationPeopleUrlState {
  const tabRaw = params.get("tab");
  const membersStatusRaw = params.get("membersStatus") ?? "";
  const invitationsStatusRaw = params.get("invitationsStatus") ?? "";
  return {
    tab: tabRaw === "invitations" ? "invitations" : "members",
    membersPage: parsePage(params.get("membersPage")),
    membersStatus: isMembershipStatus(membersStatusRaw) ? membersStatusRaw : "",
    invitationsPage: parsePage(params.get("invitationsPage")),
    invitationsStatus: isInvitationStatus(invitationsStatusRaw) ? invitationsStatusRaw : "",
  };
}

export function organizationPeopleSearchParams(state: OrganizationPeopleUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.tab === "invitations") {
    params.set("tab", "invitations");
  }
  if (state.membersStatus) {
    params.set("membersStatus", state.membersStatus);
  }
  if (state.membersPage > 1) {
    params.set("membersPage", String(state.membersPage));
  }
  if (state.invitationsStatus) {
    params.set("invitationsStatus", state.invitationsStatus);
  }
  if (state.invitationsPage > 1) {
    params.set("invitationsPage", String(state.invitationsPage));
  }
  return params;
}

export function organizationMembersRequestPath(
  organizationId: string,
  query: { status?: string; page: number; pageSize?: number },
): string {
  return withQuery(`/api/v1/platform/organizations/${organizationId}/members`, {
    status: query.status,
    page: query.page,
    pageSize: query.pageSize ?? ORGANIZATION_PEOPLE_PAGE_SIZE,
  });
}

export function organizationInvitationsRequestPath(
  organizationId: string,
  query: { status?: string; page: number; pageSize?: number },
): string {
  return withQuery(`/api/v1/platform/organizations/${organizationId}/invitations`, {
    status: query.status,
    page: query.page,
    pageSize: query.pageSize ?? ORGANIZATION_PEOPLE_PAGE_SIZE,
  });
}
