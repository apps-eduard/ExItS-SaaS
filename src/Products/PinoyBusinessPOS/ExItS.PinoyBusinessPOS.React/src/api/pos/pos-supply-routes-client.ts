import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const PATH = "/api/v1/pos/inventory/supply-routes";

const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

export const supplyRouteDtoSchema = z.object({
  routeId: guidSchema,
  organizationId: guidSchema,
  sourceLocationId: guidSchema,
  destinationLocationId: guidSchema,
  isPreferred: z.boolean(),
  isActive: z.boolean(),
  notes: z.string().nullable().optional(),
  createdAtUtc: z.string(),
  updatedAtUtc: z.string(),
});

export type SupplyRouteDto = z.infer<typeof supplyRouteDtoSchema>;

export type UpsertSupplyRouteItem = {
  sourceLocationId: string;
  isPreferred?: boolean;
  isActive?: boolean;
  notes?: string | null;
};

export async function listSupplyRoutes(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<SupplyRouteDto[]> {
  const data = await posRequest<unknown>({ method: "GET", path: PATH, workspace, signal });
  return z.array(supplyRouteDtoSchema).parse(data);
}

export async function listSupplyRoutesByDestination(
  workspace: PosWorkspaceScope,
  destinationLocationId: string,
  signal?: AbortSignal,
): Promise<SupplyRouteDto[]> {
  const data = await posRequest<unknown>({
    method: "GET",
    path: `${PATH}/by-destination/${destinationLocationId}`,
    workspace,
    signal,
  });
  return z.array(supplyRouteDtoSchema).parse(data);
}

export async function upsertSupplyRoutesForDestination(
  workspace: PosWorkspaceScope,
  destinationLocationId: string,
  routes: UpsertSupplyRouteItem[],
  signal?: AbortSignal,
): Promise<SupplyRouteDto[]> {
  const data = await posRequest<unknown>({
    method: "PUT",
    path: `${PATH}/by-destination/${destinationLocationId}`,
    workspace,
    signal,
    body: { destinationLocationId, routes },
  });
  return z.array(supplyRouteDtoSchema).parse(data);
}
