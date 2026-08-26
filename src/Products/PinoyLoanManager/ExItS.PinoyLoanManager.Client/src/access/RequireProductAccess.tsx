import type { ReactNode } from "react";
import { AccessStatusPage } from "@/access/AccessStatusPage";
import { OrganizationSelectPage } from "@/access/OrganizationSelectPage";
import { useProductAccess } from "@/access/ProductAccessProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { SessionLoading } from "@/session/SessionGuards";

export function RequireProductAccess({ children }: { children: ReactNode }) {
  const { t } = useI18n();
  const { phase, retry, access } = useProductAccess();

  if (phase === "loading") {
    return <SessionLoading />;
  }
  if (phase === "select-organization") {
    return <OrganizationSelectPage />;
  }
  if (phase === "zero-organizations") {
    return (
      <AccessStatusPage title={t("access.noOrgTitle")} description={t("access.noOrgDescription")} />
    );
  }
  if (phase === "account-scope") {
    return (
      <AccessStatusPage
        title={t("access.accountScopeTitle")}
        description={t("access.accountScopeDescription")}
      />
    );
  }
  if (phase === "subscription-inactive") {
    return (
      <AccessStatusPage
        title={t("access.subscriptionTitle")}
        description={t("access.subscriptionDescription")}
      />
    );
  }
  if (phase === "denied") {
    return (
      <AccessStatusPage
        title={t("access.deniedTitle")}
        description={
          access?.reasonCode === "product_assignment_missing"
            ? t("access.deniedAssignment")
            : t("access.deniedDescription")
        }
      />
    );
  }
  if (phase === "error") {
    return (
      <AccessStatusPage
        title={t("access.errorTitle")}
        description={t("access.errorDescription")}
        action={{ label: t("access.retry"), onClick: retry }}
      />
    );
  }
  return children;
}
