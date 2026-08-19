import { Link, useLocation } from "react-router-dom";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";
import { navigationRegistry } from "@/lib/navigation/navigation-registry";

function normalizePath(pathname: string): string {
  if (pathname.length > 1 && pathname.endsWith("/")) {
    return pathname.slice(0, -1);
  }
  return pathname;
}

function labelForPath(pathname: string, t: (key: MessageKey) => string): string | null {
  const path = normalizePath(pathname);
  for (const section of navigationRegistry) {
    for (const item of section.items) {
      if (!item.href) {
        continue;
      }
      if (item.href.split("?")[0] === path) {
        return t(item.labelKey);
      }
    }
  }
  return null;
}

export function AppBreadcrumbs() {
  const { t } = usePreferences();
  const location = useLocation();
  const path = normalizePath(location.pathname);
  const isOverview = path === "/admin";
  const currentLabel = isOverview
    ? t("nav.overview")
    : (labelForPath(path, t) ?? path.replace(/^\/admin\/?/, ""));

  return (
    <nav aria-label={t("shell.breadcrumb")} className="min-w-0 overflow-hidden px-4 py-2">
      <ol className="flex flex-wrap items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
        {isOverview ? (
          <li className="truncate text-foreground" aria-current="page">
            {currentLabel}
          </li>
        ) : (
          <>
            <li>
              <Link className="text-primary hover:underline" to="/admin">
                {t("nav.overview")}
              </Link>
            </li>
            <li className="flex min-w-0 items-center">
              <span aria-hidden="true" className="px-1">
                /
              </span>
              <span className="truncate text-foreground" aria-current="page">
                {currentLabel}
              </span>
            </li>
          </>
        )}
      </ol>
    </nav>
  );
}
