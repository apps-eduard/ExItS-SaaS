import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";

export type UnderlineTabItem = {
  key: string;
  label: ReactNode;
  icon?: LucideIcon;
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
          state: activeKey === item.key ? "active" : "idle",
          testId: item.testId,
          disabled: item.disabled,
          onSelect: () => onChange(item.key),
        };
      })}
    />
  );
}
