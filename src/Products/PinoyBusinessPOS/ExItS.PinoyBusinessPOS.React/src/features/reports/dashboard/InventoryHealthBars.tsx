import { Link } from "react-router-dom";
import { cn } from "@/lib/cn";
import { usePrefersReducedMotion } from "@/features/reports/dashboard/useChartMotion";

export type InventoryHealthRow = {
  key: string;
  label: string;
  count: number;
  href?: string;
  tone?: "default" | "attention" | "danger";
};

export function InventoryHealthBars({
  rows,
  animationKey,
}: {
  rows: ReadonlyArray<InventoryHealthRow>;
  animationKey: string;
}) {
  const reduced = usePrefersReducedMotion();
  const max = Math.max(...rows.map((r) => r.count), 1);

  return (
    <ul
      className="dashboard-status-bars m-0 list-none p-0"
      data-testid="dashboard-inventory-health"
      data-animation-key={animationKey}
    >
      {rows.map((row, index) => {
        const widthPct = Math.max(row.count > 0 ? 8 : 2, (row.count / max) * 100);
        const inner = (
          <>
            <div className="dashboard-status-bars__top">
              <span className="dashboard-status-bars__label">{row.label}</span>
              <span className="dashboard-status-bars__count tabular-nums">{row.count}</span>
            </div>
            <div className="dashboard-status-bars__track" aria-hidden>
              <span
                className={cn(
                  "dashboard-status-bars__fill",
                  row.tone === "attention" && "dashboard-status-bars__fill--attention",
                  row.tone === "danger" && "dashboard-status-bars__fill--danger",
                  reduced && "dashboard-status-bars__fill--instant",
                )}
                style={{
                  width: `${widthPct}%`,
                  transitionDelay: reduced ? "0ms" : `${index * 60}ms`,
                }}
              />
            </div>
          </>
        );

        return (
          <li
            key={row.key}
            className={cn(
              "dashboard-status-bars__row",
              row.tone === "attention" && "dashboard-status-bars__row--attention",
              row.tone === "danger" && "dashboard-status-bars__row--danger",
            )}
            data-testid={`inventory-health-${row.key}`}
          >
            {row.href ? (
              <Link to={row.href} className="dashboard-status-bars__link">
                {inner}
              </Link>
            ) : (
              inner
            )}
          </li>
        );
      })}
    </ul>
  );
}
