import { cn } from "@/lib/utils";

export function ctaClassName(
  variant: "primary" | "secondary" | "ghost" = "primary",
  className?: string,
) {
  const base =
    "inline-flex min-h-11 items-center justify-center gap-2 whitespace-nowrap rounded-lg px-5 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright ring-offset-base";

  const variants = {
    primary:
      "bg-gradient-to-r from-brand to-brandBright text-primary border border-borderDefault hover:brightness-110",
    secondary:
      "bg-transparent text-brandBright border border-brand/50 hover:bg-surface",
    ghost: "bg-transparent text-primary border border-transparent hover:text-brandBright px-0",
  } as const;

  return cn(base, variants[variant], className);
}
