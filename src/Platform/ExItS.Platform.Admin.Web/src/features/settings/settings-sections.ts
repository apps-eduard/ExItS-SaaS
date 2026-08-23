import type { MessageKey } from "@/lib/i18n/messages";

export const PLATFORM_SETTINGS_BASE_PATH = "/admin/settings";

export type SettingsSectionId =
  | "general"
  | "email"
  | "security"
  | "integrations"
  | "feature-flags"
  | "regional"
  | "advanced";

/**
 * Platform Settings admin CRUD is not exposed by Platform API today.
 * SMTP / auth policy values live in deploy configuration only.
 * Keep sections truthful — no fabricated current values or save success.
 */
export type SettingsSectionDefinition = {
  id: SettingsSectionId;
  pathSegment: string;
  navLabelKey: MessageKey;
  titleKey: MessageKey;
  descriptionKey: MessageKey;
  /** Stable gap id for operators and the PA-SETTINGS report. */
  backendApiGap: string;
  gapBodyKey: MessageKey;
  ownershipKeys: readonly MessageKey[];
};

export const SETTINGS_SECTIONS: readonly SettingsSectionDefinition[] = [
  {
    id: "general",
    pathSegment: "general",
    navLabelKey: "settings.nav.general",
    titleKey: "settings.general.title",
    descriptionKey: "settings.general.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_GENERAL",
    gapBodyKey: "settings.general.gap",
    ownershipKeys: ["settings.ownership.organizationBranding"],
  },
  {
    id: "email",
    pathSegment: "email",
    navLabelKey: "settings.nav.email",
    titleKey: "settings.email.title",
    descriptionKey: "settings.email.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_EMAIL",
    gapBodyKey: "settings.email.gap",
    ownershipKeys: ["settings.ownership.secrets"],
  },
  {
    id: "security",
    pathSegment: "security",
    navLabelKey: "settings.nav.security",
    titleKey: "settings.security.title",
    descriptionKey: "settings.security.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_SECURITY",
    gapBodyKey: "settings.security.gap",
    ownershipKeys: ["settings.ownership.peopleAccess"],
  },
  {
    id: "integrations",
    pathSegment: "integrations",
    navLabelKey: "settings.nav.integrations",
    titleKey: "settings.integrations.title",
    descriptionKey: "settings.integrations.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_INTEGRATIONS",
    gapBodyKey: "settings.integrations.gap",
    ownershipKeys: ["settings.ownership.billing", "settings.ownership.operations"],
  },
  {
    id: "feature-flags",
    pathSegment: "feature-flags",
    navLabelKey: "settings.nav.featureFlags",
    titleKey: "settings.featureFlags.title",
    descriptionKey: "settings.featureFlags.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_FEATURE_FLAGS",
    gapBodyKey: "settings.featureFlags.gap",
    ownershipKeys: ["settings.ownership.productsCommercial"],
  },
  {
    id: "regional",
    pathSegment: "regional",
    navLabelKey: "settings.nav.regional",
    titleKey: "settings.regional.title",
    descriptionKey: "settings.regional.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_REGIONAL",
    gapBodyKey: "settings.regional.gap",
    ownershipKeys: ["settings.ownership.organizationRegional"],
  },
  {
    id: "advanced",
    pathSegment: "advanced",
    navLabelKey: "settings.nav.advanced",
    titleKey: "settings.advanced.title",
    descriptionKey: "settings.advanced.description",
    backendApiGap: "BACKEND_API_GAP:PLATFORM_SETTINGS_ADVANCED",
    gapBodyKey: "settings.advanced.gap",
    ownershipKeys: ["settings.ownership.governance"],
  },
] as const;

export function settingsSectionHref(section: SettingsSectionDefinition): string {
  return `${PLATFORM_SETTINGS_BASE_PATH}/${section.pathSegment}`;
}

export function findSettingsSection(pathSegment: string | undefined): SettingsSectionDefinition | null {
  if (!pathSegment) {
    return null;
  }
  return SETTINGS_SECTIONS.find((section) => section.pathSegment === pathSegment) ?? null;
}

export const SETTINGS_BACKEND_API_GAPS = SETTINGS_SECTIONS.map((section) => section.backendApiGap);
