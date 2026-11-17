import { useEffect, useState } from "react";
import { listAccountProfiles } from "../auth/platform-client";
import { switchToAuthorizedOrganizationProfile } from "../auth/profile-preference";
import { useSession } from "../auth/SessionProvider";
import { AUTH_PREVIEW_PATH, WORKSPACE_PATH } from "../auth/model";
import { Button } from "../components/button/Button";
import { AppIcon } from "../icons/ExIcon";
import type { IconName } from "../icons/registry";
import { formatAmount, loadPersonalHome, type HomeSnapshot, type MoneyTotal } from "./home-data";
import { PersonalShell } from "./PersonalShell";
import styles from "./PersonalHome.module.scss";

const ACTIONS: Array<{
  label: string;
  icon: IconName;
  tone: "lent" | "owe" | "stores" | "reminders";
  ready: boolean;
}> = [
  { label: "I Lent", icon: "arrow_upward", tone: "lent", ready: false },
  { label: "I Owe", icon: "arrow_downward", tone: "owe", ready: false },
  { label: "Stores", icon: "storefront", tone: "stores", ready: false },
  { label: "Tasks", icon: "checklist", tone: "reminders", ready: false },
];

function whenLabel(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(date);
}

function greetingFor(name: string, now = new Date()) {
  const hour = now.getHours();
  if (hour < 12) return `Good morning, ${name}`;
  if (hour < 18) return `Good afternoon, ${name}`;
  return `Good evening, ${name}`;
}

function summary(snapshot: HomeSnapshot) {
  if (Object.keys(snapshot.unavailable).length > 0) return "Some of your records could not be loaded.";
  if (snapshot.attention.length === 1) return "1 item needs attention.";
  if (snapshot.attention.length > 1) return `${snapshot.attention.length} items need attention.`;
  return "Nothing needs attention right now.";
}

function activityIcon(title: string): IconName {
  const value = title.toLowerCase();
  if (value.includes("lent")) return "arrow_upward";
  if (value.includes("owed") || value.includes("owe")) return "arrow_downward";
  if (value.includes("store") || value.includes("payment")) return "storefront";
  if (value.includes("task") || value.includes("to-do") || value.includes("todo")) return "checklist";
  return "schedule";
}

function Amounts({ totals }: { totals: MoneyTotal[] }) {
  if (totals.length === 0) {
    return <p className={styles.amountEmpty}>No records</p>;
  }
  return (
    <ul className={styles.amounts}>
      {totals.map((total) => (
        <li key={total.currency}>{formatAmount(total.amount, total.currency)}</li>
      ))}
    </ul>
  );
}

function MetricTile({
  label,
  tone,
  icon,
  totals,
}: {
  label: string;
  tone: "lent" | "owe";
  icon: IconName;
  totals: MoneyTotal[];
}) {
  return (
    <article className={styles.metric} data-tone={tone} data-empty={totals.length === 0 ? "true" : undefined}>
      <div className={styles.metricHead}>
        <span className={styles.metricIcon} aria-hidden="true">
          <AppIcon name={icon} size={20} />
        </span>
        <span className={styles.metricLabel}>{label}</span>
      </div>
      <Amounts totals={totals} />
    </article>
  );
}

function SectionTitle({
  id,
  icon,
  children,
  tone,
}: {
  id?: string;
  icon: IconName;
  children: string;
  tone?: "warning" | "lent" | "owe" | "stores" | "people" | "reminders" | "neutral";
}) {
  return (
    <div className={styles.sectionHead}>
      <span className={styles.sectionMark} data-tone={tone ?? "neutral"} aria-hidden="true">
        <AppIcon name={icon} size={22} />
      </span>
      <h2 id={id}>{children}</h2>
    </div>
  );
}

