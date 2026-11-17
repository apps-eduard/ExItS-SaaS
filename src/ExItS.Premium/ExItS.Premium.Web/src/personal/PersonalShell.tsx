import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import { Button } from "../components/button/Button";
import { SegmentedControl } from "../components/segmented/SegmentedControl";
import { Tooltip } from "../components/tooltip/Tooltip";
import { AppIcon } from "../icons/ExIcon";
import type { IconName } from "../icons/registry";
import { useAppearance } from "../theme/AppearanceProvider";
import type { ThemeMode } from "../theme/appearance";
import styles from "./PersonalShell.module.scss";

const THEME_OPTIONS: Array<{ value: ThemeMode; label: string }> = [
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
  { value: "system", label: "System" },
];

const SIDEBAR_KEY = "exits.premium.sidebar";

type NavId = "home" | "utang" | "stores" | "tasks" | "activity" | "settings";

const PRIMARY_NAV: Array<{ id: NavId; label: string; icon: IconName; ready: boolean }> = [
  { id: "home", label: "Home", icon: "home", ready: true },
  { id: "utang", label: "Utang", icon: "account_balance_wallet", ready: false },
  { id: "stores", label: "Stores to Pay", icon: "storefront", ready: false },
  { id: "tasks", label: "Tasks", icon: "checklist", ready: false },
  { id: "activity", label: "Activity", icon: "schedule", ready: false },
];

const SETTINGS_ITEM = { id: "settings" as const, label: "Settings", icon: "tune" as const, ready: true };

function initials(name: string) {
  const parts = name.trim().split(/\s+/).slice(0, 2);
  return parts.map((part) => part[0]?.toUpperCase() ?? "").join("") || "E";
}

