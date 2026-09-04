import { useEffect, useRef, useState } from "react";
import { formatPeso } from "@/lib/format-money";
import { prefersReducedMotion } from "@/lib/motion";
import { cn } from "@/lib/cn";
import { KPI_COUNT_MS } from "@/features/reports/dashboard/chart-theme";

function easeOutCubic(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

/**
 * Count-up for money KPIs. Animates only when `animationKey` changes (first load / filter).
 * Skips animation under prefers-reduced-motion and in Vitest (jsdom).
 */
export function AnimatedMoneyValue({
  amount,
  animationKey,
  className,
  testId,
}: {
  amount: number;
  animationKey: string;
  className?: string;
  testId?: string;
}) {
  const reduced =
    prefersReducedMotion() ||
    (typeof import.meta !== "undefined" && import.meta.env?.MODE === "test");
  const [display, setDisplay] = useState(reduced ? amount : 0);
  const fromRef = useRef(0);
  const rafRef = useRef<number | null>(null);

  useEffect(() => {
    if (reduced) {
      setDisplay(amount);
      return;
    }

    const from = fromRef.current;
    const to = amount;
    const started = performance.now();
    const duration = KPI_COUNT_MS;

    const tick = (now: number) => {
      const t = Math.min(1, (now - started) / duration);
      const value = from + (to - from) * easeOutCubic(t);
      setDisplay(value);
      if (t < 1) {
        rafRef.current = requestAnimationFrame(tick);
      } else {
        fromRef.current = to;
      }
    };

    fromRef.current = from;
    rafRef.current = requestAnimationFrame(tick);
    return () => {
      if (rafRef.current != null) {
        cancelAnimationFrame(rafRef.current);
      }
      fromRef.current = to;
    };
  }, [amount, animationKey, reduced]);

  return (
    <span
      data-testid={testId}
      className={cn("tabular-nums font-semibold", className)}
    >
      {formatPeso(display)}
    </span>
  );
}

export function AnimatedIntegerValue({
  value,
  animationKey,
  className,
  testId,
}: {
  value: number;
  animationKey: string;
  className?: string;
  testId?: string;
}) {
  const reduced =
    prefersReducedMotion() ||
    (typeof import.meta !== "undefined" && import.meta.env?.MODE === "test");
  const [display, setDisplay] = useState(reduced ? value : 0);
  const fromRef = useRef(0);
  const rafRef = useRef<number | null>(null);

  useEffect(() => {
    if (reduced) {
      setDisplay(value);
      return;
    }
    const from = fromRef.current;
    const to = value;
    const started = performance.now();
    const duration = KPI_COUNT_MS;

    const tick = (now: number) => {
      const t = Math.min(1, (now - started) / duration);
      setDisplay(Math.round(from + (to - from) * easeOutCubic(t)));
      if (t < 1) {
        rafRef.current = requestAnimationFrame(tick);
      } else {
        fromRef.current = to;
      }
    };

    rafRef.current = requestAnimationFrame(tick);
    return () => {
      if (rafRef.current != null) {
        cancelAnimationFrame(rafRef.current);
      }
      fromRef.current = to;
    };
  }, [value, animationKey, reduced]);

  return (
    <span data-testid={testId} className={cn("tabular-nums font-semibold", className)}>
      {display.toLocaleString("en-PH")}
    </span>
  );
}
