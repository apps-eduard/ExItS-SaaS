import { cn } from "@/lib/cn";

/** Shared ExItS branded loader mark — ring + core pulse. */
export function ExitsLoaderMark({
  className,
  size = "md",
}: {
  className?: string;
  size?: "sm" | "md" | "lg";
}) {
  return (
    <div
      className={cn(
        "exits-loader-mark",
        size === "sm" && "exits-loader-mark--sm",
        size === "lg" && "exits-loader-mark--lg",
        className,
      )}
      aria-hidden="true"
      data-testid="exits-loader-mark"
    >
      <div className="exits-loader-mark__ring" />
      <div className="exits-loader-mark__core" />
    </div>
  );
}
