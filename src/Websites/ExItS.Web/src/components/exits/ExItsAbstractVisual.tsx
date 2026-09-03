import { cn } from "@/lib/utils";

export type AbstractVisualVariant =
  | "dashboard"
  | "selling"
  | "inventory"
  | "customers"
  | "suppliers"
  | "network"
  | "reports";

const variantLabel: Record<AbstractVisualVariant, string> = {
  dashboard: "Operations geometry",
  selling: "Sales flow",
  inventory: "Catalog lattice",
  customers: "Customer graph",
  suppliers: "Supply links",
  network: "Branch network",
  reports: "Signal bars",
};

/**
 * Intentional abstract product composition — not a fake screenshot.
 * WEB-D-06: real captures are not available yet.
 */
export function ExItsAbstractVisual({
  variant = "dashboard",
  title,
  className,
  floating = true,
}: {
  variant?: AbstractVisualVariant;
  title?: string;
  className?: string;
  floating?: boolean;
}) {
  return (
    <figure
      className={cn(
        "exits-gradient-border exits-light-sweep relative",
        floating && "exits-float",
        className,
      )}
      data-featured="true"
      data-animated="true"
      aria-label={title ? `${title} — abstract product visual` : "Abstract ExItS product visual"}
    >
      <div className="exits-gradient-border__inner overflow-hidden shadow-glow">
        <div className="pointer-events-none absolute inset-0" aria-hidden="true">
          <div className="absolute -right-8 -top-10 h-44 w-44 rounded-full bg-magenta/30 blur-3xl" />
          <div className="absolute -bottom-10 -left-8 h-40 w-40 rounded-full bg-secondary/25 blur-3xl" />
          <div className="absolute left-1/2 top-1/3 h-32 w-32 -translate-x-1/2 rounded-full bg-brand/25 blur-3xl" />
          <div className="exits-ambient__grid opacity-50" />
        </div>

        <div className="relative aspect-[4/3] w-full p-5 sm:p-6" aria-hidden="true">
          <div className="mb-4 flex items-center justify-between">
            <span className="text-[11px] font-semibold uppercase tracking-[0.2em] text-brandBright">
              {title ?? variantLabel[variant]}
            </span>
            <span className="exits-glow-breathe inline-flex h-2.5 w-2.5 rounded-full bg-secondary shadow-[0_0_14px_rgba(34,211,238,0.9)]" />
          </div>

          {variant === "selling" ? <SellingComposition /> : null}
          {variant === "inventory" ? <InventoryComposition /> : null}
          {variant === "customers" ? <CustomersComposition /> : null}
          {variant === "suppliers" || variant === "network" ? <NetworkComposition /> : null}
          {variant === "reports" ? <ReportsComposition /> : null}
          {variant === "dashboard" ? <DashboardComposition /> : null}
        </div>
      </div>
    </figure>
  );
}

function DashboardComposition() {
  return (
    <div className="grid h-[calc(100%-1.5rem)] grid-cols-12 gap-3">
      <div className="col-span-7 rounded-2xl border border-brand/30 bg-base/60 p-3 backdrop-blur-sm">
        <div className="mb-3 h-2 w-1/3 rounded-full bg-gradient-to-r from-brand to-magenta" />
        <svg className="h-[72%] w-full" viewBox="0 0 240 100" fill="none" aria-hidden="true">
          <path
            d="M4 72 C 28 70, 36 40, 58 48 S 96 88, 122 58 S 170 20, 198 36 S 228 70, 236 64"
            stroke="url(#dashWave)"
            strokeWidth="3"
            strokeLinecap="round"
            className="[stroke-dasharray:8_6]"
            style={{ animation: "exits-pulse-line 3.5s linear infinite" }}
          />
          <defs>
            <linearGradient id="dashWave" x1="0" y1="0" x2="240" y2="0">
              <stop stopColor="#6366f1" />
              <stop offset="0.5" stopColor="#e879f9" />
              <stop offset="1" stopColor="#22d3ee" />
            </linearGradient>
          </defs>
        </svg>
      </div>
      <div className="col-span-5 flex flex-col gap-3">
        <div className="flex-1 rounded-2xl border border-magenta/25 bg-raised/50 p-3">
          <div className="h-2 w-2/3 rounded-full bg-magenta/60" />
          <div className="mt-3 flex items-end gap-1.5">
            {[45, 70, 55, 88, 62].map((h, i) => (
              <div
                key={i}
                className="flex-1 rounded-t-md bg-gradient-to-t from-brand/40 to-secondary/80"
                style={{ height: `${h}%` }}
              />
            ))}
          </div>
        </div>
        <div className="rounded-2xl border border-secondary/25 bg-elevated/70 p-3">
          <div className="flex items-center justify-between gap-2">
            {["Inv", "Ord", "POS"].map((label) => (
              <div
                key={label}
                className="flex-1 rounded-xl bg-gradient-to-br from-brand/25 to-magenta/15 px-2 py-3 text-center text-[10px] font-semibold uppercase tracking-wide text-primary/80"
              >
                {label}
              </div>
            ))}
          </div>
        </div>
      </div>
      <div className="col-span-12 mt-1 flex items-center justify-center gap-3 py-1">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="flex items-center gap-3">
            <span
              className={cn(
                "h-3 w-3 rounded-full",
                i === 0 ? "bg-secondary shadow-[0_0_12px_rgba(34,211,238,0.8)]" : "bg-brand/70",
              )}
            />
            {i < 3 ? <span className="h-px w-8 bg-gradient-to-r from-brand to-magenta/60" /> : null}
          </div>
        ))}
      </div>
    </div>
  );
}

