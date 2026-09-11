import {
  createElement,
  forwardRef,
  type HTMLAttributes,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { cn } from "@/lib/cn";
import {
  exitsCardVariants,
  type ExitsCardAccentPosition,
  type ExitsCardAccentTone,
  type ExitsCardLayout,
  type ExitsCardPadding,
  type ExitsCardRadius,
  type ExitsCardTreatment,
} from "@/components/exits/card-variants";

export type {
  ExitsCardTreatment,
  ExitsCardRadius,
  ExitsCardPadding,
  ExitsCardLayout,
  ExitsCardAccentTone,
  ExitsCardAccentPosition,
};

type CardElement = "section" | "div" | "article" | "button" | "label";

export type CardProps = {
  className?: string;
  children: ReactNode;
  as?: CardElement;
  /** Visual treatment — PILOT / NOT LOCKED. Default matches legacy bordered surface. */
  treatment?: ExitsCardTreatment;
  radius?: ExitsCardRadius;
  padding?: ExitsCardPadding;
  layout?: ExitsCardLayout;
  /** Used with treatment="accent". */
  accentTone?: ExitsCardAccentTone;
  accentPosition?: ExitsCardAccentPosition;
  /**
   * Clickable navigation / action card. Promotes default `as` to `button` when unset.
   * Do not combine with selectable radio/checkbox semantics.
   */
  interactive?: boolean;
  /** Marks selected appearance (also use treatment="selected" when preferred). */
  selected?: boolean;
  /** Native button type when rendered as `button`. */
  type?: "button" | "submit" | "reset";
} & Omit<HTMLAttributes<HTMLElement>, "as" | "type">;

/**
 * ExItS Card foundation (PILOT / NOT LOCKED).
 * Anatomy helpers: CardHeader, CardTitle, CardDescription, CardContent, CardFooter, CardMedia.
 */
export const Card = forwardRef<HTMLElement, CardProps>(function Card(
  {
    className,
    children,
    as,
    treatment = "bordered",
    radius = "standard",
    padding = "default",
    layout = "vertical",
    accentTone = "neutral",
    accentPosition = "start",
    interactive = false,
    selected = false,
    type,
    onClick,
    onKeyDown,
    ...props
  },
  ref,
) {
  const resolvedTreatment: ExitsCardTreatment =
    selected && treatment !== "accent" ? "selected" : treatment;
  const Comp: CardElement = as ?? (interactive ? "button" : "section");

  function handleKeyDown(e: KeyboardEvent<HTMLElement>) {
    onKeyDown?.(e);
    if (e.defaultPrevented) return;
    if (!interactive || Comp === "button") return;
    if (e.key === "Enter" || e.key === " ") {
      e.preventDefault();
      (e.currentTarget as HTMLElement).click();
    }
  }

  return createElement(
    Comp,
    {
      ...props,
      ref,
      type: Comp === "button" ? (type ?? "button") : undefined,
      "data-treatment": resolvedTreatment,
      "data-selected": selected ? "true" : undefined,
      "data-interactive": interactive ? "true" : undefined,
      tabIndex: interactive && Comp !== "button" ? (props.tabIndex ?? 0) : props.tabIndex,
      role:
        props.role ??
        (interactive && Comp !== "button" ? "button" : undefined),
      onClick,
      onKeyDown: handleKeyDown,
      className: cn(
        exitsCardVariants({
          treatment: resolvedTreatment,
          radius,
          padding,
          layout,
          accentTone: resolvedTreatment === "accent" ? accentTone : "neutral",
          accentPosition: resolvedTreatment === "accent" ? accentPosition : "start",
        }),
        className,
      ),
    },
    children,
  );
});

export function CardHeader({
  className,
  children,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn("flex min-w-0 items-start justify-between gap-2", className)}
      {...props}
    >
      {children}
    </div>
  );
}

export function CardTitle({
  className,
  children,
  as: Comp = "h3",
  ...props
}: HTMLAttributes<HTMLHeadingElement> & { as?: "h2" | "h3" | "h4" | "p" }) {
  return createElement(
    Comp,
    {
      ...props,
      className: cn(
        "exits-type-card-title m-0 text-[length:var(--exits-text-md)] font-semibold leading-snug text-foreground",
        className,
      ),
    },
    children,
  );
}

export function CardDescription({
  className,
  children,
  ...props
}: HTMLAttributes<HTMLParagraphElement>) {
  return (
    <p
      className={cn("m-0 text-[length:var(--exits-text-sm)] text-muted", className)}
      {...props}
    >
      {children}
    </p>
  );
}

export function CardContent({
  className,
  children,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn("min-w-0 text-[length:var(--exits-text-sm)]", className)} {...props}>
      {children}
    </div>
  );
}

export function CardFooter({
  className,
  children,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "mt-auto flex min-w-0 flex-wrap items-center justify-between gap-2 border-t border-border pt-3",
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}

export function CardMedia({
  className,
  children,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "exits-card__media shrink-0 overflow-hidden rounded-[var(--exits-radius-sm)] bg-[var(--exits-surface-muted)]",
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}
