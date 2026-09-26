import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

/**
 * Shared ExItS Button — canonical control (APPROVED / LOCKED).
 * Intent/Tone = semantic meaning. Appearance = how it is drawn.
 * Legacy `variant` / `treatment` remain supported aliases.
 *
 * @see Docs/UI/exits-button-standard.md
 * @see /ui-standards → Buttons
 */

/** Semantic meaning (why). */
export type ButtonIntentTone =
  | "primary"
  | "neutral"
  | "success"
  | "info"
  | "warning"
  | "danger";

/** Visual rendering (how). Elevated/Gradient are not intents. */
export type ButtonAppearance =
  | "solid"
  | "outline"
  | "ghost"
  | "elevated"
  | "gradient";

/** @deprecated Prefer `ButtonIntentTone`. Legacy CVA `variant` values. */
export type ButtonLegacyVariant =
  | "default"
  | "secondary"
  | "ghost"
  | "outline"
  | "destructive"
  | "success"
  | "info"
  | "warning"
  | "dangerStrong";

/** @deprecated Prefer `ButtonAppearance`. Legacy `treatment` values. */
export type ButtonLegacyTreatment = "flat" | "elevated" | "gradient";

const ELEVATION =
  "shadow-[var(--exits-shadow-sm)] hover:-translate-y-px hover:shadow-[var(--exits-shadow-md)] active:translate-y-0 active:scale-[0.99] motion-reduce:hover:translate-y-0 motion-reduce:hover:shadow-[var(--exits-shadow-sm)] motion-reduce:active:scale-100";

const GRADIENT_MOTION =
  "shadow-[var(--exits-shadow-sm)] hover:-translate-y-px active:translate-y-0 active:scale-[0.99] motion-reduce:hover:translate-y-0 motion-reduce:active:scale-100";

