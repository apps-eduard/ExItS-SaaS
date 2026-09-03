import type { LucideIcon } from "lucide-react";

import { ExItsContainer } from "@/components/exits/ExItsContainer";

export type TrustItem = {
  label: string;
  icon: LucideIcon;
};

export function ExItsTrustStrip({ items }: { items: TrustItem[] }) {
  return (
    <section className="border-b border-borderDefault" aria-label="Trust and reassurance">
      <ExItsContainer className="py-16 lg:py-20">
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {items.map((item) => {
            const Icon = item.icon;
            return (
              <li
                key={item.label}
                className="rounded-xl border border-borderDefault bg-surface px-5 py-6"
              >
                <Icon className="h-5 w-5 text-brandBright" aria-hidden="true" />
                <p className="mt-4 text-sm font-medium leading-relaxed text-primary">{item.label}</p>
              </li>
            );
          })}
        </ul>
      </ExItsContainer>
    </section>
  );
}
