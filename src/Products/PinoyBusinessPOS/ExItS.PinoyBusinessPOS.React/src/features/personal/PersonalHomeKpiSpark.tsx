import { useId } from "react";
import { personalHomeKpiSparkGeometry } from "@/features/personal/personal-home-kpi-sparkline";

export function PersonalHomeKpiSpark({
  values,
  testId,
}: {
  values: number[];
  testId?: string;
}) {
  const gradientId = useId().replace(/:/g, "");
  const geometry = personalHomeKpiSparkGeometry(values);

  if (!geometry.linePath) {
    return null;
  }

  return (
    <svg
      className="personal-home-kpi-card__spark"
      viewBox="0 0 160 40"
      aria-hidden
      preserveAspectRatio="none"
      data-testid={testId}
    >
      <defs>
        <linearGradient id={`personal-home-kpi-spark-${gradientId}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="currentColor" stopOpacity="0.35" />
          <stop offset="100%" stopColor="currentColor" stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={geometry.fillPath} fill={`url(#personal-home-kpi-spark-${gradientId})`} />
      <path
        d={geometry.linePath}
        fill="none"
        stroke="currentColor"
        strokeWidth="2.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}