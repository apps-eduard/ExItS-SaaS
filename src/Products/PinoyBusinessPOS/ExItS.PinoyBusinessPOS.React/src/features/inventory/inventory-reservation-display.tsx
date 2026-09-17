import { Lock } from "lucide-react";
import { formatQuantityDisplay } from "@/cart/sell-cart-helpers";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";

export function formatInventoryQty(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return "—";
  return formatQuantityDisplay(value);
}

export function resolveAvailableQuantity(item: {
  isTracked: boolean;
  onHandQuantity: number;
  availableQuantity?: number | null;
  reservedQuantity?: number | null;
}): number {
  if (!item.isTracked) return item.onHandQuantity;
  if (item.availableQuantity != null && Number.isFinite(item.availableQuantity)) {
    return item.availableQuantity;
  }
  const reserved =
    item.reservedQuantity != null && Number.isFinite(item.reservedQuantity)
      ? Math.max(0, item.reservedQuantity)
      : 0;
  return Math.max(0, item.onHandQuantity - reserved);
}

export function resolveReservedQuantity(item: {
  reservedQuantity?: number | null;
}): number {
  if (item.reservedQuantity != null && Number.isFinite(item.reservedQuantity)) {
    return Math.max(0, item.reservedQuantity);
  }
  return 0;
}

export function InventoryReservedBadge({
  reservedQuantity,
  unitOfMeasure,
  onClick,
  testId,
  className,
}: {
  reservedQuantity: number;
  unitOfMeasure?: string;
  onClick: () => void;
  testId?: string;
  className?: string;
}) {
  const { t } = useI18n();
  if (!(reservedQuantity > 0)) return null;

  const label = t("inventory.reservedBadge")
    .replace("{qty}", formatInventoryQty(reservedQuantity))
    .replace("{uom}", unitOfMeasure?.trim() || "")
    .replace(/\s+/g, " ")
    .trim();

  return (
    <button
      type="button"
      className={cn(
        "inventory-reserved-badge inline-flex max-w-full items-center gap-1 rounded-md border border-border bg-[var(--exits-surface-muted)] px-1.5 py-0.5 text-left text-[length:var(--exits-text-xs)] font-medium text-muted transition-colors hover:bg-[var(--exits-surface)] hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
      onClick={(event) => {
        event.preventDefault();
        event.stopPropagation();
        onClick();
      }}
      onPointerDown={(event) => event.stopPropagation()}
      onKeyDown={(event) => event.stopPropagation()}
      data-testid={testId ?? "inventory-reserved-badge"}
      aria-label={label}
    >
      <Lock className="size-3.5 shrink-0" aria-hidden strokeWidth={2} />
      <span className="min-w-0 truncate tabular-nums">{label}</span>
    </button>
  );
}
