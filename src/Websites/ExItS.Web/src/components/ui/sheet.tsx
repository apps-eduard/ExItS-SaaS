"use client";

import * as React from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "framer-motion";

import { cn } from "@/lib/utils";

export const Sheet = DialogPrimitive.Root;
export const SheetTrigger = DialogPrimitive.Trigger;
export const SheetClose = DialogPrimitive.Close;
export const SheetPortal = DialogPrimitive.Portal;

export const SheetOverlay = React.forwardRef<
  React.ElementRef<typeof DialogPrimitive.Overlay>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Overlay>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Overlay
    ref={ref}
    className={cn(
      "fixed inset-0 z-50 bg-[#080711]/75 backdrop-blur-md data-[state=open]:animate-in data-[state=closed]:animate-out",
      className,
    )}
    {...props}
  />
));
SheetOverlay.displayName = DialogPrimitive.Overlay.displayName;

export const SheetContent = React.forwardRef<
  React.ElementRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content>
>(({ className, children, ...props }, ref) => {
  const reducedMotion = useReducedMotion();

  return (
    <SheetPortal>
      <SheetOverlay />
      <DialogPrimitive.Content
        ref={ref}
        className={cn(
          "fixed right-0 top-0 z-50 h-full w-full max-w-md border-l border-borderDefault p-0 shadow-none",
          "bg-exits-drawer",
          "sm:max-w-lg",
          className,
        )}
        {...props}
      >
        <div
          className="pointer-events-none absolute inset-0"
          aria-hidden="true"
        >
          <div className="absolute -right-16 top-0 h-64 w-64 rounded-full bg-magenta/25 blur-3xl" />
          <div className="absolute -left-10 bottom-10 h-56 w-56 rounded-full bg-secondary/20 blur-3xl" />
          <div className="absolute left-1/3 top-1/3 h-40 w-40 rounded-full bg-brand/20 blur-3xl" />
        </div>
        <DialogPrimitive.Title className="sr-only">Main menu</DialogPrimitive.Title>
        <DialogPrimitive.Description className="sr-only">
          Site navigation for products, solutions, pricing, and company pages.
        </DialogPrimitive.Description>
        <AnimatePresence>
          <motion.div
            className="relative z-10 h-full"
            initial={reducedMotion ? false : { x: 28, opacity: 0.6 }}
            animate={{ x: 0, opacity: 1 }}
            transition={{ type: "spring", stiffness: 320, damping: 32 }}
          >
            {children}
          </motion.div>
        </AnimatePresence>
        <DialogPrimitive.Close className="absolute right-4 top-4 z-20 inline-flex h-11 w-11 items-center justify-center rounded-pill border border-borderDefault bg-elevated/60 text-muted transition-all hover:rotate-90 hover:border-borderActive hover:bg-raised hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brandBright">
          <X className="h-4 w-4" aria-hidden="true" />
          <span className="sr-only">Close</span>
        </DialogPrimitive.Close>
      </DialogPrimitive.Content>
    </SheetPortal>
  );
});
SheetContent.displayName = DialogPrimitive.Content.displayName;

export function SheetHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("flex flex-col space-y-1.5 text-left", className)} {...props} />;
}

export function SheetTitle({
  className,
  ...props
}: DialogPrimitive.DialogTitleProps) {
  return (
    <DialogPrimitive.Title
      className={cn("text-lg font-semibold text-primary", className)}
      {...props}
    />
  );
}

export function SheetDescription({
  className,
  ...props
}: DialogPrimitive.DialogDescriptionProps) {
  return (
    <DialogPrimitive.Description
      className={cn("text-sm text-muted", className)}
      {...props}
    />
  );
}
