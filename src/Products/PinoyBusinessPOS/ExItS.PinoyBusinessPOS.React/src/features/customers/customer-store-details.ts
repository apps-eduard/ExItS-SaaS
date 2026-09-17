import {
  extractPersonalExItsIdFromNotes,
} from "@/features/customers/customer-link-status";

const DELIVERY_PREFIX = "delivery-instructions:";

/**
 * Seller-local notes may carry a delivery-instructions line without a schema migration.
 * Canonical Personal identity tags (`exits-id:`) are preserved separately.
 */
export function extractDeliveryInstructions(
  notes: string | null | undefined,
): { deliveryInstructions: string; internalNotes: string } {
  const withoutExIts = extractPersonalExItsIdFromNotes(notes).notesWithoutExItsTag;
  if (!withoutExIts.trim()) {
    return { deliveryInstructions: "", internalNotes: "" };
  }

  const lines = withoutExIts.split(/\r?\n/);
  let deliveryInstructions = "";
  const kept: string[] = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (trimmed.toLowerCase().startsWith(DELIVERY_PREFIX)) {
      deliveryInstructions = trimmed.slice(DELIVERY_PREFIX.length).trim();
      continue;
    }
    kept.push(line);
  }

  return {
    deliveryInstructions,
    internalNotes: kept.join("\n").trim(),
  };
}

export function composeSellerLocalNotes(input: {
  internalNotes: string;
  deliveryInstructions: string;
  personalExItsId?: string | null;
}): string | null {
  const parts: string[] = [];
  const internal = input.internalNotes.trim();
  if (internal) {
    parts.push(internal);
  }
  const delivery = input.deliveryInstructions.trim();
  if (delivery) {
    parts.push(`${DELIVERY_PREFIX}${delivery}`);
  }
  const exItsId = input.personalExItsId?.trim();
  if (exItsId) {
    parts.push(`exits-id:${exItsId}`);
  }
  return parts.length > 0 ? parts.join("\n") : null;
}
