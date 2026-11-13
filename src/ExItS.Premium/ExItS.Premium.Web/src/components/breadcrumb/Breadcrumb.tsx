import { ExIcon } from "../../icons/ExIcon";
import type { IconName } from "../../icons/registry";
import styles from "./Breadcrumb.module.scss";

export type BreadcrumbItem = {
  /** Stable id for keys and tests. */
  id: string;
  label: string;
  /** Omit on the current page (non-clickable). */
  href?: string;
  /** Optional leading icon (Material Symbols Rounded via ExIcon). */
  icon?: IconName;
};

export type BreadcrumbProps = {
  items: BreadcrumbItem[];
  /** Accessible name for the nav landmark. */
  "aria-label"?: string;
  /** Show the first item's icon when present (Home / Design System). */
  showHomeIcon?: boolean;
  /** Max label characters before ellipsis (per crumb). */
  maxLabelLength?: number;
  className?: string;
  /** Optional hook when a non-current crumb is activated. */
  onNavigate?: (item: BreadcrumbItem) => void;
};

function truncateLabel(label: string, max: number) {
  const value = label.trim();
  if (value.length <= max) return value;
  return `${value.slice(0, Math.max(1, max - 1)).trimEnd()}…`;
}

function Chevron() {
  return (
    <span className={styles.separator} aria-hidden="true">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
        <path
          d="M9.5 6.5L15 12l-5.5 5.5"
          stroke="currentColor"
          strokeWidth="1.75"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
    </span>
  );
}

/**
 * Premium breadcrumb trail — Home → Section → Current.
 * Previous levels are links; the current page uses aria-current="page".
 */
export function Breadcrumb({
  items,
  "aria-label": ariaLabel = "Breadcrumb",
  showHomeIcon = true,
  maxLabelLength = 28,
  className,
  onNavigate,
}: BreadcrumbProps) {
  if (items.length === 0) return null;

  const lastIndex = items.length - 1;
  const hasMiddle = items.length > 2;

  return (
    <nav className={[styles.nav, className].filter(Boolean).join(" ")} aria-label={ariaLabel} data-testid="exits-breadcrumb">
      <ol className={styles.list}>
        {items.map((item, index) => {
          const isCurrent = index === lastIndex || !item.href;
          const isMiddle = hasMiddle && index > 0 && index < lastIndex;
          const label = truncateLabel(item.label, maxLabelLength);
          const showIcon = Boolean(showHomeIcon && index === 0 && item.icon);

          return (
            <li
              key={item.id}
              className={styles.item}
              data-current={isCurrent ? "true" : "false"}
              data-middle={isMiddle ? "true" : "false"}
            >
              {index > 0 ? <Chevron /> : null}
              {isCurrent ? (
                <span className={styles.current} aria-current="page" title={item.label}>
                  {showIcon && item.icon ? <ExIcon name={item.icon} size={16} weight={500} className={styles.icon} /> : null}
                  <span className={styles.label}>{label}</span>
                </span>
              ) : (
                <a
                  className={styles.link}
                  href={item.href}
                  title={item.label}
                  onClick={() => onNavigate?.(item)}
                >
                  {showIcon && item.icon ? <ExIcon name={item.icon} size={16} weight={500} className={styles.icon} /> : null}
                  <span className={styles.label}>{label}</span>
                </a>
              )}
            </li>
          );
        })}
        {hasMiddle ? (
          <li className={styles.collapse} aria-hidden="true">
            <Chevron />
            <span className={styles.ellipsis}>…</span>
          </li>
        ) : null}
      </ol>
    </nav>
  );
}
