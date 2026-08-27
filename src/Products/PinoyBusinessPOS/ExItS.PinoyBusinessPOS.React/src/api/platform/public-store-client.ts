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
