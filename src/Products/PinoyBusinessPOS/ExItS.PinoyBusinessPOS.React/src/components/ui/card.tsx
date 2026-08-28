import { forwardRef, type HTMLAttributes, type ReactNode } from "react";
import { cn } from "@/lib/cn";

type CardProps = {
  className?: string;
  children: ReactNode;
  as?: "section" | "div" | "article";
} & Omit<HTMLAttributes<HTMLElement>, "as">;

export const Card = forwardRef<HTMLElement, CardProps>(function Card(
  { className, children, as: Comp = "section", ...props },
  ref,
) {
  return (
    <Comp
      ref={ref}
      className={cn(
        "rounded-[var(--exits-radius-md)] border border-border bg-surface px-4 py-4",
        className,
      )}
      {...props}
    >
      {children}
    </Comp>
  );
});
