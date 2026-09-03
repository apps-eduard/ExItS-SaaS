import type { LucideIcon } from "lucide-react";
import { ArrowRight } from "lucide-react";

import { cn } from "@/lib/utils";

export type FeatureItem = {
  title: string;
  body: string;
  icon: LucideIcon;
};

export function ExItsFeatureGrid({
  items,
  columns = 3,
}: {
  items: FeatureItem[];
  columns?: 2 | 3 | 4;
}) {
  return (
    <ul
      className={cn(
        "grid gap-4",
        columns === 2 && "sm:grid-cols-2",
        columns === 3 && "sm:grid-cols-2 lg:grid-cols-3",
        columns === 4 && "sm:grid-cols-2 lg:grid-cols-4",
      )}
    >
      {items.map((item) => {
        const Icon = item.icon;
        return (
          <li
            key={item.title}
            className="exits-gradient-border group transition-transform duration-300 hover:-translate-y-1"
          >
            <div className="exits-gradient-border__inner p-6">
              <div className="flex items-start justify-between gap-3">
                <div className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-gradient-to-br from-brand/40 to-secondary/20 transition-transform duration-300 group-hover:scale-105 group-hover:shadow-[0_0_20px_rgba(34,211,238,0.25)]">
                  <Icon className="h-5 w-5 text-brandBright" aria-hidden="true" />
                </div>
                <ArrowRight
                  className="h-4 w-4 text-magenta transition-transform duration-300 group-hover:translate-x-1"
                  aria-hidden="true"
                />
              </div>
              <h3 className="mt-4 text-base font-semibold text-primary">{item.title}</h3>
              <p className="mt-2 text-sm leading-relaxed text-muted">{item.body}</p>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
