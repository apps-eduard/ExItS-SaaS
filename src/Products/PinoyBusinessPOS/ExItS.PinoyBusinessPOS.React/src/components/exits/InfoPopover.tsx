import { useLayoutEffect, useRef, useState, type ReactNode, type RefObject } from "react";
import { cn } from "@/lib/cn";

type InfoPopoverProps = {
  id: string;
  titleId: string;
  title: string;
  children: ReactNode;
  /** The control the arrow should point at. */
  anchorRef: RefObject<HTMLElement | null>;
  className?: string;
};

/**
 * Page-header help panel. On a phone it uses the header width; the arrow
 * stays under the info icon.
 */
export function InfoPopover({
  id,
  titleId,
  title,
  children,
  anchorRef,
  className,
}: InfoPopoverProps) {
  const panelRef = useRef<HTMLDivElement>(null);
  const [arrowLeft, setArrowLeft] = useState(28);

  useLayoutEffect(() => {
    const anchor = anchorRef.current;
    const panel = panelRef.current;
    if (!anchor || !panel) {
      return;
    }

    function place() {
      const anchorBox = anchor!.getBoundingClientRect();
      const panelBox = panel!.getBoundingClientRect();
      const center = anchorBox.left + anchorBox.width / 2 - panelBox.left;
      const clamped = Math.min(Math.max(center, 18), Math.max(panelBox.width - 18, 18));
      setArrowLeft(clamped);
    }

    place();
    window.addEventListener("resize", place);
    return () => window.removeEventListener("resize", place);
  }, [anchorRef]);

  return (
    <div
      ref={panelRef}
      id={id}
      role="dialog"
      aria-labelledby={titleId}
      className={cn(
        "absolute inset-x-0 top-[calc(100%+0.7rem)] z-30 rounded-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-lg sm:inset-x-auto sm:start-0 sm:w-[min(32rem,100%)]",
        className,
      )}
    >
      <span
        aria-hidden
        className="pointer-events-none absolute -top-2 size-0 -translate-x-1/2 border-x-8 border-b-8 border-x-transparent border-b-border"
        style={{ left: arrowLeft }}
      />
      <span
        aria-hidden
        className="pointer-events-none absolute -top-[7px] size-0 -translate-x-1/2 border-x-[7px] border-b-[7px] border-x-transparent border-b-surface"
        style={{ left: arrowLeft }}
      />
      <h2 id={titleId} className="m-0 text-[length:var(--exits-text-base)] font-semibold">
        {title}
      </h2>
      {children}
    </div>
  );
}
