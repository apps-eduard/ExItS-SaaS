import { Fragment, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import {
  ArrowDown,
  ArrowLeftRight,
  ArrowRight,
  ArrowUp,
  ChevronRight,
  Bell,
  BriefcaseBusiness,
  Building2,
  CircleAlert,
  Compass,
  HandCoins,
  List,
  QrCode,
  Search,
  Settings,
  Share2,
  User,
  UserPen,
  UserPlus,
  Users,
  Wallet,
} from "lucide-react";
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";
import { listMyPendingOwnershipTransfers } from "@/api/platform/ownership-transfer-client";
import {
  listBorrowedRelationships,
  listLentRelationships,
  listPersonalContacts,
} from "@/api/platform/personal-utang-client";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { PersonalInstallMoreEntry } from "@/features/personal/PersonalInstallOffer";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY } from "@/features/personal/ownership/PersonalOwnershipTransfersPage";
import { UtangAccountCard } from "@/features/personal/utang/UtangAccountCard";
import { PersonalTrackerCard, type PersonalTrackerTestIds } from "@/features/personal/PersonalTrackerCard";

import { usePreferencesOverlay } from "@/features/preferences/PreferencesOverlay";
import { PREFERENCES_DEFAULT_SECTION } from "@/features/preferences/preferences-sections";
import { useNotificationsOverlay } from "@/features/personal/NotificationsOverlay";
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

function parseSegment(raw: string | null): UtangAccountSegment {
  if (
    raw === "split" ||
    raw === "ledgers" ||
    raw === "lent" ||
    raw === "owe" ||
    raw === "mine" ||
    raw === "shared"
  ) {
    return raw;
  }
  return "all";
}