export function PersonalHome({
  name,
  email,
  snapshot,
  onLogout,
  onSwitchProfile,
  onRetry,
}: {
  name: string;
  email: string;
  snapshot: HomeSnapshot;
  onLogout: () => void;
  onSwitchProfile?: () => Promise<boolean>;
  onRetry?: () => void;
}) {
  const [actionNote, setActionNote] = useState<string | null>(null);
  const failed = Object.values(snapshot.unavailable)[0];
  const attentionEmpty = snapshot.attention.length === 0 && snapshot.upcoming.length === 0;

  return (
    <PersonalShell title="Home" name={name} email={email} onLogout={onLogout} onSwitchProfile={onSwitchProfile}>
      <div className={styles.home}>
        <section className={styles.welcome} aria-labelledby="personal-home-greeting">
          <p className={styles.welcomeEyebrow}>Personal Home</p>
          <h2 id="personal-home-greeting">{greetingFor(name)}</h2>
          <p className={styles.welcomeSummary}>{summary(snapshot)}</p>
        </section>

        {failed ? (
          <p className={styles.retry} role="alert">
            {failed}
            {onRetry ? (
              <Button type="button" variant="secondary" appearance="outline" onClick={onRetry}>
                Retry
              </Button>
            ) : null}
          </p>
        ) : null}

        <div className={styles.layout}>
          <div className={styles.main}>
            <section className={styles.tracker} aria-labelledby="personal-tracker">
              <SectionTitle id="personal-tracker" icon="account_balance_wallet" tone="lent">
                Personal Tracker
              </SectionTitle>

              <div className={styles.ownershipGroups}>
                <section className={styles.ownershipGroup} aria-labelledby="my-records" data-ownership="mine">
                  <div className={styles.ownershipHead}>
                    <span className={styles.ownershipIcon} aria-hidden="true">
                      <AppIcon name="person" size={20} />
                    </span>
                    <div>
                      <h3 id="my-records">My Records</h3>
                      <p className={styles.ownershipHint}>Records you own, including ones you shared.</p>
                    </div>
                  </div>
                  <div className={styles.metricGrid}>
                    <MetricTile label="I Owe" tone="owe" icon="arrow_downward" totals={snapshot.borrowed} />
                    <MetricTile label="I Lent" tone="lent" icon="arrow_upward" totals={snapshot.lent} />
                  </div>
                </section>

                <section className={styles.ownershipGroup} aria-labelledby="shared-with-me" data-ownership="shared">
                  <div className={styles.ownershipHead}>
                    <span className={styles.ownershipIcon} aria-hidden="true">
                      <AppIcon name="group" size={20} />
                    </span>
                    <div>
                      <div className={styles.ownershipTitleRow}>
                        <h3 id="shared-with-me">Shared with Me</h3>
                        {snapshot.sharedBorrowed.length > 0 || snapshot.sharedLent.length > 0 ? (
                          <span className={styles.ownershipBadge}>Read only</span>
                        ) : null}
                      </div>
                      <p className={styles.ownershipHint}>Owned by someone else · read only.</p>
                    </div>
                  </div>
                  <div className={styles.metricGrid}>
                    <MetricTile label="I Owe" tone="owe" icon="arrow_downward" totals={snapshot.sharedBorrowed} />
                    <MetricTile label="I Lent" tone="lent" icon="arrow_upward" totals={snapshot.sharedLent} />
                  </div>
                </section>
              </div>
            </section>

            <section
              className={styles.panel}
              data-slot="stores"
              data-empty={snapshot.stores.length === 0 ? "true" : undefined}
              aria-labelledby="stores-to-pay"
            >
              <div className={styles.storesHead}>
                <SectionTitle id="stores-to-pay" icon="storefront" tone="stores">
                  Stores to Pay
                </SectionTitle>
                {snapshot.stores.length === 0 ? (
                  <p className={styles.storesEmpty}>No stores linked</p>
                ) : (
                  <p className={styles.storesCount}>
                    {snapshot.stores.length} linked
                  </p>
                )}
              </div>
              {snapshot.stores.length > 0 ? (
                <>
                  <ul className={styles.storeList}>
                    {snapshot.stores.map((store) => (
                      <li key={store.id} className={styles.storeRow}>
                        <span className={styles.rowIcon} data-tone="stores" aria-hidden="true">
                          <AppIcon name="storefront" size={18} />
                        </span>
                        <p className={styles.rowTitle}>{store.name}</p>
                      </li>
                    ))}
                  </ul>
                  <p className={styles.caption}>Outstanding amounts are not available for these stores yet.</p>
                </>
              ) : null}
            </section>

            <section className={styles.panel} data-slot="activity" aria-labelledby="recent-activity">
              <SectionTitle id="recent-activity" icon="schedule" tone="neutral">
                Recent Activity
              </SectionTitle>
              {snapshot.activity.length === 0 ? <p className={styles.empty}>No activity yet.</p> : null}
              <ul className={styles.feed}>
                {snapshot.activity.map((item) => (
                  <li key={item.id} className={styles.feedItem}>
                    <span className={styles.rowIcon} aria-hidden="true">
                      <AppIcon name={activityIcon(item.title)} size={18} />
                    </span>
                    <div className={styles.rowBody}>
                      <p className={styles.rowTitle}>{item.title}</p>
                      {item.detail ? <p className={styles.rowMeta}>{item.detail}</p> : null}
                    </div>
                    <time className={styles.rowDate} dateTime={item.at}>
                      {whenLabel(item.at)}
                    </time>
                  </li>
                ))}
              </ul>
            </section>
          </div>

          <aside className={styles.aside} aria-label="Home sidebar">
            <section
              className={styles.attention}
              data-slot="attention"
              data-empty={attentionEmpty ? "true" : "false"}
              aria-labelledby="needs-attention"
            >
              <SectionTitle id="needs-attention" icon="notifications" tone="warning">
                Needs Attention
              </SectionTitle>
              {attentionEmpty ? <p className={styles.empty}>You're up to date.</p> : null}
              <ul className={styles.attentionList}>
                {snapshot.attention.map((item) => (
                  <li key={item} className={styles.attentionItem}>
                    <span className={styles.attentionMark} aria-hidden="true">
                      <AppIcon name="warning" size={18} />
                    </span>
                    <p className={styles.rowTitle}>{item}</p>
                  </li>
                ))}
                {snapshot.upcoming.map((item) => (
                  <li key={item.id} className={styles.attentionItem}>
                    <span className={styles.attentionMark} aria-hidden="true">
                      <AppIcon name="schedule" size={18} />
                    </span>
                    <div className={styles.rowBody}>
                      <p className={styles.rowTitle}>{item.label}</p>
                      <p className={styles.rowMeta}>Due {whenLabel(item.when)}</p>
                    </div>
                  </li>
                ))}
              </ul>
            </section>

            <section className={styles.actions} data-slot="actions" aria-label="Quick actions">
              <SectionTitle icon="bolt" tone="neutral">
                Quick Actions
              </SectionTitle>
              <div className={styles.actionGrid}>
                {ACTIONS.map((action) => (
                  <button
                    key={action.label}
                    type="button"
                    className={styles.action}
                    data-tone={action.tone}
                    onClick={() =>
                      setActionNote(
                        action.ready
                          ? null
                          : `${action.label} is not available yet. Nothing was saved.`,
                      )
                    }
                  >
                    <span className={styles.actionIcon} aria-hidden="true">
                      <AppIcon name={action.icon} size={20} />
                    </span>
                    <span className={styles.actionCopy}>
                      <span className={styles.actionLabel}>{action.label}</span>
                      <span className={styles.actionHint}>{action.ready ? "Open" : "Coming soon"}</span>
                    </span>
                  </button>
                ))}
              </div>
              {actionNote ? (
                <p className={styles.actionNote} role="status">
                  {actionNote}
                </p>
              ) : null}
            </section>

            <section className={styles.panel} data-slot="tasks" aria-labelledby="tasks">
              <SectionTitle id="tasks" icon="checklist" tone="reminders">
                Tasks
              </SectionTitle>
              {snapshot.tasks.length === 0 ? <p className={styles.empty}>No upcoming tasks.</p> : null}
              <ul className={styles.feed}>
                {snapshot.tasks.map((task) => (
                  <li key={task.id} className={styles.feedItem}>
                    <span className={styles.rowIcon} data-tone="reminders" aria-hidden="true">
                      <AppIcon name="check_box" size={18} />
                    </span>
                    <div className={styles.rowBody}>
                      <p className={styles.rowTitle}>{task.title}</p>
                      <p className={styles.rowMeta}>{task.status}</p>
                    </div>
                    <span className={styles.rowDate}>
                      {task.dueAtUtc ? whenLabel(task.dueAtUtc) : "No due date"}
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          </aside>
        </div>
      </div>
    </PersonalShell>
  );
}

export function PersonalHomeScreen() {
  const { session, logout } = useSession();
  const [snapshot, setSnapshot] = useState<HomeSnapshot | null>(null);
  const [attempt, setAttempt] = useState(0);
  const [canSwitchProfile, setCanSwitchProfile] = useState(false);

  useEffect(() => {
    if (!session || session.organizationContextLocked) return;
    let active = true;
    void listAccountProfiles()
      .then((profiles) => {
        if (!active) return;
        setCanSwitchProfile(
          profiles.some(
            (profile) =>
              profile.accountClass.trim().toLowerCase() === "organization" &&
              profile.status.trim().toLowerCase() === "active",
          ),
        );
      })
      .catch(() => {
        if (active) setCanSwitchProfile(false);
      });
    return () => {
      active = false;
    };
  }, [session]);

  useEffect(() => {
    let active = true;
    void loadPersonalHome()
      .then((next) => {
        if (active) setSnapshot(next);
      })
      .catch(async () => {
        if (!active) return;
        await logout();
        window.location.replace(AUTH_PREVIEW_PATH);
      });
    return () => {
      active = false;
    };
  }, [attempt, logout]);

  if (!session) return null;
  if (!snapshot) {
    return (
      <p className={styles.checking} role="status">
        Loading your home.
      </p>
    );
  }

  return (
    <PersonalHome
      name={session.displayName}
      email={session.email}
      snapshot={snapshot}
      onRetry={() => setAttempt((value) => value + 1)}
      onSwitchProfile={
        canSwitchProfile && !session.organizationContextLocked
          ? async () => {
              const moved = await switchToAuthorizedOrganizationProfile();
              if (moved) window.location.replace(WORKSPACE_PATH);
              return moved;
            }
          : undefined
      }
      onLogout={() => {
        void logout().finally(() => window.location.replace(AUTH_PREVIEW_PATH));
      }}
    />
  );
}
