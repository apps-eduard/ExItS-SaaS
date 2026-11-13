import { useEffect, useId, useState } from "react";
import { Button } from "../components/button/Button";
import { AppIcon } from "../icons/ExIcon";
import { AppearanceControls } from "./AppearanceControls";
import { ComponentsShowcase } from "./ComponentsShowcase";
import { Foundations } from "./Foundations";
import {
  branchContains,
  isNavBranch,
  NAV_GROUPS,
  navLeaves,
  resolveSectionId,
  type NavBranch,
  type SectionId,
} from "./nav";
import { PersonalConcept } from "./PersonalConcept";
import styles from "./Playground.module.scss";

function sectionFromLocation(): SectionId {
  const value = window.location.hash.replace("#", "");
  return resolveSectionId(value) ?? "overview";
}

function NavBranchItem({
  item,
  current,
  onSelect,
}: {
  item: NavBranch;
  current: SectionId;
  onSelect: (id: SectionId) => void;
}) {
  const panelId = useId();
  const active = branchContains(item, current);
  const [open, setOpen] = useState(active);

  useEffect(() => {
    if (active) setOpen(true);
  }, [active]);

  return (
    <div className={styles.navSubgroup} data-open={open ? "true" : undefined} data-active={active ? "true" : undefined}>
      <button
        type="button"
        className={styles.navBranch}
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((value) => !value)}
      >
        <AppIcon name={item.icon} />
        <span className={styles.navLabel}>{item.label}</span>
        <AppIcon name="expand_more" className={styles.navChevron} />
      </button>
      <div id={panelId} className={styles.navChildren} hidden={!open} role="group" aria-label={item.label}>
        {item.children.map((child) => (
          <button
            key={child.id}
            type="button"
            className={`${styles.navButton} ${styles.navChild}`}
            aria-current={child.id === current ? "page" : undefined}
            onClick={() => onSelect(child.id)}
          >
            <AppIcon name={child.icon} />
            <span className={styles.navLabel}>{child.label}</span>
          </button>
        ))}
      </div>
    </div>
  );
}

function Sidebar({
  current,
  collapsed,
  onSelect,
}: {
  current: SectionId;
  collapsed: boolean;
  onSelect: (id: SectionId) => void;
}) {
  return (
    <div className={styles.sidebarInner} data-collapsed={collapsed ? "true" : "false"}>
      <div className={styles.brand}>
        <span className={styles.mark} aria-hidden="true">
          E
        </span>
        <span className={styles.brandText}>
          <span className={styles.word}>ExItS</span>
          <span className="type-caption">Premium · Personal</span>
        </span>
      </div>
      <nav className={styles.nav} aria-label="Design system">
        {NAV_GROUPS.map((group) => (
          <div key={group.label}>
            <p className={styles.groupLabel}>{group.label}</p>
            {group.items.map((item) =>
              isNavBranch(item) ? (
                <NavBranchItem key={item.label} item={item} current={current} onSelect={onSelect} />
              ) : (
                <button
                  key={item.id}
                  type="button"
                  className={styles.navButton}
                  aria-current={item.id === current ? "page" : undefined}
                  onClick={() => onSelect(item.id)}
                >
                  <AppIcon name={item.icon} />
                  <span className={styles.navLabel}>{item.label}</span>
                </button>
              ),
            )}
          </div>
        ))}
      </nav>
    </div>
  );
}

