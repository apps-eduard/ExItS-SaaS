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

const ACTIONS: Array<{ label: string; icon: IconName }> = [
  { label: "Record money lent", icon: "payments" },
  { label: "Record money owed", icon: "request_quote" },
  { label: "Add payment", icon: "credit_card" },
  { label: "Add task", icon: "checklist" },
];

function whenLabel(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(undefined, { month: "short", day: "numeric" }).format(date);
}

function summary(snapshot: HomeSnapshot) {
  if (Object.keys(snapshot.unavailable).length > 0) return "Some of your records could not be loaded.";
  if (snapshot.attention.length === 1) return "1 item needs attention.";
  if (snapshot.attention.length > 1) return `${snapshot.attention.length} items need attention.`;
  return "Nothing needs attention right now.";
}

function Amounts({ totals, empty }: { totals: MoneyTotal[]; empty: string }) {
  if (totals.length === 0) return <p className={styles.amountQuiet}>{empty}</p>;
  return (
    <ul className={styles.amounts}>
      {totals.map((total) => <li key={total.currency}>{formatAmount(total.amount, total.currency)}</li>)}
    </ul>
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

  return (
    <PersonalShell title="Home" name={name} email={email} onLogout={onLogout} onSwitchProfile={onSwitchProfile}>
      <section className={styles.welcome}>
        <p className={styles.kicker}>Personal</p>
        <h2>Hello, {name}</h2>
        <p>{summary(snapshot)}</p>
      </section>

      {failed ? (
        <p className={styles.retry} role="alert">
          {failed}
          {onRetry ? <Button type="button" variant="secondary" appearance="outline" onClick={onRetry}>Retry</Button> : null}
        </p>
      ) : null}

      <section className={styles.overview} aria-label="Financial overview">
        <article className={styles.metric} data-tone="lent">
          <span>Money lent</span>
          <Amounts totals={snapshot.lent} empty="None yet" />
        </article>
        <article className={styles.metric} data-tone="owe">
          <span>Money owed</span>
          <Amounts totals={snapshot.borrowed} empty="None yet" />
        </article>
        <article className={styles.metric} data-tone="stores">
          <span>Stores to pay</span>
          <p className={styles.amountQuiet}>{snapshot.stores.length === 0 ? "None linked" : `${snapshot.stores.length} linked`}</p>
          {snapshot.stores.length > 0 ? <p className={styles.caption}>Amounts are not available for these stores.</p> : null}
        </article>
      </section>

      <section className={styles.panel} aria-labelledby="needs-attention">
        <h2 id="needs-attention">Needs attention</h2>
        {snapshot.attention.length === 0 && snapshot.upcoming.length === 0 ? <p className={styles.caption}>You're up to date.</p> : null}
        <ul className={styles.list}>
          {snapshot.attention.map((item) => <li key={item}>{item}</li>)}
          {snapshot.upcoming.map((item) => <li key={item.id}>{item.label} · {whenLabel(item.when)}</li>)}
        </ul>
      </section>

      <section className={styles.actions} aria-label="Quick actions">
        {ACTIONS.map((action) => (
          <button key={action.label} type="button" className={styles.action} onClick={() => setActionNote(`${action.label} is not available yet. Nothing was saved.`)}>
            <AppIcon name={action.icon} size={20} />
            <span>{action.label}</span>
          </button>
        ))}
      </section>
      {actionNote ? <p className={styles.caption} role="status">{actionNote}</p> : null}

      <div className={styles.split}>
        <section className={styles.panel} aria-labelledby="recent-activity">
          <h2 id="recent-activity">Recent activity</h2>
          {snapshot.activity.length === 0 ? <p className={styles.caption}>No activity yet.</p> : null}
          <ul className={styles.list}>
            {snapshot.activity.map((item) => (
              <li key={item.id}>{item.title}{item.detail ? ` · ${item.detail}` : ""} <span>{whenLabel(item.at)}</span></li>
            ))}
          </ul>
        </section>
        <section className={styles.panel} aria-labelledby="tasks">
          <h2 id="tasks">Tasks</h2>
          {snapshot.tasks.length === 0 ? <p className={styles.caption}>No upcoming tasks.</p> : null}
          <ul className={styles.list}>
            {snapshot.tasks.map((task) => (
              <li key={task.id}>{task.title} <span>{task.dueAtUtc ? whenLabel(task.dueAtUtc) : task.status}</span></li>
            ))}
          </ul>
        </section>
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
    void listAccountProfiles().then((profiles) => {
      if (!active) return;
      setCanSwitchProfile(profiles.some((profile) => profile.accountClass.trim().toLowerCase() === "organization" && profile.status.trim().toLowerCase() === "active"));
    }).catch(() => {
      if (active) setCanSwitchProfile(false);
    });
    return () => {
      active = false;
    };
  }, [session]);

  useEffect(() => {
    let active = true;
    void loadPersonalHome().then((next) => {
      if (active) setSnapshot(next);
    }).catch(async () => {
      if (!active) return;
      await logout();
      window.location.replace(AUTH_PREVIEW_PATH);
    });
    return () => {
      active = false;
    };
  }, [attempt, logout]);

  if (!session) return null;
  if (!snapshot) return <p className={styles.checking} role="status">Loading your home.</p>;

  return (
    <PersonalHome
      name={session.displayName}
      email={session.email}
      snapshot={snapshot}
      onRetry={() => setAttempt((value) => value + 1)}
      onSwitchProfile={canSwitchProfile && !session.organizationContextLocked ? async () => {
        const moved = await switchToAuthorizedOrganizationProfile();
        if (moved) window.location.replace(WORKSPACE_PATH);
        return moved;
      } : undefined}
      onLogout={() => {
        void logout().finally(() => window.location.replace(AUTH_PREVIEW_PATH));
      }}
    />
  );
}
