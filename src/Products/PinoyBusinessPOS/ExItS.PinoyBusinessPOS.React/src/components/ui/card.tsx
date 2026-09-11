import {
  createElement,
  forwardRef,
  type HTMLAttributes,
  type KeyboardEvent,
  type ReactNode,
} from "react";
import { cn } from "@/lib/cn";
import {
  exitsCardMediaVariants,
  exitsCardVariants,
  type ExitsCardAccentPosition,
  type ExitsCardAccentTone,
  type ExitsCardExpandScale,
  type ExitsCardLayout,
  type ExitsCardMotion,
  type ExitsCardPadding,
  type ExitsCardRadius,
  type ExitsCardTreatment,
} from "@/components/exits/card-variants";

export type {
  ExitsCardTreatment,
  ExitsCardMotion,
  ExitsCardExpandScale,
  ExitsCardRadius,
  ExitsCardPadding,
  ExitsCardLayout,
  ExitsCardAccentTone,
  ExitsCardAccentPosition,
};

type CardElement = "section" | "div" | "article" | "button" | "label";

export type CardProps = {
  className?: string;
  children?: ReactNode;
  as?: CardElement;
  /** Visual treatment — APPROVED / LOCKED. Default matches bordered surface. */
  treatment?: ExitsCardTreatment;
  /**
   * Hover motion — APPROVED / LOCKED.
   * When `interactive` and unset, defaults to `lift`.
   */
  motion?: ExitsCardMotion;
  /** Expand intensity when motion="expand". Default standard (~1.02). */
  expandScale?: ExitsCardExpandScale;
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
  /** Reserve footer band for CardReveal (no height jump on hover). */
  reveal?: boolean;
  /** Native button/disabled when rendered as `button` (e.g. selectable options). */
  disabled?: boolean;
  /** Native button type when rendered as `button`. */
  type?: "button" | "submit" | "reset";
  /** Testing / query attribute (explicit for createElement consumers). */
  "data-testid"?: string;
} & Omit<HTMLAttributes<HTMLElement>, "as" | "type" | "disabled">;

/**
 * ExItS Card foundation (APPROVED / LOCKED).
 * Anatomy helpers: CardHeader, CardTitle, CardDescription, CardContent, CardFooter, CardMedia, CardReveal.
 * @see Docs/UI/exits-card-standard.md
 */
export const Card = forwardRef<HTMLElement, CardProps>(function Card(
  {
    className,
    children,
    as,
    treatment = "bordered",
    motion,
    expandScale = "standard",
    radius = "standard",
    padding = "default",
    layout = "vertical",
    accentTone = "neutral",
    accentPosition = "start",
    interactive = false,
    selected = false,
    reveal = false,
    disabled,
    type,
    onClick,
    onKeyDown,
    ...props
  },
  ref,
) {
  const resolvedTreatment: ExitsCardTreatment =
    selected && treatment !== "accent" && treatment !== "featured"
      ? "selected"
      : treatment;
  const resolvedMotion: ExitsCardMotion = motion ?? (interactive ? "lift" : "none");
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
      disabled: Comp === "button" ? disabled : undefined,
      "data-treatment": resolvedTreatment,
      "data-motion": resolvedMotion,
      "data-expand-scale": resolvedMotion === "expand" ? expandScale : undefined,
      "data-selected": selected ? "true" : undefined,
      "data-interactive": interactive ? "true" : undefined,
      "data-reveal": reveal ? "true" : undefined,
      tabIndex: interactive && Comp !== "button" ? (props.tabIndex ?? 0) : props.tabIndex,
      role:
        props.role ??
        (interactive && Comp !== "button" ? "button" : undefined),
      onClick,
      onKeyDown: handleKeyDown,
      className: cn(
        exitsCardVariants({
          treatment: resolvedTreatment,
          motion: resolvedMotion,
          expandScale: resolvedMotion === "expand" ? expandScale : "standard",
          radius,
          padding,
          layout,
          accentTone: resolvedTreatment === "accent" ? accentTone : "neutral",
          accentPosition: resolvedTreatment === "accent" ? accentPosition : "start",
          reveal,
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
  zoom = false,
  ...props
}: HTMLAttributes<HTMLDivElement> & { zoom?: boolean; "data-testid"?: string }) {
  return (
    <div
      data-zoom={zoom ? "true" : undefined}
      className={cn(exitsCardMediaVariants({ zoom }), className)}
      {...props}
    >
      {children}
    </div>
  );
}

/**
 * Overlay actions revealed on card hover / focus-within.
 * Pair with Card `reveal` so reserved space prevents height jump.
 * Always visible under prefers-reduced-motion and focus-within (touch/keyboard safe).
 */
export function CardReveal({
  className,
  children,
  ...props
}: HTMLAttributes<HTMLDivElement> & { "data-testid"?: string }) {
  return (
    <div
      className={cn(
        "pointer-events-none absolute inset-x-0 bottom-0 z-[1] flex justify-end gap-2 p-3",
        "translate-y-1 opacity-0 transition-[opacity,transform] duration-[180ms] ease-[var(--exits-ease-standard)]",
        "group-hover/card:pointer-events-auto group-hover/card:translate-y-0 group-hover/card:opacity-100",
        "group-focus-within/card:pointer-events-auto group-focus-within/card:translate-y-0 group-focus-within/card:opacity-100",
        "motion-reduce:pointer-events-auto motion-reduce:translate-y-0 motion-reduce:opacity-100",
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}
