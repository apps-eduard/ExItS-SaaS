import type { ElementType } from "react";

import { cn } from "@/lib/utils";

export function ExItsOutlineHeading({
  as: Tag = "h1",
  outline,
  solid,
  accentPhrase,
  className,
}: {
  as?: ElementType;
  outline: string;
  solid: string;
  /** When set, this trailing phrase (must appear at the end of `solid`) uses gradient text. */
  accentPhrase?: string;
  className?: string;
}) {
  const solidLead =
    accentPhrase && solid.endsWith(accentPhrase)
      ? solid.slice(0, solid.length - accentPhrase.length).trimEnd()
      : solid;

  return (
    <Tag
      className={cn(
        "max-w-4xl text-balance font-semibold tracking-tight",
        className,
      )}
    >
      <span className="block text-[2.5rem] leading-[1.05] text-transparent [-webkit-text-stroke:1.5px_#f5f3ff] sm:text-5xl lg:text-7xl">
        {outline}
      </span>
      <span className="mt-2 block text-[2.5rem] leading-[1.1] text-primary sm:text-5xl lg:text-6xl">
        {accentPhrase && solid.endsWith(accentPhrase) ? (
          <>
            {solidLead}{" "}
            <span className="exits-text-gradient">{accentPhrase}</span>
          </>
        ) : (
          solid
        )}
      </span>
    </Tag>
  );
}
