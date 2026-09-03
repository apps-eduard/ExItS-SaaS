import type { ElementType } from "react";

import { cn } from "@/lib/utils";

export function ExItsOutlineHeading({
  as: Tag = "h1",
  outline,
  solid,
  className,
}: {
  as?: ElementType;
  outline: string;
  solid: string;
  className?: string;
}) {
  return (
    <Tag
      className={cn(
        "max-w-4xl text-balance font-semibold tracking-tight",
        className,
      )}
    >
      <span className="block text-[2.5rem] leading-[1.05] text-transparent [-webkit-text-stroke:1.5px_#f0f4f1] sm:text-5xl lg:text-7xl">
        {outline}
      </span>
      <span className="mt-2 block text-[2.5rem] leading-[1.1] text-primary sm:text-5xl lg:text-6xl">
        {solid}
      </span>
    </Tag>
  );
}