const UTANG_HUB_TRACKER_TEST_IDS: PersonalTrackerTestIds = {
  section: "utang-hub-summary",
  ribbon: "utang-hub-tracker-ribbon",
  pennant: "utang-hub-tracker-pennant",
  myLedger: "utang-hub-owed-to-me",
  myLedgerOwe: "utang-hub-i-owe-mine",
  myLedgerOweLabel: "utang-hub-i-owe-mine-label",
  myLedgerLent: "utang-hub-owed-to-me-mine",
  myLedgerLentLabel: "utang-hub-owed-to-me-mine-label",
  sharedLedger: "utang-hub-i-owe",
  sharedLedgerOwe: "utang-hub-i-owe-shared",
  sharedLedgerOweLabel: "utang-hub-i-owe-shared-label",
  sharedLedgerLent: "utang-hub-owed-to-me-shared",
  sharedLedgerLentLabel: "utang-hub-owed-to-me-shared-label",
  meta: "utang-hub-meta",
  people: "utang-hub-stat-people",
  activeMine: "utang-hub-stat-active",
  activeShared: "utang-hub-stat-active-shared",
};

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
  const showSearch = allActive.length > 0 || search.trim().length > 0;

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
    (dashboard?.sharedWithMeLentBalance ?? 0) === 0 &&
    (dashboard?.sharedWithMeBorrowedBalance ?? 0) === 0 &&
    pendingCount === 0;

  const hubActions = (
    <div className="personal-home-metrics utang-hub-actions" data-testid="utang-hub-actions">
      {(
        [
          { to: "/personal/utang/lent", testId: "utang-open-lent", tone: "success", label: t("personal.utang.lent"), Icon: ArrowUp },
          { to: "/personal/utang/owe", testId: "utang-open-owe", tone: "info", label: t("personal.utang.owe"), Icon: ArrowDown },
          {
            to: "/personal/people/add",
            testId: "utang-open-people",
            tone: "secondary",
            label: t("personal.utang.addPeople"),
            Icon: UserPlus,
            plus: true,
          },
        ] as const
      ).map((action) => {
        const ActionIcon = action.Icon;
        return (
          <Link
            key={action.testId}
            to={action.to}
            className={`personal-home-index-card personal-home-index-card--${action.tone} personal-home-index-card--solid`}
            data-testid={action.testId}
          >
            <span className="personal-home-index-card__ribbon" aria-hidden="true">
              <span className="personal-home-index-card__fold" />
              <ActionIcon className="personal-home-index-card__icon" />
            </span>
            <span className="personal-home-index-card__body">
              <span className="personal-home-index-card__title">{action.label}</span>
            </span>
            <strong
              className="personal-home-index-card__badge personal-home-index-card__badge--plus personal-home-index-card__badge--ghost"
              aria-hidden="true"
            >
              {"plus" in action ? "+" : <ChevronRight className="size-4" />}
            </strong>
          </Link>
        );
      })}
    </div>
  );

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
        <div className="personal-home-page">
          <PersonalTrackerCard dashboard={dashboard} testIds={UTANG_HUB_TRACKER_TEST_IDS} footer={hubActions} />
        </div>
      ) : (
        hubActions
      )}

      {pendingCount > 0 ? (
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-2"
          data-testid="utang-hub-pending"
          aria-label={t("personal.utang.pendingConfirmations").replace(
            "{count}",
            String(pendingCount),
          )}
        >
          <h2 className="catalog-form-section__title m-0 flex items-center gap-2">
            <CircleAlert className="size-4 shrink-0" aria-hidden="true" />
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
            className="inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)] font-semibold text-[var(--exits-primary)] no-underline"
            data-testid="utang-hub-pending-review"
          >
            {t("personal.utang.reviewAccounts")}
            <ArrowRight className="size-4 shrink-0" aria-hidden="true" />
          </Link>
        </section>
      ) : null}

      {emptyWorkspace ? (
        <div className="exits-animate-panel" data-testid="utang-hub-empty">
          <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
            title={t("personal.utang.workspaceEmptyTitle")}
            detail={t("personal.utang.workspaceEmptyDetail")}
          />
        </div>
      ) : (
        <div className="personal-home-page">
          <section
            className="catalog-form-section exits-animate-panel personal-section personal-tracker-card gap-3 utang-hub-accounts"
            data-testid="utang-hub-accounts"
            aria-label={t("personal.utang.activeAccounts")}
          >
            <h2 className="personal-tracker-card__ribbon" data-testid="utang-hub-accounts-ribbon">
              <span className="personal-tracker-card__pennant" aria-hidden>
                <Users className="personal-tracker-card__pennant-icon size-[0.95rem] shrink-0" />
              </span>
              {t("personal.utang.activeAccounts")}
            </h2>
            <div className="utang-hub-accounts__panel">

            <UnderlineTabBar
              ariaLabel={t("personal.utang.filterLabel")}
              testId="utang-hub-segments"
              className="exits-chip-bar--scroll utang-hub-filters"
              activeKey={segment}
              onChange={(key) => setSegment(parseSegment(key))}
              items={([
                {
                  key: "all",
                  label: t("personal.utang.filterAll"),
                  icon: List,
                },
                {
                  key: "split",
                  label: (
                    <span className="utang-hub-filters__split">
                      {t("personal.utang.filterOwedToMe")}
                      <span className="utang-hub-filters__divider" aria-hidden />
                      {t("personal.utang.filterIOwe")}
                    </span>
                  ),
                  icon: ArrowLeftRight,
                },
                {
                  key: "lent",
                  label: t("personal.utang.filterOwedToMe"),
                  icon: HandCoins,
                },
                {
                  key: "owe",
                  label: t("personal.utang.filterIOwe"),
                  icon: Wallet,
                },
                {
                  key: "ledgers",
                  label: (
                    <span className="utang-hub-filters__split">
                      {t("personal.utang.ownershipMine")}
                      <span className="utang-hub-filters__divider" aria-hidden />
                      {t("personal.utang.ownershipSharedWithMe")}
                    </span>
                  ),
                  icon: Users,
                },
                {
                  key: "mine",
                  label: t("personal.utang.ownershipMine"),
                  icon: User,
                },
                {
                  key: "shared",
                  label: t("personal.utang.ownershipSharedWithMe"),
                  icon: Share2,
                },
              ] as const).map((item) => ({
                ...item,
                count: allActive.length > 0 ? countSegment(allActive, item.key) : undefined,
                testId: `utang-segment-${item.key}`,
              }))}
            />

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
                  className="w-full rounded-[var(--exits-radius-md)] border border-border bg-surface pl-10 pr-3 text-[length:var(--exits-text-sm)]"
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
              variant="filtered"
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
                title={t("personal.utang.accountsFilterEmptyTitle")}
                detail={t("personal.utang.accountsFilterEmptyDetail")}
              />
            ) : segment === "split" || segment === "ledgers" ? (
              <div className="utang-hub-split" data-testid={`utang-hub-split-${segment}`}>
                {(segment === "split"
                  ? [
                      {
                        key: "lent",
                        label: t("personal.utang.filterOwedToMe"),
                        match: (row: (typeof visibleAccounts)[number]) => row.perspective === "lent",
                      },
                      {
                        key: "owe",
                        label: t("personal.utang.filterIOwe"),
                        match: (row: (typeof visibleAccounts)[number]) => row.perspective === "owe",
                      },
                    ]
                  : [
                      {
                        key: "mine",
                        label: t("personal.utang.ownershipMine"),
                        match: (row: (typeof visibleAccounts)[number]) => row.isLedgerOwner,
                      },
                      {
                        key: "shared",
                        label: t("personal.utang.ownershipSharedWithMe"),
                        match: (row: (typeof visibleAccounts)[number]) => !row.isLedgerOwner,
                      },
                    ]
                ).map((column, index) => {
                  const columnRows = visibleAccounts.filter(column.match);
                  return (
                    <Fragment key={column.key}>
                      {index > 0 ? <span className="utang-hub-split__divider" aria-hidden /> : null}
                      <section className="utang-hub-split__column" data-testid={`utang-hub-split-${column.key}`}>
                        <h3 className="utang-hub-split__title">
                          {column.label} <span className="tabular-nums">({columnRows.length})</span>
                        </h3>
                        {columnRows.length === 0 ? (
                          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                            {t("personal.utang.accountsFilterEmptyTitle")}
                          </p>
                        ) : (
                          <ul className="utang-settings-list utang-settings-list--single m-0 grid list-none gap-4 p-0">
                            {columnRows.map((row) => (
                              <li key={row.relationshipId}>
                                <UtangAccountCard row={row} />
                              </li>
                            ))}
                          </ul>
                        )}
                      </section>
                    </Fragment>
                  );
                })}
              </div>
            ) : (
              <ul className="utang-settings-list m-0 grid list-none gap-4 p-0" data-testid="utang-hub-account-list">
                {visibleAccounts.map((row) => (
                  <li key={row.relationshipId}>
                    <UtangAccountCard row={row} />
                  </li>
                ))}
              </ul>
            )}
            </div>
          </section>
        </div>
      )}
    </div>
  );
}

