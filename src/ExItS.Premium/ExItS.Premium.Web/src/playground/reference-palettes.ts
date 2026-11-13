/** Reference-only color library. Not wired to app tokens or Primary themes. */

export const REFERENCE_SHADE_STEPS = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900] as const;

export type ReferenceShadeStep = (typeof REFERENCE_SHADE_STEPS)[number];

export type ReferenceFamilyId =
  | "slate"
  | "gray"
  | "zinc"
  | "neutral"
  | "stone"
  | "red"
  | "rose"
  | "pink"
  | "fuchsia"
  | "purple"
  | "violet"
  | "indigo"
  | "blue"
  | "sky"
  | "cyan"
  | "teal"
  | "emerald"
  | "green"
  | "lime"
  | "yellow"
  | "amber"
  | "orange"
  | "brown"
  | "olive"
  | "mint"
  | "charcoal"
  | "ivory"
  | "coral"
  | "peach"
  | "terracotta"
  | "burgundy"
  | "gold"
  | "sage"
  | "forest"
  | "turquoise"
  | "azure"
  | "navy"
  | "lavender"
  | "periwinkle"
  | "magenta";

export type ReferenceFamily = {
  id: ReferenceFamilyId;
  label: string;
  shades: Record<ReferenceShadeStep, string>;
};

