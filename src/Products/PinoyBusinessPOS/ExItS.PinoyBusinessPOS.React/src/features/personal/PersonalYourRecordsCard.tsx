import type { LucideIcon } from "lucide-react";
import { ChevronRight, Layers, Share2, Store, Users } from "lucide-react";
import { Link } from "react-router-dom";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

export const PERSONAL_YOUR_RECORDS_TEST_IDS = {
  section: "personal-your-records",
  ribbon: "personal-your-records-ribbon",
  pennant: "personal-your-records-pennant",
  people: "personal-your-records-tile-people",
  myRecords: "personal-your-records-tile-my-records",
  shared: "personal-your-records-tile-shared",
  stores: "personal-your-records-tile-stores",
} as const;

type YourRecordsTileConfig = {
  identity: "people" | "lent" | "owe" | "stores";
  titleKey: MessageKey;
  footerKey: MessageKey;
  to: string;
  testId: string;
  icon: LucideIcon;
  count: number;
};

function YourRecordsTile({ tile }: { tile: YourRecordsTileConfig }) {
  const { t } = useI18n();
  const Icon = tile.icon;
  return (
    <Link
      to={tile.to}
      className={cn(
        "personal-your-records-tile",
        `personal-your-records-tile--${tile.identity}`,
      )}
      data-testid={tile.testId}
      data-shell="soft-glass"
      data-recipe={`ext soft-glass your-records-tile ${tile.identity}`}
      aria-label={`${t(tile.titleKey)}, ${tile.count}`}
    >
      <span className="personal-your-records-tile__head">
        <span className="personal-your-records-tile__chip" aria-hidden>
          <Icon className="personal-your-records-tile__chip-icon" strokeWidth={2.25} />
        </span>
        <ChevronRight className="personal-your-records-tile__chevron" aria-hidden />
      </span>
      <span className="personal-your-records-tile__title">{t(tile.titleKey)}</span>
      <strong className="personal-your-records-tile__count tabular-nums">{tile.count}</strong>
      <span className="personal-your-records-tile__footer">{t(tile.footerKey)}</span>
    </Link>
  );
}

export function PersonalYourRecordsCard({
  peopleCount,
  myRecordsCount,
  sharedCount,
  storeCount,
}: {
  peopleCount: number;
  myRecordsCount: number;
  sharedCount: number;
  storeCount: number;
}) {
  const { t } = useI18n();

  const tiles: YourRecordsTileConfig[] = [
    {
      identity: "people",
      titleKey: "personal.home.people",
      footerKey: "personal.home.yourRecordsPeopleFooter",
      to: "/personal/people",
      testId: PERSONAL_YOUR_RECORDS_TEST_IDS.people,
      icon: Users,
      count: peopleCount,
    },
    {
      identity: "lent",
      titleKey: "personal.home.yourRecordsMyRecords",
      footerKey: "personal.home.yourRecordsMyRecordsFooter",
      to: "/personal/utang",
      testId: PERSONAL_YOUR_RECORDS_TEST_IDS.myRecords,
      icon: Layers,
      count: myRecordsCount,
    },
    {
      identity: "owe",
      titleKey: "personal.home.sharedLedgerStat",
      footerKey: "personal.home.yourRecordsSharedFooter",
      to: "/personal/utang",
      testId: PERSONAL_YOUR_RECORDS_TEST_IDS.shared,
      icon: Share2,
      count: sharedCount,
    },
    {
      identity: "stores",
      titleKey: "personal.home.stores",
      footerKey: "personal.home.yourRecordsStoresFooter",
      to: "/personal/linked-merchants",
      testId: PERSONAL_YOUR_RECORDS_TEST_IDS.stores,
      icon: Store,
      count: storeCount,
    },
  ];

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section personal-your-records"
      aria-label={t("personal.home.yourRecords")}
      data-testid={PERSONAL_YOUR_RECORDS_TEST_IDS.section}
      data-shell="soft-glass"
    >
      <div className="personal-your-records__header">
        <h2
          className="personal-your-records__title personal-tracker-card__title"
          data-testid={PERSONAL_YOUR_RECORDS_TEST_IDS.ribbon}
        >
          <span
            className="personal-your-records__pennant personal-tracker-card__pennant"
            aria-hidden
            data-testid={PERSONAL_YOUR_RECORDS_TEST_IDS.pennant}
          >
            <Layers className="personal-your-records__pennant-icon personal-tracker-card__pennant-icon size-[1.05rem] shrink-0" />
          </span>
          {t("personal.home.yourRecords")}
        </h2>
      </div>
      <div className="personal-your-records__tiles" data-testid="personal-your-records-tiles">
        {tiles.map((tile) => (
          <YourRecordsTile key={tile.testId} tile={tile} />
        ))}
      </div>
    </section>
  );
}