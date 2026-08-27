import { useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowLeftRight,
  Bell,
  BellRing,
  Building2,
  Compass,
  CreditCard,
  Download,
  Handshake,
  Link2,
  ListTodo,
  QrCode,
  Receipt,
  ScanLine,
  Settings,
  ShoppingCart,
  Store,
  UserPen,
  UserPlus,
  Users,
  Wallet,
  type LucideIcon,
} from "lucide-react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { Button } from "@/components/ui/button";
import { InstallExitsOffer } from "@/features/store/InstallExitsOffer";
import {
  PERSONAL_GUIDE_CATEGORIES,
  PERSONAL_GUIDE_CATEGORY_TITLE_KEYS,
  PERSONAL_GUIDE_FEATURES,
  personalGuideFeaturesByCategory,
  type PersonalGuideCategory,
  type PersonalGuideFeature,
} from "@/features/personal/guide/personal-guide-features";
import { usePersonalGuideProgress } from "@/features/personal/guide/use-personal-guide-progress";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { useSession } from "@/session/SessionProvider";
import { cn } from "@/lib/cn";

const FEATURE_ICONS: Record<string, LucideIcon> = {
  profile: UserPen,
  "personal-qr": QrCode,
  "account-security": Settings,
  "install-pwa": Download,
  people: Users,
  invitations: UserPlus,
  "customer-links": Link2,
  utang: Wallet,
  "utang-settlement": Handshake,
  todo: ListTodo,
  "todo-reminders": BellRing,
  stores: Store,
  "business-qr": ScanLine,
  "shopping-cart": ShoppingCart,
  checkout: CreditCard,
  "my-orders": Receipt,
  notifications: Bell,
  "start-business": Building2,
  "business-switching": ArrowLeftRight,
  "ownership-transfer": ArrowLeftRight,
};

type GuideFilter = "all" | "not-explored" | "completed";

function FeatureCard({
  feature,
  learned,
  opened,
  expanded,
  onToggleExpand,
  onMarkLearned,
}: {
  feature: PersonalGuideFeature;
  learned: boolean;
  opened: boolean;
  expanded: boolean;
  onToggleExpand: () => void;
  onMarkLearned: (learned: boolean) => void;
}) {
  const { t } = useI18n();
  const Icon = FEATURE_ICONS[feature.code] ?? Compass;
  const panelId = `guide-feature-panel-${feature.code}`;
  const titleId = `guide-feature-title-${feature.code}`;
  const switchId = `guide-feature-learned-${feature.code}`;
  const stateLabel = learned
    ? t("personal.guide.stateLearned")
    : opened
      ? t("personal.guide.stateInProgress")
      : t("personal.guide.stateNotExplored");

  return (
    <article
      className={cn(
        "personal-guide-card rounded-[var(--exits-radius-md)] border border-border bg-surface",
        learned && "personal-guide-card--learned",
      )}
      data-testid={`guide-card-${feature.code}`}
      data-learned={learned ? "true" : "false"}
    >
      <button
        type="button"
        className="personal-guide-card__header flex min-h-11 w-full items-start gap-3 px-3 py-3 text-left"
        aria-expanded={expanded}
        aria-controls={panelId}
        data-testid={`guide-card-toggle-${feature.code}`}
        onClick={onToggleExpand}
      >
        <span
          className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)]"
          aria-hidden
        >
          <Icon className="size-4" />
        </span>
        <span className="min-w-0 flex-1">
          <span id={titleId} className="block text-[length:var(--exits-text-sm)] font-semibold">
            {t(feature.titleKey)}
          </span>
          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
            {t(feature.descriptionKey)}
          </span>
          <span
            className="personal-guide-card__state mt-1 inline-block text-[length:var(--exits-text-xs)] font-medium text-muted"
            data-testid={`guide-card-state-${feature.code}`}
          >
            {stateLabel}
          </span>
        </span>
        <span className="sr-only">
          {expanded ? t("personal.guide.collapse") : t("personal.guide.expand")}
        </span>
      </button>

      <div id={panelId} hidden={!expanded} className="border-t border-border px-3 py-3">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("personal.guide.whatYouCanDo")}
        </p>
        <ul className="mt-2 mb-0 list-disc space-y-1 pl-5 text-[length:var(--exits-text-sm)]">
          {feature.bulletKeys.map((key) => (
            <li key={key}>{t(key)}</li>
          ))}
        </ul>
        {feature.availabilityNoteKey ? (
          <p className="mt-2 mb-0 text-[length:var(--exits-text-xs)] text-muted">
            {t(feature.availabilityNoteKey)}
          </p>
        ) : null}
        {feature.code === "install-pwa" ? (
          <div className="mt-3">
            <InstallExitsOffer />
          </div>
        ) : null}
        <div className="mt-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <Button asChild className="min-h-11 w-full sm:w-auto" data-testid={`guide-try-${feature.code}`}>
            <Link to={feature.route}>{t("personal.guide.tryIt")}</Link>
          </Button>
          <label
            htmlFor={switchId}
            className="flex min-h-11 cursor-pointer items-center gap-2 text-[length:var(--exits-text-sm)]"
          >
            <input
              id={switchId}
              type="checkbox"
              role="switch"
              className="size-5 accent-[var(--exits-primary)]"
              checked={learned}
              data-testid={`guide-learned-${feature.code}`}
              aria-labelledby={titleId}
              onChange={(event) => onMarkLearned(event.target.checked)}
            />
            {t("personal.guide.markLearned")}
          </label>
        </div>
      </div>
    </article>
  );
}

