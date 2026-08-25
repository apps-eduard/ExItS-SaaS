export type BusinessSetupPreset = {
  codeHints: string[];
  titleKey: string;
  blurbKey: string;
  bulletKeys: string[];
};

/** Display-only presets. Does not invent server commercial rules. */
export const BUSINESS_SETUP_PRESETS: BusinessSetupPreset[] = [
  {
    codeHints: ["sari-sari", "sarisari", "sari_sari"],
    titleKey: "onboarding.business.preset.sariSari.title",
    blurbKey: "onboarding.business.preset.sariSari.blurb",
    bulletKeys: [
      "onboarding.business.preset.bullet.fastSell",
      "onboarding.business.preset.bullet.retailUnits",
      "onboarding.business.preset.bullet.inventory",
      "onboarding.business.preset.bullet.utangIfPlan",
    ],
  },
  {
    codeHints: ["grocery", "mini-grocery", "mini_grocery"],
    titleKey: "onboarding.business.preset.grocery.title",
    blurbKey: "onboarding.business.preset.grocery.blurb",
    bulletKeys: [
      "onboarding.business.preset.bullet.fastSell",
      "onboarding.business.preset.bullet.retailUnits",
      "onboarding.business.preset.bullet.inventory",
      "onboarding.business.preset.bullet.utangIfPlan",
    ],
  },
  {
    codeHints: ["pharmacy", "drugstore"],
    titleKey: "onboarding.business.preset.pharmacy.title",
    blurbKey: "onboarding.business.preset.pharmacy.blurb",
    bulletKeys: [
      "onboarding.business.preset.bullet.fastSell",
      "onboarding.business.preset.bullet.inventory",
      "onboarding.business.preset.bullet.retailUnits",
    ],
  },
  {
    codeHints: ["restaurant", "food", "cafe"],
    titleKey: "onboarding.business.preset.restaurant.title",
    blurbKey: "onboarding.business.preset.restaurant.blurb",
    bulletKeys: [
      "onboarding.business.preset.bullet.fastSell",
      "onboarding.business.preset.bullet.inventory",
    ],
  },
  {
    codeHints: ["hardware"],
    titleKey: "onboarding.business.preset.hardware.title",
    blurbKey: "onboarding.business.preset.hardware.blurb",
    bulletKeys: [
      "onboarding.business.preset.bullet.fastSell",
      "onboarding.business.preset.bullet.retailUnits",
      "onboarding.business.preset.bullet.inventory",
    ],
  },
];

export const DEFAULT_BUSINESS_SETUP_PRESET: BusinessSetupPreset = {
  codeHints: [],
  titleKey: "onboarding.business.preset.general.title",
  blurbKey: "onboarding.business.preset.general.blurb",
  bulletKeys: [
    "onboarding.business.preset.bullet.fastSell",
    "onboarding.business.preset.bullet.retailUnits",
    "onboarding.business.preset.bullet.inventory",
    "onboarding.business.preset.bullet.utangIfPlan",
  ],
};

export function resolveBusinessSetupPreset(codeOrName: string | null | undefined): BusinessSetupPreset {
  const needle = (codeOrName ?? "").trim().toLowerCase();
  if (!needle) return DEFAULT_BUSINESS_SETUP_PRESET;
  for (const preset of BUSINESS_SETUP_PRESETS) {
    if (preset.codeHints.some((hint) => needle.includes(hint))) {
      return preset;
    }
  }
  return DEFAULT_BUSINESS_SETUP_PRESET;
}
