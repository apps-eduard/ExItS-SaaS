import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export function ExItsGlowCard({
  children,
  className,
  featured = false,
  animatedBorder = false,
}: {
  children: ReactNode;
  className?: string;
  featured?: boolean;
  animatedBorder?: boolean;
}) {
  return (
    <div
      className={cn(
        "exits-gradient-border exits-light-sweep group transition-transform duration-300 hover:-translate-y-1",
        className,
      )}
      data-featured={featured ? "true" : "false"}
      data-animated={animatedBorder || featured ? "true" : "false"}
    >
      <div className="exits-gradient-border__inner p-6">
        <div
          className="pointer-events-none absolute inset-0 opacity-0 transition-opacity duration-300 group-hover:opacity-100"
          aria-hidden="true"
          style={{
            background:
              "radial-gradient(520px circle at var(--glow-x, 70%) var(--glow-y, 0%), rgba(217,70,239,0.16), transparent 42%)",
          }}
        />
        <div className="relative z-10">{children}</div>
      </div>
    </div>
  );
}