export function PersonalMorePage() {
  const { t } = useI18n();
  const location = useLocation();
  const { openPreferences } = usePreferencesOverlay();
  const { openNotifications } = useNotificationsOverlay();
  const { canSwitch, switching, switchToBusiness, online } = useSwitchToBusiness();
  const pendingOwnershipQuery = useQuery({
    queryKey: PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY,
    queryFn: ({ signal }) => listMyPendingOwnershipTransfers(signal),
    staleTime: 60_000,
    retry: false,
  });
  const pendingOwnershipCount = pendingOwnershipQuery.data?.length ?? 0;
  const ownershipTileLabel =
    pendingOwnershipCount > 0
      ? t("personal.ownershipTransfers.moreTileCount").replace(
          "{count}",
          String(pendingOwnershipCount),
        )
      : t("personal.ownershipTransfers.moreTile");

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

      <PersonalInstallMoreEntry />

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-3"
        data-testid="personal-more-guide"
      >
        <h2 className="catalog-form-section__title text-muted">
          {t("personal.guide.title")}
        </h2>
        <ActionTileGrid
          tiles={[
            {
              key: "guide",
              label: t("personal.more.exploreExits"),
              icon: Compass,
              testId: "more-open-guide",
              to: "/personal/guide",
              primary: true,
            },
          ]}
        />
      </section>

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
              to: "/personal/invitations",
            },
            {
              key: "notifications",
              label: t("personal.social.notificationsTitle"),
              icon: Bell,
              testId: "more-open-notifications",
              onClick: () =>
                openNotifications({
                  returnTo: `${location.pathname}${location.search}`,
                }),
            },
            {
              key: "qr",
              label: t("personal.social.qrTitle"),
              icon: QrCode,
              testId: "more-open-qr",
              to: "/personal/my-qr",
            },
            {
              key: "ownership",
              label: ownershipTileLabel,
              icon: ArrowLeftRight,
              testId: "more-open-ownership-transfers",
              to: "/personal/ownership-transfers",
            },
            {
              key: "workplaces",
              label: t("personal.workplaces.moreTile"),
              icon: BriefcaseBusiness,
              testId: "more-open-workplaces",
              to: "/personal/workplaces",
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
              onClick: () =>
                openPreferences({
                  section: PREFERENCES_DEFAULT_SECTION,
                  returnTo: `${location.pathname}${location.search}`,
                }),
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
              to: "/personal/businesses",
              primary: true,
            },
          ]}
        />
      </section>
    </div>
  );
}