export function Playground() {
  const [section, setSection] = useState<SectionId>(sectionFromLocation);
  const [collapsed, setCollapsed] = useState(false);
  const [navOpen, setNavOpen] = useState(false);
  const [appearanceOpen, setAppearanceOpen] = useState(false);

  useEffect(() => {
    const onHash = () => setSection(sectionFromLocation());
    window.addEventListener("hashchange", onHash);
    return () => window.removeEventListener("hashchange", onHash);
  }, []);

  useEffect(() => {
    const nav = document.getElementById("section-drawer");
    const appearance = document.getElementById("appearance-dialog");
    if (nav instanceof HTMLDialogElement) {
      if (navOpen && !nav.open) nav.showModal();
      if (!navOpen && nav.open) nav.close();
    }
    if (appearance instanceof HTMLDialogElement) {
      if (appearanceOpen && !appearance.open) appearance.showModal();
      if (!appearanceOpen && appearance.open) appearance.close();
    }
  }, [navOpen, appearanceOpen]);

  function select(id: SectionId) {
    setSection(id);
    const next = `/design-system#${id}`;
    if (`${window.location.pathname}${window.location.hash}` !== next) {
      window.history.replaceState(null, "", next);
    }
    setNavOpen(false);
    document.getElementById("content")?.focus();
  }

  const current = navLeaves().find((item) => item.id === section);

  return (
    <div className={styles.shell} data-collapsed={collapsed ? "true" : "false"}>
      <a className={styles.skip} href="#content">
        Skip to content
      </a>
      <aside className={styles.sidebar}>
        <Sidebar current={section} collapsed={collapsed} onSelect={select} />
        <div className={styles.sidebarTools}>
          <Button
            variant="ghost"
            iconOnly
            icon={collapsed ? <AppIcon name="left_panel_open" /> : <AppIcon name="left_panel_close" />}
            aria-pressed={collapsed}
            aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
            onClick={() => setCollapsed((value) => !value)}
          />
          <Button
            className={styles.appearanceLauncher}
            variant="ghost"
            iconOnly
            icon={<AppIcon name="tune" />}
            aria-label="Appearance"
            onClick={() => setAppearanceOpen(true)}
          />
        </div>
      </aside>
      <div className={styles.center}>
        <header className={styles.topbar}>
          <Button variant="ghost" iconOnly icon={<AppIcon name="menu" />} aria-label="Open sections" onClick={() => setNavOpen(true)} />
          <p className={styles.topTitle}>{current?.label ?? "Design system"}</p>
          <Button variant="ghost" iconOnly icon={<AppIcon name="tune" />} aria-label="Appearance" onClick={() => setAppearanceOpen(true)} />
        </header>
        <main id="content" tabIndex={-1} className={styles.main}>
          <div className={styles.content}>
            <Foundations id={section} />
            <ComponentsShowcase id={section} />
            {section === "mobile-preview" ? (
              <>
                <header className={styles.intro}>
                  <h1 className="type-page">Mobile preview</h1>
                  <p className="type-secondary">A 390px frame of the personal concept. On a narrow screen the frame uses the available width.</p>
                </header>
                <div className={styles.device}>
                  <PersonalConcept />
                </div>
              </>
            ) : null}
            {section === "personal-concept" ? (
              <>
                <header className={styles.intro}>
                  <h1 className="type-page">Personal UI concept</h1>
                  <p className="type-secondary">A visual study for balance, actions, search, a transaction, and a primary action. It does not call the API.</p>
                </header>
                <PersonalConcept />
              </>
            ) : null}
          </div>
        </main>
      </div>
      <aside className={styles.inspector} aria-label="Appearance">
        <h2 className="type-section">Appearance</h2>
        <AppearanceControls name="inspector" />
      </aside>
      <dialog
        id="section-drawer"
        className={styles.drawer}
        aria-label="Sections"
        onClose={() => setNavOpen(false)}
        onClick={(event) => {
          if (event.target === event.currentTarget) setNavOpen(false);
        }}
      >
        <div className={styles.drawerBody}>
          <div className={styles.dialogHeader}>
            <h2 className="type-section">Sections</h2>
            <Button variant="ghost" iconOnly icon={<AppIcon name="close" />} aria-label="Close sections" onClick={() => setNavOpen(false)} />
          </div>
          <Sidebar current={section} collapsed={false} onSelect={select} />
        </div>
      </dialog>
      <dialog
        id="appearance-dialog"
        className={styles.dialog}
        aria-labelledby="appearance-dialog-title"
        onClose={() => setAppearanceOpen(false)}
        onClick={(event) => {
          if (event.target === event.currentTarget) setAppearanceOpen(false);
        }}
      >
        <div className={styles.dialogHeader}>
          <h2 id="appearance-dialog-title" className="type-section">
            Appearance
          </h2>
          <Button variant="ghost" iconOnly icon={<AppIcon name="close" />} aria-label="Close appearance" onClick={() => setAppearanceOpen(false)} />
        </div>
        <div className={styles.dialogBody}>
          <AppearanceControls name="dialog" />
        </div>
      </dialog>
    </div>
  );
}
