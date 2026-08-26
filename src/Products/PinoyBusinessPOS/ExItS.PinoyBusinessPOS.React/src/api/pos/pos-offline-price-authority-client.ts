import { z } from "zod";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const AUTHORITIES_PATH = "/api/v1/pos/offline-price-authorities";

/** .NET Guid strings are not always RFC UUID version-nibble compliant. */
const guidSchema = z
  .string()
  .regex(/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/);

/**
 * A server-signed lease to sell one product at one price while this device is offline
 * (RMAP-21 Review Repair 01).
 *
 * The client stores it and replays it verbatim. It must never be edited here: every field is
 * covered by `signature`, and the server refuses a lease it did not sign. That is what makes an
 * offline Cash sale final — the price came from the server before the network dropped, so
 * reconnecting cannot silently reprice a sale the customer already paid for.
 */
export const offlinePriceAuthoritySchema = z.object({
  authorityId: guidSchema,
  organizationId: guidSchema,
  branchId: guidSchema.nullable().optional(),
  // Echoed back from the ids this client just sent, and bound by `signature` rather than by their
  // shape, so they are matched as opaque cache keys instead of re-validated as GUIDs.
  productId: z.string().min(1),
  sellingUnitId: z.string().min(1).nullable().optional(),
  unitPrice: z.number(),
  unitOfMeasure: z.string().min(1),
  sellingMode: z.string().min(1),
  issuedAtUtc: z.string().min(1),
  expiresAtUtc: z.string().min(1),
  signature: z.string().min(1),
});

export const issueOfflinePriceAuthoritiesResponseSchema = z.object({
  authorities: z.array(offlinePriceAuthoritySchema),
  issuedAtUtc: z.string().min(1),
  expiresAtUtc: z.string().min(1),
});

export type OfflinePriceAuthority = z.infer<typeof offlinePriceAuthoritySchema>;
export type IssueOfflinePriceAuthoritiesResponse = z.infer<
  typeof issueOfflinePriceAuthoritiesResponseSchema
>;

/** One requested lease: a product, optionally at one of its sell units. */
export type OfflinePriceAuthorityRequestItem = {
  productId: string;
  sellingUnitId?: string | null;
};

/** The server refuses to mint an unbounded price book from one sell-floor load. */
export const MAX_OFFLINE_PRICE_AUTHORITIES_PER_REQUEST = 500;

/**
 * POST /api/v1/pos/offline-price-authorities — organization + branch scoped like catalog browse.
 * Issuing records nothing and moves no money; it only commits the server to a price for a
 * bounded window.
 */
export async function issueOfflinePriceAuthorities(
  workspace: PosWorkspaceScope,
  items: ReadonlyArray<OfflinePriceAuthorityRequestItem>,
  signal?: AbortSignal,
): Promise<IssueOfflinePriceAuthoritiesResponse> {
  if (items.length === 0) {
    return { authorities: [], issuedAtUtc: "", expiresAtUtc: "" };
  }

  // `sellingUnitIds` is positional and an empty GUID means "base unit", so the two arrays are
  // built together rather than filtered independently.
  const productIds: string[] = [];
  const sellingUnitIds: string[] = [];
  let anySellingUnit = false;
  for (const item of items.slice(0, MAX_OFFLINE_PRICE_AUTHORITIES_PER_REQUEST)) {
    productIds.push(item.productId);
    sellingUnitIds.push(item.sellingUnitId ?? "00000000-0000-0000-0000-000000000000");
    if (item.sellingUnitId) {
      anySellingUnit = true;
    }
  }

  const raw = await posRequest<unknown>({
    method: "POST",
    workspace,
    signal,
    path: AUTHORITIES_PATH,
    body: {
      productIds,
      ...(anySellingUnit ? { sellingUnitIds } : {}),
    },
  });
  return issueOfflinePriceAuthoritiesResponseSchema.parse(raw);
}
