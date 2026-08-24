import {
  Bell,
  Building2,
  HandCoins,
  QrCode,
  Settings,
  UserPlus,
  Users,
  Wallet,
} from "lucide-react";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { PageHeader } from "@/components/exits/PageHeader";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useSwitchToBusiness } from "@/workspace/use-switch-to-business";

export function PersonalUtangHubPage() {
  const { t } = useI18n();

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-utang-hub"
    >
      <PageHeader
        title={t("personal.utang.title")}
        description={t("personal.utang.lede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-utang-hub"
      />
      <section className="catalog-form-section exits-animate-panel personal-section gap-3">
        <h2 className="catalog-form-section__title text-muted">{t("personal.home.quickActions")}</h2>
        <ActionTileGrid
          tiles={[
            {
              key: "people",
              label: t("personal.utang.people"),
              icon: Users,
              testId: "utang-open-people",
              to: "/personal/utang/people",
              primary: true,
            },
            {
              key: "lent",
              label: t("personal.utang.lent"),
              icon: HandCoins,
              testId: "utang-open-lent",
              to: "/personal/utang/lent",
            },
            {
              key: "owe",
              label: t("personal.utang.owe"),
              icon: Wallet,
              testId: "utang-open-owe",
              to: "/personal/utang/owe",
            },
          ]}
        />
      </section>
    </div>
  );
}

export function PersonalMorePage() {
  const { t } = useI18n();
  const { canSwitch, switching, switchToBusiness, online } = useSwitchToBusiness();

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-more-page"
    >
      <PageHeader
        title={t("personal.more.title")}
        description={t("personal.more.lede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-more"
      />

      {canSwitch ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-3"
          data-testid="personal-more-account"
        >
          <h2 className="catalog-form-section__title text-muted">
            {t("personal.more.group.account")}
          </h2>
          <ActionTileGrid
            tiles={[
              {
                key: "switch",
                label: switching
                  ? t("personal.more.switchingBusiness")
                  : t("personal.more.switchToBusiness"),
                icon: Building2,
                testId: "more-switch-to-business",
                primary: true,
                disabled: switching || !online,
                onClick: () => void switchToBusiness(),
              },
            ]}
          />
          {!online ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-muted"
              data-testid="more-switch-offline"
            >
              {t("offline.requiredContextSwitch")}
            </p>
          ) : null}
        </section>
      ) : null}

      <PersonalCommerceNav active="none" variant="section" />

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        data-testid="personal-more-social"
      >
        <h2 className="catalog-form-section__title text-muted">
          {t("personal.more.group.social")}
        </h2>
        <ActionTileGrid
          tiles={[
            {
              key: "invites",
              label: t("personal.social.invitationsTitle"),
              icon: UserPlus,
              testId: "more-open-invitations",
              to: "/personal/utang/invitations",
            },
            {
              key: "notifications",
              label: t("personal.social.notificationsTitle"),
              icon: Bell,
              testId: "more-open-notifications",
              to: "/personal/notifications",
            },
            {
              key: "qr",
              label: t("personal.social.qrTitle"),
              icon: QrCode,
              testId: "more-open-qr",
              to: "/personal/my-qr",
            },
          ]}
        />
      </section>

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        data-testid="personal-more-business"
      >
        <h2 className="catalog-form-section__title text-muted">
          {t("personal.more.group.business")}
        </h2>
        <ActionTileGrid
          tiles={[
            {
              key: "preferences",
              label: t("preferences.title"),
              icon: Settings,
              testId: "more-open-preferences",
              to: "/settings/preferences",
            },
            {
              key: "start",
              label: t("personal.more.startBusiness"),
              icon: Building2,
              testId: "more-open-start-business",
              to: "/personal/explore-pos",
              primary: true,
            },
          ]}
        />
      </section>
    </div>
  );
}
