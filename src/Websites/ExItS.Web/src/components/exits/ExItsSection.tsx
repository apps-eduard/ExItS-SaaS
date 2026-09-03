import type { ReactNode } from "react";

export function ExItsSection({
  children,
  className,
  id,
}: {
  children: ReactNode;
  className?: string;
  id?: string;
}) {
  return (
    <section id={id} className={["py-16", className ?? ""].join(" ").trim()}>
      {children}
    </section>
  );
}

