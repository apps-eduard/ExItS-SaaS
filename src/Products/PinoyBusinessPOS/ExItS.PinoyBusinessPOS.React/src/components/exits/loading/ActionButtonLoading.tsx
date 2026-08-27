import type { ReactNode } from "react";
import { Button, type ButtonProps } from "@/components/ui/button";
import { InlineSpinner } from "@/components/exits/loading/InlineSpinner";
import { cn } from "@/lib/cn";

/** Mutation/action control: spinner inside the button, duplicate submit blocked. */
export function ActionButtonLoading({
  loading,
  children,
  className,
  disabled,
  ...props
}: ButtonProps & {
  loading: boolean;
  children: ReactNode;
}) {
  return (
    <Button
      {...props}
      className={cn(className)}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      data-loading={loading ? "true" : undefined}
    >
      {loading ? <InlineSpinner /> : null}
      {children}
    </Button>
  );
}
