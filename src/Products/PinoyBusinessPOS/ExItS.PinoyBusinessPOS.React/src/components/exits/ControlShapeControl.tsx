import { SettingsSelect } from "@/components/ui/settings-select";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import type { ControlShapePreference } from "@/lib/preferences/ui-preferences";

/** Lightweight CSS shape glyphs — lucide has no soft/pill pair that reads clearly. */
function ShapeIcon({ kind }: { kind: ControlShapePreference }) {
  if (kind === "standard") {
    return (
      <span
        className="box-border size-3.5 shrink-0 border-2 border-current rounded-[3px]"
        aria-hidden="true"
      />
    );
  }
  if (kind === "soft") {
    return (
      <span
        className="box-border size-3.5 shrink-0 border-2 border-current rounded-[5px]"
        aria-hidden="true"
      />
    );
  }
  return (
    <span
      className="box-border h-2.5 w-3.5 shrink-0 border-2 border-current rounded-full"
      aria-hidden="true"
    />
  );
}

export function ControlShapeControl() {
  const { t } = useI18n();
  const { preferences, setControlShape } = usePreferences();

  return (
    <SettingsSelect<ControlShapePreference>
      label={t("appearance.controlShape.label")}
      value={preferences.controlShape}
      onChange={setControlShape}
      variant="segmented"
      testId="preferences-control-shape"
      options={[
        {
          value: "standard",
          label: t("appearance.controlShape.standard"),
          icon: <ShapeIcon kind="standard" />,
        },
        {
          value: "soft",
          label: t("appearance.controlShape.soft"),
          icon: <ShapeIcon kind="soft" />,
        },
        {
          value: "pill",
          label: t("appearance.controlShape.pill"),
          icon: <ShapeIcon kind="pill" />,
        },
      ]}
    />
  );
}
