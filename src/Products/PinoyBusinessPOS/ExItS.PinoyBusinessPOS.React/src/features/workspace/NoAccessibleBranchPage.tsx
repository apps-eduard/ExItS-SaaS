import { Link, useLocation } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { PageHeader } from "@/components/exits/PageHeader";
import { rememberPreferencesReturnTo } from "@/features/preferences/preferences-return";
import { Store } from "lucide-react";
import { useI18n } from "@/i18n/I18nProvider";

export function NoAccessibleBranchPage() {
  const { t } = useI18n();
  const location = useLocation();
  const preferencesReturnTo = `${location.pathname}${location.search}`;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="no-accessible-branch">
      <PageHeader title={t("noLocation.title")} description={t("noLocation.lede")} />
      <EmptyState
              align="center"
              icon={<Store className="size-5" strokeWidth={1.75} />} title={t("noLocation.title")} detail={t("noLocation.detail")} />
      <Button asChild variant="ghost">
        <Link
          to="/settings/preferences"
          state={{ returnTo: preferencesReturnTo }}
          onClick={() => rememberPreferencesReturnTo(preferencesReturnTo)}
        >
          {t("preferences.title")}
        </Link>
      </Button>
    </div>
  );
}