export const buttonVariantsCva = cva(
  [
    "group/button exits-motion-press exits-motion-interaction inline-flex items-center justify-center gap-2 text-[length:var(--exits-text-sm)] font-medium",
    "transition-[background-color,color,box-shadow,border-color,transform,filter] duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)]",
    "active:scale-[0.985] motion-reduce:active:scale-100",
    "focus-visible:outline-none focus-visible:border-[var(--exits-primary)] focus-visible:shadow-[0_0_0_1px_color-mix(in_srgb,var(--exits-primary)_28%,transparent)]",
    "disabled:pointer-events-none disabled:opacity-50 disabled:translate-y-0 disabled:scale-100 disabled:shadow-none",
  ].join(" "),
  {
    variants: {
      intent: {
        primary: "",
        neutral: "",
        success: "",
        info: "",
        warning: "",
        danger: "",
      },
      appearance: {
        solid: "",
        outline: "",
        ghost: "",
        elevated: ELEVATION,
        gradient: GRADIENT_MOTION,
      },
      size: {
        default:
          "h-[var(--exits-control-height)] min-h-[var(--exits-control-height)] px-[var(--exits-control-padding-x)]",
        icon: "size-[var(--exits-control-height)] min-h-[var(--exits-control-height)] min-w-[var(--exits-control-height)] p-0",
        large:
          "h-[var(--exits-control-height-lg)] min-h-[var(--exits-control-height-lg)] px-5 text-[length:var(--exits-text-md)]",
      },
      shape: {
        standard: "rounded-[var(--exits-radius-md)]",
        soft: "rounded-[var(--exits-radius-soft)]",
        pill: "rounded-full",
        round: "rounded-full",
        auto: "rounded-[var(--exits-control-radius)]",
      },
      /**
       * Fill strength for solid/elevated/gradient.
       * soft = tinted surface (product default for Success/Info/Warning/Danger).
       * strong = Prime-style filled semantic color (Severities gallery).
       */
      emphasis: {
        soft: "",
        strong: "",
      },
    },
    compoundVariants: [
      {
        intent: "primary",
        appearance: "solid",
        class: "bg-primary text-primary-foreground hover:bg-[var(--exits-primary-hover)]",
      },
      {
        intent: "primary",
        appearance: "elevated",
        class: "bg-primary text-primary-foreground hover:bg-[var(--exits-primary-hover)]",
      },
      {
        intent: "primary",
        appearance: "outline",
        class:
          "border border-[var(--exits-primary)] bg-surface text-[var(--exits-primary)] hover:bg-[color-mix(in_srgb,var(--exits-primary)_10%,var(--exits-surface))]",
      },
      {
        intent: "primary",
        appearance: "ghost",
        class:
          "bg-transparent text-[var(--exits-primary)] hover:bg-[color-mix(in_srgb,var(--exits-primary)_10%,transparent)]",
      },
      {
        intent: "primary",
        appearance: "gradient",
        class:
          "bg-gradient-to-b from-[var(--exits-primary)] to-[var(--exits-primary-hover)] text-primary-foreground hover:from-[var(--exits-primary)] hover:to-[var(--exits-primary-hover)] hover:brightness-[1.02]",
      },
      {
        intent: "neutral",
        appearance: "solid",
        class:
          "border border-border bg-[var(--exits-surface-muted)] text-foreground hover:border-[var(--exits-border-strong)]",
      },
      {
        intent: "neutral",
        appearance: "elevated",
        class:
          "border border-border bg-[var(--exits-surface-muted)] text-foreground hover:border-[var(--exits-border-strong)]",
      },
      {
        intent: "neutral",
        appearance: "outline",
        class:
          "border border-border bg-surface text-foreground hover:bg-[var(--exits-surface-muted)] hover:border-[var(--exits-border-strong)]",
      },
      {
        intent: "neutral",
        appearance: "ghost",
        class: "bg-transparent text-foreground hover:bg-[var(--exits-surface-muted)]",
      },
      {
        intent: "neutral",
        appearance: "gradient",
        class:
          "border border-border bg-[var(--exits-surface-muted)] text-foreground hover:border-[var(--exits-border-strong)]",
      },
      {
        intent: "success",
        appearance: "solid",
        emphasis: "soft",
        class:
          "border border-border bg-[var(--exits-success-soft)] text-[var(--exits-success)] hover:border-[var(--exits-success)]",
      },
      {
        intent: "success",
        appearance: "elevated",
        emphasis: "soft",
        class:
          "border border-border bg-[var(--exits-success-soft)] text-[var(--exits-success)] hover:border-[var(--exits-success)]",
      },
      {
        intent: "success",
        appearance: "solid",
        emphasis: "strong",
        class:
          "border border-transparent bg-[var(--exits-success)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-success)]",
      },
      {
        intent: "success",
        appearance: "elevated",
        emphasis: "strong",
        class:
          "border border-transparent bg-[var(--exits-success)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-success)]",
      },
      {
        intent: "success",
        appearance: "outline",
        class:
          "border border-[var(--exits-success)] bg-surface text-[var(--exits-success)] hover:bg-[var(--exits-success-soft)]",
      },
      {
        intent: "success",
        appearance: "ghost",
        class:
          "bg-transparent text-[var(--exits-success)] hover:bg-[var(--exits-success-soft)]",
      },
      {
        intent: "success",
        appearance: "gradient",
        emphasis: "soft",
        class:
          "border border-border bg-gradient-to-b from-[var(--exits-success-soft)] to-[color-mix(in_srgb,var(--exits-success)_18%,var(--exits-success-soft))] text-[var(--exits-success)] hover:brightness-100",
      },
      {
        intent: "success",
        appearance: "gradient",
        emphasis: "strong",
        class:
          "border border-transparent bg-gradient-to-b from-[var(--exits-success)] to-[color-mix(in_srgb,var(--exits-success)_78%,#000)] text-white hover:brightness-100",
      },
      {
        intent: "info",
        appearance: "solid",
        emphasis: "soft",
        class:
          "border border-border bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[var(--exits-info)]",
      },
      {
        intent: "info",
        appearance: "elevated",
        emphasis: "soft",
        class:
          "border border-border bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[var(--exits-info)]",
      },
      {
        intent: "info",
        appearance: "solid",
        emphasis: "strong",
        class:
          "border border-transparent bg-[var(--exits-info)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-info)]",
      },
      {
        intent: "info",
        appearance: "elevated",
        emphasis: "strong",
        class:
          "border border-transparent bg-[var(--exits-info)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-info)]",
      },
      {
        intent: "info",
        appearance: "outline",
        class:
          "border border-[var(--exits-info)] bg-surface text-[var(--exits-info)] hover:bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))]",
      },
      {
        intent: "info",
        appearance: "ghost",
        class:
          "bg-transparent text-[var(--exits-info)] hover:bg-[color-mix(in_srgb,var(--exits-info)_10%,transparent)]",
      },
      {
        intent: "info",
        appearance: "gradient",
        emphasis: "soft",
        class:
          "border border-border bg-[color-mix(in_srgb,var(--exits-info)_10%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[var(--exits-info)]",
      },
      {
        intent: "info",
        appearance: "gradient",
        emphasis: "strong",
        class:
          "border border-transparent bg-gradient-to-b from-[var(--exits-info)] to-[color-mix(in_srgb,var(--exits-info)_78%,#000)] text-white hover:brightness-100",
      },
      {
        intent: "warning",
        appearance: "solid",
        emphasis: "soft",
        class:
          "border border-border bg-[var(--exits-warning-soft)] text-[var(--exits-warning)] hover:border-[var(--exits-warning)]",
      },
      {
        intent: "warning",
        appearance: "elevated",
        emphasis: "soft",
        class:
          "border border-border bg-[var(--exits-warning-soft)] text-[var(--exits-warning)] hover:border-[var(--exits-warning)]",
      },
      {
        intent: "warning",
        appearance: "solid",
        emphasis: "strong",
        class:
          "border border-transparent bg-[var(--exits-warning)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-warning)]",
      },
      {
        intent: "warning",
        appearance: "elevated",
        emphasis: "strong",
        class:
          "border border-transparent bg-[var(--exits-warning)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-warning)]",
      },
      {
        intent: "warning",
        appearance: "outline",
        class:
          "border border-[var(--exits-warning)] bg-surface text-[var(--exits-warning)] hover:bg-[var(--exits-warning-soft)]",
      },
      {
        intent: "warning",
        appearance: "ghost",
        class:
          "bg-transparent text-[var(--exits-warning)] hover:bg-[var(--exits-warning-soft)]",
      },
      {
        intent: "warning",
        appearance: "gradient",
        emphasis: "soft",
        class:
          "border border-border bg-[var(--exits-warning-soft)] text-[var(--exits-warning)] hover:border-[var(--exits-warning)]",
      },
      {
        intent: "warning",
        appearance: "gradient",
        emphasis: "strong",
        class:
          "border border-transparent bg-gradient-to-b from-[var(--exits-warning)] to-[color-mix(in_srgb,var(--exits-warning)_78%,#000)] text-white hover:brightness-100",
      },
      {
        intent: "danger",
        appearance: "solid",
        emphasis: "soft",
        class:
          "border border-destructive/35 bg-[var(--exits-danger-soft)] text-destructive hover:border-destructive/50",
      },
      {
        intent: "danger",
        appearance: "elevated",
        emphasis: "soft",
        class:
          "border border-destructive/35 bg-[var(--exits-danger-soft)] text-destructive hover:border-destructive/50",
      },
      {
        intent: "danger",
        appearance: "outline",
        class:
          "border border-destructive/50 bg-surface text-destructive hover:bg-[var(--exits-danger-soft)]",
      },
      {
        intent: "danger",
        appearance: "ghost",
        class: "bg-transparent text-destructive hover:bg-[var(--exits-danger-soft)]",
      },
      {
        intent: "danger",
        appearance: "gradient",
        emphasis: "soft",
        class:
          "border border-destructive/35 bg-[var(--exits-danger-soft)] text-destructive hover:border-destructive/50",
      },
      {
        intent: "danger",
        appearance: "solid",
        emphasis: "strong",
        class: "bg-[var(--exits-danger)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-danger)]",
      },
      {
        intent: "danger",
        appearance: "elevated",
        emphasis: "strong",
        class: "bg-[var(--exits-danger)] text-white hover:brightness-95 focus-visible:ring-[var(--exits-danger)]",
      },
      {
        intent: "danger",
        appearance: "gradient",
        emphasis: "strong",
        class:
          "bg-gradient-to-b from-[var(--exits-danger)] to-[color-mix(in_srgb,var(--exits-danger)_78%,#000)] text-white hover:brightness-100 focus-visible:ring-[var(--exits-danger)]",
      },
    ],
    defaultVariants: {
      intent: "primary",
      appearance: "solid",
      size: "default",
      shape: "auto",
      emphasis: "soft",
    },
  },
);

