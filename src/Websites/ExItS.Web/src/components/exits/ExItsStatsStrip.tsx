import type { LucideIcon } from "lucide-react";

import { ExItsContainer } from "@/components/exits/ExItsContainer";

export type StatItem = {
  label: string;
  icon: LucideIcon;
};

export function ExItsStatsStrip({ items }: { items: StatItem[] }) {
  return (
    <section className="border-b border-borderDefault bg-surface" aria-label="Platform capabilities">
      <ExItsContainer className="py-8">
        <ul className="flex gap-3 overflow-x-auto pb-1 md:grid md:grid-cols-3 md:overflow-visible lg:grid-cols-6">
          {items.map((item) => {
            const Icon = item.icon;
            return (
              <li
                key={item.label}
                className="min-w-[12.5rem] rounded-xl border border-borderDefault bg-base px-4 py-4 md:min-w-0"
              >
                <Icon className="h-5 w-5 text-brandBright" aria-hidden="true" />
                <p className="mt-3 text-sm font-medium leading-snug text-primary">{item.label}</p>
              </li>
            );
          })}
        </ul>
      </ExItsContainer>
    </section>
  );
}
