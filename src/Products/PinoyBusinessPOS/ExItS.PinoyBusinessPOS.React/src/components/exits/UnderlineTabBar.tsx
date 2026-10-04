import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { usePreferences } from "@/hooks/usePreferences";

export type UnderlineTabItem = {
  key: string;
  label: ReactNode;
  icon?: LucideIcon;
  /** Authoritative count — `0` shows; omit when unknown. */
  count?: number | null;
  testId?: string;
  disabled?: boolean;
};

type UnderlineTabBarProps = {
  items: ReadonlyArray<UnderlineTabItem>;
  activeKey: string;
  onChange: (key: string) => void;
  ariaLabel: string;
  testId?: string;
  className?: string;
};

/**
 * Page tab selector. Renders the global ExitsChipBar filter chips
 * (same size, active style, and enter/press motion as Your stores).
 */
export function UnderlineTabBar({
  items,
  activeKey,
  onChange,
  ariaLabel,
  testId,
  className,
}: UnderlineTabBarProps) {
  const { preferences } = usePreferences();

  if (preferences.tabStyle === "underline") {
    return (
      <ExitsTabs
        variant="underline"
        scrollable
        ariaLabel={ariaLabel}
        testId={testId}
        className={className}
        value={activeKey}
        onValueChange={onChange}
        items={items.map((item) => ({
          key: item.key,
          label: item.label,
          icon: item.icon,
          count: item.count ?? undefined,
          disabled: item.disabled,
          testId: item.testId,
        }))}
      />
    );
  }

  return (
    <ExitsChipBar
      variant="filter"
      ariaLabel={ariaLabel}
      testId={testId}
      className={className}
      items={items.map((item) => {
        const Icon = item.icon;
        return {
          key: item.key,
          label: item.label,
          icon: Icon ? <Icon /> : undefined,
          count: item.count,
          state: activeKey === item.key ? "active" : "idle",
          testId: item.testId,
          disabled: item.disabled,
          onSelect: () => onChange(item.key),
        };
      })}
    />
  );
}