export type ButtonEmphasis = "soft" | "strong";

export type ResolvedButtonVisual = {
  intent: ButtonIntentTone;
  appearance: ButtonAppearance;
  emphasis: ButtonEmphasis;
};

/**
 * Map legacy variant/treatment → canonical intent + appearance.
 * New `intent` / `appearance` / `emphasis` win when provided.
 */
export function resolveButtonVisual(options: {
  intent?: ButtonIntentTone | null;
  appearance?: ButtonAppearance | null;
  emphasis?: ButtonEmphasis | null;
  /**
   * @deprecated Prefer `emphasis`. Alias for danger solid strength.
   */
  dangerFill?: ButtonEmphasis | null;
  /** @deprecated */
  variant?: ButtonLegacyVariant | null;
  /** @deprecated Prefer appearance elevated|gradient|solid */
  treatment?: ButtonLegacyTreatment | null;
}): ResolvedButtonVisual {
  const explicitEmphasis = options.emphasis ?? options.dangerFill ?? null;
  const hasNew = options.intent != null || options.appearance != null || explicitEmphasis != null;

  if (hasNew) {
    const intent = options.intent ?? "primary";
    let appearance = options.appearance ?? "solid";
    if (options.appearance == null && options.treatment === "elevated") {
      appearance = "elevated";
    } else if (options.appearance == null && options.treatment === "gradient") {
      appearance = "gradient";
    }
    return { intent, appearance, emphasis: explicitEmphasis ?? "soft" };
  }

  const variant = options.variant ?? "default";
  const treatment = options.treatment ?? "flat";

  let intent: ButtonIntentTone = "primary";
  let appearance: ButtonAppearance = "solid";
  let emphasis: ButtonEmphasis = "soft";

  switch (variant) {
    case "default":
      intent = "primary";
      appearance = "solid";
      break;
    case "secondary":
      intent = "neutral";
      appearance = "solid";
      break;
    case "outline":
      intent = "neutral";
      appearance = "outline";
      break;
    case "ghost":
      intent = "neutral";
      appearance = "ghost";
      break;
    case "success":
      intent = "success";
      appearance = "solid";
      break;
    case "info":
      intent = "info";
      appearance = "solid";
      break;
    case "warning":
      intent = "warning";
      appearance = "solid";
      break;
    case "destructive":
      intent = "danger";
      appearance = "solid";
      emphasis = "soft";
      break;
    case "dangerStrong":
      intent = "danger";
      appearance = "solid";
      emphasis = "strong";
      break;
    default:
      intent = "primary";
      appearance = "solid";
  }

  if (treatment === "elevated") {
    appearance = "elevated";
  } else if (treatment === "gradient") {
    if (variant === "default" || variant === "dangerStrong" || variant === "success") {
      appearance = "gradient";
    }
  }

  return { intent, appearance, emphasis };
}

