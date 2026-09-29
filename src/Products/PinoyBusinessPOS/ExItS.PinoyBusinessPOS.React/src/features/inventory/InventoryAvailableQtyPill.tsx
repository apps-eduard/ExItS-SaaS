import { buttonVariants } from "@/components/ui/button";
import { formatInventoryQty } from "@/features/inventory/inventory-reservation-display";
import { cn } from "@/lib/cn";

type InventoryAvailableQtyPillProps = {
  quantity: number;
  lowStock?: boolean;
  outOfStock?: boolean;
  testId: string;
  className?: string;
};

/**
 * Inventory Available quantity — ExItS Button Standard:
 * `EXITS BUTTON + ELEVATED + PILL + PRIMARY` (warn/danger when stock is low/out).
 */
export function InventoryAvailableQtyPill({
  quantity,
  lowStock = false,
  outOfStock = false,
  testId,
  className,
}: InventoryAvailableQtyPillProps) {
  const intent = outOfStock ? "danger" : lowStock ? "warning" : "primary";
  return (
    <span
      className={cn(
        buttonVariants({
          intent,
          appearance: "elevated",
          shape: "pill",
          emphasis: "strong",
        }),
        "pointer-events-none !h-auto !min-h-0 px-2.5 py-1 text-[length:var(--exits-text-sm)] font-semibold tabular-nums",
        className,
      )}
      data-intent={intent}
      data-appearance="elevated"
      data-shape="pill"
      data-testid={testId}
    >
      {formatInventoryQty(quantity)}
    </span>
  );
}
