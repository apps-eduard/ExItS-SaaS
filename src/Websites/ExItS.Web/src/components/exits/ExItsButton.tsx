import * as React from "react";

import { Button, type ButtonProps } from "@/components/ui/button";

export type ExItsButtonProps = ButtonProps;

export function ExItsButton({
  variant,
  size,
  className,
  ...props
}: ExItsButtonProps) {
  return (
    <Button
      variant={variant ?? "primary"}
      size={size}
      className={className}
      {...props}
    />
  );
}

