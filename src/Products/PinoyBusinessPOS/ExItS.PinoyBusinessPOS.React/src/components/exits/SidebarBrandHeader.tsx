import { useI18n } from "@/i18n/I18nProvider";

/** Shared desktop sidebar brand — logo mark + product name (Compact/Reveal hide text via CSS). */
export function SidebarBrandHeader({ testId = "sidebar-brand" }: { testId?: string }) {
  const { t } = useI18n();
  const productName = t("app.name");

  return (
    <div className="admin-sidebar__brand" data-testid={testId} aria-label={productName}>
      <span className="admin-sidebar__mark" aria-hidden="true">
        E
      </span>
      <p className="admin-sidebar__product admin-sidebar__brand-label m-0" aria-hidden="true">
        {productName}
      </p>
    </div>
  );
}
