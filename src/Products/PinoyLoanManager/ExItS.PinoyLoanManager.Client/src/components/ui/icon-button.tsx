import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Button } from "@/components/ui/button";

export function IconButton({
  label,
  children,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { label: string; children: ReactNode }) {
  return (
    <Button type="button" variant="ghost" size="icon" aria-label={label} {...props}>
      {children}
    </Button>
  );
}
