import { Slot } from "@radix-ui/react-slot";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import "@/features/ui-standards/org-enterprise-hybrid-glass/org-enterprise-hybrid-glass.css";
import { cn } from "@/lib/cn";

/** Product default: ext org-ehg btn-glass solid primary flat md no-shadow */
export const EXITS_ORG_EHG_BTN_DEFAULT_COMMAND =
  "ext org-ehg btn-glass solid primary flat md no-shadow" as const;

export type ExitsOrgEhgButtonIntensity = "solid" | "soft" | "medium" | "strong" | "ultra";
export type ExitsOrgEhgButtonIntent =
  | "standard"
  | "primary"
  | "secondary"
  | "success"
  | "info"
  | "warn"
  | "danger"
  | "amber"
  | "violet"
  | "lime"
  | "orange"
  | "inventory";
export type ExitsOrgEhgButtonAppearance =
  | "standard"
  | "soft"
  | "soft-outline"
  | "soft-glass"
  | "outline"
  | "raised"
  | "ghost"
  | "glossy"
  | "glossy-glass"
  | "glossy-raise"
  | "gradient"
  | "gradient-glass"
  | "rounded"
  | "flat"
  | "medallion"
  | "text";
export type ExitsOrgEhgButtonSize = "xs" | "sm" | "md" | "lg" | "xl";

export type ExitsOrgEhgButtonProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children"> & {
  children: ReactNode;
  asChild?: boolean;
  intensity?: ExitsOrgEhgButtonIntensity;
  intent?: ExitsOrgEhgButtonIntent;
  appearance?: ExitsOrgEhgButtonAppearance;
  size?: ExitsOrgEhgButtonSize;
  /** When false, applies `org-ehg-btn-glass--no-shadow`. Default false (locked recipe). */
  shadow?: boolean;
  testId?: string;
};

export function exitsOrgEhgButtonClassName({
  intensity = "solid",
  intent = "primary",
  appearance = "flat",
  size = "md",
  shadow = false,
  className,
}: Pick<
  ExitsOrgEhgButtonProps,
  "intensity" | "intent" | "appearance" | "size" | "shadow" | "className"
> = {}): string {
  return cn(
    "org-ehg-btn-glass",
    `org-ehg-btn-glass--${intensity}`,
    `org-ehg-btn-glass--intent-${intent}`,
    `org-ehg-btn-glass--appearance-${appearance}`,
    `org-ehg-btn-glass--size-${size}`,
    !shadow && "org-ehg-btn-glass--no-shadow",
    className,
  );
}

export function exitsOrgEhgButtonCommand({
  intensity = "solid",
  intent = "primary",
  appearance = "flat",
  size = "md",
  shadow = false,
}: Pick<
  ExitsOrgEhgButtonProps,
  "intensity" | "intent" | "appearance" | "size" | "shadow"
> = {}): string {
  const parts = ["ext", "org-ehg", "btn-glass", intensity, intent, appearance, size];
  if (!shadow) parts.push("no-shadow");
  return parts.join(" ");
}

/**
 * Product Org EHG button glass.
 * Default: ext org-ehg btn-glass solid primary flat md no-shadow
 */
export function ExitsOrgEhgButton({
  children,
  asChild = false,
  intensity = "solid",
  intent = "primary",
  appearance = "flat",
  size = "md",
  shadow = false,
  className,
  testId,
  type = "button",
  ...props
}: ExitsOrgEhgButtonProps) {
  const Comp = asChild ? Slot : "button";
  return (
    <Comp
      type={asChild ? undefined : type}
      className={exitsOrgEhgButtonClassName({
        intensity,
        intent,
        appearance,
        size,
        shadow,
        className,
      })}
      data-shell="org-ehg"
      data-intensity={intensity}
      data-intent={intent}
      data-appearance={appearance}
      data-size={size}
      data-shadow={shadow ? "on" : "off"}
      data-recipe={exitsOrgEhgButtonCommand({ intensity, intent, appearance, size, shadow })}
      data-testid={testId}
      {...props}
    >
      {children}
    </Comp>
  );
}
