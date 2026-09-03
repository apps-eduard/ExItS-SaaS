import type { LucideIcon } from "lucide-react";
import { ArrowRight } from "lucide-react";

import { ExItsContainer } from "@/components/exits/ExItsContainer";
import { ExItsStagger } from "@/components/exits/ExItsStagger";
import { cn } from "@/lib/utils";

export type StatItem = {
  label: string;
  icon: LucideIcon;
};

export function ExItsStatsStrip({
  items,
  columns,
}: {
  items: StatItem[];
  columns?: 4 | 5 | 6;
}) {
  const columnClass =
    columns === 4
      ? "lg:grid-cols-4"
      : columns === 5
        ? "lg:grid-cols-5"
        : "lg:grid-cols-6";

  return (
    <section className="exits-section-tone-navy exits-section-fade" aria-label="Platform capabilities">
      <ExItsContainer className="py-10">
        <ExItsStagger
          className={`flex gap-3 overflow-x-auto pb-1 md:grid md:grid-cols-3 md:overflow-visible ${columnClass}`}
          stagger={0.07}
          y={16}
        >
          {items.map((item) => {
            const Icon = item.icon;
            return (
              <div
                key={item.label}
                className={cn(
                  "exits-gradient-border group min-w-[13rem] md:min-w-0",
                  "transition-transform duration-300 hover:-translate-y-1",
                )}
              >
                <div className="exits-gradient-border__inner px-4 py-5">
                  <div className="flex items-start justify-between gap-3">
                    <div className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-gradient-to-br from-brand/35 to-magenta/25 shadow-[0_0_20px_rgba(139,92,246,0.25)] transition-transform duration-300 group-hover:scale-105">
                      <Icon className="h-5 w-5 text-secondary" aria-hidden="true" />
                    </div>
                    <ArrowRight
                      className="h-4 w-4 text-brandBright opacity-0 transition-all duration-300 group-hover:translate-x-1 group-hover:opacity-100"
                      aria-hidden="true"
                    />
                  </div>
                  <p className="mt-4 text-sm font-medium leading-snug text-primary">{item.label}</p>
                </div>
              </div>
            );
          })}
        </ExItsStagger>
      </ExItsContainer>
    </section>
  );
}