export function PersonalShell({
  title,
  name,
  email,
  onLogout,
  onSwitchProfile,
  children,
}: {
  title: string;
  name: string;
  email: string;
  onLogout: () => void;
  onSwitchProfile?: () => Promise<boolean>;
  children: ReactNode;
}) {
  const appearance = useAppearance();
  const menuId = useId();
  const settingsId = useId();
  const navId = useId();
  const themeGroupId = useId();
  const avatarRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const [collapsed, setCollapsed] = useState(() => window.localStorage.getItem(SIDEBAR_KEY) === "collapsed");
  const [menuOpen, setMenuOpen] = useState(false);
  const [themeOpen, setThemeOpen] = useState(false);
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    window.localStorage.setItem(SIDEBAR_KEY, collapsed ? "collapsed" : "expanded");
  }, [collapsed]);

  function closeMenu(restoreFocus = false) {
    setMenuOpen(false);
    setThemeOpen(false);
    if (restoreFocus) avatarRef.current?.focus();
  }

  useEffect(() => {
    if (!menuOpen) return;

    function onPointerDown(event: PointerEvent) {
      const target = event.target;
      if (!(target instanceof Node)) return;
      if (menuRef.current?.contains(target) || avatarRef.current?.contains(target)) return;
      closeMenu();
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      closeMenu(true);
    }

    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [menuOpen]);

  function choose(item: { id: NavId; label: string; ready: boolean }) {
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

  function renderNavItem(item: { id: NavId; label: string; icon: IconName; ready: boolean }, compact: boolean) {
    const active = (item.id === "home" && !settingsOpen) || (item.id === "settings" && settingsOpen);
    const button = (
      <button
        type="button"
        className={styles.navItem}
        data-active={active ? "true" : undefined}
        aria-current={active ? "page" : undefined}
        aria-label={item.label}
        onClick={() => choose(item)}
      >
        <AppIcon name={item.icon} size={22} />
        <span className={styles.navLabel}>{item.label}</span>
      </button>
    );

    if (!compact) return <div key={item.id}>{button}</div>;
    return (
      <div key={item.id} className={styles.tipWrap}>
        <Tooltip content={item.label} placement="end">
          {button}
        </Tooltip>
      </div>
    );
  }

  const collapseLabel = collapsed ? "Expand sidebar" : "Collapse sidebar";
  const pageTitle = settingsOpen ? "Settings" : title;

  return (
    <div className={styles.shell} data-collapsed={collapsed ? "true" : "false"}>
      <aside className={styles.sidebar} aria-label="Personal">
        <div className={styles.sidebarHead}>
          <a className={styles.brand} href="/personal/home" aria-label="ExItS Personal home">
            <span className={styles.mark} aria-hidden="true">
              E
            </span>
            <span className={styles.brandWord}>ExItS</span>
          </a>
          <Tooltip content={collapseLabel} placement={collapsed ? "end" : "top"}>
            <button
              type="button"
              className={styles.collapse}
              aria-expanded={!collapsed}
              aria-controls={navId}
              aria-label={collapseLabel}
              onClick={() => setCollapsed((value) => !value)}
            >
              <AppIcon name={collapsed ? "left_panel_open" : "left_panel_close"} size={20} />
            </button>
          </Tooltip>
        </div>

        <nav className={styles.nav} id={navId}>
          {PRIMARY_NAV.map((item) => renderNavItem(item, collapsed))}
        </nav>

        <div className={styles.sidebarFoot}>{renderNavItem(SETTINGS_ITEM, collapsed)}</div>
      </aside>

      <div className={styles.body}>
        <header className={styles.topbar}>
          <button
            type="button"
            className={styles.menuButton}
            aria-label="Open navigation"
            aria-expanded={drawerOpen}
            onClick={() => setDrawerOpen(true)}
          >
            <AppIcon name="menu" size={22} />
          </button>
          <h1 className={styles.pageTitle}>{pageTitle}</h1>
          <div className={styles.actions}>
            <Tooltip content={appearance.resolvedTheme === "dark" ? "Switch to light" : "Switch to dark"}>
              <Button
                type="button"
                variant="ghost"
                iconOnly
                className={styles.themeToggle}
                aria-label={appearance.resolvedTheme === "dark" ? "Switch to light mode" : "Switch to dark mode"}
                aria-pressed={appearance.resolvedTheme === "dark"}
                icon={<AppIcon name={appearance.resolvedTheme === "dark" ? "light_mode" : "dark_mode"} size={20} />}
                onClick={() => appearance.setTheme(appearance.resolvedTheme === "dark" ? "light" : "dark")}
              />
            </Tooltip>
            <Tooltip content="Design System">
              <a className={styles.designSystem} href="/design-system" aria-label="Design System">
                <AppIcon name="widgets" size={20} />
              </a>
            </Tooltip>
            <div className={styles.profile}>
              <button
                ref={avatarRef}
                type="button"
                className={styles.avatar}
                aria-expanded={menuOpen}
                aria-haspopup="menu"
                aria-controls={menuOpen ? menuId : undefined}
                aria-label={`Account menu for ${name}`}
                onClick={() => {
                  if (menuOpen) closeMenu();
                  else setMenuOpen(true);
                }}
              >
                <span aria-hidden="true">{initials(name)}</span>
              </button>
              {menuOpen ? (
                <div
                  ref={menuRef}
                  className={styles.menu}
                  id={menuId}
                  role="menu"
                  aria-label={`Account menu for ${name}`}
                >
                  <div className={styles.menuIdentity}>
                    <p className={styles.menuName}>{name}</p>
                    <p className={styles.menuKind}>Personal</p>
                    <p className={styles.menuEmail}>{email}</p>
                  </div>

                  <div className={styles.menuDivider} role="separator" />

                  {onSwitchProfile ? (
                    <>
                      <button
                        type="button"
                        className={styles.menuItem}
                        role="menuitem"
                        onClick={() => {
                          closeMenu();
                          void onSwitchProfile().then((moved) => {
                            if (!moved) setNotice("That account profile is not available.");
                          });
                        }}
                      >
                        <AppIcon name="apartment" size={20} />
                        <span className={styles.menuItemLabel}>Switch profile</span>
                        <AppIcon name="chevron_right" size={18} />
                      </button>
                      <div className={styles.menuDivider} role="separator" />
                    </>
                  ) : null}

                  <button
                    type="button"
                    className={styles.menuItem}
                    role="menuitem"
                    aria-expanded={themeOpen}
                    aria-controls={themeGroupId}
                    onClick={() => setThemeOpen((open) => !open)}
                  >
                    <AppIcon name="palette" size={20} />
                    <span className={styles.menuItemLabel}>Appearance</span>
                    <AppIcon name="chevron_right" size={18} />
                  </button>
                  {themeOpen ? (
                    <div className={styles.themeChoices} id={themeGroupId} role="group" aria-label="Theme">
                      {THEME_OPTIONS.map((option) => {
                        const selected = appearance.theme === option.value;
                        return (
                          <button
                            key={option.value}
                            type="button"
                            className={styles.themeChoice}
                            role="menuitemradio"
                            aria-checked={selected}
                            onClick={() => {
                              appearance.setTheme(option.value);
                              closeMenu(true);
                            }}
                          >
                            <span className={styles.themeChoiceLabel}>{option.label}</span>
                            {selected ? <AppIcon name="check" size={18} /> : null}
                          </button>
                        );
                      })}
                    </div>
                  ) : null}

                  <div className={styles.menuDivider} role="separator" />

                  <button
                    type="button"
                    className={`${styles.menuItem} ${styles.menuItemDanger}`}
                    role="menuitem"
                    onClick={() => {
                      closeMenu();
                      onLogout();
                    }}
                  >
                    <AppIcon name="logout" size={20} />
                    <span className={styles.menuItemLabel}>Sign out</span>
                  </button>
                </div>
              ) : null}
            </div>
          </div>
        </header>

        <div className={styles.content} data-page={settingsOpen ? "settings" : "home"}>
          {notice ? (
            <p className={styles.notice} role="status">
              {notice}
            </p>
          ) : null}
          {settingsOpen ? (
            <section className={styles.settingsPage} id={settingsId} aria-labelledby="personal-settings-title">
              <header className={styles.settingsIntro}>
                <h2 id="personal-settings-title">Settings</h2>
                <p>Manage how ExItS Personal looks on this device. More account preferences will appear here as they become available.</p>
              </header>
              <article className={styles.settingsCard}>
                <div className={styles.settingsCardHead}>
                  <div>
                    <h3>Appearance</h3>
                    <p>Choose Light, Dark, or follow your system preference.</p>
                  </div>
                  <Button type="button" variant="ghost" onClick={() => setSettingsOpen(false)}>
                    Back to Home
                  </Button>
                </div>
                <SegmentedControl
                  label="Theme"
                  name="personal-theme"
                  value={appearance.theme}
                  options={[
                    { value: "light", label: "Light" },
                    { value: "dark", label: "Dark" },
                    { value: "system", label: "System" },
                  ]}
                  onChange={(theme) => appearance.setTheme(theme as ThemeMode)}
                />
              </article>
            </section>
          ) : (
            children
          )}
        </div>
      </div>

      {drawerOpen ? (
        <div className={styles.drawerLayer}>
          <button type="button" className={styles.scrim} aria-label="Close navigation" onClick={() => setDrawerOpen(false)} />
          <div className={styles.drawer} role="dialog" aria-label="Navigation">
            <nav className={styles.drawerNav}>
              {[...PRIMARY_NAV, SETTINGS_ITEM].map((item) => renderNavItem(item, false))}
            </nav>
          </div>
        </div>
      ) : null}

      <nav className={styles.bottom} aria-label="Personal mobile">
        {PRIMARY_NAV.map((item) => (
          <button
            key={item.id}
            type="button"
            className={styles.bottomItem}
            aria-current={item.id === "home" && !settingsOpen ? "page" : undefined}
            aria-label={item.label}
            onClick={() => choose(item)}
          >
            <AppIcon name={item.icon} size={20} />
            <span>{item.id === "stores" ? "Stores" : item.label}</span>
          </button>
        ))}
      </nav>
    </div>
  );
}