function CategorySection({
  category,
  features,
  learnedCodes,
  openedCodes,
  expandedCodes,
  onToggleExpand,
  onMarkLearned,
}: {
  category: PersonalGuideCategory;
  features: PersonalGuideFeature[];
  learnedCodes: readonly string[];
  openedCodes: ReadonlySet<string>;
  expandedCodes: ReadonlySet<string>;
  onToggleExpand: (code: string) => void;
  onMarkLearned: (code: string, learned: boolean) => void;
}) {
  const { t } = useI18n();
  const [open, setOpen] = useState(true);
  const headingId = `guide-category-${category}`;
  const panelId = `guide-category-panel-${category}`;

  if (features.length === 0) {
    return null;
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section gap-2"
      data-testid={`guide-category-${category}`}
      aria-labelledby={headingId}
    >
      <button
        type="button"
        id={headingId}
        className="catalog-form-section__title m-0 flex min-h-11 w-full items-center justify-between gap-2 text-left text-muted"
        aria-expanded={open}
        aria-controls={panelId}
        data-testid={`guide-category-toggle-${category}`}
        onClick={() => setOpen((value) => !value)}
      >
        <span>{t(PERSONAL_GUIDE_CATEGORY_TITLE_KEYS[category])}</span>
        <span className="text-[length:var(--exits-text-xs)] font-medium tabular-nums">
          {features.filter((feature) => learnedCodes.includes(feature.code)).length}/{features.length}
        </span>
      </button>
      <div id={panelId} hidden={!open} className="grid gap-2">
        {features.map((feature) => (
          <FeatureCard
            key={feature.code}
            feature={feature}
            learned={learnedCodes.includes(feature.code)}
            opened={openedCodes.has(feature.code)}
            expanded={expandedCodes.has(feature.code)}
            onToggleExpand={() => onToggleExpand(feature.code)}
            onMarkLearned={(learned) => onMarkLearned(feature.code, learned)}
          />
        ))}
      </div>
    </section>
  );
}

