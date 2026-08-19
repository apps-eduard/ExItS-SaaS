import { NavLink } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Tooltip } from "@/components/ui/tooltip";
import { NavIcon } from "@/components/exits/nav-icons";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { resolveNavigation } from "@/lib/navigation/resolve-navigation";
import { cn } from "@/lib/utils";
import type { ResolvedNavigationItem } from "@/lib/navigation/navigation-types";

export function AppNav({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const sections = resolveNavigation({
    permissionStatus: authorization.status,
    hasAnyPermission: authorization.hasAnyPermission,
    isPlatformAdministrator: authorization.isPlatformAdministrator,
    developmentToolsAllowed: areDevelopmentToolsAllowed(),
  });

  return (
    <nav aria-label={t("shell.primaryNav")} className="flex flex-col gap-5 px-2 py-3">
      {sections.map((section) => (
        <div key={section.id}>
          {collapsed ? null : (
            <p className="px-2 pb-1 text-[11px] font-medium tracking-wide text-muted uppercase">
              {t(section.labelKey)}
            </p>
          )}
          <ul className="grid gap-1">
            {section.items.map((item) => (
              <li key={item.id}>
                <NavItem collapsed={collapsed} item={item} onNavigate={onNavigate} />
              </li>
            ))}
          </ul>
        </div>
      ))}
    </nav>
  );
}

function NavItem({
  item,
  collapsed,
  onNavigate,
}: {
  item: ResolvedNavigationItem;
  collapsed: boolean;
  onNavigate?: () => void;
}) {
  const { t } = usePreferences();
  const label = t(item.labelKey);
  const planned = item.presentation === "planned" || item.presentation === "context";
  const underDevelopment = item.presentation === "underDevelopment";
  const hint = item.presentation === "context" ? t("nav.contextHint") : t("nav.plannedHint");

  const content = (
    <span
      className={cn(
        "flex min-h-11 items-center gap-2 rounded-md px-2 lg:min-h-9",
        collapsed && "justify-center",
      )}
    >
      <NavIcon name={item.icon} />
      {collapsed ? null : (
        <span className="flex min-w-0 flex-1 items-center justify-between gap-2">
          <span className="truncate">{label}</span>
          {planned ? <Badge tone="neutral">{t("nav.planned")}</Badge> : null}
          {underDevelopment ? <Badge tone="neutral">{t("nav.underDevelopment")}</Badge> : null}
        </span>
      )}
    </span>
  );

  if (underDevelopment || planned || !item.href) {
    const statusLabel = underDevelopment
      ? `${label}. ${t("nav.underDevelopment")}`
      : planned
        ? `${label}. ${t("nav.planned")}`
        : `${label}. ${hint}`;
    const status = (
      <span
        aria-disabled="true"
        aria-label={statusLabel}
        className="block w-full cursor-default text-[length:var(--exits-text-sm)] text-muted"
      >
        {content}
      </span>
    );
    return collapsed ? <Tooltip content={statusLabel}>{status}</Tooltip> : status;
  }

  const link = (
    <NavLink
      to={item.href}
      end={item.href === "/admin"}
      onClick={onNavigate}
      className={({ isActive }) =>
        cn(
          "block rounded-md text-[length:var(--exits-text-sm)] font-medium",
          isActive
            ? "bg-surface-muted text-foreground shadow-[inset_2px_0_0_0_var(--exits-primary)]"
            : "text-muted hover:bg-surface-muted/70 hover:text-foreground",
        )
      }
    >
      {content}
    </NavLink>
  );

  return collapsed ? <Tooltip content={label}>{link}</Tooltip> : link;
}
