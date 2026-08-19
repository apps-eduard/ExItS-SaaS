import { Link, useLocation } from "react-router-dom";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import type { MessageKey } from "@/lib/i18n/messages";
import { itemsForPathname, resolveKnownReactRoute } from "@/lib/navigation/known-react-routes";

function labelForAuthorizedPath(pathname: string, t: (key: MessageKey) => string): string | null {
  const item = itemsForPathname(pathname)[0];
  return item ? t(item.labelKey) : null;
}

export function AppBreadcrumbs() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const location = useLocation();
  const path =
    location.pathname.length > 1 && location.pathname.endsWith("/")
      ? location.pathname.slice(0, -1)
      : location.pathname;
  const isOverview = path === "/admin";
  const resolution = resolveKnownReactRoute({
    pathname: path,
    permissionStatus: authorization.status,
    hasAnyPermission: authorization.hasAnyPermission,
    isPlatformAdministrator: authorization.isPlatformAdministrator,
    developmentToolsAllowed: areDevelopmentToolsAllowed(),
  });
  const currentLabel = isOverview
    ? t("nav.overview")
    : resolution === "under-development"
      ? (labelForAuthorizedPath(path, t) ?? t("underDevelopment.title"))
      : t("shell.notFound.title");

  return (
    <nav aria-label={t("shell.breadcrumb")} className="min-w-0 overflow-hidden px-4 py-2">
      <ol className="flex flex-wrap items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
        {isOverview ? (
          <li className="truncate text-foreground" aria-current="page">
            {currentLabel}
          </li>
        ) : resolution === "pending" ? (
          <li>
            <Link className="text-primary hover:underline" to="/admin">
              {t("nav.overview")}
            </Link>
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
