import * as AvatarPrimitive from "@radix-ui/react-avatar";
import { cn } from "@/lib/utils";

export function Avatar({ initials, className }: { initials: string; className?: string }) {
  return (
    <AvatarPrimitive.Root
      className={cn(
        "inline-flex size-10 items-center justify-center rounded-full bg-[var(--exits-primary-soft)] text-sm font-semibold text-primary",
        className,
      )}
    >
      <AvatarPrimitive.Fallback delayMs={0}>{initials}</AvatarPrimitive.Fallback>
    </AvatarPrimitive.Root>
  );
}
