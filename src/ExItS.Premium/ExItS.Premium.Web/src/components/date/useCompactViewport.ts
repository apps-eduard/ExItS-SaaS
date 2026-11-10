import { useEffect, useState } from "react";

export const COMPACT_VIEWPORT_QUERY = "(max-width: 767px)";

function matchesCompact() {
  if (typeof window === "undefined" || typeof window.matchMedia !== "function") return false;
  return window.matchMedia(COMPACT_VIEWPORT_QUERY).matches;
}

export function useCompactViewport() {
  const [compact, setCompact] = useState(matchesCompact);

  useEffect(() => {
    if (typeof window.matchMedia !== "function") return;
    const media = window.matchMedia(COMPACT_VIEWPORT_QUERY);
    const update = () => setCompact(media.matches);
    update();
    if (typeof media.addEventListener !== "function") return;
    media.addEventListener("change", update);
    return () => media.removeEventListener("change", update);
  }, []);

  return compact;
}
