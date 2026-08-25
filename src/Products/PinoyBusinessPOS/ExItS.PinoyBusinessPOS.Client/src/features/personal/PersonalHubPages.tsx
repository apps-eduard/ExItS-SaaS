import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router-dom";
import {
  Bell,
  Building2,
  HandCoins,
  QrCode,
  Search,
  Settings,
  UserPen,
  UserPlus,
  Users,
  Wallet,
} from "lucide-react";
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";
import {
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
} from "@/api/platform/personal-utang-client";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { DashboardMetricCard } from "@/features/reports/DashboardMetricCards";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { UtangAccountCard } from "@/features/personal/utang/UtangAccountCard";
import {
  countSegment,
  filterUtangAccounts,
  isActiveUtangAccount,
  mergeUtangAccounts,
  sortUtangAccounts,
  type UtangAccountSegment,
} from "@/features/personal/utang/utang-workspace";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useSwitchToBusiness } from "@/workspace/use-switch-to-business";
import { cn } from "@/lib/cn";

function parseSegment(raw: string | null): UtangAccountSegment {
  if (raw === "lent" || raw === "owe") return raw;
  return "all";
}

export function PersonalUtangHubPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const [searchParams, setSearchParams] = useSearchParams();
  const segment = parseSegment(searchParams.get("segment"));
  const [search, setSearch] = useState("");

  const dashboardQuery = useQuery({
    queryKey: ["personal", "dashboard"],
    queryFn: ({ signal }) => getPersonalDashboard(signal),
  });
  const contactsQuery = useQuery({
    queryKey: ["personal", "utang", "contacts"],
    queryFn: ({ signal }) => listPersonalContacts(signal),
    enabled: online,
  });
  const lentQuery = useQuery({
    queryKey: ["personal", "utang", "lent"],
    queryFn: ({ signal }) => listLentRelationships(signal),
    enabled: online,
  });
  const oweQuery = useQuery({
    queryKey: ["personal", "utang", "owe"],
    queryFn: ({ signal }) => listBorrowedRelationships(signal),
    enabled: online,
  });

  const accountsLoading =
    online && (contactsQuery.isPending || lentQuery.isPending || oweQuery.isPending);
  const accountsError =
    online && (contactsQuery.isError || lentQuery.isError || oweQuery.isError);

  const allActive = useMemo(() => {
    if (!contactsQuery.data || !lentQuery.data || !oweQuery.data) {
      return [];
    }
    return sortUtangAccounts(
      mergeUtangAccounts(lentQuery.data, oweQuery.data, contactsQuery.data).filter(
        isActiveUtangAccount,
      ),
    );
  }, [contactsQuery.data, lentQuery.data, oweQuery.data]);

  const visibleAccounts = useMemo(
    () => filterUtangAccounts(allActive, segment, search),
    [allActive, search, segment],
  );

  const dashboard = dashboardQuery.data;
  const pendingCount = dashboard?.pendingConfirmationCount ?? 0;
  const showSearch = allActive.length >= 4 || search.trim().length > 0;

  function setSegment(next: UtangAccountSegment) {
    const params = new URLSearchParams(searchParams);
    if (next === "all") {
      params.delete("segment");
    } else {
      params.set("segment", next);
    }
    setSearchParams(params, { replace: true });
  }

  if (dashboardQuery.isPending) {
    return <LoadingSkeleton />;
  }

  const emptyWorkspace =
    !accountsLoading &&
    !accountsError &&
    allActive.length === 0 &&
    (dashboard?.totalLentBalance ?? 0) === 0 &&
    (dashboard?.totalBorrowedBalance ?? 0) === 0 &&
    pendingCount === 0;

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-utang-hub"
    >
      <PageHeader
        title={t("personal.utang.title")}
        description={t("personal.utang.workspaceLede")}
        backTo={personalPageBackNav.home.to}
        backLabel={t(personalPageBackNav.home.labelKey)}
        backTestId="page-header-back-utang-hub"
      />

      {dashboard ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          aria-label={t("personal.home.utangSummary")}
          data-testid="utang-hub-summary"
        >
          <div className="personal-summary-grid personal-summary-grid--balances" role="list">
            <DashboardMetricCard
              label={t("personal.home.owedToMe")}
              icon={HandCoins}
              tone="emphasis"
              testId="utang-hub-owed-to-me"
              to="/personal/utang/lent"
            >
              <MoneyDisplay amount={dashboard.totalLentBalance} />
            </DashboardMetricCard>
            <DashboardMetricCard
              label={t("personal.home.iOwe")}
              icon={Wallet}
              tone={dashboard.totalBorrowedBalance > 0 ? "attention" : "default"}
              testId="utang-hub-i-owe"
              to="/personal/utang/owe"
            >
              <MoneyDisplay amount={dashboard.totalBorrowedBalance} />
            </DashboardMetricCard>
          </div>
          {dashboard.activeRelationshipCount > 0 || dashboard.contactCount > 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="utang-hub-meta">
              {t("personal.utang.workspaceMeta")
                .replace("{active}", String(dashboard.activeRelationshipCount))
                .replace("{people}", String(dashboard.contactCount))}
            </p>
          ) : null}
        </section>
      ) : null}

      {pendingCount > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          data-testid="utang-hub-pending"
          aria-label={t("personal.utang.pendingConfirmations").replace(
            "{count}",
            String(pendingCount),
          )}
        >
          <h2 className="catalog-form-section__title m-0">
            {t("personal.utang.needsConfirmation")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.utang.pendingConfirmations").replace("{count}", String(pendingCount))}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.utang.pendingConfirmationsLede")}
          </p>
          <Link
            to="/personal/utang/lent"
            className="inline-flex min-h-11 items-center gap-1 text-[length:var(--exits-text-sm)] font-semibold text-[var(--exits-primary)] no-underline"
            data-testid="utang-hub-pending-review"
          >
            {t("personal.utang.reviewAccounts")}
          </Link>
        </section>
      ) : null}

      {emptyWorkspace ? (
        <div className="exits-animate-panel" data-testid="utang-hub-empty">
          <EmptyState
            title={t("personal.utang.workspaceEmptyTitle")}
            detail={t("personal.utang.workspaceEmptyDetail")}
          />
          <div className="mt-3">
            <ActionTileGrid
              emphasizePrimary
              tiles={[
                {
                  key: "lent",
                  label: t("personal.utang.lent"),
                  icon: HandCoins,
                  testId: "utang-open-lent",
                  to: "/personal/utang/lent",
                  primary: true,
                },
                {
                  key: "owe",
                  label: t("personal.utang.owe"),
                  icon: Wallet,
                  testId: "utang-open-owe",
                  to: "/personal/utang/owe",
                },
                {
                  key: "people",
                  label: t("personal.utang.people"),
                  icon: Users,
                  testId: "utang-open-people",
                  to: "/personal/people",
                },
              ]}
            />
          </div>
        </div>
      ) : (
        <>
          <section
            className="catalog-form-section exits-animate-panel personal-section gap-3"
            data-testid="utang-hub-accounts"
            aria-label={t("personal.utang.activeAccounts")}
          >
            <h2 className="catalog-form-section__title text-muted">
              {t("personal.utang.activeAccounts")}
            </h2>

            <div
              className="utang-segment-bar"
              role="tablist"
              aria-label={t("personal.utang.filterLabel")}
              data-testid="utang-hub-segments"
            >
              {(
                [
                  ["all", t("personal.utang.filterAll")],
                  ["lent", t("personal.utang.filterOwedToMe")],
                  ["owe", t("personal.utang.filterIOwe")],
                ] as const
              ).map(([id, label]) => {
                const count = countSegment(allActive, id);
                const selected = segment === id;
                return (
                  <button
                    key={id}
                    type="button"
                    role="tab"
                    aria-selected={selected}
                    className={cn("utang-segment", selected && "utang-segment--selected")}
                    data-testid={`utang-segment-${id}`}
                    onClick={() => setSegment(id)}
                  >
                    {label}
                    {allActive.length > 0 ? (
                      <span className="utang-segment__count tabular-nums">{count}</span>
                    ) : null}
                  </button>
                );
              })}
            </div>

            {showSearch ? (
              <label className="utang-search relative block">
                <span className="sr-only">{t("personal.utang.searchLabel")}</span>
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted"
                  aria-hidden
                />
                <input
                  type="search"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder={t("personal.utang.searchPlaceholder")}
                  className="min-h-11 w-full rounded-[var(--exits-radius-md)] border border-border bg-surface pl-10 pr-3 text-[length:var(--exits-text-sm)]"
                  data-testid="utang-hub-search"
                />
              </label>
            ) : null}

            {accountsLoading ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("personal.utang.accountsLoading")}
              </p>
            ) : accountsError ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="utang-hub-accounts-error">
                {t("personal.utang.accountsUnavailable")}
              </p>
            ) : visibleAccounts.length === 0 ? (
              <EmptyState
                title={t("personal.utang.accountsFilterEmptyTitle")}
                detail={t("personal.utang.accountsFilterEmptyDetail")}
              />
            ) : (
              <ul className="exits-list m-0 grid list-none gap-2 p-0">
                {visibleAccounts.map((row) => (
                  <li key={row.relationshipId}>
                    <UtangAccountCard row={row} />
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section
            className="catalog-form-section exits-animate-panel personal-section gap-2"
            aria-label={t("personal.home.quickActions")}
          >
            <ActionTileGrid
              tiles={[
                {
                  key: "lent",
                  label: t("personal.utang.lent"),
                  icon: HandCoins,
                  testId: "utang-open-lent",
                  to: "/personal/utang/lent",
                  primary: true,
                },
                {
                  key: "owe",
                  label: t("personal.utang.owe"),
                  icon: Wallet,
                  testId: "utang-open-owe",
                  to: "/personal/utang/owe",
                },
                {
                  key: "people",
                  label: t("personal.utang.people"),
                  icon: Users,
                  testId: "utang-open-people",
                  to: "/personal/people",
                },
              ]}
            />
          </section>
        </>
      )}
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
              key: "profile",
              label: t("personal.profile.edit"),
              icon: UserPen,
              testId: "more-open-profile",
              to: "/personal/profile?edit=1",
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