export type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  Omit<VariantProps<typeof buttonVariantsCva>, "intent" | "appearance" | "emphasis"> & {
    asChild?: boolean;
    /** Semantic tone (preferred). */
    intent?: ButtonIntentTone;
    /** Visual treatment (preferred): solid | outline | ghost | elevated | gradient */
    appearance?: ButtonAppearance;
    /**
     * Fill strength for solid/elevated/gradient (soft tint vs strong filled).
     * Severities gallery uses `strong`. Product defaults remain `soft`.
     */
    emphasis?: ButtonEmphasis;
    /**
     * @deprecated Prefer `emphasis`. Kept for danger solid strength call sites.
     */
    dangerFill?: ButtonEmphasis;
    /**
     * @deprecated Prefer `intent` + `appearance`.
     * Legacy: default→primary+solid, secondary→neutral+solid (muted),
     * outline/ghost→neutral+…, destructive→danger+solid(soft), dangerStrong→danger+solid(strong).
     */
    variant?: ButtonLegacyVariant;
    /**
     * @deprecated Prefer `appearance` elevated | gradient | solid.
     */
    treatment?: ButtonLegacyTreatment;
  };

export const buttonIconMotion = {
  continue:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover/button:translate-x-0",
  back: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:-translate-x-0.5 motion-reduce:transition-none motion-reduce:group-hover/button:translate-x-0",
  open: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:translate-x-px group-hover/button:-translate-y-px motion-reduce:transition-none motion-reduce:group-hover/button:translate-x-0 motion-reduce:group-hover/button:translate-y-0",
  add: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-110 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
  view: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-105 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
  delete:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:-translate-y-px group-hover/button:scale-105 motion-reduce:transition-none motion-reduce:group-hover/button:translate-y-0 motion-reduce:group-hover/button:scale-100",
  more: "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-105 group-hover/button:opacity-90 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
  refresh:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:rotate-[20deg] motion-reduce:transition-none motion-reduce:group-hover/button:rotate-0",
  warning:
    "transition-transform duration-[var(--exits-motion-fast)] ease-[var(--exits-ease-standard)] group-hover/button:scale-105 motion-reduce:transition-none motion-reduce:group-hover/button:scale-100",
} as const;

