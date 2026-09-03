import { z } from "zod";
import { platformRequest } from "@/api/platform/platform-http";

function pick(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export const publicStoreLandingSchema = z.object({
  publicOrganizationId: z.string(),
  displayName: z.string(),
  orderingAvailable: z.boolean(),
});

export type PublicStoreLandingDto = z.infer<typeof publicStoreLandingSchema>;

export const publicStoreBranchLocationSchema = z.object({
  branchId: z.string(),
  name: z.string(),
  code: z.string(),
  isPrimary: z.boolean(),
});

export const publicStoreBranchesSchema = z.object({
  publicOrganizationId: z.string(),
  displayName: z.string(),
  branches: z.array(publicStoreBranchLocationSchema),
});

export type PublicStoreBranchLocationDto = z.infer<typeof publicStoreBranchLocationSchema>;
export type PublicStoreBranchesDto = z.infer<typeof publicStoreBranchesSchema>;

export async function lookupPublicStoreLanding(
  publicOrganizationId: string,
  signal?: AbortSignal,
): Promise<PublicStoreLandingDto> {
  const encoded = encodeURIComponent(publicOrganizationId.trim());
  const raw = await platformRequest<unknown>({
    path: `/api/v1/public/stores/${encoded}`,
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  return publicStoreLandingSchema.parse({
    publicOrganizationId: pick(r, "publicOrganizationId", "PublicOrganizationId"),
    displayName: pick(r, "displayName", "DisplayName"),
    orderingAvailable: Boolean(pick(r, "orderingAvailable", "OrderingAvailable")),
  });
}

export async function lookupPublicStoreBranches(
  publicOrganizationId: string,
  signal?: AbortSignal,
): Promise<PublicStoreBranchesDto> {
  const encoded = encodeURIComponent(publicOrganizationId.trim());
  const raw = await platformRequest<unknown>({
    path: `/api/v1/public/stores/${encoded}/branches`,
    signal,
  });
  const r = (raw ?? {}) as Record<string, unknown>;
  const branchesRaw = (pick(r, "branches", "Branches") as unknown[]) ?? [];
  return publicStoreBranchesSchema.parse({
    publicOrganizationId: pick(r, "publicOrganizationId", "PublicOrganizationId"),
    displayName: pick(r, "displayName", "DisplayName"),
    branches: branchesRaw.map((item) => {
      const b = (item ?? {}) as Record<string, unknown>;
      return {
        branchId: String(pick(b, "branchId", "BranchId") ?? ""),
        name: String(pick(b, "name", "Name") ?? ""),
        code: String(pick(b, "code", "Code") ?? ""),
        isPrimary: Boolean(pick(b, "isPrimary", "IsPrimary")),
      };
    }),
  });
}
