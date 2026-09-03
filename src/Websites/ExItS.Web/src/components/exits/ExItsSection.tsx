import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export function ExItsSection({
  children,
  className,
  id,
  tone = "default",
}: {
  children: ReactNode;
  className?: string;
  id?: string;
  tone?: "default" | "surface" | "elevated" | "accent" | "navy" | "violet" | "energy";
}) {
  const toneClass =
    tone === "surface"
      ? "exits-section-tone-surface"
      : tone === "elevated"
        ? "exits-section-tone-elevated"
        : tone === "accent"
          ? "exits-section-tone-accent"
          : tone === "navy"
            ? "exits-section-tone-navy"
            : tone === "violet"
              ? "exits-section-tone-violet"
              : tone === "energy"
                ? "exits-section-tone-energy"
                : "";

  return (
    <section id={id} className={cn("exits-section-fade py-16", toneClass, className)}>
      {children}
    </section>
  );
}
