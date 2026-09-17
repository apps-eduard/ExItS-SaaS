import { FoldVertical, UnfoldVertical } from "lucide-react";
import { IconButton } from "@/components/ui/icon-button";
import { useI18n } from "@/i18n/I18nProvider";

/**
 * Shared desktop sidebar brand for Organization Admin + Operations.
 * Toggle collapses/expands all navigation groups — never resizes the sidenav.
 */
export function SidebarBrandHeader({
  testId = "sidebar-brand",
  allGroupsExpanded,
  onToggleAllGroups,
}: {
  testId?: string;
  /** When true, the control offers Collapse all; otherwise Expand all. */
  allGroupsExpanded: boolean;
  onToggleAllGroups: () => void;
}) {
  const { t } = useI18n();
  const productName = t("app.name");
  const toggleLabel = allGroupsExpanded
    ? t("shell.collapseAllNavGroups")
    : t("shell.expandAllNavGroups");

  return (
    <div className="admin-sidebar__brand" data-testid={testId} aria-label={productName}>
      <div className="admin-sidebar__brand-main">
        <span className="admin-sidebar__mark" aria-hidden="true">
          <span className="admin-sidebar__mark-glyph">E</span>
        </span>
        <p className="admin-sidebar__product admin-sidebar__brand-label m-0" aria-hidden="true">
          {productName}
        </p>
      </div>
      <IconButton
        label={toggleLabel}
        aria-pressed={allGroupsExpanded}
        data-testid={`${testId}-collapse-toggle`}
        className="admin-sidebar__collapse-toggle"
        onClick={onToggleAllGroups}
      >
        {allGroupsExpanded ? (
          <FoldVertical
            aria-hidden="true"
            className="admin-sidebar__collapse-icon size-[17px]"
            strokeWidth={2.15}
          />
        ) : (
          <UnfoldVertical
            aria-hidden="true"
            className="admin-sidebar__collapse-icon size-[17px]"
            strokeWidth={2.15}
          />
        )}
      </IconButton>
    </div>
  );
}
