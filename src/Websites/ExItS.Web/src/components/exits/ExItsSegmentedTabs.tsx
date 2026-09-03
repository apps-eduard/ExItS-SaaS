"use client";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export type SegmentedTabItem = {
  id: string;
  label: string;
  title: string;
  body: string;
};

export function ExItsSegmentedTabs({ items }: { items: SegmentedTabItem[] }) {
  return (
    <Tabs defaultValue={items[0]?.id}>
      <TabsList className="h-auto w-full justify-start overflow-x-auto rounded-xl p-1">
        {items.map((item) => (
          <TabsTrigger
            key={item.id}
            value={item.id}
            className="min-h-11 shrink-0 rounded-lg px-4"
          >
            {item.label}
          </TabsTrigger>
        ))}
      </TabsList>
      {items.map((item) => (
        <TabsContent key={item.id} value={item.id}>
          <div className="rounded-xl border border-borderDefault bg-surface px-6 py-6">
            <h3 className="text-xl font-semibold text-primary">{item.title}</h3>
            <p className="mt-3 max-w-3xl text-sm leading-relaxed text-muted sm:text-base">
              {item.body}
            </p>
          </div>
        </TabsContent>
      ))}
    </Tabs>
  );
}
