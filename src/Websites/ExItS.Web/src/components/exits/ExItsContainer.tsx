import type { ReactNode } from "react";

export function ExItsContainer({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={[
        "mx-auto w-full max-w-7xl px-6",
        "sm:px-8",
        "lg:px-10",
        className ?? "",
      ].join(" ")}
    >
      {children}
    </div>
  );
}