export function PersonalGuidePage() {
  const { t } = useI18n();
  const { session } = useSession();
  const accountKey = session?.userId?.trim() || null;
  const guide = usePersonalGuideProgress(accountKey);
  const [filter, setFilter] = useState<GuideFilter>("all");
  const [openedCodes, setOpenedCodes] = useState<Set<string>>(() => new Set());
  const [expandedCodes, setExpandedCodes] = useState<Set<string>>(() => new Set());

  function toggleExpand(code: string) {
    setOpenedCodes((current) => {
      const next = new Set(current);
      next.add(code);
      return next;
    });
    setExpandedCodes((current) => {
      const next = new Set(current);
      if (next.has(code)) {
        next.delete(code);
      } else {
        next.add(code);
      }
      return next;
    });
  }

  const visibleFeatures = PERSONAL_GUIDE_FEATURES.filter((feature) => {
    const learned = guide.isLearned(feature.code);
    if (filter === "completed") {
      return learned;
    }
    if (filter === "not-explored") {
      return !learned;
    }
    return true;
  });

  return (
    <div
      className="personal-page personal-guide-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-guide-page"
    >
      <PageHeader
        title={t("personal.guide.title")}
        titleIcon={Compass}
        description={t("personal.guide.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-guide"
      />

      <section
        className="catalog-form-section exits-animate-panel personal-section gap-2"
        data-testid="guide-progress"
        aria-label={t("personal.guide.progress")
          .replace("{explored}", String(guide.explored))
          .replace("{total}", String(guide.total))}
      >
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold" data-testid="guide-progress-text">
          {t("personal.guide.progress")
            .replace("{explored}", String(guide.explored))
            .replace("{total}", String(guide.total))}
        </p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted" data-testid="guide-progress-percent">
          {t("personal.guide.percent").replace("{percent}", String(guide.percent))}
        </p>
        <div
          className="personal-guide-progress h-2 overflow-hidden rounded-full bg-[var(--exits-surface-muted)]"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={guide.total}
          aria-valuenow={guide.explored}
          aria-label={t("personal.guide.progress")
            .replace("{explored}", String(guide.explored))
            .replace("{total}", String(guide.total))}
        >
          <div
            className="h-full rounded-full bg-primary"
            style={{ width: `${guide.percent}%` }}
            data-testid="guide-progress-bar"
          />
        </div>
      </section>

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("personal.guide.filterAria")}
        testId="guide-filters"
        items={[
          {
            key: "all",
            label: t("personal.guide.filterAll"),
            state: filter === "all" ? "active" : "idle",
            testId: "guide-filter-all",
            onSelect: () => setFilter("all"),
          },
          {
            key: "not-explored",
            label: t("personal.guide.filterNotExplored"),
            state: filter === "not-explored" ? "active" : "idle",
            testId: "guide-filter-not-explored",
            onSelect: () => setFilter("not-explored"),
          },
          {
            key: "completed",
            label: t("personal.guide.filterCompleted"),
            state: filter === "completed" ? "active" : "idle",
            testId: "guide-filter-completed",
            onSelect: () => setFilter("completed"),
          },
        ]}
      />

      {visibleFeatures.length === 0 ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="guide-empty-filter">
          {t("personal.guide.emptyFilter")}
        </p>
      ) : (
        PERSONAL_GUIDE_CATEGORIES.map((category) => (
          <CategorySection
            key={category}
            category={category}
            features={personalGuideFeaturesByCategory(category).filter((feature) =>
              visibleFeatures.some((row) => row.code === feature.code),
            )}
            learnedCodes={guide.learnedCodes}
            openedCodes={openedCodes}
            expandedCodes={expandedCodes}
            onToggleExpand={toggleExpand}
            onMarkLearned={guide.setLearned}
          />
        ))
      )}

      <label className="flex min-h-11 cursor-pointer items-center gap-2 text-[length:var(--exits-text-sm)]">
        <input
          type="checkbox"
          role="switch"
          className="size-5 accent-[var(--exits-primary)]"
          checked={!guide.homeCardDismissed}
          data-testid="guide-show-home-card"
          onChange={(event) => guide.setHomeCardDismissed(!event.target.checked)}
        />
        {t("personal.guide.showHomeCard")}
      </label>
    </div>
  );
}
