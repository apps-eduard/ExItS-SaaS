import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const personalDashboardSchema = z.object({
  userIdentityId: guidSchema,
  accountProfileId: guidSchema,
  accountClass: z.string(),
  utangAvailable: z.boolean(),
  contactCount: z.number(),
  activeRelationshipCount: z.number(),
  totalLentBalance: z.number(),
  totalBorrowedBalance: z.number(),
  pendingConfirmationCount: z.number().optional().default(0),
});

export type PersonalDashboardDto = z.infer<typeof personalDashboardSchema>;

function normalizeDashboard(raw: unknown): unknown {
  if (!raw || typeof raw !== "object") {
    return raw;
  }
  const r = raw as Record<string, unknown>;
  return {
    userIdentityId: r.userIdentityId ?? r.UserIdentityId,
    accountProfileId: r.accountProfileId ?? r.AccountProfileId,
    accountClass: r.accountClass ?? r.AccountClass,
    utangAvailable: r.utangAvailable ?? r.UtangAvailable ?? false,
    contactCount: r.contactCount ?? r.ContactCount ?? 0,
    activeRelationshipCount: r.activeRelationshipCount ?? r.ActiveRelationshipCount ?? 0,
    totalLentBalance: Number(r.totalLentBalance ?? r.TotalLentBalance ?? 0),
    totalBorrowedBalance: Number(r.totalBorrowedBalance ?? r.TotalBorrowedBalance ?? 0),
    pendingConfirmationCount: Number(
      r.pendingConfirmationCount ?? r.PendingConfirmationCount ?? 0,
    ),
  };
}

export async function getPersonalDashboard(signal?: AbortSignal): Promise<PersonalDashboardDto> {
  const raw = await platformRequest<unknown>({
    path: "/api/v1/personal/dashboard",
    signal,
  });
  return personalDashboardSchema.parse(normalizeDashboard(raw));
}
