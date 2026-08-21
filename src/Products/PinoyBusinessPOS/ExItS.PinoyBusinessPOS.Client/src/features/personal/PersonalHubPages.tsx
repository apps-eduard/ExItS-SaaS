import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function PersonalUtangHubPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-utang-hub">
      <PageHeader title={t("personal.utang.title")} description={t("personal.utang.lede")} />
      <div className="flex flex-col gap-2">
        <Button asChild className="min-h-11 justify-start" data-testid="utang-open-people">
          <Link to="/personal/utang/people">{t("personal.utang.people")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="utang-open-lent"
        >
          <Link to="/personal/utang/lent">{t("personal.utang.lent")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="utang-open-owe"
        >
          <Link to="/personal/utang/owe">{t("personal.utang.owe")}</Link>
        </Button>
      </div>
    </div>
  );
}

export function PersonalTodoHubPage() {
  const { t } = useI18n();
  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-todo-hub">
      <PageHeader title={t("personal.todo.title")} description={t("personal.todo.lede")} />
    </div>
  );
}

export function PersonalMorePage() {
  const { t } = useI18n();
  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-more-page">
      <PageHeader title={t("personal.more.title")} description={t("personal.more.lede")} />
      <div className="flex flex-col gap-2">
        <Button asChild className="min-h-11 justify-start" data-testid="more-open-stores">
          <Link to="/personal/linked-merchants">{t("personal.more.stores")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="more-open-invitations"
        >
          <Link to="/personal/utang/invitations">{t("personal.social.invitationsTitle")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="more-open-notifications"
        >
          <Link to="/personal/notifications">{t("personal.social.notificationsTitle")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="more-open-qr"
        >
          <Link to="/personal/my-qr">{t("personal.social.qrTitle")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="more-open-orders"
        >
          <Link to="/personal/orders">{t("personal.nav.orders")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="more-open-preferences"
        >
          <Link to="/settings/preferences">{t("preferences.title")}</Link>
        </Button>
        <Button
          asChild
          variant="ghost"
          className="min-h-11 justify-start"
          data-testid="more-open-start-business"
        >
          <Link to="/personal/start-business">{t("personal.more.startBusiness")}</Link>
        </Button>
      </div>
    </div>
  );
}

export function PersonalStartBusinessPlaceholderPage() {
  const { t } = useI18n();
  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-start-business-placeholder">
      <PageHeader
        title={t("personal.more.startBusiness")}
        description={t("personal.more.startBusinessComing")}
      />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/personal/more">{t("personal.more.back")}</Link>
      </Button>
    </div>
  );
}
