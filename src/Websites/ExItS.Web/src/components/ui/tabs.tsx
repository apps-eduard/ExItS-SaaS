"use client";

import * as React from "react";
import * as TabsPrimitive from "@radix-ui/react-tabs";

import { cn } from "@/lib/utils";

export const Tabs = TabsPrimitive.Root;

export function TabsList({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof TabsPrimitive.List>) {
  return (
    <TabsPrimitive.List
      className={cn(
        "inline-flex h-11 items-center justify-center gap-1 rounded-md border border-borderDefault bg-surface p-1 text-primary",
        className,
      )}
      {...props}
    />
  );
}

export function TabsTrigger({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof TabsPrimitive.Trigger>) {
  return (
    <TabsPrimitive.Trigger
      className={cn(
        "inline-flex items-center justify-center whitespace-nowrap rounded-sm px-4 py-2 text-sm font-semibold text-muted",
        "ring-offset-base transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright",
        "data-[state=active]:bg-gradient-to-r data-[state=active]:from-brand data-[state=active]:to-brandBright data-[state=active]:text-primary data-[state=active]:shadow-none",
        className,
      )}
      {...props}
    />
  );
}

export function TabsContent({
  className,
  ...props
}: React.ComponentPropsWithoutRef<typeof TabsPrimitive.Content>) {
  return (
    <TabsPrimitive.Content
      className={cn(
        "mt-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright",
        className,
      )}
      {...props}
    />
  );
}

