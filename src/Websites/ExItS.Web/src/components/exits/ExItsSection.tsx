import type { ReactNode } from "react";

export function ExItsSection({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <section className={["py-16", className ?? ""].join(" ").trim()}>
      {children}
    </section>
  );
}