/** Curated HEX scales for visual reference only. */
export const REFERENCE_PALETTES: readonly ReferenceFamily[] = [
  {
    id: "slate",
    label: "Slate",
    shades: {
      50: "#f8fafc",
      100: "#f1f5f9",
      200: "#e2e8f0",
      300: "#cbd5e1",
      400: "#94a3b8",
      500: "#64748b",
      600: "#475569",
      700: "#334155",
      800: "#1e293b",
      900: "#0f172a",
    },
  },
  {
    id: "gray",
    label: "Gray",
    shades: {
      50: "#f9fafb",
      100: "#f3f4f6",
      200: "#e5e7eb",
      300: "#d1d5db",
      400: "#9ca3af",
      500: "#6b7280",
      600: "#4b5563",
      700: "#374151",
      800: "#1f2937",
      900: "#111827",
    },
  },
  {
    id: "zinc",
    label: "Zinc",
    shades: {
      50: "#fafafa",
      100: "#f4f4f5",
      200: "#e4e4e7",
      300: "#d4d4d8",
      400: "#a1a1aa",
      500: "#71717a",
      600: "#52525b",
      700: "#3f3f46",
      800: "#27272a",
      900: "#18181b",
    },
  },
  {
    id: "neutral",
    label: "Neutral",
    shades: {
      50: "#fafafa",
      100: "#f5f5f5",
      200: "#e5e5e5",
      300: "#d4d4d4",
      400: "#a3a3a3",
      500: "#737373",
      600: "#525252",
      700: "#404040",
      800: "#262626",
      900: "#171717",
    },
  },
  {
    id: "stone",
    label: "Stone",
    shades: {
      50: "#fafaf9",
      100: "#f5f5f4",
      200: "#e7e5e4",
      300: "#d6d3d1",
      400: "#a8a29e",
      500: "#78716c",
      600: "#57534e",
      700: "#44403c",
      800: "#292524",
      900: "#1c1917",
    },
  },
  {
    id: "red",
    label: "Red",
    shades: {
      50: "#fef2f2",
      100: "#fee2e2",
      200: "#fecaca",
      300: "#fca5a5",
      400: "#f87171",
      500: "#ef4444",
      600: "#dc2626",
      700: "#b91c1c",
      800: "#991b1b",
      900: "#7f1d1d",
    },
  },
  {
    id: "rose",
    label: "Rose",
    shades: {
      50: "#fff1f2",
      100: "#ffe4e6",
      200: "#fecdd3",
      300: "#fda4af",
      400: "#fb7185",
      500: "#f43f5e",
      600: "#e11d48",
      700: "#be123c",
      800: "#9f1239",
      900: "#881337",
    },
  },
  {
    id: "pink",
    label: "Pink",
    shades: {
      50: "#fdf2f8",
      100: "#fce7f3",
      200: "#fbcfe8",
      300: "#f9a8d4",
      400: "#f472b6",
      500: "#ec4899",
      600: "#db2777",
      700: "#be185d",
      800: "#9d174d",
      900: "#831843",
    },
  },
  {
    id: "fuchsia",
    label: "Fuchsia",
    shades: {
      50: "#fdf4ff",
      100: "#fae8ff",
      200: "#f5d0fe",
      300: "#f0abfc",
      400: "#e879f9",
      500: "#d946ef",
      600: "#c026d3",
      700: "#a21caf",
      800: "#86198f",
      900: "#701a75",
    },
  },
  {
    id: "purple",
    label: "Purple",
    shades: {
      50: "#faf5ff",
      100: "#f3e8ff",
      200: "#e9d5ff",
      300: "#d8b4fe",
      400: "#c084fc",
      500: "#a855f7",
      600: "#9333ea",
      700: "#7e22ce",
      800: "#6b21a8",
      900: "#581c87",
    },
  },
  {
    id: "violet",
    label: "Violet",
    shades: {
      50: "#f5f3ff",
      100: "#ede9fe",
      200: "#ddd6fe",
      300: "#c4b5fd",
      400: "#a78bfa",
      500: "#8b5cf6",
      600: "#7c3aed",
      700: "#6d28d9",
      800: "#5b21b6",
      900: "#4c1d95",
    },
  },
  {
    id: "indigo",
    label: "Indigo",
    shades: {
      50: "#eef2ff",
      100: "#e0e7ff",
      200: "#c7d2fe",
      300: "#a5b4fc",
      400: "#818cf8",
      500: "#6366f1",
      600: "#4f46e5",
      700: "#4338ca",
      800: "#3730a3",
      900: "#312e81",
    },
  },
  {
    id: "blue",
    label: "Blue",
    shades: {
      50: "#eff6ff",
      100: "#dbeafe",
      200: "#bfdbfe",
      300: "#93c5fd",
      400: "#60a5fa",
      500: "#3b82f6",
      600: "#2563eb",
      700: "#1d4ed8",
      800: "#1e40af",
      900: "#1e3a8a",
    },
  },
  {
    id: "sky",
    label: "Sky",
    shades: {
      50: "#f0f9ff",
      100: "#e0f2fe",
      200: "#bae6fd",
      300: "#7dd3fc",
      400: "#38bdf8",
      500: "#0ea5e9",
      600: "#0284c7",
      700: "#0369a1",
      800: "#075985",
      900: "#0c4a6e",
    },
  },
  {
    id: "cyan",
    label: "Cyan",
    shades: {
      50: "#ecfeff",
      100: "#cffafe",
      200: "#a5f3fc",
      300: "#67e8f9",
      400: "#22d3ee",
      500: "#06b6d4",
      600: "#0891b2",
      700: "#0e7490",
      800: "#155e75",
      900: "#164e63",
    },
  },
  {
    id: "teal",
    label: "Teal",
    shades: {
      50: "#f0fdfa",
      100: "#ccfbf1",
      200: "#99f6e4",
      300: "#5eead4",
      400: "#2dd4bf",
      500: "#14b8a6",
      600: "#0d9488",
      700: "#0f766e",
      800: "#115e59",
      900: "#134e4a",
    },
  },
  {
    id: "emerald",
    label: "Emerald",
    shades: {
      50: "#ecfdf5",
      100: "#d1fae5",
      200: "#a7f3d0",
      300: "#6ee7b7",
      400: "#34d399",
      500: "#10b981",
      600: "#059669",
      700: "#047857",
      800: "#065f46",
      900: "#064e3b",
    },
  },
  {
    id: "green",
    label: "Green",
    shades: {
      50: "#f0fdf4",
      100: "#dcfce7",
      200: "#bbf7d0",
      300: "#86efac",
      400: "#4ade80",
      500: "#22c55e",
      600: "#16a34a",
      700: "#15803d",
      800: "#166534",
      900: "#14532d",
    },
  },
  {
    id: "lime",
    label: "Lime",
    shades: {
      50: "#f7fee7",
      100: "#ecfccb",
      200: "#d9f99d",
      300: "#bef264",
      400: "#a3e635",
      500: "#84cc16",
      600: "#65a30d",
      700: "#4d7c0f",
      800: "#3f6212",
      900: "#365314",
    },
  },
  {
    id: "yellow",
    label: "Yellow",
    shades: {
      50: "#fefce8",
      100: "#fef9c3",
      200: "#fef08a",
      300: "#fde047",
      400: "#facc15",
      500: "#eab308",
      600: "#ca8a04",
      700: "#a16207",
      800: "#854d0e",
      900: "#713f12",
    },
  },
  {
    id: "amber",
    label: "Amber",
    shades: {
      50: "#fffbeb",
      100: "#fef3c7",
      200: "#fde68a",
      300: "#fcd34d",
      400: "#fbbf24",
      500: "#f59e0b",
      600: "#d97706",
      700: "#b45309",
      800: "#92400e",
      900: "#78350f",
    },
  },
  {
    id: "orange",
    label: "Orange",
    shades: {
      50: "#fff7ed",
      100: "#ffedd5",
      200: "#fed7aa",
      300: "#fdba74",
      400: "#fb923c",
      500: "#f97316",
      600: "#ea580c",
      700: "#c2410c",
      800: "#9a3412",
      900: "#7c2d12",
    },
  },
  {
    id: "brown",
    label: "Brown",
    shades: {
      50: "#faf6f3",
      100: "#f0e6de",
      200: "#e0cbbd",
      300: "#c9a690",
      400: "#b07d5c",
      500: "#8f5a3c",
      600: "#75482f",
      700: "#5c3926",
      800: "#43291c",
      900: "#2d1b13",
    },
  },
  {
    id: "olive",
    label: "Olive",
    shades: {
      50: "#f7f8f0",
      100: "#eef0dc",
      200: "#d9deb0",
      300: "#c0c882",
      400: "#a3ad58",
      500: "#828a3d",
      600: "#676e31",
      700: "#505627",
      800: "#3b4020",
      900: "#282c16",
    },
  },
  {
    id: "mint",
    label: "Mint",
    shades: {
      50: "#f2fbf7",
      100: "#d9f5e9",
      200: "#b4ead3",
      300: "#84d9b6",
      400: "#4fc396",
      500: "#2aa978",
      600: "#1f8a62",
      700: "#1a6e50",
      800: "#175641",
      900: "#123a2d",
    },
  },
  {
    id: "charcoal",
    label: "Charcoal",
    shades: {
      50: "#f4f5f6",
      100: "#e4e6e9",
      200: "#c8ccd2",
      300: "#a3aab4",
      400: "#7a8491",
      500: "#5b6572",
      600: "#464f5b",
      700: "#363d47",
      800: "#252a31",
      900: "#16191e",
    },
  },
  {
    id: "ivory",
    label: "Ivory",
    shades: {
      50: "#fffcf5",
      100: "#faf5e8",
      200: "#f3ead0",
      300: "#e8d9b0",
      400: "#d8c28a",
      500: "#c4a86a",
      600: "#a8894f",
      700: "#876b3d",
      800: "#65502f",
      900: "#433520",
    },
  },
  {
    id: "coral",
    label: "Coral",
    shades: {
      50: "#fff5f3",
      100: "#ffe6e1",
      200: "#ffc9bf",
      300: "#ffa394",
      400: "#ff7a66",
      500: "#f2553d",
      600: "#d63c26",
      700: "#b12f1e",
      800: "#8a2619",
      900: "#5e1a11",
    },
  },
  {
    id: "peach",
    label: "Peach",
    shades: {
      50: "#fff7f2",
      100: "#ffeadc",
      200: "#ffd2b5",
      300: "#ffb388",
      400: "#ff935c",
      500: "#f2753a",
      600: "#d45a24",
      700: "#ae461c",
      800: "#853616",
      900: "#58240f",
    },
  },
  {
    id: "terracotta",
    label: "Terracotta",
    shades: {
      50: "#fbf4f0",
      100: "#f4e2d8",
      200: "#e7c3b0",
      300: "#d59e82",
      400: "#c07a58",
      500: "#a85c3b",
      600: "#8c482e",
      700: "#6f3925",
      800: "#532b1c",
      900: "#371c13",
    },
  },
  {
    id: "burgundy",
    label: "Burgundy",
    shades: {
      50: "#faf3f5",
      100: "#f1dfe5",
      200: "#e0b9c5",
      300: "#c88a9d",
      400: "#ad5f78",
      500: "#8f3f5b",
      600: "#75324a",
      700: "#5c273b",
      800: "#431c2b",
      900: "#2c121c",
    },
  },
  {
    id: "gold",
    label: "Gold",
    shades: {
      50: "#fffbeb",
      100: "#fef3c7",
      200: "#fde68a",
      300: "#fcd34d",
      400: "#f0b429",
      500: "#d97706",
      600: "#b45309",
      700: "#92400e",
      800: "#78350f",
      900: "#5a280b",
    },
  },
  {
    id: "sage",
    label: "Sage",
    shades: {
      50: "#f4f7f4",
      100: "#e4ebe4",
      200: "#c6d5c7",
      300: "#a3bba5",
      400: "#7f9c84",
      500: "#628068",
      600: "#4e6753",
      700: "#3e5243",
      800: "#2e3c32",
      900: "#1e2822",
    },
  },
  {
    id: "forest",
    label: "Forest",
    shades: {
      50: "#f1f7f2",
      100: "#dcecde",
      200: "#b5d6ba",
      300: "#86b990",
      400: "#569c68",
      500: "#357d49",
      600: "#28643a",
      700: "#1f4f2f",
      800: "#173b23",
      900: "#0f2718",
    },
  },
  {
    id: "turquoise",
    label: "Turquoise",
    shades: {
      50: "#f0fdff",
      100: "#ccfbfb",
      200: "#99f3f3",
      300: "#5ee6e6",
      400: "#2bd4d4",
      500: "#14b8b8",
      600: "#0d9494",
      700: "#0f7676",
      800: "#115e5e",
      900: "#134e4e",
    },
  },
  {
    id: "azure",
    label: "Azure",
    shades: {
      50: "#f0f7ff",
      100: "#dcecff",
      200: "#b6d6ff",
      300: "#85b8ff",
      400: "#4f95ff",
      500: "#2674f0",
      600: "#1859c9",
      700: "#1446a0",
      800: "#123578",
      900: "#0e2350",
    },
  },
  {
    id: "navy",
    label: "Navy",
    shades: {
      50: "#f2f5fb",
      100: "#e0e7f4",
      200: "#c0cde6",
      300: "#96aad2",
      400: "#6a86bb",
      500: "#4a68a0",
      600: "#385284",
      700: "#2c4169",
      800: "#1f2e4c",
      900: "#141e33",
    },
  },
  {
    id: "lavender",
    label: "Lavender",
    shades: {
      50: "#f7f5ff",
      100: "#ece8ff",
      200: "#d7d0ff",
      300: "#b9adff",
      400: "#9784f5",
      500: "#7a63e0",
      600: "#634dbf",
      700: "#4f3d99",
      800: "#3b2e73",
      900: "#281f4d",
    },
  },
  {
    id: "periwinkle",
    label: "Periwinkle",
    shades: {
      50: "#f4f6ff",
      100: "#e5eaff",
      200: "#c9d3ff",
      300: "#a7b6ff",
      400: "#8394f5",
      500: "#6777e0",
      600: "#515fc0",
      700: "#404c99",
      800: "#2f3872",
      900: "#1f254c",
    },
  },
  {
    id: "magenta",
    label: "Magenta",
    shades: {
      50: "#fdf2f9",
      100: "#fce7f4",
      200: "#f9c7e6",
      300: "#f39ad2",
      400: "#e86bb8",
      500: "#d946a6",
      600: "#b92d88",
      700: "#95236d",
      800: "#711b53",
      900: "#4c1238",
    },
  },
] as const;

