import { useEffect, useId, useState, type ReactNode } from "react";
import { Button } from "../components/button/Button";
import { SegmentedControl } from "../components/segmented/SegmentedControl";
import { Tooltip } from "../components/tooltip/Tooltip";
import { AppIcon } from "../icons/ExIcon";
import type { IconName } from "../icons/registry";
import { useAppearance } from "../theme/AppearanceProvider";
import type { ThemeMode } from "../theme/appearance";
import styles from "./PersonalShell.module.scss";

const SIDEBAR_KEY = "exits.premium.sidebar";

type NavId = "home" | "utang" | "stores" | "tasks" | "activity" | "settings";

const NAV: Array<{ id: NavId; label: string; icon: IconName; ready: boolean }> = [
  { id: "home", label: "Home", icon: "dashboard", ready: true },
  { id: "utang", label: "Utang", icon: "account_balance_wallet", ready: false },
  { id: "stores", label: "Stores to Pay", icon: "storefront", ready: false },
  { id: "tasks", label: "Tasks", icon: "checklist", ready: false },
  { id: "activity", label: "Activity", icon: "inbox", ready: false },
  { id: "settings", label: "Settings", icon: "tune", ready: true },
];

function initials(name: string) {
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((part) => part[0]?.toUpperCase() ?? "").join("") || "E";
}

export function PersonalShell({
  title,
  name,
  email,
  onLogout,
  children,
}: {
  title: string;
  name: string;
  email: string;
  onLogout: () => void;
  children: ReactNode;
}) {
  const appearance = useAppearance();
  const menuId = useId();
  const settingsId = useId();
  const [collapsed, setCollapsed] = useState(() => window.localStorage.getItem(SIDEBAR_KEY) === "collapsed");
  const [menuOpen, setMenuOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    window.localStorage.setItem(SIDEBAR_KEY, collapsed ? "collapsed" : "expanded");
  }, [collapsed]);

  function choose(item: (typeof NAV)[number]) {
    setDrawerOpen(false);
    if (item.id === "settings") {
      setSettingsOpen(true);
      setNotice(null);
      return;
    }
    if (!item.ready) {
      setNotice(`${item.label} is not available yet.`);
      return;
    }
    setNotice(null);
    setSettingsOpen(false);
  }

  const navButtons = (compact: boolean) =>
    NAV.map((item) => (
      <button
        key={item.id}
        type="button"
        className={styles.navItem}
        aria-current={item.id === "home" && !settingsOpen ? "page" : undefined}
        aria-label={compact ? item.label : undefined}
        title={compact ? item.label : undefined}
        onClick={() => choose(item)}
      >
        <AppIcon name={item.icon} size={22} />
        {compact ? null : <span>{item.label}</span>}
      </button>
    ));

  return (
    <div className={styles.shell} data-collapsed={collapsed ? "true" : "false"}>
      <aside className={styles.sidebar} aria-label="Personal">
        <a className={styles.brand} href="/personal/home">
          <span className={styles.mark} aria-hidden="true">E</span>
          <span className={styles.brandWord}>ExItS</span>
        </a>
        <nav className={styles.nav}>{navButtons(collapsed)}</nav>
        <button
          type="button"
          className={styles.collapse}
          aria-pressed={collapsed}
          onClick={() => setCollapsed((value) => !value)}
        >
          <AppIcon name={collapsed ? "left_panel_open" : "left_panel_close"} size={22} />
          <span>{collapsed ? "Expand sidebar" : "Collapse sidebar"}</span>
        </button>
      </aside>

      <div className={styles.body}>
        <header className={styles.topbar}>
          <button type="button" className={styles.menuButton} aria-label="Open navigation" aria-expanded={drawerOpen} onClick={() => setDrawerOpen(true)}>
            <AppIcon name="menu" size={22} />
          </button>
          <h1 className={styles.pageTitle}>{title}</h1>
          <div className={styles.actions}>
            <Tooltip content="Design System">
              <a className={styles.designSystem} href="/design-system" aria-label="Design System">
                <AppIcon name="widgets" size={22} />
              </a>
            </Tooltip>
            <div className={styles.profile}>
            <button
              type="button"
              className={styles.avatar}
              aria-expanded={menuOpen}
              aria-controls={menuId}
              aria-label={`Account menu for ${name}`}
              onClick={() => setMenuOpen((open) => !open)}
            >
              <span aria-hidden="true">{initials(name)}</span>
            </button>
            {menuOpen ? (
              <div className={styles.menu} id={menuId} role="menu">
                <p className={styles.menuName}>{name}</p>
                <p className={styles.menuEmail}>{email}</p>
                <Button type="button" variant="secondary" appearance="outline" onClick={() => { setMenuOpen(false); setSettingsOpen(true); }}>
                  Appearance
                </Button>
                <Button type="button" variant="danger" appearance="outline" onClick={onLogout}>
                  Log out
                </Button>
              </div>
            ) : null}
            </div>
          </div>
        </header>
        <div className={styles.content}>
          {notice ? <p className={styles.notice} role="status">{notice}</p> : null}
          {settingsOpen ? (
            <section className={styles.settings} id={settingsId} aria-label="Settings">
              <div className={styles.settingsHead}>
                <h2>Settings</h2>
                <Button type="button" variant="ghost" onClick={() => setSettingsOpen(false)}>Close</Button>
              </div>
              <SegmentedControl
                label="Appearance"
                name="personal-theme"
                value={appearance.theme}
                options={[
                  { value: "light", label: "Light" },
                  { value: "dark", label: "Dark" },
                  { value: "system", label: "System" },
                ]}
                onChange={(theme) => appearance.setTheme(theme as ThemeMode)}
              />
            </section>
          ) : children}
        </div>
      </div>

      {drawerOpen ? (
        <div className={styles.drawerLayer}>
          <button type="button" className={styles.scrim} aria-label="Close navigation" onClick={() => setDrawerOpen(false)} />
          <div className={styles.drawer} role="dialog" aria-label="Navigation">
            <nav>{navButtons(false)}</nav>
          </div>
        </div>
      ) : null}

      <nav className={styles.bottom} aria-label="Personal mobile">
        {NAV.filter((item) => item.id !== "settings").map((item) => (
          <button key={item.id} type="button" className={styles.bottomItem} aria-current={item.id === "home" && !settingsOpen ? "page" : undefined} onClick={() => choose(item)}>
            <AppIcon name={item.icon} size={22} />
            <span>{item.id === "stores" ? "Stores" : item.label}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}