export type ButtonIconMotionKind = keyof typeof buttonIconMotion;

/** Build className from canonical or legacy props (used by tests and Button). */
export function resolveButtonClassName(
  options: {
    intent?: ButtonIntentTone | null;
    appearance?: ButtonAppearance | null;
    emphasis?: ButtonEmphasis | null;
    dangerFill?: ButtonEmphasis | null;
    variant?: ButtonLegacyVariant | null;
    treatment?: ButtonLegacyTreatment | null;
    size?: VariantProps<typeof buttonVariantsCva>["size"];
    shape?: VariantProps<typeof buttonVariantsCva>["shape"];
    className?: string;
  } = {},
): string {
  const visual = resolveButtonVisual(options);
  return cn(
    buttonVariantsCva({
      intent: visual.intent,
      appearance: visual.appearance,
      emphasis: visual.emphasis,
      size: options.size,
      shape: options.shape,
    }),
    options.className,
  );
}

/**
 * Class builder accepting canonical intent/appearance **or** legacy variant/treatment.
 * Keeps existing `buttonVariants({ variant, treatment, shape })` call sites working.
 */
export function buttonVariants(
  options: {
    intent?: ButtonIntentTone | null;
    appearance?: ButtonAppearance | null;
    emphasis?: ButtonEmphasis | null;
    dangerFill?: ButtonEmphasis | null;
    variant?: ButtonLegacyVariant | null;
    treatment?: ButtonLegacyTreatment | null;
    size?: VariantProps<typeof buttonVariantsCva>["size"];
    shape?: VariantProps<typeof buttonVariantsCva>["shape"];
    className?: string;
  } = {},
): string {
  return resolveButtonClassName(options);
}

/** @deprecated Use buttonVariants / resolveButtonClassName. */
export const buttonVariantsCompat = buttonVariants;

export function Button({
  className,
  intent,
  appearance,
  emphasis,
  dangerFill,
  variant,
  treatment,
  size,
  shape,
  asChild = false,
  ...props
}: ButtonProps) {
  const Comp = asChild ? Slot : "button";
  const visual = resolveButtonVisual({ intent, appearance, emphasis, dangerFill, variant, treatment });
  return (
    <Comp
      className={resolveButtonClassName({
        intent,
        appearance,
        emphasis,
        dangerFill,
        variant,
        treatment,
        size,
        shape,
        className,
      })}
      data-intent={visual.intent}
      data-appearance={visual.appearance}
      data-emphasis={visual.emphasis}
      {...props}
    />
  );
}
