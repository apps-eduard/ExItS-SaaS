import { DensityControl } from "@/components/exits/DensityControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { PrimaryColorControl } from "@/components/exits/PrimaryColorControl";
import { ControlShapeControl } from "@/components/exits/ControlShapeControl";
import { MotionControl } from "@/components/exits/MotionControl";
import { useI18n } from "@/i18n/I18nProvider";
import { PreferencesSectionPanel } from "@/features/preferences/PreferencesSectionPanel";

/**
 * Appearance — Theme, Primary color, Control shape, Density, Motion.
 * Compact layout with consistent group rhythm (no cramped dividers).
 */
export function AppearancePreferences() {
  const { t } = useI18n();

  return (
    <PreferencesSectionPanel
      title={t("preferences.section.appearance")}
      titleId="preferences-appearance-heading"
      testId="preferences-section-appearance"
      surface="plain"
    >
      <div className="flex flex-col gap-5 px-4 pb-1">
        <ThemeControl />
        <PrimaryColorControl />
        <ControlShapeControl />
        <DensityControl />
        <MotionControl />
      </div>
    </PreferencesSectionPanel>
  );
}
