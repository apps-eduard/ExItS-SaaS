import { cn } from "@/lib/cn";

export function InlineSpinner({
  className,
  testId = "inline-spinner",
}: {
  className?: string;
  testId?: string;
}) {
  return (
    <span
      className={cn("exits-inline-spinner", className)}
      aria-hidden="true"
      data-testid={testId}
    />
  );
}
