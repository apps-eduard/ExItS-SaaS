import type { ReactNode } from "react";
import { PersonAvatar } from "@/components/exits/PersonAvatar";
import { formatActivityDateTime } from "@/features/purchasing/purchase-order-activity";
import { cn } from "@/lib/cn";

export type ExitsActivityTimelineTone =
  | "primary"
  | "info"
  | "success"
  | "warning"
  | "danger"
  | "neutral";

export type ExitsActivityTimelineItem = {
  id: string;
  atUtc: string;
  title: string;
  description?: ReactNode;
  tone?: ExitsActivityTimelineTone;
  icon?: ReactNode;
  /** Actor display name for the card avatar; omit when unknown / loading. */
  actorName?: string | null;
  actorLoading?: boolean;
  children?: ReactNode;
  testId?: string;
};

export type ExitsActivityTimelineProps = {
  items: readonly ExitsActivityTimelineItem[];
  empty?: ReactNode;
  className?: string;
  testId?: string;
};

/**
 * Alternating vertical activity timeline (center rail + date/time + card).
 * Narrow containers collapse to a single-column rail layout via container query.
 */
export function ExitsActivityTimeline({
  items,
  empty,
  className,
  testId = "exits-activity-timeline",
}: ExitsActivityTimelineProps) {
  if (items.length === 0) {
    return empty ? <>{empty}</> : null;
  }

  return (
    <ol className={cn("exits-activity-timeline", className)} data-testid={testId}>
      {items.map((item, index) => {
        const { date, time } = formatActivityDateTime(item.atUtc);
        const side = index % 2 === 0 ? "start" : "end";
        const isLast = index === items.length - 1;
        const tone = item.tone ?? "primary";
        const avatarName = item.actorLoading ? " " : (item.actorName?.trim() || "?");

        return (
          <li
            key={item.id}
            className="exits-activity-timeline__item"
            data-side={side}
            data-testid={item.testId ?? `exits-activity-item-${item.id}`}
          >
            <div className="exits-activity-timeline__when">
              <span className="exits-activity-timeline__date">{date}</span>
              {time ? <span className="exits-activity-timeline__time">{time}</span> : null}
            </div>

            <div className="exits-activity-timeline__rail" aria-hidden>
              <span
                className={cn(
                  "exits-activity-timeline__marker",
                  `exits-activity-timeline__marker--${tone}`,
                )}
              >
                {item.icon}
              </span>
              {!isLast ? <span className="exits-activity-timeline__line" /> : null}
            </div>

            <article className="exits-activity-timeline__card">
              <header className="exits-activity-timeline__card-head">
                <PersonAvatar
                  name={avatarName}
                  size="sm"
                  className={cn(
                    "exits-activity-timeline__avatar",
                    item.actorLoading && "exits-activity-timeline__avatar--loading",
                  )}
                />
                <h3 className="exits-activity-timeline__title">{item.title}</h3>
              </header>
              {item.description ? (
                <div className="exits-activity-timeline__description">{item.description}</div>
              ) : null}
              {item.children ? (
                <div className="exits-activity-timeline__extra">{item.children}</div>
              ) : null}
            </article>
          </li>
        );
      })}
    </ol>
  );
}
