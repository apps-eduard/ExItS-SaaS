import type { ReactNode } from "react";
import { Check } from "lucide-react";

/** Soft Glass choice card — title above, value/caption below (same recipe as Morning period). */
export function PreferredChoiceCard({
  title,
  caption,
  selected,
  disabled,
  onClick,
  testId,
  icon,
}: {
  title: string;
  caption: string;
  selected?: boolean;
  disabled?: boolean;
  onClick?: () => void;
  testId: string;
  icon?: ReactNode;
}) {
  return (
    <button
      type="button"
      className={
        "personal-preferred-period" +
        (selected ? " personal-preferred-period--selected" : "")
      }
      disabled={disabled}
      aria-pressed={selected}
      data-testid={testId}
      onClick={onClick}
    >
      <span className="personal-preferred-period__title">
        {icon ? <span className="personal-preferred-period__icon">{icon}</span> : null}
        {title}
      </span>
      <span className="personal-preferred-period__caption">
        {caption}
        {selected ? (
          <Check
            className="personal-preferred-period__check"
            strokeWidth={3}
            aria-hidden
          />
        ) : null}
      </span>
    </button>
  );
}

/** Non-interactive Soft Glass fact line (title above, value below) — same surface as Morning. */
export function PreferredInfoCard({
  title,
  value,
  testId,
  icon,
  tone,
}: {
  title: string;
  value: ReactNode;
  testId: string;
  icon?: ReactNode;
  /** Soft Glass shell tone: open = success; closed = danger or warn. */
  tone?: "success" | "warn" | "danger" | "neutral";
}) {
  return (
    <div
      className={
        "personal-preferred-period personal-preferred-period--info" +
        (tone && tone !== "neutral" ? ` personal-preferred-period--tone-${tone}` : "")
      }
      data-testid={testId}
    >
      <span className="personal-preferred-period__title">
        {icon ? <span className="personal-preferred-period__icon">{icon}</span> : null}
        {title}
      </span>
      <span className="personal-preferred-period__caption">{value}</span>
    </div>
  );
}
