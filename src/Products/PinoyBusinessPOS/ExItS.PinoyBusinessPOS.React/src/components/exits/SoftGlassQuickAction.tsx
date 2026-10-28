import type { ButtonHTMLAttributes, ReactNode } from "react";
import { ChevronRight, type LucideIcon } from "lucide-react";
import {
  ORG_EHG_QA_GLASS_CLASS_NAME,
  ORG_EHG_QA_GLASS_NEW_IDENTITY,
} from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-qa-glass-global-default";
import {
  softGlassQaTrailing,
  type SoftGlassIdentity,
  type SoftGlassQaTrailing,
} from "@/features/ui-standards/soft-glass-card-global-default";
import { cn } from "@/lib/cn";

export type SoftGlassQuickActionShell = "soft-glass" | "ehg";

export type SoftGlassQuickActionProps = Omit<
  ButtonHTMLAttributes<HTMLButtonElement>,
  "children"
> & {
  /**
   * Color identity. Convert/restyle: keep existing. New QA: use `primary`
   * (Preferences) — see `exits-org-ehg-qa-glass.mdc`.
   */
  identity?: SoftGlassIdentity;
  label: ReactNode;
  icon: LucideIcon;
  /**
   * When set (including `0`), trailing is the Soft Glass count badge.
   * Otherwise use `trailing` or default chevron.
   */
  count?: number | null;
  /** Force trailing when not using count. Default: chevron. */
  trailing?: SoftGlassQaTrailing;
  /**
   * Default `ehg` — Org EHG card-soft (Stores to pay · Stores lock).
   * Pass `soft-glass` only when the user names Soft Glass Enterprise QA.
   */
  shell?: SoftGlassQuickActionShell;
  testId?: string;
  className?: string;
};

/**
 * Quick Action row: icon chip · label · trailing (count badge | chevron | add).
 * Default shell is Org EHG (Stores to pay lock). New tiles default to `primary`.
 */
export function SoftGlassQuickAction({
  identity = ORG_EHG_QA_GLASS_NEW_IDENTITY,
  label,
  icon: Icon,
  count = null,
  trailing,
  shell = "ehg",
  testId,
  className,
  type = "button",
  ...props
}: SoftGlassQuickActionProps) {
  const end = softGlassQaTrailing({ count, trailing });
  const ehg = shell === "ehg";
  const trailToken =
    end === "count" ? "trail-count" : end === "add" ? "trail-add" : "trail-chevron";

  return (
    <button
      type={type}
      className={cn(
        "personal-qa-glass",
        `personal-qa-glass--${identity}`,
        end === "count" && "personal-qa-glass--with-count",
        ehg && ORG_EHG_QA_GLASS_CLASS_NAME,
        className,
      )}
      data-shell={ehg ? "org-ehg" : "soft-glass"}
      data-identity={identity}
      data-trailing={end}
      data-recipe={
        ehg
          ? `ext org-ehg qa-glass ${identity} md icon-soft card-soft ${trailToken} no-shadow no-icon-shadow`
          : undefined
      }
      data-testid={testId}
      {...props}
    >
      <span
        className={cn("personal-qa-glass__icon", ehg && "personal-qa-glass__icon--soft")}
        aria-hidden="true"
      >
        <Icon className="size-[0.95rem] shrink-0" aria-hidden />
      </span>
      <span className="personal-qa-glass__label">{label}</span>
      {end === "count" ? (
        <strong className="personal-qa-glass__badge" aria-hidden="true">
          {count ?? 0}
        </strong>
      ) : end === "add" ? (
        <span
          className={cn("personal-qa-glass__plus", `personal-qa-glass__plus--${identity}`)}
          aria-hidden="true"
        >
          +
        </span>
      ) : (
        <ChevronRight className="personal-qa-glass__chevron" aria-hidden />
      )}
    </button>
  );
}
