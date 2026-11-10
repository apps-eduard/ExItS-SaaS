import { useEffect, useState } from "react";

export const PICKER_COMPACT_QUERY = "(max-width: 767px)";

export function useViewport(viewport: "auto" | "desktop" | "mobile" = "auto"): "desktop" | "mobile" {
  const [compact, setCompact] = useState(() => {
    if (viewport !== "auto") return viewport === "mobile";
    return window.matchMedia?.(PICKER_COMPACT_QUERY).matches ?? false;
  });

  useEffect(() => {
    if (viewport !== "auto") return;
    const query = window.matchMedia?.(PICKER_COMPACT_QUERY);
    if (!query) return;
    const onChange = () => setCompact(query.matches);
    onChange();
    query.addEventListener("change", onChange);
    return () => query.removeEventListener("change", onChange);
  }, [viewport]);

  if (viewport === "mobile") return "mobile";
  if (viewport === "desktop") return "desktop";
  return compact ? "mobile" : "desktop";
}
