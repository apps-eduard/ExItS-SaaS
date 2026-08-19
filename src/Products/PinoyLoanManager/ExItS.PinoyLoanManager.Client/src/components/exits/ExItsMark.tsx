import { cn } from "@/lib/cn";

const sizes = {
  sm: "size-7 text-[11px]",
  md: "size-8 text-xs",
  lg: "size-12 text-lg",
} as const;

export function ExItsMark({
  size = "md",
  className,
}: {
  size?: keyof typeof sizes;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-primary font-bold text-primary-foreground",
        sizes[size],
        className,
      )}
      aria-hidden="true"
    >
      E
    </div>
  );
}
