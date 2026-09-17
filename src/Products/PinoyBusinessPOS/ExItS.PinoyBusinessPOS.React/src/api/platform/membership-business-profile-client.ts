import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

const membershipBusinessProfileSchema = z.object({
  membershipId: guidSchema,
  organizationId: guidSchema,
  userId: guidSchema,
  displayName: z.string(),
  role: z.string(),
  roleDisplay: z.string().nullable().optional().default(null),
  department: z.string().nullable().optional().default(null),
  jobTitle: z.string().nullable().optional().default(null),
  workPhone: z.string().nullable().optional().default(null),
  workEmail: z.string().nullable().optional().default(null),
  isBusinessContact: z.boolean(),
  updatedAtUtc: z.string(),
});

export type MembershipBusinessProfile = z.infer<typeof membershipBusinessProfileSchema>;

function normalizeProfile(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") return raw;
  const r = raw as Record<string, unknown>;
  return {
    membershipId: pick(r, "membershipId", "MembershipId"),
    organizationId: pick(r, "organizationId", "OrganizationId"),
    userId: pick(r, "userId", "UserId"),
    displayName: pick(r, "displayName", "DisplayName") ?? "",
    role: pick(r, "role", "Role") ?? "",
    roleDisplay: pick(r, "roleDisplay", "RoleDisplay") ?? null,
    department: pick(r, "department", "Department") ?? null,
    jobTitle: pick(r, "jobTitle", "JobTitle") ?? null,
    workPhone: pick(r, "workPhone", "WorkPhone") ?? null,
    workEmail: pick(r, "workEmail", "WorkEmail") ?? null,
    isBusinessContact: pick(r, "isBusinessContact", "IsBusinessContact") ?? false,
    updatedAtUtc: pick(r, "updatedAtUtc", "UpdatedAtUtc"),
  };
}

export async function getMembershipBusinessProfile(
  organizationId: string,
  membershipId: string,
  signal?: AbortSignal,
): Promise<MembershipBusinessProfile> {
  const raw = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${organizationId}/members/${membershipId}/business-profile`,
    signal,
  });
  return membershipBusinessProfileSchema.parse(normalizeProfile(raw));
}

export type UpdateMembershipBusinessProfileRequest = {
  department?: string | null;
  jobTitle?: string | null;
  workPhone?: string | null;
  workEmail?: string | null;
  isBusinessContact: boolean;
};

export async function updateMembershipBusinessProfile(
  organizationId: string,
  membershipId: string,
  body: UpdateMembershipBusinessProfileRequest,
  signal?: AbortSignal,
): Promise<MembershipBusinessProfile> {
  const raw = await platformRequest<unknown>({
    method: "PUT",
    path: `/api/v1/platform/organizations/${organizationId}/members/${membershipId}/business-profile`,
    body,
    signal,
  });
  return membershipBusinessProfileSchema.parse(normalizeProfile(raw));
}
