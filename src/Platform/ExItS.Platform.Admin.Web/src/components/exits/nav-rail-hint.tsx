import type { ReactNode } from "react";
import { Tooltip } from "@/components/ui/tooltip";

export function NavRailHint({
  label,
  description,
  children,
}: {
  label: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <Tooltip
      content={label}
      description={description}
      side="right"
      align="center"
      variant="nav"
      delayDuration={450}
    >
      {children}
    </Tooltip>
  );
}
