import type { LucideIcon } from "lucide-react";

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
            className="rounded-xl border border-borderDefault bg-surface p-6"
          >
            <Icon className="h-5 w-5 text-brandBright" aria-hidden="true" />
            <h3 className="mt-4 text-base font-semibold text-primary">{item.title}</h3>
            <p className="mt-2 text-sm leading-relaxed text-muted">{item.body}</p>
          </li>
        );
      })}
    </ul>
  );
}
