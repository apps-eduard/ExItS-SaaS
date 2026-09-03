import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

/** Atmospheric animated gradient orbs — violet / magenta / cyan. */
export function ExItsAnimatedGradient({
  className,
  intensity = "default",
}: {
  className?: string;
  intensity?: "default" | "subtle" | "strong";
}) {
  const orbOpacity =
    intensity === "strong" ? "opacity-100" : intensity === "subtle" ? "opacity-60" : "opacity-85";

  return (
    <div className={cn("exits-ambient", className)} aria-hidden="true">
      <div className="absolute inset-0 bg-exits-hero" />
      <div
        className={cn(
          "exits-ambient__orb exits-glow-breathe -right-10 -top-8 h-72 w-72 bg-magenta/45 md:h-[22rem] md:w-[22rem]",
          orbOpacity,
        )}
      />
      <div
        className={cn(
          "exits-ambient__orb exits-ambient__orb--alt -left-12 bottom-0 h-64 w-64 bg-secondary/30 md:h-80 md:w-80",
          orbOpacity,
        )}
      />
      <div
        className={cn(
          "exits-ambient__orb left-1/3 top-1/3 h-56 w-56 bg-brand/35 md:h-72 md:w-72",
          orbOpacity,
        )}
      />
      <div className="exits-ambient__orb bottom-1/4 right-1/4 h-40 w-40 bg-emerald/20" />
      <div className="exits-ambient__grid hidden md:block" />
    </div>
  );
}

export function ExItsAmbientShell({
  children,
  className,
  intensity = "default",
}: {
  children: ReactNode;
  className?: string;
  intensity?: "default" | "subtle" | "strong";
}) {
  return (
    <div className={cn("relative overflow-hidden", className)}>
      <ExItsAnimatedGradient intensity={intensity} />
      <div className="relative z-10">{children}</div>
    </div>
  );
}
