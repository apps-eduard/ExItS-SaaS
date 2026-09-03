"use client";

import { useState } from "react";
import { LayoutGroup, motion, useReducedMotion } from "framer-motion";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { cn } from "@/lib/utils";

export type SegmentedTabItem = {
  id: string;
  label: string;
  title: string;
  body: string;
};

export function ExItsSegmentedTabs({ items }: { items: SegmentedTabItem[] }) {
  const [value, setValue] = useState(items[0]?.id ?? "");
  const reducedMotion = useReducedMotion();

  return (
    <Tabs value={value} onValueChange={setValue}>
      <LayoutGroup>
        <TabsList className="relative h-auto w-full justify-start gap-1 overflow-x-auto rounded-pill border border-borderDefault bg-night/70 p-1.5 backdrop-blur-md">
          {items.map((item) => {
            const active = value === item.id;
            return (
              <TabsTrigger
                key={item.id}
                value={item.id}
                className={cn(
                  "relative z-10 min-h-11 shrink-0 rounded-pill border-0 bg-transparent px-5 text-muted shadow-none",
                  "data-[state=active]:bg-transparent data-[state=active]:text-white data-[state=active]:shadow-none",
                  "hover:text-primary",
                )}
              >
                {active && !reducedMotion ? (
                  <motion.span
                    layoutId="exits-tab-pill"
                    className="absolute inset-0 -z-10 rounded-pill bg-exits-cta shadow-cta"
                    transition={{ type: "spring", stiffness: 380, damping: 34 }}
                  />
                ) : null}
                {active && reducedMotion ? (
                  <span className="absolute inset-0 -z-10 rounded-pill bg-exits-cta shadow-cta" />
                ) : null}
                {item.label}
              </TabsTrigger>
            );
          })}
        </TabsList>
      </LayoutGroup>
      {items.map((item) => (
        <TabsContent
          key={item.id}
          value={item.id}
          className="mt-5 data-[state=inactive]:hidden"
        >
          <div
            className={cn(
              "exits-gradient-border",
              "animate-[exits-tab-in_0.35s_ease-out]",
            )}
          >
            <div className="exits-gradient-border__inner px-6 py-6">
              <h3 className="text-xl font-semibold text-primary">{item.title}</h3>
              <p className="mt-3 max-w-3xl text-sm leading-relaxed text-muted sm:text-base">
                {item.body}
              </p>
            </div>
          </div>
        </TabsContent>
      ))}
    </Tabs>
  );
}
