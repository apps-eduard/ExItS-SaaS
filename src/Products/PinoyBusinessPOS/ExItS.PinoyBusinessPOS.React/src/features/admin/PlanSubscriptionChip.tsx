import {
  resolvePlanSubscriptionChipVariant,
} from "@/features/admin/plan-subscription-chip";
import { cn } from "@/lib/cn";

/** Compact highlighted subscription chip — display-only from catalog plan. */
export function PlanSubscriptionChip({
  planKey,
  planDisplayName,
  className,
}: {
  planKey?: string | null;
  planDisplayName: string;
  className?: string;
}) {
  const variant = resolvePlanSubscriptionChipVariant(planKey, planDisplayName);

  return (
    <span
      className={cn("admin-plan-chip", `admin-plan-chip--${variant}`, className)}
      data-testid="org-plan-chip"
      data-plan={variant}
    >
      {planDisplayName}
    </span>
  );
}
