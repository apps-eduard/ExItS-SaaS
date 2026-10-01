import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link, useLocation, useSearchParams } from "react-router-dom";
import {
  ArrowLeftRight,
  ArrowRight,
  Bell,
  BriefcaseBusiness,
  Building2,
  CircleAlert,
  Compass,
  HandCoins,
  QrCode,
  Search,
  Settings,
  PenLine,
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
import { CountChip } from "@/components/exits/CountChip";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { DashboardMetricCard } from "@/features/reports/DashboardMetricCards";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
import { PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY } from "@/features/personal/ownership/PersonalOwnershipTransfersPage";
import { UtangAccountCard } from "@/features/personal/utang/UtangAccountCard";
import {
  UTANG_OWNERSHIP_MINE,
  UTANG_OWNERSHIP_SHARED,
  UTANG_READ_ONLY_CHIP,
} from "@/features/personal/utang/utang-ownership-ui";
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
    (dashboard?.sharedWithMeLentBalance ?? 0) === 0 &&
    (dashboard?.sharedWithMeBorrowedBalance ?? 0) === 0 &&
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
          <div className="flex min-w-0 flex-col gap-3">
            <div className="flex min-w-0 flex-col gap-2" data-testid="utang-hub-owed-to-me">
              <h3 className="m-0 inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)] font-semibold text-muted">
                <HandCoins className="size-[1.1rem] shrink-0 text-primary" aria-hidden />
                {t("personal.home.owedToMe")}
              </h3>
              <div className="personal-summary-grid personal-summary-grid--balances" role="list">
                <DashboardMetricCard
                  label={
                    <StatusChip
                      tone={UTANG_OWNERSHIP_MINE.chipTone}
                      appearance={UTANG_OWNERSHIP_MINE.appearance}
                      shape={UTANG_OWNERSHIP_MINE.shape}
                      data-testid="utang-hub-owed-to-me-mine-label"
                    >
                      {t("personal.utang.ownershipMine")}
                    </StatusChip>
                  }
                  icon={HandCoins}
                  tone={UTANG_OWNERSHIP_MINE.metricTone}
                  testId="utang-hub-owed-to-me-mine"
                  to="/personal/utang/lent"
                >
                  <MoneyDisplay amount={dashboard.totalLentBalance} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={
                    <StatusChip
                      tone={UTANG_OWNERSHIP_SHARED.chipTone}
                      appearance={UTANG_OWNERSHIP_SHARED.appearance}
                      shape={UTANG_OWNERSHIP_SHARED.shape}
                      data-testid="utang-hub-owed-to-me-shared-label"
                    >
                      {t("personal.utang.ownershipSharedWithMe")}
                    </StatusChip>
                  }
                  icon={HandCoins}
                  tone={UTANG_OWNERSHIP_SHARED.metricTone}
                  testId="utang-hub-owed-to-me-shared"
                  to="/personal/utang/lent"
                  tag={
                    <StatusChip
                      tone={UTANG_READ_ONLY_CHIP.tone}
                      appearance={UTANG_READ_ONLY_CHIP.appearance}
                      shape={UTANG_READ_ONLY_CHIP.shape}
                      data-testid="utang-hub-owed-to-me-shared-readonly"
                    >
                      {t("personal.utang.readOnly")}
                    </StatusChip>
                  }
                >
                  <MoneyDisplay amount={dashboard.sharedWithMeLentBalance} />
                </DashboardMetricCard>
              </div>
            </div>
            <div
              className="flex min-w-0 flex-col gap-2 border-t border-border pt-3"
              data-testid="utang-hub-i-owe"
            >
              <h3 className="m-0 inline-flex items-center gap-1.5 text-[length:var(--exits-text-sm)] font-semibold text-muted">
                <Wallet className="size-[1.1rem] shrink-0 text-primary" aria-hidden />
                {t("personal.home.iOwe")}
              </h3>
              <div className="personal-summary-grid personal-summary-grid--balances" role="list">
                <DashboardMetricCard
                  label={
                    <StatusChip
                      tone={UTANG_OWNERSHIP_MINE.chipTone}
                      appearance={UTANG_OWNERSHIP_MINE.appearance}
                      shape={UTANG_OWNERSHIP_MINE.shape}
                      data-testid="utang-hub-i-owe-mine-label"
                    >
                      {t("personal.utang.ownershipMine")}
                    </StatusChip>
                  }
                  icon={Wallet}
                  tone={UTANG_OWNERSHIP_MINE.metricTone}
                  testId="utang-hub-i-owe-mine"
                  to="/personal/utang/owe"
                >
                  <MoneyDisplay amount={dashboard.totalBorrowedBalance} />
                </DashboardMetricCard>
                <DashboardMetricCard
                  label={
                    <StatusChip
                      tone={UTANG_OWNERSHIP_SHARED.chipTone}
                      appearance={UTANG_OWNERSHIP_SHARED.appearance}
                      shape={UTANG_OWNERSHIP_SHARED.shape}
                      data-testid="utang-hub-i-owe-shared-label"
                    >
                      {t("personal.utang.ownershipSharedWithMe")}
                    </StatusChip>
                  }
                  icon={Wallet}
                  tone={UTANG_OWNERSHIP_SHARED.metricTone}
                  testId="utang-hub-i-owe-shared"
                  to="/personal/utang/owe"
                  tag={
                    <StatusChip
                      tone={UTANG_READ_ONLY_CHIP.tone}
                      appearance={UTANG_READ_ONLY_CHIP.appearance}
                      shape={UTANG_READ_ONLY_CHIP.shape}
                      data-testid="utang-hub-i-owe-shared-readonly"
                    >
                      {t("personal.utang.readOnly")}
                    </StatusChip>
                  }
                >
                  <MoneyDisplay amount={dashboard.sharedWithMeBorrowedBalance} />
                </DashboardMetricCard>
              </div>
            </div>
          </div>
          <div
            className="flex flex-wrap items-center gap-2 border-t border-border pt-3"
            data-testid="utang-hub-meta"
          >
            <Link to="/personal/people" className="inline-flex no-underline">
              <CountChip
                label={t("personal.home.people")}
                count={dashboard.contactCount}
                tone="info"
              />
            </Link>
            <Link to="/personal/utang" className="inline-flex no-underline">
              <CountChip
                label={t("personal.home.activeMine")}
                count={dashboard.activeRelationshipCount}
                tone="primary"
              />
            </Link>
            <Link to="/personal/utang" className="inline-flex no-underline">
              <CountChip
                label={t("personal.home.activeShared")}
                count={dashboard.sharedWithMeActiveCount ?? 0}
                tone="info"
              />
            </Link>
          </div>
        </section>
      ) : null}

      <div
        className="exits-animate-panel flex min-w-0 flex-nowrap items-center gap-2 overflow-x-auto"
        data-testid="utang-hub-actions"
      >
        <Button asChild className="shrink-0" data-testid="utang-hub-record">
          <Link to="/personal/utang/lent">
            <PenLine className="size-4 shrink-0" aria-hidden />
            {t("personal.utang.recordLent")}
          </Link>
        </Button>
        <Button asChild variant="outline" className="shrink-0">
          <Link to="/personal/utang/lent" data-testid="utang-open-lent">
            <HandCoins className="size-4 shrink-0 text-primary" aria-hidden />
            {t("personal.utang.lent")}
          </Link>
        </Button>
        <Button asChild variant="outline" className="shrink-0">
          <Link to="/personal/utang/owe" data-testid="utang-open-owe">
            <Wallet className="size-4 shrink-0 text-primary" aria-hidden />
            {t("personal.utang.owe")}
          </Link>
        </Button>
        <Button asChild variant="outline" className="shrink-0">
          <Link to="/personal/people" data-testid="utang-open-people">
            <Users className="size-4 shrink-0 text-primary" aria-hidden />
            {t("personal.utang.people")}
          </Link>
        </Button>
      </div>

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
        <>
          <section
            className="catalog-form-section exits-animate-panel personal-section gap-3"
            data-testid="utang-hub-accounts"
            aria-label={t("personal.utang.activeAccounts")}
          >
            <h2 className="catalog-form-section__title flex items-center gap-2 text-muted">
              <Users className="size-4 shrink-0" aria-hidden="true" />
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
        </>
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
              to: "/personal/explore-pos",
              primary: true,
            },
          ]}
        />
      </section>
    </div>
  );
}
