import { platformRequest } from "./platform-http";
import { asRecord, bool, nullableStr, str } from "./widget";

export type PersonalBusinessCard = {
  organizationId: string;
  displayName: string;
  roleDisplay: string;
  planDisplayName: string | null;
  subscriptionStatus: string | null;
  kind: "owned" | "workplace";
};

export async function listProductAffiliations(
  signal?: AbortSignal,
): Promise<PersonalBusinessCard[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/product-affiliations",
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  const result: PersonalBusinessCard[] = [];
  for (const item of items) {
    const r = asRecord(item);
    const organizationId = str(r, "organizationId", "OrganizationId");
    if (!organizationId) continue;
    const membershipRole = str(r, "membershipRole", "MembershipRole");
    const canManageBilling = bool(r, "canManageBilling", "CanManageBilling");
    const isOwner =
      membershipRole === "OrganizationOwner" ||
      (!membershipRole && canManageBilling);
    if (!isOwner) continue;
    result.push({
      organizationId,
      displayName:
        str(r, "organizationDisplayName", "OrganizationDisplayName") ||
        organizationId,
      roleDisplay:
        str(r, "roleDisplay", "RoleDisplay") || membershipRole || "Owner",
      planDisplayName: nullableStr(r, "planDisplayName", "PlanDisplayName"),
      subscriptionStatus: nullableStr(
        r,
        "subscriptionStatus",
        "SubscriptionStatus",
      ),
      kind: "owned",
    });
  }
  return result;
}

export async function listWorkplaces(
  signal?: AbortSignal,
): Promise<PersonalBusinessCard[]> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/workplaces",
    signal,
  });
  const items = Array.isArray(raw) ? raw : [];
  const result: PersonalBusinessCard[] = [];
  for (const item of items) {
    const r = asRecord(item);
    const organizationId = str(r, "organizationId", "OrganizationId");
    const status = str(r, "membershipStatus", "MembershipStatus");
    if (!organizationId || status.toLowerCase() !== "active") continue;
    result.push({
      organizationId,
      displayName:
        str(r, "organizationDisplayName", "OrganizationDisplayName") ||
        organizationId,
      roleDisplay:
        str(r, "productRoleDisplay", "ProductRoleDisplay") || "Staff",
      planDisplayName: nullableStr(r, "planDisplayName", "PlanDisplayName"),
      subscriptionStatus: null,
      kind: "workplace",
    });
  }
  return result;
}
