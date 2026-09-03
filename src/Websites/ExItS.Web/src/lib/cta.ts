import { cn } from "@/lib/utils";

/**
 * Site-wide CTA class system.
 * Public CTAs stay on documented destinations (WEB-D-04 unresolved).
 * Architecture supports later Create Account / Sign In without redesign.
 */
export type CtaVariant = "primary" | "secondary" | "ghost" | "menu";

export function ctaClassName(variant: CtaVariant = "primary", className?: string) {
  const base =
    "inline-flex min-h-12 items-center justify-center gap-2 whitespace-nowrap rounded-pill px-6 text-sm font-semibold transition-all duration-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright ring-offset-base active:scale-[0.98]";

  const variants = {
    primary:
      "group exits-cta-gradient text-white border border-white/10 shadow-cta hover:-translate-y-0.5 hover:scale-[1.02] hover:brightness-110",
    secondary:
      "group/cta bg-elevated/70 text-primary border border-transparent [background-clip:padding-box] shadow-[inset_0_0_0_1px_rgba(196,181,253,0.28)] backdrop-blur-sm hover:-translate-y-0.5 hover:bg-raised/80 hover:shadow-[inset_0_0_0_1px_rgba(232,121,249,0.45),0_0_24px_rgba(139,92,246,0.2)]",
    ghost:
      "group/cta bg-transparent text-primary border border-transparent hover:text-brandBright px-0 min-h-11",
    menu:
      "bg-elevated/80 text-primary border border-borderDefault px-4 shadow-[inset_0_0_0_1px_rgba(196,181,253,0.18)] backdrop-blur-md hover:border-borderActive hover:bg-raised/90 hover:shadow-glow",
  } as const;

  return cn(base, variants[variant], className);
}

/** Future-ready header CTA config (WEB-D-04). Only enabled actions render. */
export type HeaderCtaAction = {
  id: "get-started" | "create-account" | "sign-in";
  label: string;
  href: string;
  variant?: CtaVariant;
  enabled: boolean;
};

export const headerCtaActions: HeaderCtaAction[] = [
  {
    id: "get-started",
    label: "Get Started",
    href: "/contact",
    variant: "primary",
    enabled: true,
  },
  {
    id: "create-account",
    label: "Create Account",
    href: "#",
    variant: "primary",
    enabled: false,
  },
  {
    id: "sign-in",
    label: "Sign In",
    href: "#",
    variant: "secondary",
    enabled: false,
  },
];
