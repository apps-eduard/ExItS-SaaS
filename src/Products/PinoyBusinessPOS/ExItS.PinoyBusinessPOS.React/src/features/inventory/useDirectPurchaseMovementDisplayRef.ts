import { useQuery } from "@tanstack/react-query";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import type { PosStockMovementDto } from "@/api/pos/pos-inventory-client";
import { getDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import {
  resolveDirectPurchaseDisplayLabel,
  resolveDirectPurchaseReceiptId,
} from "@/features/inventory/inventory-movement-direct-purchase-ref";
import { useOptionalWorkspace } from "@/workspace/WorkspaceProvider";

const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function asDisplayReceiptNumber(value: string | null | undefined): string | null {
  const trimmed = value?.trim();
  if (!trimmed || GUID_PATTERN.test(trimmed)) {
    return null;
  }
  return trimmed;
}

/**
 * Resolves the clickable Direct Buy document number for a movement.
 * Prefers server <c>transactionReference</c>; otherwise loads the receipt by id.
 */
export function useDirectPurchaseMovementDisplayRef(
  movement: PosStockMovementDto | null | undefined,
  workspaceOverride?: PosWorkspaceScope | null,
): {
  receiptId: string | null;
  label: string | null;
} {
  const workspaceContext = useOptionalWorkspace();
  const workspace = workspaceOverride ?? workspaceContext?.boundWorkspace ?? null;
  const receiptId = movement ? resolveDirectPurchaseReceiptId(movement) : null;
  const fromMovement = movement ? resolveDirectPurchaseDisplayLabel(movement) : null;

  const receiptQuery = useQuery({
    queryKey: [
      "direct-purchase-receipt",
      "movement-ref",
      workspace?.organizationId,
      receiptId,
    ],
    enabled: Boolean(workspace && receiptId && !fromMovement),
    queryFn: ({ signal }) => getDirectPurchaseReceipt(workspace!, receiptId!, signal),
    staleTime: 60_000,
  });

  const label =
    fromMovement ?? asDisplayReceiptNumber(receiptQuery.data?.receiptNumber);

  return { receiptId, label };
}