export function referenceShadeName(familyId: string, step: ReferenceShadeStep) {
  return `${familyId}-${step}`;
}

export function referenceShadeLabel(familyLabel: string, step: ReferenceShadeStep) {
  return `${familyLabel} ${step}`;
}

export type FilteredReferenceFamily = ReferenceFamily & {
  matchingSteps: ReferenceShadeStep[];
};

export function filterReferencePalettes(
  query: string,
  families: readonly ReferenceFamily[] = REFERENCE_PALETTES,
): FilteredReferenceFamily[] {
  const raw = query.trim().toLowerCase();
  const needle = raw.replace(/^#/, "");

  return families
    .map((family) => {
      const matchingSteps = needle
        ? REFERENCE_SHADE_STEPS.filter((step) => {
            const name = referenceShadeName(family.id, step);
            const display = referenceShadeLabel(family.label, step).toLowerCase();
            const hex = family.shades[step].toLowerCase();
            const hexBare = hex.replace(/^#/, "");
            return (
              family.id.includes(needle) ||
              family.label.toLowerCase().includes(needle) ||
              name.includes(needle) ||
              display.includes(needle) ||
              String(step).includes(needle) ||
              hex.includes(raw) ||
              hexBare.includes(needle)
            );
          })
        : [...REFERENCE_SHADE_STEPS];
      if (matchingSteps.length === 0) return null;
      return {
        ...family,
        shades: Object.fromEntries(matchingSteps.map((step) => [step, family.shades[step]])) as ReferenceFamily["shades"],
        matchingSteps,
      };
    })
    .filter((family): family is FilteredReferenceFamily => family !== null);
}

export function countReferenceSwatches(families: readonly ReferenceFamily[] = REFERENCE_PALETTES) {
  return families.reduce((total, family) => total + Object.keys(family.shades).length, 0);
}
