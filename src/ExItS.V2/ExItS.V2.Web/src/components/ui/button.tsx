import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { Loader2 } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import * as React from "react";
import { buttonHover, buttonTap, motionDuration } from "@/motion/presets";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg text-sm font-semibold transition-[color,background-color,box-shadow,opacity] duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:size-4 [&_svg]:shrink-0",
  {
    variants: {
      variant: {
        primary:
          "bg-primary text-primary-foreground shadow-sm hover:bg-primary/90",
        secondary:
          "bg-secondary text-secondary-foreground shadow-sm hover:bg-secondary/90",
        outline:
          "border border-border bg-card text-foreground shadow-sm hover:bg-muted",
        ghost: "text-foreground hover:bg-muted",
        destructive:
          "bg-destructive text-destructive-foreground shadow-sm hover:bg-destructive/90",
      },
      size: {
        sm: "h-8 px-3 text-xs",
        md: "h-10 px-4",
        lg: "h-11 px-5 text-base",
        icon: "size-10",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "md",
    },
  },
);

export type ButtonProps = React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
    loading?: boolean;
  };

const MotionButton = motion.create("button");

function Button({
  className,
  variant,
  size,
  asChild = false,
  loading = false,
  disabled,
  children,
  ...props
}: ButtonProps) {
  const reduced = useReducedMotion();
  const isDisabled = Boolean(disabled || loading);
  const classes = cn(buttonVariants({ variant, size, className }));

  if (asChild) {
    return (
      <Slot
        className={classes}
        data-loading={loading || undefined}
        {...props}
      >
        {children}
      </Slot>
    );
  }

  if (reduced || isDisabled) {
    return (
      <button
        className={classes}
        disabled={isDisabled}
        data-loading={loading || undefined}
        {...props}
      >
        {loading ? <Loader2 className="animate-spin" aria-hidden /> : null}
        {children}
      </button>
    );
  }

  const {
    onDrag: _onDrag,
    onDragStart: _onDragStart,
    onDragEnd: _onDragEnd,
    onAnimationStart: _onAnimationStart,
    ...buttonProps
  } = props;

  return (
    <MotionButton
      className={classes}
      disabled={isDisabled}
      data-loading={loading || undefined}
      whileHover={buttonHover}
      whileTap={buttonTap}
      transition={{ duration: motionDuration.press }}
      {...buttonProps}
    >
      {loading ? <Loader2 className="animate-spin" aria-hidden /> : null}
      {children}
    </MotionButton>
  );
}

export { Button, buttonVariants };
