/** Shared playground page-background options (Button glass + Input glass). DEV / UI Standards only. */

export type OrgEhgPlaygroundBgOption =
  | "soft-ambient"
  | "subtle-mesh"
  | "soft-aurora"
  | "neutral-solid"
  | "soft-vignette"
  | "horizon-wash"
  | "spotlight-glow";

export const ORG_EHG_PLAYGROUND_BG_OPTIONS: ReadonlyArray<{
  label: string;
  value: OrgEhgPlaygroundBgOption;
}> = [
  { label: "1. Soft Ambient Gradient (recommended)", value: "soft-ambient" },
  { label: "2. Subtle Mesh Gradient", value: "subtle-mesh" },
  { label: "3. Soft Aurora Background", value: "soft-aurora" },
  { label: "4. Neutral Solid Surface", value: "neutral-solid" },
  { label: "5. Soft Vignette", value: "soft-vignette" },
  { label: "6. Horizon Wash", value: "horizon-wash" },
  { label: "7. Spotlight Glow", value: "spotlight-glow" },
];

/** Default intensity so glass reads over decorative layers in the preview. */
export const ORG_EHG_PLAYGROUND_BG_DEFAULT_INTENSITY = 0.45;

export const ORG_EHG_PLAYGROUND_BG_DEFAULT: OrgEhgPlaygroundBgOption = "soft-ambient";