function SellingComposition() {
  return (
    <div className="relative flex h-[calc(100%-1.5rem)] flex-col gap-3">
      <div className="flex flex-1 gap-3">
        <div className="w-[36%] rounded-2xl border border-borderDefault bg-base/60 p-3">
          <div className="space-y-2">
            {[1, 2, 3, 4].map((n) => (
              <div
                key={n}
                className="h-8 rounded-xl border border-brand/20 bg-gradient-to-r from-brand/15 to-transparent"
              />
            ))}
          </div>
        </div>
        <div className="relative flex-1 overflow-hidden rounded-2xl border border-magenta/30 bg-base/70 p-4">
          <svg className="h-full w-full" viewBox="0 0 240 120" fill="none" aria-hidden="true">
            <path
              d="M8 88 C 40 80, 52 40, 84 48 S 140 96, 172 68 S 220 28, 232 36"
              stroke="url(#sellStroke)"
              strokeWidth="3.5"
              strokeLinecap="round"
            />
            <circle cx="172" cy="68" r="5" fill="#22d3ee" className="exits-glow-breathe" />
            <defs>
              <linearGradient id="sellStroke" x1="0" y1="0" x2="240" y2="0">
                <stop stopColor="#8b5cf6" />
                <stop offset="0.55" stopColor="#e879f9" />
                <stop offset="1" stopColor="#22d3ee" />
              </linearGradient>
            </defs>
          </svg>
        </div>
      </div>
      <div className="h-12 rounded-2xl border border-borderDefault bg-raised/60 px-4 py-3">
        <div className="h-full w-2/5 rounded-pill bg-exits-cta" />
      </div>
    </div>
  );
}

function InventoryComposition() {
  return (
    <div className="grid h-[calc(100%-1.5rem)] grid-cols-3 gap-3">
      {Array.from({ length: 9 }).map((_, i) => (
        <div
          key={i}
          className={cn(
            "rounded-2xl border p-3",
            i % 3 === 0
              ? "border-brand/40 bg-brand/15"
              : i % 3 === 1
                ? "border-magenta/30 bg-magenta/10"
                : "border-secondary/25 bg-secondary/10",
          )}
        >
          <div className="h-2 w-1/2 rounded-full bg-white/25" />
          <div className="mt-3 h-8 rounded-xl bg-white/5" />
        </div>
      ))}
    </div>
  );
}

function CustomersComposition() {
  return (
    <div className="relative flex h-[calc(100%-1.5rem)] items-center justify-center">
      <div className="absolute h-40 w-40 rounded-full border border-brand/30" />
      <div className="absolute h-28 w-28 rounded-full border border-magenta/30" />
      <div className="z-10 grid grid-cols-3 gap-4">
        {[0, 1, 2, 3, 4, 5].map((i) => (
          <div
            key={i}
            className={cn(
              "h-10 w-10 rounded-full border",
              i === 2
                ? "border-secondary bg-brand/50 shadow-[0_0_20px_rgba(34,211,238,0.45)]"
                : "border-borderDefault bg-elevated",
            )}
          />
        ))}
      </div>
      <svg className="pointer-events-none absolute inset-6" viewBox="0 0 200 160" aria-hidden="true">
        <path d="M40 40 L100 80 L160 40" stroke="rgba(139,92,246,0.45)" strokeWidth="1.5" fill="none" />
        <path d="M40 120 L100 80 L160 120" stroke="rgba(34,211,238,0.35)" strokeWidth="1.5" fill="none" />
      </svg>
    </div>
  );
}

function NetworkComposition() {
  return (
    <div className="relative flex h-[calc(100%-1.5rem)] items-center justify-center">
      <svg className="h-full w-full" viewBox="0 0 280 180" aria-hidden="true">
        <path d="M50 90 H230" stroke="rgba(139,92,246,0.4)" strokeWidth="1.5" />
        <path d="M140 30 V150" stroke="rgba(34,211,238,0.3)" strokeWidth="1.5" />
        <path d="M70 50 L210 130" stroke="rgba(232,121,249,0.28)" strokeWidth="1.5" />
        <path d="M70 130 L210 50" stroke="rgba(99,102,241,0.28)" strokeWidth="1.5" />
        {[
          [50, 90],
          [140, 30],
          [230, 90],
          [140, 150],
          [90, 60],
          [190, 120],
        ].map(([cx, cy], i) => (
          <circle
            key={i}
            cx={cx}
            cy={cy}
            r={i === 0 ? 11 : 7}
            fill={i % 2 === 0 ? "rgba(139,92,246,0.7)" : "rgba(34,211,238,0.55)"}
          />
        ))}
      </svg>
    </div>
  );
}

function ReportsComposition() {
  return (
    <div className="flex h-[calc(100%-1.5rem)] items-end gap-3 pb-2">
      {[35, 55, 42, 78, 60, 88, 50, 72].map((h, i) => (
        <div key={i} className="flex flex-1 flex-col justify-end gap-2">
          <div
            className="rounded-t-xl bg-gradient-to-t from-brand/30 via-magenta/55 to-secondary/80"
            style={{ height: `${h}%` }}
          />
        </div>
      ))}
    </div>
  );
}
