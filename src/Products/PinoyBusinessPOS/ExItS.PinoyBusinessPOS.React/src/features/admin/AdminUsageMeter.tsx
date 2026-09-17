import { cn } from "@/lib/cn";

export function AdminUsageMeter({
  label,
  used,
  allowed,
  testId,
}: {
  label: string;
  used: number;
  allowed: number;
  testId: string;
}) {
  const pct = allowed > 0 ? Math.min(100, Math.round((used / allowed) * 100)) : 0;
  const atLimit = allowed > 0 && used >= allowed;
  return (
    <li className="admin-usage-meter" data-testid={testId}>
      <div className="admin-usage-meter__row">
        <span className="admin-usage-meter__label">{label}</span>
        <strong className="admin-usage-meter__value">
          {used} / {allowed}
        </strong>
      </div>
      {allowed > 0 ? (
        <div
          className="admin-usage-meter__track"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={allowed}
          aria-valuenow={used}
          aria-label={label}
        >
          <span
            className={cn(
              "admin-usage-meter__fill",
              atLimit && "admin-usage-meter__fill--limit",
            )}
            style={{ width: `${pct}%` }}
          />
        </div>
      ) : null}
    </li>
  );
}
