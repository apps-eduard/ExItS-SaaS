import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const organizationActorDisplayNameSchema = z.object({
  actorId: guidSchema,
  displayName: z.string(),
  actorStatus: z.string(),
});

export const resolveOrganizationActorDisplayNamesResponseSchema = z.object({
  items: z.array(organizationActorDisplayNameSchema),
});

export type OrganizationActorDisplayName = z.infer<typeof organizationActorDisplayNameSchema>;
export type ActorStatus = "Active" | "Suspended" | "FormerStaff" | "NotAvailable" | string;

function pickActor(raw: Record<string, unknown>): OrganizationActorDisplayName {
  return organizationActorDisplayNameSchema.parse({
    actorId: String(raw.actorId ?? raw.ActorId ?? ""),
    displayName: String(raw.displayName ?? raw.DisplayName ?? ""),
    actorStatus: String(raw.actorStatus ?? raw.ActorStatus ?? "NotAvailable"),
  });
}

/**
 * Org-scoped batch actor display names for internal detail/history UI.
 * Authorized for any active org member (not ManageMemberships).
 */
export async function resolveOrganizationActorDisplayNames(
  organizationId: string,
  actorIds: string[],
  signal?: AbortSignal,
): Promise<OrganizationActorDisplayName[]> {
  const distinct = [
    ...new Set(
      actorIds
        .map((id) => id.trim())
        .filter((id) => /^[0-9a-fA-F-]{36}$/.test(id)),
    ),
  ].sort();

  if (distinct.length === 0) {
    return [];
  }

  const raw = await platformRequest<unknown>({
    method: "POST",
    path: `/api/v1/platform/organizations/${organizationId}/actor-display-names`,
    body: { actorIds: distinct },
    signal,
  });

  const envelope = raw as { items?: unknown[]; Items?: unknown[] };
  const items = envelope.items ?? envelope.Items ?? [];
  return items
    .filter((item): item is Record<string, unknown> => item != null && typeof item === "object")
    .map(pickActor);
}
